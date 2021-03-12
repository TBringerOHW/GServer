using System;
using System.Reflection;

namespace GServer.RPC
{
    public enum InvokeType
    {
        Client,
        Server,
        MultiCast
    }
    public class InvokeAttribute: Attribute
    {
        public InvokeType Type { get; set; }

        public InvokeAttribute()
        {
            Type = InvokeType.MultiCast;
        }

        public InvokeAttribute(InvokeType invokeType)
        {
            Type = invokeType;
        }
    }

    public class InvokeHelper
    {
        public Object classInstance;
        public MethodInfo method;
        public InvokeType type;
        public InvokeHelper(Object instance, MethodInfo methodInfo, InvokeType invokeType)
        {
            classInstance = instance;
            method = methodInfo;
            type = invokeType;
        }
    }

    
    public enum SyncType
    {
        Receive,
        Send,
        Exchange
    }

    public class SyncAttribute : Attribute
    {
        private SyncType SyncType { get; set; }

        public SyncAttribute()
        {
            SyncType = SyncType.Receive;
        }

        public SyncAttribute(SyncType syncType)
        {
            SyncType = syncType;
        }
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
}