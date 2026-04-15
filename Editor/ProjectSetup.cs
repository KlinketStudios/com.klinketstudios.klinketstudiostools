using UnityEngine;
using System.Collections.Generic;
using System.IO;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using static System.IO.Path;
using static UnityEditor.AssetDatabase;
using Application = UnityEngine.Application;
using Task = System.Threading.Tasks.Task;

public static class ProjectSetup 
{
    
    [MenuItem("Tools/Setup/Import Essentials/Step 1")]
    public static void CreateFolders()
    {
        Folders.Create(
            "Animations",
            "Mats",
            "Prefabs",
            "Sprites",
            "Textures",
            "Scripts/ScriptableObjectScripts", 
            "ScriptableObjects",
            "Models/textures",
            "Audio/SFX",
            "Audio/Music",
            "Input");
        Refresh();
        
        Folders.Delete("TutorialInfo");
        Assets.DeleteAsset("", "Readme.asset");
        Assets.MoveAsset("InputSystem_Actions.inputactions", "Input/InputSystem_Actions.inputactions");
        
        //var request = Client.Embed("com.klinketstudios.klinketstudiostools");

        //while(!request.IsCompleted) 
        //    await Task.Delay(10);
        
        //MoveAsset("Packages/com.klinketstudios.klinketstudiostools/Assets/KlinketStudiosTools", "Assets/KlinketStudiosTools");
        Refresh();
    }
    
    [MenuItem("Tools/Setup/Import Essentials/Step 2")]
    static void ImportEssentials()
    {
        Assets.ImportAsset("TimeScale Toolbar.unitypackage","bl4st/Editor ExtensionsUtilities");
        Assets.ImportAsset("vHierarchy 2.unitypackage","kubacho lab/Editor ExtensionsUtilities");
        Assets.ImportAsset("In-game Debug Console.unitypackage","yasirkula/ScriptingGUI");
        Assets.ImportAsset("Smart Console.unitypackage","EdgarDev/Editor ExtensionsUtilities");
        Assets.ImportAsset("Hot Reload Edit Code Without Compiling.unitypackage","The Naughty Cult/Editor ExtensionsUtilities");
        Packages.InstallPackages(new[] { "com.unity.cinemachine" });
        string packagePath = TMP_EditorUtility.packageFullPath + "/Package Resources/TMP Essential Resources.unitypackage";
        ImportPackage(packagePath, false);
    }
    
    [MenuItem("Tools/Setup/Import Essentials/Step 3")]
    static void MoveImportedEssentials()
    {
        Folders.Create("AssetPackages");
        Refresh();
        Folders.Move("AssetPackages", "bl4st");
        Folders.Move("AssetPackages", "vHierarchy");
    
        Refresh();
    }

    [MenuItem("Tools/Setup/Import Packages/AI")]
    public static void InstallPackages()
    {
        Packages.InstallPackages(new[]
        {
            "com.unity.ml-agents@4.0.1"
        });
        
        Assets.ImportCustomLocalPackage("Assets/KlinketStudiosTools/Packages/MLAgents.unitypackage");
    }

    
    static class Assets
    {
        public static void ImportAsset(string asset, string folder)
        {
            //string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string basePath = GetDirectoryName(Application.dataPath);
            string assetsFolder = Combine(basePath,  GetFullPath("Packages/com.klinketstudios.klinketstudiostools"),"Packages/AssetsStoreAssets");
            ImportPackage(Combine(assetsFolder, folder, asset), false);
        }

        public static void ImportCustomLocalPackage(string path)
        {
            Combine(Application.dataPath, path);
            ImportPackage(path, false);
        }
        
        public static void MoveAsset(string sourcePath, string destinationPath)
        {
            string path = Combine("Assets", sourcePath);
            string destPath = Combine("Assets", destinationPath);
            
            AssetDatabase.MoveAsset(path, destPath);
        }

        public static void DeleteAsset(string pathToRootFolder, string assetName)
        {
            string rootPath =  Combine("Assets",  pathToRootFolder);
            
            AssetDatabase.DeleteAsset(Combine(rootPath, assetName));
        }
    }

    static class Packages
    {
        static AddRequest request;
        static Queue<string> packagesToInstall = new Queue<string>();

        static async void StartNextPackageInstallation()
        {
            request = Client.Add(packagesToInstall.Dequeue());
            
            while(!request.IsCompleted) 
                await Task.Delay(10);
            
            if(request.Status == StatusCode.Success)
               Debug.Log("Installed: " + request.Result.packageId);
            else if (request.Status >= StatusCode.Failure) 
                Debug.LogError(request.Error.message);

            if (packagesToInstall.Count > 0)
            {
                await Task.Delay(1000);
                StartNextPackageInstallation();
            }
        }
        
        public static void InstallPackages(string[] packages)
        {
            foreach (string package in packages)
            {
                packagesToInstall.Enqueue(package);
            }

            if (packagesToInstall.Count > 0)
            {
                StartNextPackageInstallation();
            }
        }
    }

    static class Folders
    {
        public static void Delete(string folderName)
        {
            string pathToDelete = Combine("Assets", folderName);
            
            if(IsValidFolder(pathToDelete))
            {
                AssetDatabase.DeleteAsset(pathToDelete);
            }
        }

        public static void Move(string newParent, string folderName)
        {
            string sourcePath = $"Assets/{folderName}";
            if (IsValidFolder(sourcePath))
            {
                string destinationPath = $"Assets/{newParent}/{folderName}";
                string error = MoveAsset(sourcePath, destinationPath);

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"Failed to move  {folderName}: {error}");
                }
            }
        }
        
        static void CreateSubFolders(string rootPath, string folderHierarchy)
        {
            var folders = folderHierarchy.Split('/');
            var currentPath = rootPath;
            foreach (var folderName in folders)
            {
                currentPath = Combine(currentPath, folderName);
                if (!Directory.Exists(currentPath))
                {
                    Directory.CreateDirectory(currentPath);
                }
            }
        }
        public static void Create(params string[] folders)
        {
            var path = Application.dataPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            foreach (var folder in folders)
            {
                CreateSubFolders(path, folder);
            }
        }
    }
}
