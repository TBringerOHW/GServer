using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using GServer.Containers;
using GServer.Messages;

// ReSharper disable UseNullPropagation
// ReSharper disable ArrangeAccessorOwnerBody

namespace GServer.Connection
{
    internal class Packet : ISerializable
    {
        public int Priority { get; set; }
        public Message Msg { get; }

        public bool Resend {
            // ReSharper disable once ArrangeAccessorOwnerBody
            get { return Msg.Reliable && !Msg.Sequenced; }
        }

        public byte[] Serialize() {
            var buffer = Msg.Serialize();
            return DataStorage.CreateForWrite().Push(buffer.Length).Push(buffer).Serialize();
        }

        public Packet(Message msg) {
            Msg = msg;
            Priority = 0;
        }
    }

    internal class MessageQueue : IEnumerable<KeyValuePair<MessageCounter, Message>>
    {
        private readonly SortedList<MessageCounter, Message> _msgQueue;

        public MessageQueue() {
            _msgQueue = new SortedList<MessageCounter, Message>();
        }

        public void Add(Message msg) {
            if (!_msgQueue.ContainsKey(msg.MessageId))
                _msgQueue.Add(msg.Header.MessageId, msg);
        }

        public IEnumerator<KeyValuePair<MessageCounter, Message>> GetEnumerator() {
            return _msgQueue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _msgQueue.GetEnumerator();
        }

        public void Remove(Message msg) {
            _msgQueue.Remove(msg.Header.MessageId);
        }
    }

    public class Connection
    {
        public IPEndPoint EndPoint { get; set; }
        public readonly Token Token;
        public DateTime LastActivity { get; private set; }
        

        public Connection(IPEndPoint endPoint)
            : this(endPoint, Token.GenerateToken()) { }

        public Connection(IPEndPoint endPoint, Token token) {
            EndPoint = endPoint;
            Token = token;
            LastActivity = DateTime.Now;
            _ackPerMsgType = new Dictionary<short, Ack>();
            _lastSequencedMessageNumPerType = new Dictionary<short, MessageCounter>();
            _lastOrderedMessageNumPerType = new Dictionary<short, MessageCounter>();
            _lastUnOrderedMessageNumPerType = new Dictionary<short, MessageCounter>();
            _messageQueuePerType = new SortedDictionary<short, MessageQueue>();
            _arrivedReliableMessagePerType = new Dictionary<short, Pair<CustomList<MessageCounter>, MessageCounter>>();
            _handlers = new Dictionary<short, Action<Message>>();
        }

        public event Action<Connection> Disconnected;

        public int BufferCount {
            get {
                lock (_messageBuffer) {
                    return _messageBuffer.Count;
                }
            }
        }
        
        public int BufferCountWithResend
        {
            get
            {
                int count;
                lock (_messageBuffer)
                {
                    count = _messageBuffer.Count(packet => packet.Resend);
                }
                return count;
            }
        }

        public int BufferCountWithoutResend
        {
            get
            {
                int count;
                lock (_messageBuffer)
                {
                    count = _messageBuffer.Count(packet => !packet.Resend);
                }
                return count;
            }
        }
        
        public int ReliableMessageCount
        {
            get
            {
                int count;
                lock (_messageBuffer)
                {
                    count = _messageBuffer.Count(packet => !packet.Msg.Reliable);
                }
                return count;
            }
        }

        
        private readonly List<Packet> _messageBuffer = new List<Packet>();

        internal void MarkToSend(Message msg) {
            var p = new Packet(msg);
            lock (_messageBuffer) {
                _messageBuffer.Add(p);
            }
        }
        
        private const long PacketSizeLimit = 4096;
        private const int PacketsAmountLimit = 512;

        internal List<byte[]> GetPacketsToSend(long packetSizeLimit = PacketSizeLimit)
        {
            var i = 0;
            var toSend = new List<Packet>();
            var ds = DataStorage.CreateForWrite();
            var packetsToSend = new List<byte[]>();

            long packetLength = 0;
            lock (_ackPerMsgType) {
                foreach (var ack in _ackPerMsgType) {
                    var buffer = ack.Value.GetAcks();

                    if (Equals(buffer, Ack.Empty)) continue;
                    foreach (var msg in buffer) {
                        i++;

                        if (packetLength >= packetSizeLimit)
                        {
                            foreach (var element in toSend) {
                                ds.Push(element.Serialize());
                            }
                            packetsToSend.Add(ds.Serialize());
                            ds.Clear();
                            toSend.Clear();
                            packetLength = 0;
                        }
                        
                        var packet = new Packet(Message.Ack(ack.Key, msg.Val1, Token, msg.Val2));
                        packetLength += packet.Msg.Body.Length;
                        
                        toSend.Add(packet);
                    }
                }
            }
            lock (_messageBuffer) {
                var toDelete = new List<Packet>();
                _messageBuffer.Sort((x, y) => {
                    var p = x.Priority.CompareTo(y.Priority);
                    return p == 0 ? x.Msg.MessageId.CompareTo(y.Msg.MessageId) : -p;
                });
                if (_messageBuffer.Count > PacketsAmountLimit) Console.WriteLine($"[Info][{DateTime.Now}][GServer.Connection][{nameof(GetPacketsToSend)}] _messageBuffer.Count [{_messageBuffer.Count}] > [{PacketsAmountLimit}]! Reliable count = [{ReliableMessageCount:###}]. Resend count = [{BufferCountWithResend:###}]. No Resend count = [{BufferCountWithoutResend:###}]");
                for (; i < PacketsAmountLimit && i < _messageBuffer.Count; i++) {
                
                    if (packetLength >= packetSizeLimit)
                    {
                        foreach (var element in toSend) {
                            ds.Push(element.Serialize());
                        }
                        packetsToSend.Add(ds.Serialize());
                        ds.Clear();
                        toSend.Clear();
                        packetLength = 0;
                    }
                    
                    packetLength += _messageBuffer[i].Msg.Body.Length;
                    toSend.Add(_messageBuffer[i]);
                    
                    if (!_messageBuffer[i].Resend) {
                        toDelete.Add(_messageBuffer[i]);
                    }
                }
                for (; i < _messageBuffer.Count; i++) {
                    _messageBuffer[i].Priority++;
                }

                foreach (var element in toDelete) {
                    _messageBuffer.Remove(element);
                }
            }

            if (toSend.Count > 0)
            {
                foreach (var element in toSend) {
                    ds.Push(element.Serialize());
                }
                packetsToSend.Add(ds.Serialize());
            }
            return packetsToSend;
        }

        internal byte[] GetBytesToSend() {
            var i = 0;
            var toSend = new List<Packet>();
            long packetLength = 0;
            lock (_ackPerMsgType) {
                foreach (var ack in _ackPerMsgType) {
                    var buffer = ack.Value.GetAcks();

                    if (Equals(buffer, Ack.Empty)) continue;
                    foreach (var msg in buffer) {
                        i++;
                        
                        if (packetLength >= PacketSizeLimit) break;
                        var packet = new Packet(Message.Ack(ack.Key, msg.Val1, Token, msg.Val2));
                        packetLength += packet.Msg.Body.Length;
                        
                        toSend.Add(packet);
                    }
                }
            }
            lock (_messageBuffer) {
                var toDelete = new List<Packet>();
                _messageBuffer.Sort((x, y) => {
                    var p = x.Priority.CompareTo(y.Priority);
                    return p == 0 ? x.Msg.MessageId.CompareTo(y.Msg.MessageId) : -p;
                });
                for (; i < 128 && i < _messageBuffer.Count; i++) {
                    
                    if (packetLength >= PacketSizeLimit) break;
                    packetLength += _messageBuffer[i].Msg.Body.Length;
                    
                    toSend.Add(_messageBuffer[i]);
                    if (!_messageBuffer[i].Resend) {
                        toDelete.Add(_messageBuffer[i]);
                    }
                }
                for (; i < _messageBuffer.Count; i++) {
                    _messageBuffer[i].Priority++;
                }

                foreach (var element in toDelete) {
                    _messageBuffer.Remove(element);
                }
            }

            var ds = DataStorage.CreateForWrite();
            foreach (var element in toSend) {
                ds.Push(element.Serialize());
            }
            return ds.Serialize();
        }

        internal void UpdateActivity() {
            LastActivity = DateTime.Now;
        }

        //TODO Was internal previously
        public void Disconnect() {
            if (Disconnected != null)
                Disconnected.Invoke(this);
        }

        #region Reliable

        private readonly IDictionary<short, Ack> _ackPerMsgType;

        private readonly IDictionary<short, Pair<CustomList<MessageCounter>, MessageCounter>>
            _arrivedReliableMessagePerType;

        internal void ReceiveReliable(Message msg) {
            lock (_ackPerMsgType) {
                if (_ackPerMsgType.ContainsKey(msg.Header.Type)) {
                    _ackPerMsgType[msg.Header.Type].ReceiveReliable(msg);
                }
                else {
                    var ack = new Ack();
                    _ackPerMsgType.Add(msg.Header.Type, ack);
                    ack.MessageArrived += AckArrivedHandler;
                    ack.ReceiveReliable(msg);
                }
            }
        }

        internal bool HasAlreadyArrived(Message msg) {
            var msgId = msg.MessageId;
            lock (_arrivedReliableMessagePerType) {
                if (!_arrivedReliableMessagePerType.ContainsKey(msg.Header.Type)) {
                    var list = new CustomList<MessageCounter>();
                    _arrivedReliableMessagePerType.Add(msg.Header.Type,
                        new Pair<CustomList<MessageCounter>, MessageCounter>(list, -1));
                }
                var pair = _arrivedReliableMessagePerType[msg.Header.Type];
                var arrivedMessages = pair.Val1;

                var node = arrivedMessages.First;
                CustomNode<MessageCounter> res = null;
                var pos = 0;
                if (msgId >= pair.Val2) {
                    if (msgId == pair.Val2) {
                        return true;
                    }
                    if (msgId > pair.Val2 + 1) {
                        arrivedMessages.PushBack(pair.Val2);
                        arrivedMessages.PushBack(msgId);
                        _arrivedReliableMessagePerType[msg.Header.Type].Val2 = msgId;
                        return false;
                    }
                    _arrivedReliableMessagePerType[msg.Header.Type].Val2 = msgId;
                    return false;
                }
                if (arrivedMessages.Empty) {
                    return true;
                }
                if (msgId < arrivedMessages.First.Value) {
                    return true;
                }
                for (int i = 0, count = arrivedMessages.Count; i < count && node.Value <= msgId; i++) {
                    if (node.Value == msgId) {
                        return true;
                    }
                    if (node.Value < msgId) {
                        res = node;
                        pos = i;
                    }
                    node = node.Next;
                }

                if ((pos & 1) == 1) {
                    return true;
                }
                if (res != null && res.Value == msgId - 1) {
                    if (res.Next.Value == msgId + 1) {
                        arrivedMessages.RemoveBetween(res.Prev, res.Next.Next);
                    }
                    else {
                        res.Value = msgId;
                    }
                }
                else if (res != null && res.Next.Value == msgId + 1) {
                    res.Next.Value = msgId;
                }
                else {
                    arrivedMessages.InsertAfter(res, msgId);
                    if (res != null) arrivedMessages.InsertAfter(res.Next, msgId);
                }
                return false;
            }
        }

        private void AckArrivedHandler(MessageCounter arg1, short arg2) {
            lock (_messageBuffer) {
                var toRemove = _messageBuffer.FirstOrDefault(m =>
                    m.Msg.MessageId == arg1
                    && m.Msg.Header.Type == arg2);
                if (toRemove != null) {
                    _messageBuffer.Remove(toRemove);
                }
            }
        }

        internal void ProcessAck(Message msg) {
            Ack ack = null;
            var ds = DataStorage.CreateForRead(msg.Body);
            var bitField = ds.ReadInt32();
            var msgType = ds.ReadInt16();
            lock (_ackPerMsgType) {
                if (_ackPerMsgType.ContainsKey(msgType)) {
                    ack = _ackPerMsgType[msgType];
                }
            }
            if (ack != null) ack.ProcessReceivedAckBitfield(bitField, msg.MessageId, msgType);
        }

        internal void StoreReliable(Message msg) {
            lock (_ackPerMsgType) {
                if (_ackPerMsgType.ContainsKey(msg.Header.Type)) return;
                var ack = new Ack();
                _ackPerMsgType.Add(msg.Header.Type, ack);
                ack.MessageArrived += AckArrivedHandler;
            }
        }

        #endregion

        #region Sequenced

        private readonly IDictionary<short, MessageCounter> _lastSequencedMessageNumPerType;

        internal bool IsMessageInItsOrder(short type, MessageCounter num) {
            lock (_lastSequencedMessageNumPerType) {
                if (_lastSequencedMessageNumPerType.ContainsKey(type)) {
                    if (_lastSequencedMessageNumPerType[type] < num) {
                        _lastSequencedMessageNumPerType[type] = num;
                        return true;
                    }
                    else {
                        return false;
                    }
                }
                else {
                    _lastSequencedMessageNumPerType.Add(type, num);
                    return true;
                }
            }
        }

        #endregion

        #region Ordered

        private readonly IDictionary<short, MessageCounter> _lastOrderedMessageNumPerType;
        private readonly SortedDictionary<short, MessageQueue> _messageQueuePerType;

        internal List<Message> MessagesToInvoke(Message msg) {
            var messagesToInvoke = new List<Message>();

            lock (_lastOrderedMessageNumPerType) {
                lock (_messageQueuePerType) {
                    if (!_lastOrderedMessageNumPerType.ContainsKey(msg.Header.Type)) {
                        _lastOrderedMessageNumPerType.Add(msg.Header.Type, 0);
                    }
                    if (!_messageQueuePerType.ContainsKey(msg.Header.Type)) {
                        _messageQueuePerType.Add(msg.Header.Type, new MessageQueue());
                    }
                    if (msg.MessageId < _lastOrderedMessageNumPerType[msg.Header.Type]) {
                        return messagesToInvoke;
                    }
                    var currentTypeQueue = _messageQueuePerType[msg.Header.Type];
                    currentTypeQueue.Add(msg);
                    foreach (var element in currentTypeQueue) {
                        if (element.Key == _lastOrderedMessageNumPerType[msg.Header.Type]) {
                            messagesToInvoke.Add(element.Value);
                            _lastOrderedMessageNumPerType[msg.Header.Type]++;
                        }
                    }
                    foreach (var element in messagesToInvoke) {
                        currentTypeQueue.Remove(element);
                    }
                    return messagesToInvoke;
                }
            }
        }

        #endregion

        #region Unordered

        private readonly IDictionary<short, MessageCounter> _lastUnOrderedMessageNumPerType;

        #endregion

        internal MessageCounter GetMessageId(Message msg) {
            MessageCounter result;
            if (msg.Ordered) {
                lock (_lastOrderedMessageNumPerType) {
                    if (!_lastOrderedMessageNumPerType.ContainsKey(msg.Header.Type)) {
                        _lastOrderedMessageNumPerType.Add(msg.Header.Type, 0);
                    }
                    result = _lastOrderedMessageNumPerType[msg.Header.Type];
                    _lastOrderedMessageNumPerType[msg.Header.Type]++;
                }
            }
            else if (msg.Sequenced) {
                lock (_lastSequencedMessageNumPerType) {
                    if (!_lastSequencedMessageNumPerType.ContainsKey(msg.Header.Type)) {
                        _lastSequencedMessageNumPerType.Add(msg.Header.Type, 0);
                    }
                    result = _lastSequencedMessageNumPerType[msg.Header.Type];
                    _lastSequencedMessageNumPerType[msg.Header.Type]++;
                }
            }
            else {
                lock (_lastUnOrderedMessageNumPerType) {
                    if (!_lastUnOrderedMessageNumPerType.ContainsKey(msg.Header.Type)) {
                        _lastUnOrderedMessageNumPerType.Add(msg.Header.Type, 0);
                    }
                    result = _lastUnOrderedMessageNumPerType[msg.Header.Type];
                    _lastUnOrderedMessageNumPerType[msg.Header.Type]++;
                }
            }
            return result;
        }

        private readonly IDictionary<short, Action<Message>> _handlers;

        public void AddHandler(short type, Action<Message> callback) {
            lock (_handlers) {
                if (!_handlers.ContainsKey(type)) {
                    _handlers.Add(type, callback);
                }
            }
        }

        public void RemoveHandler(short type) {
            lock (_handlers) {
                if (_handlers.ContainsKey(type)) {
                    _handlers.Remove(type);
                }
            }
        }

        internal void InvokeIfBinded(Message msg) {
            lock (_handlers) {
                if (_handlers.ContainsKey(msg.Header.Type)) {
                    _handlers[msg.Header.Type].Invoke(msg);
                }
            }
        }
    }
}
