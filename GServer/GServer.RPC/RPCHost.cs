using System;
using GServer.Containers;
using GServer.Messages;

namespace GServer.RPC
{
    public class RPCHost : Host
    {
        public event Action<DateTime> PingMessageReceived;
        public RPCHost(int port) : base(port)
        {
            AddHandler((short) MessageType.RPCResend, HandleMulticast);

            AddHandler((short) MessageType.RPCSendToEndPoint, HandleRPCMessage);
            
            AddHandler((short) MessageType.Ping, HandlePingMessage);

            AddHandler((short) MessageType.FieldsPropertiesSync, (m, c) =>
            {
                //TODO Not implemented
            });
        }

        private void HandlePingMessage(Message msg, Connection.Connection con)
        {
            var date = DateTime.FromBinary(BitConverter.ToInt64(msg.Body, 0));
            PingMessageReceived?.Invoke(date);
        }

        protected virtual void HandleMulticast(Message m, Connection.Connection c)
        {
            var mode = (m.Ordered ? Mode.Ordered : Mode.None) & (m.Reliable ? Mode.Reliable : Mode.None) & (m.Sequenced ? Mode.Sequenced : Mode.None);
            var message = new Message((short) MessageType.RPCSendToEndPoint, mode, m.Body);

            var connections = GetConnections();

            foreach (var connection in connections)
            {
                if (c != connection)
                    Send(message, connection);
            }

            //HandleRPCMessage(m, c);
        }

        protected virtual void HandleRPCMessage(Message m, Connection.Connection c)
        {
            var ds = DataStorage.CreateForRead(m.Body);
            var methodName = ds.ReadString();

            NetworkController.Instance.RPCMessage(methodName, ds);
        }
        
#if UNITY_ENGINE
        protected override void InvokeHandler(ReceiveHandler handler, Message msg, Connection.Connection connection)
        {
            if (BaseSyncDispatcher.IsInitialized)
            {
                BaseSyncDispatcher.RunOnMainThread(handler, msg, connection);
            }
        }
#endif

    }
}