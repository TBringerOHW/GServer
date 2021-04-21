namespace GServer.Containers
{
    public class Quaternion : ISerializable, IDeserializable, IDeepSerializable, IDeepDeserializable
    {
        [DsSerialize]
        public float X { get; set; }

        [DsSerialize]
        public float Y { get; set; }

        [DsSerialize]
        public float Z { get; set; }

        [DsSerialize]
        public float W { get; set; }

        public void FillDeserialize(byte[] buffer) {
            var ds = DataStorage.CreateForRead(buffer);
            ReadFromDs(ds);
        }

        public void PushToDs(DataStorage ds) {
            ds.Push(X).Push(Y).Push(Z).Push(W);
        }

        public void ReadFromDs(DataStorage ds) {
            X = ds.ReadFloat();
            Y = ds.ReadFloat();
            Z = ds.ReadFloat();
            W = ds.ReadFloat();
        }

        public byte[] Serialize() {
            var ds = DataStorage.CreateForWrite();
            PushToDs(ds);
            return ds.Serialize();
        }
    }
}