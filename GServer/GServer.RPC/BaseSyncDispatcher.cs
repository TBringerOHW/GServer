using System.Collections.Generic;
using System.Threading;
using GServer.Messages;


namespace GServer.RPC
{
    public class BaseSyncDispatcher
    {
        private static BaseSyncDispatcher _instance;
        private static volatile bool _queued = false;
        protected static readonly List<HandlerAction> Backlog = new List<HandlerAction>();

        protected static List<HandlerAction> Actions = new List<HandlerAction>();
#if UNITY_ENGINE
        private static Dictionary<int, Coroutine> NetworkViewCoroutines = new Dictionary<int, Coroutine>();
#endif

        public static bool IsInitialized
        {
            get => _instance != null;
        }

        protected class HandlerAction
        {
            public ReceiveHandler Action;
            public Message Message;
            public Connection.Connection Connection;

            public void Invoke()
            {
                Action.Invoke(Message, Connection);
            }
        }

        public static void RunOnMainThread(ReceiveHandler action, Message message, Connection.Connection connection)
        {
            lock (Backlog)
            {
                Backlog.Add(new HandlerAction() {Action = action, Connection = connection, Message = message});
                _queued = true;
            }
        }
        
#region [Platform Depended Code]        
#if UNITY_ENGINE

        private static Dictionary<int, Coroutine> NetworkViewCoroutines = new Dictionary<int, Coroutine>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance == null)
            {
                _instance = new GameObject("Dispatcher").AddComponent<Dispatcher>();
                DontDestroyOnLoad(_instance.gameObject);
            }
        }

        private IEnumerator SyncCoroutine(NetworkView NetworkView)
        {
            var yieldInstruction = new WaitForSeconds(NetworkView.GetSyncPeriod());
            while (true)
            {
                NetworkView.SyncNow();
                yield return yieldInstruction;
            }
        }

        internal static void StartSync(NetworkView NetworkView)
        {
            var coroutine = _instance.StartCoroutine(_instance.SyncCoroutine(NetworkView));
            NetworkViewCoroutines.Add(NetworkView.GetHashCode(), coroutine);
        }

        internal static void StopSync(NetworkView NetworkView)
        {
            var hash = NetworkView.GetHashCode();

            if (NetworkViewCoroutines.ContainsKey(hash))
            {
                var coroutine = NetworkViewCoroutines[hash];
                _instance.StopCoroutine(coroutine);
                NetworkViewCoroutines.Remove(hash);
            }
        }
        
#else
        private static Timer _timer;
        
        internal static void StartSync(NetworkView networkView)
        {
            var syncPeriod = (int) networkView.GetSyncPeriod();
            _timer = new Timer(o=> SyncAction(networkView));
            _timer.Change(20,syncPeriod);
        }

        internal static void StopSync(NetworkView networkView)
        {
            _timer.Dispose();
            _timer = null;
        }
        
        private static void SyncAction(NetworkView networkView)
        {
                networkView.SyncNow();
        }
#endif
#endregion
        
    }
}