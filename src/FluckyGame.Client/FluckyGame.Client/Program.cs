using System;
using System.Net.Sockets;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;

namespace FluckyGame.Client
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            //TODO: error handling !!!
            var settings = JsonConvert.DeserializeObject<ClientSettings>(File.ReadAllText("client.json"));

            TcpClient tcpClient;

            try
            {
                tcpClient = new TcpClient(settings.server.hostname, settings.server.port);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Konnte keine Verbindung herstellen:\n\n" + ex.Message, "Konnte keine Verbindung herstellen!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (Game1 game = new Game1(tcpClient, settings))
                game.Run();
        }
    }
}

