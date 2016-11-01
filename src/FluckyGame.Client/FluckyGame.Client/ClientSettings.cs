using System;

namespace FluckyGame.Client
{
    internal class ClientSettings
    {
        public Resolution resolution;
        public Server server;

        public struct Resolution
        {
            public int width;
            public int height;
            public bool fullscreen;
        }

        public struct Server
        {
            public string hostname;
            public int port;
        }
    }
}
