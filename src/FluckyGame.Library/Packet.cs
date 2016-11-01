using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace FluckyGame.Library
{
    [Serializable]
    public class Packet : Dictionary<string, object>
    {
        private static readonly IFormatter formatter;

        static Packet()
        {
            formatter = new BinaryFormatter();
        }

        public Packet() : base() { }

        public Packet(int capacity) : base(capacity) { }

        public Packet(IEqualityComparer<string> comparer) : base(comparer) { }

        public Packet(IDictionary<string, object> dictionary) : base(dictionary) { }

        public Packet(int capacity, IEqualityComparer<string> comparer) : base(capacity, comparer) { }

        public Packet(IDictionary<string, object> dictionary, IEqualityComparer<string> comparer) : base(dictionary, comparer) { }

        public Packet(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public static Packet Receive(Stream stream)
        {
            var obj = formatter.Deserialize(stream);

            var packet = obj as Packet;
            if (packet == null)
                throw new Exception("no packet received!");

            return packet;
        }

        public void Send(Stream stream)
        {
            formatter.Serialize(stream, this);
        }
    }
}
