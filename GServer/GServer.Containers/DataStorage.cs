using System;
using System.IO;

// ReSharper disable UseNullPropagation
// ReSharper disable ArrangeAccessorOwnerBody

namespace GServer.Containers
{
    public class DataStorage : ISerializable, IDisposable
    {
        private readonly MemoryStream Stream;
        private readonly BinaryReader Reader;
        private readonly BinaryWriter Writer;

        #region [Constructors]

        private DataStorage(byte[] buffer) {
            Stream = new MemoryStream(buffer);
            Reader = new BinaryReader(Stream);
        }

        private DataStorage() {
            Stream = new MemoryStream();
            Writer = new BinaryWriter(Stream);
        }

        public static DataStorage CreateForRead(byte[] buffer) {
            return new DataStorage(buffer);
        }

        public static DataStorage CreateForWrite() {
            return new DataStorage();
        }

        #endregion

        public byte[] Serialize() {
            return Stream.ToArray();
        }

        #region [Push]

        #region [ValueBased]

        public DataStorage Push(byte val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }
        
        public DataStorage Push(ushort val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(short val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(int val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(long val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(bool val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(char val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(double val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(decimal val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(float val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(Guid val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val.ToByteArray());
            return this;
        }

        public DataStorage Push(byte[] val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(val);
            return this;
        }

        [Obsolete]
        public DataStorage Push(IDeepSerializable val) {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            val.PushToDs(this);
            return this;
        }

        #endregion
        
        #region [KeyBased]
        public DataStorage Push(string key, byte val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, short val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, int val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, long val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, bool val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, char val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, double val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, decimal val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, float val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, string val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, Guid val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val.ToByteArray());
            return this;
        }

        public DataStorage Push(string key, byte[] val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            Writer.Write(val);
            return this;
        }

        public DataStorage Push(string key, IDeepSerializable val)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");
            Writer.Write(key);
            val.PushToDs(this);
            return this;
        }
        
        #endregion

        #endregion

        #region [Read]
        
        public byte ReadByte() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadByte();
        }

        public byte[] ReadBytes(int count) {
            if (Reader == null) {
                throw new Exception("DataStorage in write only mode");
            }
            return Reader.ReadBytes(count);
        }
        
        public ushort ReadUInt16() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadUInt16();
        }

        public short ReadInt16() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadInt16();
        }

        public int ReadInt32() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadInt32();
        }

        public long ReadInt64() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadInt64();
        }

        public char ReadChar() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadChar();
        }

        public bool ReadBoolean() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadBoolean();
        }

        public string ReadString() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadString();
        }

        public double ReadDouble() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadDouble();
        }

        public decimal ReadDecimal() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadDecimal();
        }

        public float ReadFloat() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadSingle();
        }

        public Guid ReadGuid() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return new Guid(Reader.ReadBytes(16));
        }

        #endregion
        
        #region Auto serialize methods
        public Pair<Type, object> ReadPair()
        {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            var typeName = Reader.ReadString();
            object obj = ReadObject(typeName);

            Pair<Type, object> pair = new Pair<Type, object>(Type.GetType(typeName), obj);
            return pair;
        }

        public object ReadObject(string typeName)
        {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");

            object obj = null;

            switch (typeName)
            {
                case "System.Int32":
                    obj = ReadInt32();
                    break;
                case "System.Byte":
                    obj = ReadByte();
                    break;
                case "System.Boolean":
                    obj = ReadBoolean();
                    break;
                case "System.Char":
                    obj = ReadChar();
                    break;
                case "System.Decimal":
                    obj = ReadDecimal();
                    break;
                case "System.Double":
                    obj = ReadDouble();
                    break;
                case "System.Single":
                    obj = ReadFloat();
                    break;
                case "System.Int64":
                    obj = ReadInt64();
                    break;
                case "System.Int16":
                    obj = ReadInt16();
                    break;
                case "System.String":
                    obj = ReadString();
                    break;
            }

            return obj;
        }

        public DataStorage Push(string typeName, object obj)
        {
            if (Writer == null)
                throw new Exception("DataStorage in read only mode");

            Writer.Write(typeName);
            
            switch (typeName)
            {
                case "System.Int32":
                    Writer.Write((int)obj);
                    break;
                case "System.Byte":
                    Writer.Write((byte)obj);
                    break;
                case "System.Boolean":
                    Writer.Write((bool)obj);
                    break;
                case "System.Char":
                    Writer.Write((char)obj);
                    break;
                case "System.Decimal":
                    Writer.Write((decimal)obj);
                    break;
                case "System.Double":
                    Writer.Write((double)obj);
                    break;
                case "System.Single":
                    Writer.Write((float)obj);
                    break;
                case "System.Int64":
                    Writer.Write((long)obj);
                    break;
                case "System.Int16":
                    Writer.Write((short)obj);
                    break;
                case "System.String":
                    Writer.Write(obj.ToString());
                    break;
            }

            return this;
        }
        
        #endregion

        public byte[] ReadToEnd() {
            if (Reader == null)
                throw new Exception("DataStorage in write only mode");
            return Reader.ReadBytes((int) (Stream.Length - Stream.Position));
        }

        public bool Empty {
            get { return Stream.Position == Stream.Length; }
        }

        public void Dispose() {
            if (Reader != null) Reader.Close();
            if (Writer != null) Writer.Close();
            if (Stream != null) Stream.Close();
        }

        ~DataStorage() {
            Dispose();
        }
    }
}