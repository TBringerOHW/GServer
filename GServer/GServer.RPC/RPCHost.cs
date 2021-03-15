using GServer.Containers;
using GServer.Messages;

namespace GServer.RPC
{
    public class RPCHost : Host
    {
        public RPCHost(int port) : base(port)
        {
            AddHandler((short)MessageType.Resend, (m, c) =>
            {
                var mode = (m.Ordered ? Mode.Ordered : Mode.None) & (m.Reliable ? Mode.Reliable : Mode.None) & (m.Sequenced ? Mode.Sequenced : Mode.None);
                var message = new Message((short)MessageType.SendToEndPoint, mode, m.Body);
                foreach (var connection in GetConnections())
                {
                    if (c != connection)
                    {
                        Send(message, connection);
                    }
                }
            });
            AddHandler((short)MessageType.SendToEndPoint, (m, c) =>
            {
                var ds = DataStorage.CreateForRead(m.Body);
                var methodName = ds.ReadString();

                NetworkController.Instance.RPCMessage(methodName, ds);
            });
        }

        protected override void InvokeHandler(ReceiveHandler handler, Message msg, Connection.Connection connection)
        {
            if (BaseSyncDispatcher.IsInitialized)
            {
                BaseSyncDispatcher.RunOnMainThread(handler, msg, connection);
            }
            
            base.InvokeHandler(handler, msg, connection);
        }
    }
}