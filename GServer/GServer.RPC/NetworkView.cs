using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GServer.Containers;
using GServer.Messages;
//#define LOG_ENABLED

namespace GServer.RPC
{
    public class NetworkView
    {
        private const string SyncMethodPrefix = "syncObject@";

        private readonly int _hash;

        private static int _countOfViews;
        private float _syncPeriod;

        private readonly NetworkController _netCon = NetworkController.Instance;

        private readonly Dictionary<string, InvokeHelper> _methods = new Dictionary<string, InvokeHelper>();
        private readonly Dictionary<string, Dictionary<string, InfoHelper>> _properties = new Dictionary<string, Dictionary<string, InfoHelper>>();
        private readonly Dictionary<string, IMarshallable> _methodsArguments = new Dictionary<string, IMarshallable>();
        private readonly Dictionary<string, int> _countOfClasses = new Dictionary<string, int>();
        private readonly Dictionary<string, object> _stringToObject = new Dictionary<string, object>();
        private readonly Dictionary<string, int> _cache = new Dictionary<string, int>();
        private readonly Dictionary<int, int> _hashToNum = new Dictionary<int, int>();

        private string GetCacheKeyName(object c, string key)
        {
            return key + "@" + GetUniqueClassString(c);
        }

        public NetworkView(float syncPeriod = 1f)
        {
            _syncPeriod = syncPeriod;
            _hash = ++_countOfViews;
        }

        public void StartSyncProcess()
        {
            BaseSyncDispatcher.StartSync(this);
        }

        public void InitInvokeRPCOnly(params object[] classes)
        {
            foreach (var targetClass in classes)
            {
                if (!targetClass.GetType().IsClass) continue;

                CountClasses(targetClass);

                FindInvokableMethods(targetClass);
            }
        }

        public void InitInvokeSyncOnly(params object[] classes)
        {
            foreach (var targetClass in classes)
            {
                if (!targetClass.GetType().IsClass) continue;

                CountClasses(targetClass);
                RegisterSyncObject(targetClass);
            }
        }

        public void InitInvoke(params object[] classes)
        {
            foreach (var targetClass in classes)
            {
                if (!targetClass.GetType().IsClass) continue;

                CountClasses(targetClass);

                FindInvokableMethods(targetClass);
                RegisterSyncObject(targetClass);
            }
        }

        private void RegisterSyncObject(object targetClass)
        {
            var propertyMap = FindSyncFields(targetClass);
            foreach (var property in FindSyncProperties(targetClass))
            {
                propertyMap.Add(property.Key, property.Value);
            }

            lock (_properties)
            {
                _properties.Add(GetUniqueClassString(targetClass), propertyMap);
            }
            _netCon.RegisterInvoke(GetSyncMethod(targetClass), this);
        }

        private Dictionary<string, InfoHelper> FindSyncProperties(object targetClass)
        {
            var propInfos = ReflectionHelper.GetPropertiesWithAttribute(targetClass.GetType(), new SyncAttribute().GetType());
            var propertyMap = new Dictionary<string, InfoHelper>();
            //Console.WriteLine(propInfos);
            foreach (var propInfo in propInfos)
            {
#if LOG_ENABLED
                this.LogObjectMessage(nameof(FindSyncProperties), $"Registered property [{propInfo.Name}] for object [{targetClass}]", DebugLogger.ELogMessageType.Info);
#endif
                NetworkController.ShowMessage("Register property " + propInfo.Name);
                var propName = propInfo.Name;
                propertyMap.Add(propName, new InfoHelper(propInfo));
                var nonBasicObj = ReflectionHelper.CheckNonBasicType(propInfo.PropertyType);
                if (nonBasicObj != null)
                {
                    var str = nonBasicObj.ToString();
                    if (!_methodsArguments.ContainsKey(str ?? throw new InvalidOperationException()))
                    {
                        _methodsArguments.Add(str, nonBasicObj as IMarshallable);
                    }
                }
            }

            return propertyMap;
        }

        private Dictionary<string, InfoHelper> FindSyncFields(object targetClass)
        {
            var fieldInfos = ReflectionHelper.GetFieldsWithAttribute(targetClass.GetType(), new SyncAttribute().GetType());
            var propertyMap = new Dictionary<string, InfoHelper>();
            //Console.WriteLine(fieldInfos);
            foreach (var fieldInfo in fieldInfos)
            {
#if LOG_ENABLED
                this.LogObjectMessage(nameof(FindSyncFields), $"Registered field [{fieldInfo.Name}] for object [{targetClass}]", DebugLogger.ELogMessageType.Info);
#endif
                NetworkController.ShowMessage("Register field " + fieldInfo.Name);

                var propName = fieldInfo.Name;
                propertyMap.Add(propName, new InfoHelper(fieldInfo));
                var nonBasicObj = ReflectionHelper.CheckNonBasicType(fieldInfo.FieldType);
                if (nonBasicObj != null)
                {
                    var str = nonBasicObj.ToString();
                    if (!_methodsArguments.ContainsKey(str ?? throw new InvalidOperationException()))
                    {
                        _methodsArguments.Add(str, nonBasicObj as IMarshallable);
                    }
                }
            }

            return propertyMap;
        }

        private void FindInvokableMethods(object targetClass)
        {
            var miInfos = ReflectionHelper.GetMethodsWithAttribute(targetClass.GetType(), typeof(InvokeAttribute));
            foreach (var member in miInfos)
            {
                var customAttributes = member.GetCustomAttributes(typeof(InvokeAttribute), false);
                if (customAttributes.Length > 0)
                {
                }
                else
                {
                    continue;
                }

                var methodName = GetUniqueClassString(targetClass) + "." + member.Name;
#if LOG_ENABLED
                this.LogObjectMessage(nameof(FindInvokableMethods), $"Registered method [{member}] as [{methodName}]", DebugLogger.ELogMessageType.Info);
#endif
                NetworkController.ShowMessage("Register method " + member + " as " + methodName);

                _methods.Add(methodName, new InvokeHelper(targetClass, member));
                _netCon.RegisterInvoke(methodName, this);
                var res = ReflectionHelper.GetMethodParamsObjects(member);

                if (res == null || res.Count <= 0) continue;

                foreach (var param in res)
                {
                    if (_methodsArguments.ContainsKey(param.Key)) continue;
                    _methodsArguments.Add(param.Key, param.Value);
                    NetworkController.ShowMessage($"Register non-basic type {param.Value.GetType()}");
#if LOG_ENABLED
                    this.LogObjectMessage(nameof(FindInvokableMethods), $"Registered marshallable type [{param.Value.GetType()}]", DebugLogger.ELogMessageType.Info);
#endif
                }
            }
        }

        private void CountClasses(object targetClass)
        {
            var key = targetClass.ToString();
            if (_countOfClasses.TryGetValue(key ?? throw new InvalidOperationException(), out var num))
            {
                num += 1;
                _countOfClasses.Remove(key);
            }
            else
            {
                num = 1;
            }

            _countOfClasses.Add(key, num);
            _hashToNum.Add(targetClass.GetHashCode(), num);
            _stringToObject.Add(GetUniqueClassString(targetClass), targetClass);
        }

        internal float GetSyncPeriod()
        {
            return _syncPeriod;
        }

        public void SetSyncPeriod(float syncPeriod)
        {
            _syncPeriod = syncPeriod;
            BaseSyncDispatcher.StopSync(this);
            BaseSyncDispatcher.StartSync(this);
        }

        ~NetworkView()
        {
            BaseSyncDispatcher.StopSync(this);
        }

        private string GetUniqueClassString(object c)
        {
            var hash = c.GetHashCode();
            if (!_hashToNum.TryGetValue(hash, out var num))
            {
                NetworkController.ShowException(new Exception("object not invoked"));
                return null;
            }

            var uniqStr = $"{_hash}{c.GetType().Name}#{num}";

            return uniqStr; //_hash + "_" + c + "#" + num;
        }

        private IMarshallable GetArgument(string name)
        {
            if (!_methodsArguments.TryGetValue(name, out var arg))
            {
                return null;
            }

            if (arg is IList)
            {
                return (IMarshallable)Activator.CreateInstance(arg.GetType());
            }
            
            return arg;
        }

        internal void SyncNow()
        {
            string[] propClone;
            lock (_properties)
            {
                propClone = new string[_properties.Keys.Count];
                _properties.Keys.CopyTo(propClone, 0);
            }

            foreach (var one in propClone)
            {
                if (!_stringToObject.TryGetValue(one, out var c))
                {
                    continue;
                }

                var fields = GetClassFields(c);
                if (fields.Count == 0)
                {
                    continue;
                }

                var ds = DataStorage.CreateForWrite();

                var method = GetSyncMethod(c);
                ds.Push(method);

                foreach (var field in fields)
                {
                    ds.Push(field.Key);
                    if (field.Value is IMarshallable imObj)
                    {
                        PushCustomType(imObj, ds);
                        continue;
                    }

                    PushBasicType(field.Value, ds);
                }

                NetworkController.Instance.SendMessage(ds, (short) MessageType.RPCSendToEndPoint);
            }
        }

        private string GetSyncMethod(object c)
        {
            return SyncMethodPrefix + GetUniqueClassString(c);
        }

        private Dictionary<string, object> GetClassFields(object c)
        {
            var result = new Dictionary<string, object>();

            Dictionary<string, InfoHelper> props;
            lock (_properties)
            {
                _properties.TryGetValue(GetUniqueClassString(c), out props);
            }
            if (props != null)
            {
                foreach (var prop in props)
                {
                    var value = prop.Value.Get(c);
                    var cacheKey = GetCacheKeyName(c, prop.Key);

                    //object clone = value;
                    //if (value is ICloneable)
                    //{
                    //    clone = (value as ICloneable).Clone();
                    //}

                    if (!_cache.ContainsKey(cacheKey))
                    {
                        _cache.Add(cacheKey, value.GetHashCode());
                    }
                    else if (!_cache[cacheKey].Equals(value.GetHashCode()))
                    {
                        _cache[cacheKey] = value.GetHashCode();
                    }

                    result.Add(prop.Key, value);
                }
            }

            return result;
        }

        // internal void SetClassFields(string cl, Dictionary<string, object> fields)
        // {
        //     Object c;
        //     if (!stringToObject.TryGetValue(cl, out c))
        //     {
        //         NetworkController.ShowException(new Exception("invalid sync object"));
        //     }
        //     Dictionary<string, InfoHelper> props;
        //     if (!properties.TryGetValue(getUniqueClassString(c), out props))
        //     {
        //         NetworkController.ShowException(new Exception("object's fields hasn't been reflected"));
        //     }
        //     foreach (KeyValuePair<string, object> prop in fields)
        //     {
        //         InfoHelper info;
        //         if (props.TryGetValue(prop.Key, out info))
        //         {
        //             info.Set(c, prop.Value);
        //         }
        //     }
        // }

        private InvokeHelper GetHelper(string method)
        {
            return _methods.TryGetValue(method, out var arg) ? arg : null;
        }

        public void Call(object c, string method, params object[] args)
        {
            method = GetUniqueClassString(c) + "." + method;
            var helper = GetHelper(method);

            if (helper == null) return;

            ServerCall(method, args);

            /* Obsolete
             
             switch (helper.type)
            {
                case InvokeType.Local:
                    ClientCall(helper, args);
                    break;
                case InvokeType.Remote:
                    ServerCall(method, args);
                    break;
                case InvokeType.MultiCast: 
                    ClientCall(helper, args);
                    ServerCall(method, args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }*/
        }

        private void ServerCall(string method, object[] args)
        {
            var ds = DataStorage.CreateForWrite();
            ds.Push(method);
            foreach (var obj in args)
            {
                if (IsValidBasicType(obj.GetType()))
                {
                    PushBasicType(obj, ds);
                }
                else
                {
                    PushCustomType(obj as IMarshallable, ds);
                }
            }

            NetworkController.Instance.SendMessage(ds, (short) MessageType.RPCSendToEndPoint);
        }

        private void ClientCall(InvokeHelper helper, params object[] args)
        {
            helper.Method.Invoke(helper.ClassInstance, args);
        }

        internal void RPC(string method, DataStorage request)
        {
            if (method.StartsWith(SyncMethodPrefix))
            {
                SyncRPC(method, request);
                //syncRPCThread = new Thread(() => SyncRPC(method, request));
                //syncRPCThread.Start();
                return;
            }

            var helper = GetHelper(method);
            if (helper == null) return;
            ClientCall(helper, ParseRequest(request));
        }

        private void SyncRPC(string method, DataStorage ds)
        {
            var split = method.Split(new[] {SyncMethodPrefix}, 2, StringSplitOptions.None);
            if (split.Length != 2) return;
            var strObj = split[1];
            if (!_stringToObject.TryGetValue(strObj, out var c)) return;
            
            Dictionary<string, InfoHelper> props;
            lock (_properties)
            {
                _properties.TryGetValue(GetUniqueClassString(c), out props);
            }
            if (props == null) return;

            while (!ds.Empty)
            {
                var field = ds.ReadString();
                if (!props.TryGetValue(field, out var info))
                {
                    continue;
                }

                var type = info.Type.GetFullName();
                var dsType = ds.ReadString();
                if (type != null && !type.Equals(dsType)) type = dsType;
                var value = ParseObject(type, ds);
                if (value == null)
                {
                    continue;
                }

                var cacheKey = GetCacheKeyName(c, field);

                //object clone = value;
                //if (value is ICloneable)
                //{
                //    clone = (value as ICloneable).Clone();
                //}

                if (!_cache.ContainsKey(cacheKey))
                {
                    _cache.Add(cacheKey, value.GetHashCode());
                    info.Set(c, value);
                }
                else
                {
                    var cachedHash = _cache[cacheKey];
                    var valueHash = value.GetHashCode();
                    if (!cachedHash.Equals(valueHash))
                    {
                        _cache[cacheKey] = value.GetHashCode();
                        info.Set(c, value);
                    }
                }
            }
        }

        private object[] ParseRequest(DataStorage ds)
        {
            var result = new Dictionary<int, object>();
            var i = 0;
            while (!ds.Empty)
            {
                var key = ds.ReadString();
                var resObject = ParseObject(key, ds);
                if (resObject == null)
                {
                    NetworkController.ShowException(new Exception("invalid rpc parameter"));
                }

                result.Add(i, resObject);
                i++;
            }

            var resultSlice = new object[i];
            foreach (var res in result)
            {
                resultSlice[res.Key] = res.Value;
            }

            return resultSlice;
        }

        private object ParseObject(string key, DataStorage ds)
        {
            var marshallable = GetArgument(key);
            if (marshallable != null)
            {
                return ParseCustomType(marshallable, ds);
            }

            return ParseBasicType(key, ds);
        }

        private object ParseCustomType(IMarshallable obj, DataStorage ds)
        {
            obj.ReadFromDs(ds);
            return obj;
        }

        private object ParseBasicType(string typ, DataStorage ds)
        {
            return ds.ReadObject(typ);
        }

        private void PushCustomType(IMarshallable obj, DataStorage ds)
        {
            var imObj = obj;
            if (imObj == null)
                NetworkController.ShowException(new Exception("invalid rpc parameter"));

            ds.Push(obj.GetType().GetFullName(), imObj);
        }

        private void PushBasicType(object obj, DataStorage ds)
        {
            try
            {
                ds.Push(obj.GetType().GetFullName(), obj);
            }
            catch (Exception e)
            {
                NetworkController.ShowException(e);
            }
        }

        internal static bool IsValidBasicType(Type type)
        {
            var basicTypeName = type.GetBasicTypeName();
            switch (basicTypeName)
            {
                case "System.Byte":
                case "System.SByte":
                case "System.Boolean":
                case "System.Char":
                case "System.Decimal":
                case "System.Double":
                case "System.Single":
                case "System.Int16":
                case "System.UInt16":
                case "System.Int32":
                case "System.UInt32":
                case "System.Int64":
                case "System.UInt64":
                case "System.String":
                    return true;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _hash;
        }
    }

    internal class InfoHelper
    {
        internal readonly Func<object, object> Get;
        internal readonly Action<object, object> Set;
        internal readonly Type Type;
        internal string Name;

        internal InfoHelper(PropertyInfo prop)
        {
            Get = prop.GetValue;
            Set = prop.SetValue;
            Type = prop.PropertyType;
            Name = prop.Name;
        }

        internal InfoHelper(FieldInfo prop)
        {
            Get = prop.GetValue;
            Set = prop.SetValue;
            Type = prop.FieldType;
            Name = prop.Name;
        }
    }
}