using UnityEngine;

namespace Netly
{
    public class Netly : MonoBehaviour
    {
        private void Start()
        {
            NetlyEnvironment.MainThread.IsAutomatic = false;
        }

        private void Update()
        {
            NetlyEnvironment.MainThread.Dispatch();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void OnAfterSceneLoad()
        {
            var instance = new GameObject("[RUNTIME] NETLY");
            instance.AddComponent<Netly>();
            DontDestroyOnLoad(instance);
        }
    }
}