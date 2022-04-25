#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS || UNITY_WII || UNITY_ANDROID || UNITY_PS4 || UNITY_XBOXONE || UNITY_LUMIN || UNITY_TIZEN || UNITY_TVOS || UNITY_WEBGL || UNITY_ANALYTICS || UNITY_WINRT
#define UNITY
#endif

#if UNITY
using UnityEngine;
using Zenet.Network;

namespace Zenet.Manager
{
    public class ManualCallback : MonoBehaviour
    {
        private static ManualCallback Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            Instance = this;
            //Change to manual for unity main thread release callback's
            Callback.Manual = true;
        }

        private void Update()
        {
            //Release callback's on queue
            Callback.Update();
        }
    }
}
#endif