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
        MemoryStream memoryStream;

        private static readonly Queue<IFormatter> formatters;

        static Packet()
        {
            formatters = new Queue<IFormatter>();
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
            IFormatter formatter;

            lock(formatters)
            {
                if (formatters.Count == 0)
                    formatter = new BinaryFormatter();
                else
                    formatter = formatters.Dequeue();
            }

            object obj;

            try
            {
                obj = formatter.Deserialize(stream);
            }
            finally
            {
                lock (formatters)
                    formatters.Enqueue(formatter);
            }

            var packet = obj as Packet;
            if (packet == null)
                throw new Exception("no packet received!");

            return packet;
        }

        public void ClearCache()
        {
            memoryStream = null;
        }

        public void Send(Stream stream)
        {
            IFormatter formatter;

            lock (formatters)
            {
                if (formatters.Count == 0)
                    formatter = new BinaryFormatter();
                else
                    formatter = formatters.Dequeue();
            }

            if (memoryStream == null)
            {
                memoryStream = new MemoryStream();

                try
                {
                    formatter.Serialize(memoryStream, this);
                }
                finally
                {
                    lock (formatters)
                        formatters.Enqueue(formatter);
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            memoryStream.CopyTo(stream);
        }
    }
}
