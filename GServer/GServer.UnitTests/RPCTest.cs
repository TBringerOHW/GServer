using System.Threading;
using GServer.RPC;
using NUnit.Framework;
using static GServer.UnitTests.RPCTestCommon;


namespace MyNamespace
{
    public class BaseClass
    {
        [Invoke]
        public void ATestMethod()
        {
                
        }
    }
}
namespace GServer.UnitTests
{
    /// <summary>
    /// !!!Attention!!!
    /// # Execution of this tests require use of another machine.
    /// !!!Attention!!!
    /// </summary>
    public class RPCTest
    {
        public class BaseClass
        {
            [Invoke]
            public void ATestMethod()
            {
                
            }
        }

        public class ExtensionClass : BaseClass
        {
            [Invoke]
            public void BTestMethod()
            {
                
            }
        }

        public class AnotherBaseClass
        {
            [Invoke]
            public void BTestMethod()
            {
                
            }
        }
        
        [Test]
        public void FieldSyncTest()
        {
        }
        
        [Test]
        public void PropertySyncTest()
        {
            Init(out var server, out var client, out _, out _, true);
            server.SyncProperty += 10;
            
            Thread.Sleep(5000);

            Assert.AreEqual(client.SyncProperty, server.SyncProperty);
        }
        
        #region [Methods]
        
        [Test]
        public void RPCClientSyncTest()
        {
            Init(out var server, out var client, out var serverNetworkView, out var clientNetworkView);

            var bufferValue = client.SyncField;
            clientNetworkView.Call(client, "RPCClientTest");
            
            Thread.Sleep(5000);
            
            Assert.AreEqual(bufferValue + BaseSyncEntity.ClientSyncTestChangeAmount, client.SyncField);
            Assert.Pass($"[{bufferValue}] + [{BaseSyncEntity.ClientSyncTestChangeAmount}] = [{client.SyncField}]");
        }
        
        [Test]
        public void RPCServerSyncTest()
        {
            Init(out var server, out var client, out var serverNetworkView, out var clientNetworkView);

            var bufferValue = server.SyncField;
            serverNetworkView.Call(server, "RPCServerTest");
            
            Thread.Sleep(5000);

            Assert.AreEqual(bufferValue + BaseSyncEntity.ServerSyncTestChangeAmount, server.SyncField);
            Assert.Pass($"[{bufferValue}] + [{BaseSyncEntity.ServerSyncTestChangeAmount}] = [{server.SyncField}]");
        }
        
        [Test]
        public void RPCMulticastSyncTest()
        {
            Init(out var server, out var client, out var serverNetworkView, out var clientNetworkView);
            
            var bufferValue = server.SyncField;
            
            serverNetworkView.Call(server, "RPCMultiCastTest");

            var clientRPCCallInvoked = false;
            client.FieldChanged += () => clientRPCCallInvoked = true;
            while (!clientRPCCallInvoked)
            {
                Thread.Sleep(500);
            }

            Assert.AreNotEqual(bufferValue, server.SyncField, "Server value not changed!");
            Assert.AreEqual(client.SyncField, server.SyncField, $"[{client.SyncField}] Client value != Server value!");
            Assert.Pass($"Client value = [{client.SyncField}], Server value = [{server.SyncField}], Value before test = [{bufferValue}], Excepted change = [{BaseSyncEntity.MulticastSyncTestChangeAmount}]");
        }

        #endregion
    }
    

}