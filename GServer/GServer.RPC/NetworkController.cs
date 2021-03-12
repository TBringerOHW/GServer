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
        private static NetworkController instance;

        public static NetworkController Instance
        {
            get
            {
                if (instance == null)
                    instance = new NetworkController();
                return instance;
            }
        }

        private Host host;

        internal Dictionary<string, NetworkView> invokes = new Dictionary<string, NetworkView>();

        public Action<Exception> OnException
        {
            get => host.OnException;
            set => host.OnException += value;
        }

        public Action OnConnect
        {
            get => host.OnConnect;
            set => host.OnConnect += value;
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
            return Instance.invokes.Values.ToList();
        }

        internal void RegisterInvoke(string method, NetworkView networkView)
        {
            invokes.Add(method, networkView);
        }

        internal void RPCMessage(string method, DataStorage ds)
        {
            NetworkView networkView;
            if (invokes.TryGetValue(method, out networkView))
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

                host = newHost;
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

                host = new Host(ListeningPort);

                host.StartListen();

                host.OnException += OnException;
                host.OnConnect += OnConnect;

                Timer timer = new Timer(o => host.Tick());
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

            host.AddHandler(type, action);
        }

        public void SendMessage(Connection.Connection connection, DataStorage dataStorage, short messageType, Mode mode = Mode.None)
        {
            host.Send(new Message(messageType, mode, dataStorage), connection);
        }

        public void SendMessage(Message message)
        {
            host.Send(message);
        }

        public void SendMessage(DataStorage dataStorage, short messageType, Mode mode = Mode.None)
        {
            host.Send(new Message(messageType, mode, dataStorage));
        }

        public IEnumerable<Connection.Connection> GetConnections()
        {
            return host.GetConnections();
        }

        public void Dispose()
        {
            host.Dispose();
            foreach (var item in host.GetConnections())
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
                return host.BeginConnect(ipEndPoint);
            }
            catch (FormatException e)
            {
                ShowException(e);
                return false;
            }
        }

        public void ForseSendAllMessages()
        {
            host.Tick();
        }
    }
}