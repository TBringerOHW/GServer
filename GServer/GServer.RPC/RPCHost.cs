using GServer.Messages;

namespace GServer.RPC
{
    public class RPCHost : Host
    {
        public RPCHost(int port) : base(port) {}

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