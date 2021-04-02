using System;
using System.Collections.Generic;
using System.Linq;
using GServer.Containers;

namespace GServer.RPC
{
    public class MarshallableList<TMarshallable> : List<TMarshallable>, IMarshallableCollection where TMarshallable : IMarshallable 
    {
        public void PushToDs(DataStorage ds)
        {
            ds.Push(Count);
            ds.Push(GetType().GenericTypeArguments.Single().FullName);
            foreach (var marshallable in this)
            {
                marshallable.PushToDs(ds);
            }
        }

        public void ReadFromDs(DataStorage ds)
        {
            var count = ds.ReadInt32();
            var marshallableTypeName = ds.ReadString();
            var marshallableType = Type.GetType(marshallableTypeName);
            for (var elementNumber = 0; elementNumber < count; elementNumber++)
            {
                var marshallable = (TMarshallable)Activator.CreateInstance(marshallableType);
                marshallable.ReadFromDs(ds);
                Add(marshallable);
            }
        }

        public void Fill()
        {
            Add(Activator.CreateInstance<TMarshallable>());
        }

        public object GetCollectionElementInstance()
        {
            return Activator.CreateInstance<TMarshallable>();
        }
    }

    public class MarshallableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IMarshallableCollection, IMarshallable
    {
        public virtual void PushToDs(DataStorage ds)
        {
            ds.Push(Count);
            var dictArguments = GetType().GenericTypeArguments;
            ds.Push(dictArguments[0].FullName);
            ds.Push(dictArguments[1].FullName);
        }

        public virtual void ReadFromDs(DataStorage ds)
        {
        }

        protected virtual void CustomReadFromDs(DataStorage ds, Action<Type, Type> readAction)
        {
            var count = ds.ReadInt32();
            var keyType = Type.GetType(ds.ReadString());
            var valueType = Type.GetType(ds.ReadString());
            for (var kvpNumber = 0; kvpNumber < count; kvpNumber++)
            {
                readAction(keyType,valueType);
            }
        }

        public void Fill()
        {
            var kvp = (KeyValuePair<TKey, TValue>)GetCollectionElementInstance();
            Add(kvp.Key, kvp.Value);
        }

        public virtual object GetCollectionElementInstance()
        {
            return new KeyValuePair<TKey,TValue>(Activator.CreateInstance<TKey>(), Activator.CreateInstance<TValue>());
        }
    }
    
    public class MarshallableDictionaryMValue<TKey, TMarshallableValue>: MarshallableDictionary<TKey, TMarshallableValue> where TMarshallableValue : IMarshallable
    {
        public override void PushToDs(DataStorage ds)
        {
            base.PushToDs(ds);
            foreach (var keyValuePair in this)
            {
                ds.Push(keyValuePair.Key);
                keyValuePair.Value.PushToDs(ds);
            }
        }
        
        public override void ReadFromDs(DataStorage ds)
        {
            base.ReadFromDs(ds);
            CustomReadFromDs(ds, (keyType, valueType) =>
            {
                var kvp = (KeyValuePair<TKey,TMarshallableValue>)GetCollectionElementInstance();
                kvp = new KeyValuePair<TKey, TMarshallableValue>((TKey) ds.ReadObject(ds.ReadString()), kvp.Value);
                kvp.Value.ReadFromDs(ds);
                Add(kvp.Key, kvp.Value);
            });
        }

        public override object GetCollectionElementInstance()
        {
            return new KeyValuePair<TKey,TMarshallableValue>(Activator.CreateInstance<TKey>(), Activator.CreateInstance<TMarshallableValue>());
        }
    }

    public class MarshallableDictionaryMKey<TMarshallableKey, TValue> : MarshallableDictionary<TMarshallableKey, TValue> where TMarshallableKey : IMarshallable
    {
        public override void PushToDs(DataStorage ds)
        {
            base.PushToDs(ds);
            foreach (var keyValuePair in this)
            {
                keyValuePair.Key.PushToDs(ds);
                ds.Push(keyValuePair.Value);
            }
        }

        public override void ReadFromDs(DataStorage ds)
        {
            base.ReadFromDs(ds);
            CustomReadFromDs(ds, (keyType, valueType) =>
            {
                var kvp = (KeyValuePair<TMarshallableKey,TValue>)GetCollectionElementInstance();
                kvp.Key.ReadFromDs(ds);
                kvp = new KeyValuePair<TMarshallableKey, TValue>(kvp.Key, (TValue) ds.ReadObject(ds.ReadString()));
                Add(kvp.Key, kvp.Value);
            });
        }
        
        public override object GetCollectionElementInstance()
        {
            return new KeyValuePair<TMarshallableKey,TValue>(Activator.CreateInstance<TMarshallableKey>(),
                Activator.CreateInstance<TValue>());
        }
    }

    public class MarshallableDictionaryMKeyValue<TMarshallableKey, TMarshallableValue> : MarshallableDictionary<TMarshallableKey, TMarshallableValue> 
        where TMarshallableKey : IMarshallable
        where TMarshallableValue : IMarshallable
    {
        public override void PushToDs(DataStorage ds)
        {
            base.PushToDs(ds);
            foreach (var keyValuePair in this)
            {
                keyValuePair.Key.PushToDs(ds);
                keyValuePair.Value.PushToDs(ds);
            }
        }

        public override void ReadFromDs(DataStorage ds)
        {
            base.ReadFromDs(ds);
            CustomReadFromDs(ds, (keyType, valueType) =>
            {
                var kvp = (KeyValuePair<TMarshallableKey,TMarshallableValue>)GetCollectionElementInstance();
                kvp.Key.ReadFromDs(ds);
                kvp.Value.ReadFromDs(ds);
            });
        }
        
        public override object GetCollectionElementInstance()
        {
            return new KeyValuePair<TMarshallableKey,TMarshallableValue>(Activator.CreateInstance<TMarshallableKey>(), Activator.CreateInstance<TMarshallableValue>());
        }
    }

    public interface IMarshallableCollection
    {
        void Fill();
        object GetCollectionElementInstance();
    }
}