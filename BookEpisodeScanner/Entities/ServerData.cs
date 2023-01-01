using System;
using System.Collections.Generic;
using System.Text;

namespace BookEpisodeScanner.Entities
{
    public class ServerData
    {
        public ServerStatus ServerStatus { get; set; }
        public string RequiredAppVersion { get; set; }
    }
    public class ServerStatus
    {
        public string AccessTime { get; set; }
        public string ResultCode { get; set; }
        public string Token { get; set; }
    }
}
