using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Xna.Framework;

namespace FluckyGame.Server
{
    internal static class Program
    {
        internal static List<Client> clients;

        private static Thread acceptThread;

        internal static Random random;
        internal static List<Entity> entities;

        private static void Main(string[] args)
        {
            Console.WriteLine("Initializing FluckyGame Server 1.0...");

            clients = new List<Client>();

            acceptThread = new Thread(AcceptConnections);
            acceptThread.IsBackground = true;
            acceptThread.Start();

            //Game Server Initialization
            random = new Random();
            entities = new List<Entity>();

            const int testSize = 200;
            const int testStep = 50;
            for (var z = -testSize; z <= testSize; z += testStep)
                for (var x = -testSize; x <= testSize; x += testStep)
                {
                    entities.Add(new BouncingEntity(
                        Guid.NewGuid().ToString(),
                        (new List<string>() { "box", "cone", "cylinder", "figure", "monkey", "sphere", "torus" }).OrderBy(o => random.Next()).First(),
                        new Vector3(x, 50, z),
                        Vector3.Zero,
                        new Vector3(5)));
                }

            const int minTime = 1000 / 60; //60 FPS

            int count = 0;
            var dateTime = DateTime.Now;
            while (true)
            {
                var started = DateTime.Now;

                var changedEntities = new List<Entity>();

                lock (entities)
                {
                    foreach (var entity in entities)
                        entity.Update();
                }

                count++;
                if((DateTime.Now - dateTime).TotalSeconds >= 1)
                {
                    Console.WriteLine("{0} Updates per Second ({1} clients)", count, clients.Count);
                    count = 0;
                    dateTime = DateTime.Now;
                }

                int elapsed = (int)(DateTime.Now - started).TotalMilliseconds;
                if (elapsed < minTime)
                    Thread.Sleep(minTime - elapsed);
            }
        }

        private static void AcceptConnections()
        {
            var listener = new TcpListener(IPAddress.Any, 8001);
            listener.Start();
            while (true)
            {
                var client = new Client(listener.AcceptTcpClient());
                lock(clients)
                    clients.Add(client);
            }
        }
    }
}
