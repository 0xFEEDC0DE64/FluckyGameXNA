using System;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Xna.Framework;
using FluckyGame.Library;

namespace FluckyGame.Server
{
    internal class Client
    {
        TcpClient tcpClient;
        NetworkStream networkStream;
        Thread thread;
        bool disconnecting;

        Entity playerEntity;

        public Client(TcpClient tcpClient)
        {
            Console.WriteLine("New connection from {0}", tcpClient.Client.RemoteEndPoint);
            this.tcpClient = tcpClient;
            this.networkStream = this.tcpClient.GetStream();

            lock (Program.entities)
            {
                foreach (var entity in Program.entities)
                    SendPacket(new Packet() {
                        {"type", "NEW" },
                        {"id", entity.Id },
                        {"model", entity.Model },
                        {"position", entity.Position },
                        {"rotation", entity.Rotation },
                        {"scalation", entity.Scalation }
                    });

                Program.entities.Add(playerEntity = new Entity(Guid.NewGuid().ToString(), "dude", Vector3.Zero, Vector3.Zero, new Vector3(1)));

                lock (Program.clients)
                    foreach (var client in Program.clients)
                        if(client != this)
                            client.SendPacket(new Packet() {
                                {"type", "NEW" },
                                {"id", playerEntity.Id },
                                {"model", playerEntity.Model },
                                {"position", playerEntity.Position },
                                {"rotation", playerEntity.Rotation },
                                {"scalation", playerEntity.Scalation }
                            });
            }

            thread = new Thread(ReceivePackets);
            thread.IsBackground = true;
            thread.Start();
        }

        private void ReceivePackets()
        {
            try
            {
                while (true)
                {
                    var packet = Packet.Receive(networkStream);

                    var type = (string)packet["type"];

                    if (type == "ADD")
                    {
                        lock (Program.entities)
                        {
                            var entity = new Entity(
                                Guid.NewGuid().ToString(),
                                (string)packet["model"],
                                (Vector3)packet["position"],
                                (Vector3)packet["rotation"],
                                (Vector3)packet["scalation"]
                            );

                            Program.entities.Add(entity);

                            lock (Program.clients)
                                foreach (var client in Program.clients)
                                    client.SendPacket(new Packet() {
                                        {"type", "NEW" },
                                        {"id", entity.Id },
                                        {"model", entity.Model },
                                        {"position", entity.Position },
                                        {"rotation", entity.Rotation },
                                        {"scalation", entity.Scalation }
                                    });
                        }
                    }
                    else if (type == "PLAYER")
                    {
                        playerEntity.Position = (Vector3)packet["position"];
                        playerEntity.Rotation = (Vector3)packet["rotation"];

                        lock (Program.clients)
                            foreach (var client in Program.clients)
                                if(client != this)
                                    client.SendPacket(new Packet() {
                                        {"type", "UPDATE" },
                                        {"id", playerEntity.Id },
                                        {"position", playerEntity.Position },
                                        {"rotation", playerEntity.Rotation }
                                    });
                    }
                    else
                        throw new Exception("Unknown packet type!");
                }
            }
            catch(Exception ex)
            { 
                if(!disconnecting)
                    Console.WriteLine(ex.Message);
                lock (Program.clients)
                    Program.clients.Remove(this);
                lock (Program.entities)
                    Program.entities.Remove(playerEntity);
                lock (Program.clients)
                    foreach (var client in Program.clients)
                        client.SendPacket(new Packet() {
                            { "type", "REMOVE" },
                            { "id", playerEntity.Id }
                        });
                networkStream.Dispose();
                tcpClient.Dispose();
                return;
            }
        }

        public void SendPacket(Packet packet)
        {
            if (disconnecting)
                return;
            try
            {
                //TODO: lock to avoid message mixing (one message sending while other)
                packet.Send(networkStream);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                disconnecting = true;
                thread.Abort();
            }
        }
    }
}
