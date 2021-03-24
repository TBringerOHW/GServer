using System;
using System.Reflection;

namespace GServer.RPC
{
    #region [Invoke]

    
    public class InvokeAttribute: Attribute
    {
    }

    public class InvokeHelper
    {
        public readonly object ClassInstance;
        public readonly MethodInfo Method;
        public InvokeHelper(object instance, MethodInfo methodInfo)
        {
            ClassInstance = instance;
            Method = methodInfo;
        }
    }

    #endregion

    #region [Sync]
    
    public enum SyncType
    {
        Receive,
        Send,
        Exchange
    }

    public class SyncAttribute : Attribute
    {
    }

    public class SyncHelper
    {
        public Object Class;
        public MemberInfo Field;
        public SyncType SyncType;

        public SyncHelper(Object c, MemberInfo field, SyncType syncType)
        {
            Class = c;
            Field = field;
            SyncType = syncType;
        }
    }

    #endregion
}