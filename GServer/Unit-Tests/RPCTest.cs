using System.Linq;
using System.Net;
using System.Threading;
using GServer.RPC;
using NUnit.Framework;

namespace Unit_Tests
{
    public class RPCTest
    {
        [Test]
        public void RPCClientSyncTest()
        {
            Init(out var client, out var server, out var networkView);

            var bufferValue = client.SyncField;
            networkView.Call(client, "RPCClientTest");
            Assert.Less(bufferValue, client.SyncField);
        }
        
        [Test]
        public void RPCServerSyncTest()
        {
            Init(out var client, out var server, out var networkView);

            var bufferValue = server.SyncField;
            networkView.Call(server, "RPCServerTest");
            Assert.Less(bufferValue, server.SyncField);
        }
        
        [Test]
        public void RPCMulticastSyncTest()
        {
            Init(out var client, out var server, out var networkView);

            var bufferValue = client.SyncField;
            
            networkView.Call(server, "RPCMulticastTest");
            Thread.Sleep(1000);

            Assert.Less(bufferValue, client.SyncField);
            Assert.AreEqual(client.SyncField, server.SyncField);
        }
        
        [Test]
        public void RPCFieldSyncTest()
        { 
            Init(out var client, out var server, out var networkView);
            
            client.SyncField += 100;
            Thread.Sleep(500);

            Assert.AreEqual(client.SyncField, server.SyncField);
        }

        private static void Init(out BaseSyncEntity client, out BaseSyncEntity server, out NetworkView networkView)
        {
            server = new ServerSyncEntity(25575);
            NetworkController.Instance.Init(server, 25575);

            client = new ClientSyncEntity(25565);
            client.BeginConnect(new IPEndPoint(IPAddress.Loopback, 25575));

            while (!client.GetConnections().Any())
            {
                Thread.Sleep(25);
            }

            networkView = new NetworkView(0.25f);
            networkView.InitInvoke();
        }
    }
    
    public class BaseSyncEntity : RPCHost
    {
        [Sync] public int SyncField = 5;
        
        public BaseSyncEntity(int port) : base(port)
        {
            StartListen();
            
            var timer = new Timer(o=>Tick());
            timer.Change(10,100);
        }

        [Invoke(InvokeType.Client)]
        public void RPCClientTest()
        {
            SyncField += 1;
        }
        
        [Invoke(InvokeType.Server)]
        public void RPCServerTest()
        {
            SyncField += 2;
        }
        
        [Invoke(InvokeType.MultiCast)]
        public void RPCMultiCastTest()
        {
            SyncField += 3;
        }
    }

    public class ClientSyncEntity : BaseSyncEntity
    {
        public ClientSyncEntity(int port) : base(port)
        {
        }
    }

    public class ServerSyncEntity : BaseSyncEntity
    {
        public ServerSyncEntity(int port) : base(port)
        {
        }
    }
}