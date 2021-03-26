using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using GServer.Messages;
[assembly: InternalsVisibleTo("GServer.UnitTests")]
namespace GServer.RPC
{
    public class BaseSyncDispatcher
#if UNITY_ENGINE
        : MonoBehaviour
#endif
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
        private static readonly Dictionary<NetworkView, Timer> Timers = new Dictionary<NetworkView, Timer>();

        private static bool TimerIsValid(NetworkView networkView)
        {
            return Timers.ContainsKey(networkView) && Timers[networkView] != null;
        }

        internal static void StartSync(NetworkView networkView)
        {
            if (TimerIsValid(networkView)) return;

            var syncPeriod = (int) networkView.GetSyncPeriod();

            var timer = new Timer(o => SyncAction(networkView));
            timer.Change(20, syncPeriod);
            Timers.Add(networkView, timer);
        }

        public static void StopSync(NetworkView networkView) //Param required for unity dispatcher version.
        {
            if (!TimerIsValid(networkView)) return;
            
            Timers[networkView].Dispose();
            Timers.Remove(networkView);
        }

        private static void SyncAction(NetworkView networkView)
        {
            networkView.SyncNow();
        }
#endif

        #endregion
    }
}