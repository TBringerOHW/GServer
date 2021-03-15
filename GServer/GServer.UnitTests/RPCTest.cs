using System;
using System.Net;
using System.Threading;
using GServer.RPC;
using NUnit.Framework;

namespace GServer.UnitTests
{
    public class RPCTest
    {
        [Test]
        public void RPCFieldSyncTest()
        {
            Init(out var server, out var client, out var networkView, out var clientNetworkView);
            
            client.SyncField += 10;
            Thread.Sleep(5000);

            Assert.AreEqual(client.SyncField, server.SyncField);
        }
        
        #region [Methods]
        
        [Test]
        public void RPCClientSyncTest()
        {
            Init(out var server, out var client, out var serverNetworkView, out var clientNetworkView);

            var bufferValue = client.SyncField;
            clientNetworkView.Call(client, "RPCClientTest");
            
            Assert.Less(bufferValue, client.SyncField);
        }
        
        [Test]
        public void RPCServerSyncTest()
        {
            Init(out var server, out var client, out var serverNetworkView, out var clientNetworkView);

            var bufferValue = server.SyncField;
            serverNetworkView.Call(server, "RPCServerTest");
            
            Assert.Less(bufferValue, server.SyncField);
        }
        
        [Test]
        public void RPCMulticastSyncTest()
        {
            Init(out var server, out var client, out var serverNetworkView, out var clientNetworkView);

            var bufferValue = client.SyncField;
            
            clientNetworkView.Call(client, "RPCMultiCastTest");// RPCMultiCastTest old RPCMultiCastTest
            
            Thread.Sleep(5000);

            Assert.Less(bufferValue, client.SyncField);
            Assert.AreEqual(client.SyncField, server.SyncField);
        }

        #endregion

        #region [Common]

        private class BaseSyncEntity
        {
            [Sync] public int SyncField = 0;
            public Guid Guid;

            public BaseSyncEntity()
            {
                Guid = Guid.NewGuid();
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

        private static void Init(out BaseSyncEntity serverSyncEntity, out BaseSyncEntity clientSyncEntity, out NetworkView serverNetworkView, out NetworkView clientNetworkView)
        {
            const int serverPort = 25565;
            const int clientPort = 25575;
            
            var server = new RPCHost(serverPort);
            var serverConnected = false;
            server.StartListen();

            var client = new RPCHost(clientPort);
            var clientConnected = false;
            client.StartListen();

            var timer = new Timer(state =>
            {
                server.Tick();
                client.Tick();
            });
            timer.Change(10, 100);
            
            server.OnConnect = () => { serverConnected = true; };
            client.OnConnect = () => { clientConnected = true; };
            
            server.BeginConnect(new IPEndPoint(IPAddress.Loopback, clientPort));
            client.BeginConnect(new IPEndPoint(IPAddress.Loopback, serverPort));

            while (!serverConnected || !clientConnected)
            {
                Thread.Sleep(100);
            }
            
            NetworkController.Instance.Init(server, serverPort);
            
            serverNetworkView = new NetworkView(0.5f);
            serverSyncEntity = new BaseSyncEntity();
            serverNetworkView.InitInvoke(serverSyncEntity);

            clientNetworkView = new NetworkView(0.5f);
            clientSyncEntity = new BaseSyncEntity();
            clientNetworkView.InitInvoke(clientSyncEntity);
        }

        #endregion
        
    }
    

}