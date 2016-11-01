using System;
using System.Net.Sockets;
using System.Windows.Forms;

namespace FluckyGame.Client
{
    static class Program
    {
        static void Main(string[] args)
        {
            TcpClient tcpClient;

            try
            {
                tcpClient = new TcpClient("home.brunner.ninja", 8001);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Konnte keine Verbindung herstellen:\n\n" + ex.Message, "Konnte keine Verbindung herstellen!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (Game1 game = new Game1(tcpClient))
                game.Run();
        }
    }
}

