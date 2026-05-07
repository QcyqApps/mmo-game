using System;

namespace MmoGame.Backend
{
    [Serializable]
    public class NakamaConfig
    {
        public string scheme = "http";
        public string host = "127.0.0.1";
        public int port = 7350;
        public string serverKey = "defaultkey";
    }
}
