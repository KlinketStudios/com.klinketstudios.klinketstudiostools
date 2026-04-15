namespace KlinketStudiosTools
{
    using System;
    using UnityEngine;
    using UnityEngine.Animations;

    [RequireComponent(typeof(AimConstraint))]
    public class LookAtMainCamera : MonoBehaviour
    {
        [Space(15)] [SerializeField] private bool lockX;

        [SerializeField] private AimConstraint.WorldUpType worldUpType;

        public void Awake()
        {
            AimConstraint constraint = GetComponent<AimConstraint>();

            constraint.worldUpType = worldUpType;

            constraint.locked = true;
            constraint.constraintActive = true;

            if (lockX)
            {
                constraint.rotationAxis = Axis.Y;
            }
            else
            {
                constraint.rotationAxis = Axis.X | Axis.Y;
            }


            constraint.AddSource(new ConstraintSource
                { sourceTransform = Camera.main.transform, weight = 1 });
        }
    }
}