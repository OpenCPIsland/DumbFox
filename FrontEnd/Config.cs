using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontEnd
{
    public class AppConfig
    {
        public DatabaseConfig Database { get; set; } = new();
        public ServersConfig Servers { get; set; } = new();
    }

    public class DatabaseConfig
    {
        public string ConnectionString { get; set; } = "";
    }

    public class ServersConfig
    {
        public WebApiConfig WebApi { get; set; } = new();
        public PublicApiConfig PublicApi { get; set; } = new();
        public TcpConfig Tcp { get; set; } = new();
        public WebSocketConfig WebSocket { get; set; } = new();
        public bool IsIntimate { get; set; }
        public string hostname { get; set; } = "127.0.0.1";
        public string WSHostname { get; set; } = "127.0.0.1";
        public List<int> AlertCommandUserIds { get; set; } = [];
    }

    public class WebApiConfig
    {
        public bool Enabled { get; set; }
        public int HttpPort { get; set; }
        public int HttpsPort { get; set; }
    }

    public class PublicApiConfig
    {
        public bool Enabled { get; set; }
        public int JgcPort { get; set; }
        public int MixPort { get; set; }
        public int GamePort { get; set; }
    }

    public class TcpConfig
    {
        public bool Enabled { get; set; }
        public int Port { get; set; }
    }

    public class WebSocketConfig
    {
        public bool Enabled { get; set; }
        public int WsPort { get; set; }
        public int WssPort { get; set; }
    }
}
