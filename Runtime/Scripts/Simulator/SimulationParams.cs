using UnityEditor;
using UnityEngine;

namespace Mox
{
    namespace Simulator
    {
        [ExecuteInEditMode]
        public class SimulationParams : MonoBehaviour
        {
            internal static bool _destroy = false; // non-static fields will be reset after editor play stopped

            public void Start()
            {
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }

            public void Update()
            {
                if (_destroy)
                {
                    Debug.Log("MoxSimulationParams - Destroy");

                    GameObject.DestroyImmediate(gameObject);
                }
            }

            public void OnApplicationQuit()
            {
                Debug.Log("MoxSimulationParams - OnApplicationQuit");

#if UNITY_EDITOR
                if (EditorApplication.isPlaying == true)
                {
                    Debug.Log("MoxSimulationParams - OnApplicationQuit -> playing");

                    _destroy = true; // destroying the gameobject directly here does not work, probably because it will be regenerated after editor play ended...
                }
#endif

            }
        }
    }
}


