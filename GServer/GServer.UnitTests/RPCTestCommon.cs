using System;
using System.Net;
using System.Threading;
using GServer.Containers;
using GServer.RPC;

namespace GServer.UnitTests
{
    public class RPCTestCommon
    {
        public enum RPCTestPart
        {
            Default,
            InvokeTypeClient,
            InvokeTypeServer,
            InvokeTypeMulticast,
            SyncField,
            SyncProperty
        }
        
        public static IPEndPoint ServerIPEndPoint => new IPEndPoint(IPAddress.Parse("172.16.10.160"), 25000);
        public static IPEndPoint ClientIPEndPoint => new IPEndPoint(IPAddress.Parse("172.16.10.161"), 25001);
        
        public class TestRPCHost : RPCHost
        {
            public string HostName;
            public TestRPCHost(int port) : base(port)
            {
                HostName = port == 25000 ? $"Server" : $"Client #{port - 25000}";
            }
        }

        public class BaseSyncEntity
        {
            public event Action FieldChanged;
            public event Action PropertyChanged;
            public event Action<int> RPCMethodInvoked;
            
            
            public const int ClientSyncTestChangeAmount = 1;
            public const int ServerSyncTestChangeAmount = 2;
            public const int MulticastSyncTestChangeAmount = 3;
            
            [DsSerialize] [Sync] public int SyncField;

            [DsSerialize]
            [Sync]
            public int SyncProperty
            {
                get => propertyBackingField;
                set
                {
                    propertyBackingField = value;
                    PropertyChanged?.Invoke();
                }
            }
            private int propertyBackingField;

            public Guid Guid;

            public BaseSyncEntity()
            {
                Guid = Guid.NewGuid();
            }

            [Invoke]
            public void RPCClientTest()
            {
                SyncField += ClientSyncTestChangeAmount;
                RPCMethodInvoked?.Invoke(ClientSyncTestChangeAmount);
            }
        
            [Invoke]
            public void RPCServerTest()
            {
                SyncField += ServerSyncTestChangeAmount;
                RPCMethodInvoked?.Invoke(ServerSyncTestChangeAmount);
            }
        
            [Invoke]
            public void RPCMultiCastTest()
            {
                SyncField += MulticastSyncTestChangeAmount;
                RPCMethodInvoked?.Invoke(MulticastSyncTestChangeAmount);
            }
        }

        public static void InitHost(out BaseSyncEntity syncEntity, out NetworkView networkView, bool isServer, bool fieldSyncEnabled = true)
        {

            CreateSyncEntity(out syncEntity, out networkView, fieldSyncEnabled);
            
            var port = isServer ? ServerIPEndPoint.Port : ClientIPEndPoint.Port;
            var host = CreateHost(port);
            NetworkController.Instance.Init(host, port);

            bool isConnectionEstablished = false;

            if (!isServer)
            {
                host.BeginConnect(ServerIPEndPoint);
                host.OnConnect += () => isConnectionEstablished = true;
            }
            else
            {
                host.ConnectionCreated += connection => { isConnectionEstablished = true; };
            }

            while (!isConnectionEstablished)
            {
                Thread.Sleep(1000);
            }
        }

        private static TestRPCHost CreateHost(int port)
        {
            var host = new TestRPCHost(port);
            host.StartListen();
            var timer = new Timer(state => { host.Tick(); });
            timer.Change(10, 100);
            return host;
        }

        public static void Init(out BaseSyncEntity serverSyncEntity, out BaseSyncEntity clientSyncEntity, out NetworkView serverNetworkView, out NetworkView clientNetworkView, bool syncEnabled = false)
        {
            const int serverPort = 25000;
            const int clientPort = 25005;
            
            var server = new TestRPCHost(serverPort);
            var serverConnected = false;
            server.StartListen();

            var client = new TestRPCHost(clientPort);
            var clientConnected = false;
            client.StartListen();

            var timer = new Timer(state =>
            {
                server.Tick();
                client.Tick();
            });
            timer.Change(10, 100);
            
            server.OnConnect = () => { serverConnected = true;clientConnected = true; };
            client.OnConnect = () => { clientConnected = true; };

            var serverEndPoint = new IPEndPoint(IPAddress.Loopback, clientPort);
            server.BeginConnect(serverEndPoint);
            //var clientEndPoint = new IPEndPoint(IPAddress.Loopback, serverPort);
            //client.BeginConnect(clientEndPoint);

            while (!serverConnected || !clientConnected)
            {
                Thread.Sleep(100);
            }
            
            NetworkController.Instance.Init(server, serverPort);
            
            CreateSyncEntity(out serverSyncEntity, out serverNetworkView, syncEnabled);
            CreateSyncEntity(out clientSyncEntity, out clientNetworkView, syncEnabled);
        }

        public static void CreateSyncEntity(out BaseSyncEntity syncEntity, out NetworkView networkView, bool syncEnabled)
        {
            networkView = new NetworkView(0.5f);
            syncEntity = new BaseSyncEntity();
            if (syncEnabled)
            {
                networkView.InitInvoke(syncEntity);
            }
            else
            {
                networkView.InitRPCOnly(syncEntity);
            }
        }
    }
}