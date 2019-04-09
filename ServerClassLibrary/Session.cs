using System;
using System.Net;

namespace ServerClassLibrary
{
    public class Session
    {
        public IPAddress IP { get; set; }
        public Guid serverid { get; set; }

        public Session(IPAddress IP, Guid serverid)
        {
            this.IP = IP;
            this.serverid = serverid;
        }
    }
}
