namespace KlinketStudiosTools.Utils
{
    using UnityEngine;

    public class Utils
    {
        public static void SetLayerRecursively(GameObject go, int layerNumber)
        {
            if (go == null) return;

            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
            }
        }
    }

}