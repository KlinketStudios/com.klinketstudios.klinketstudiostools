using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KlinketStudiosTools
{
    public interface IDependencyProvider {}

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class InjectAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class NameSpecificInjectAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class ProvideAttribute : Attribute {}


    /// <summary>
    /// need to return type (string, object)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class NameSpecificProvideAttribute : Attribute {}

    public class DependencyInjector : Singlton<DependencyInjector>
    {
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        
        readonly Dictionary<Type, object> registry = new Dictionary<Type, object>();
        readonly Dictionary<string, object> nameSpecificRegistry = new Dictionary<string, object>();
        
        protected override void Awake()
        {
            base.Awake();

            var providers = FindMonoBehaviours().OfType<IDependencyProvider>();
            foreach (var provider in providers)
            {
                RegisterProvider(provider);
            }

            var nameSpecificProviders = FindMonoBehaviours().OfType<IDependencyProvider>();
            foreach (var provider in nameSpecificProviders)
            { 
                NameSpecificRegisterProvider(provider);
            }

            var injectables = FindMonoBehaviours().Where(IsInjectable);
            foreach (var injectable in injectables)
            {
                Inject(injectable);
            }

            var nameSpecificInjectables = FindMonoBehaviours().Where(IsNameSpecificInjectable);
            foreach (var injectable in nameSpecificInjectables)
            {
                NameSpecificInject(injectable);
            }
        }

        void Inject(object instance)
        {
            var type = instance.GetType();
            var injectableFeilds = type.GetFields(bindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
            foreach (var injectableFeild in injectableFeilds)
            {
                var fieldType = injectableFeild.FieldType;
                var resolvedInstance = Resolve(fieldType);
                if (resolvedInstance == null)
                {
                    throw new Exception($"Failed to inject{fieldType.Name} into {type.Name}");
                }
                
                injectableFeild.SetValue(instance,resolvedInstance);
                print($"Injected {fieldType.Name} into {type.Name}");
            }
        }
        
        void NameSpecificInject(object instance)
        {
            var type = instance.GetType();
            var injectableFeilds = type.GetFields(bindingFlags)
                .Where(member => Attribute.IsDefined(member, typeof(NameSpecificInjectAttribute)));
            foreach (var injectableFeild in injectableFeilds)
            {
                var fieldName = injectableFeild.Name;
                var resolvedInstance = NameSpecificResolve(fieldName);
                if (resolvedInstance == null)
                {
                    throw new Exception($"Failed to inject{fieldName} into {type.Name}");
                }
                
                injectableFeild.SetValue(instance, resolvedInstance);
                print($"Injected {fieldName} into {type.Name}");
            }
        }

        object Resolve(Type type)
        {
            registry.TryGetValue(type, out var resolvedInstance);
            return resolvedInstance;
        }
        object NameSpecificResolve(string name)
        {
            nameSpecificRegistry.TryGetValue(name, out var resolvedInstance);
            return resolvedInstance;
        }
        
        static bool IsInjectable(MonoBehaviour obj)
        {
            var members = obj.GetType().GetMembers(bindingFlags);
            return members.Any(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
        }
        static bool IsNameSpecificInjectable(MonoBehaviour obj)
        {
            var members = obj.GetType().GetMembers(bindingFlags);
            return members.Any(member => Attribute.IsDefined(member, typeof(NameSpecificInjectAttribute)));
        }
        
        void RegisterProvider(IDependencyProvider provider)
        {
            var methods = provider.GetType().GetMethods(bindingFlags);
            foreach (var method in methods)
            {
                if(!Attribute.IsDefined(method,typeof(ProvideAttribute))) 
                    continue;
                var returnType = method.ReturnType;
                var providedInstance = method.Invoke(provider, null);
                if (providedInstance != null)
                {
                    registry.Add(returnType, providedInstance);
                    print($"Registered {returnType.Name} from {provider.GetType().Name}");
                }
                else
                {
                    throw new Exception($"Provider {provider.GetType().Name} returned null for {returnType.Name}");
                }
            }

            var fields = provider.GetType().GetFields(bindingFlags);
            foreach (var field in fields)
            {
                if (!Attribute.IsDefined(field, typeof(ProvideAttribute)))
                {
                    continue;
                }

                var returnType = field.FieldType;
                var providedInstance = field.GetValue(provider);
                if (providedInstance != null)
                {
                    registry.Add(returnType, providedInstance);
                    print($"Registered {returnType.Name} from {provider.GetType().Name}");
                }
                else
                {
                    throw new Exception($"Provider {provider.GetType().Name} returned null for {returnType.Name}");
                }
            }
        }
        
        void NameSpecificRegisterProvider(IDependencyProvider provider)
        {
            var methods = provider.GetType().GetMethods(bindingFlags);
            foreach (var method in methods)
            {
                if(!Attribute.IsDefined(method,typeof(NameSpecificProvideAttribute))) 
                    continue;
                var providedInstance = ((string, object))method.Invoke(provider, null);
                
                if (!string.IsNullOrEmpty(providedInstance.Item1) && providedInstance.Item2 != null)
                {
                    if (providedInstance is (string, object))
                    {
                        nameSpecificRegistry.Add(providedInstance.Item1 , providedInstance.Item2);
                        print($"Registered ({providedInstance.Item1}) of type {providedInstance.Item2.GetType().Name} from {provider.GetType().Name}");
                    }
                }
                else
                {
                    throw new Exception($"Provider {provider.GetType().Name} returned null for {method.ReturnType.Name}");
                }
            }

            var fields = provider.GetType().GetFields(bindingFlags);
            foreach (var field in fields)
            {
                if (!Attribute.IsDefined(field, typeof(NameSpecificProvideAttribute)))
                {
                    continue;
                }


                var name = field.Name;
                var providedInstance = field.GetValue(provider);
                if (providedInstance != null)
                {
                    nameSpecificRegistry.Add(field.Name, providedInstance);
                    print($"Registered ({name}) of type {providedInstance} from {provider.GetType().Name}");
                }
                else
                {
                    throw new Exception($"Provider {provider.GetType().Name} returned null for {field.FieldType.Name}");
                }
            }
        }

        static MonoBehaviour[] FindMonoBehaviours()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.InstanceID);
        }
    }
}
