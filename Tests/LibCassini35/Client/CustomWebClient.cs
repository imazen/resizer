using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace LibCassini.Client {
    public class CustomWebClient:WebClient {
        public CustomWebClient() : base() { }
        protected override WebRequest GetWebRequest(Uri address) {
            WebRequest r =  base.GetWebRequest(address);
            r.Timeout = 5000;
            return r;
        }
    }
}
