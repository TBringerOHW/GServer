using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using GServer.Containers;
using GServer.Messages;

namespace GServer.RPC
{
    public class NetworkController
    {
        private static NetworkController _instance;

        public static NetworkController Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new NetworkController();
                return _instance;
            }
        }

        private Host _host;

        private readonly Dictionary<string, NetworkView> _invokes = new Dictionary<string, NetworkView>();

        public Action<Exception> OnException
        {
            get => _host.OnException;
            set => _host.OnException += value;
        }

        public Action OnConnect
        {
            get => _host.OnConnect;
            set => _host.OnConnect += value;
        }

        public Action<string> OnMessage { get; set; }
        public int ListeningPort { get; private set; }

        private NetworkController()
        {
        }


        internal static void ShowException(Exception e)
        {
            Instance.OnException?.Invoke(e);
        }

        internal static void ShowMessage(string message)
        {
            Instance.OnMessage?.Invoke(message);
        }

        internal static List<NetworkView> GetNetViews()
        {
            return Instance._invokes.Values.ToList();
        }

        internal void RegisterInvoke(string method, NetworkView networkView)
        {
            _invokes.Add(method, networkView);
        }

        internal void RPCMessage(string method, DataStorage ds)
        {
            if (_invokes.TryGetValue(method, out var networkView))
            {
                networkView.RPC(method, ds);
            }
        }

        public bool Init(Host newHost, int port = 0, int period = 100)
        {
            try
            {
                if (port == 0)
                {
                    port = NetworkExtensions.FreeTcpPort();
                }

                _host = newHost;
                ListeningPort = port;
                
                Timer timer = new Timer(o => newHost.Tick());
                timer.Change(0, period);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        public bool Init(int port = 0, int period = 100)
        {
            try
            {
                if (port == 0)
                {
                    port = NetworkExtensions.FreeTcpPort();
                }

                ListeningPort = port;

                _host = new Host(ListeningPort);

                _host.StartListen();

                _host.OnException += OnException;
                _host.OnConnect += OnConnect;

                Timer timer = new Timer(o => _host.Tick());
                timer.Change(0, period);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        public void AddHandler(short type, ReceiveHandler action)
        {
            if (type >= 0 && type <= 40)
            {
                throw new ArgumentException("The value can't be >= 0 && <= 40");
            }

            _host.AddHandler(type, action);
        }

        public void SendMessage(Connection.Connection connection, DataStorage dataStorage, short messageType, Mode mode = Mode.None)
        {
            _host.Send(new Message(messageType, mode, dataStorage), connection);
        }

        public void SendMessage(Message message)
        {
            _host.Send(message);
        }

        public void SendMessage(DataStorage dataStorage, short messageType, Mode mode = Mode.None)
        {
            _host.Send(new Message(messageType, mode, dataStorage));
        }

        public IEnumerable<Connection.Connection> GetConnections()
        {
            return _host.GetConnections();
        }

        public void Dispose()
        {
            _host.Dispose();
            foreach (var item in _host.GetConnections())
            {
                item.Disconnect();
            }
        }

        public void Disconnect(Connection.Connection connection)
        {
            connection.Disconnect();
        }

        public bool BeginConnect(string ip, int port)
        {
            try
            {
                var ipEndPoint = NetworkExtensions.CreateIPEndPoint(string.Concat(ip, ":", port.ToString()));
                return _host.BeginConnect(ipEndPoint);
            }
            catch (FormatException e)
            {
                ShowException(e);
                return false;
            }
        }

        public void ForceSendAllMessages()
        {
            _host.Tick();
        }
    }
}