using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerClassLibrary
{
    public class Session
    {
        public Guid sessionid { get; set; }
        public Guid serverid { get; set; }

        public Session(Guid sessionid, Guid serverid)
        {
            this.sessionid = sessionid;
            this.serverid = serverid;
        }
    }
}
