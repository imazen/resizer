using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Reflection;
using System.Diagnostics;

namespace LibCassini.Client {
    public class ClientRequest:IDisposable {

        WebClient wc;
        Server s;
        string url;
        public ClientRequest(Server server, string url) {
            wc = new CustomWebClient();
            s = server;
            wc.BaseAddress = s.RootUrl;
            this.url = url;
        }

        public WebClient Data { get { return wc; } }

        /// <summary>
        /// Executes the request and returns the response. Disposes of the object.
        /// </summary>
        /// <returns></returns>
        public ClientResponse Execute(){
            WebResponse response;
            try{
                string fullUrl = s.RootUrl.TrimEnd('/') + '/' + url.TrimStart('/');
                Debug.WriteLine("Requesting " + fullUrl);
                wc.OpenRead(fullUrl);

                //Access the private WebResponse value through reflection
                FieldInfo fieldInfo =typeof(WebClient).GetField("m_WebResponse", BindingFlags.Instance | BindingFlags.NonPublic);
                response = (WebResponse)fieldInfo.GetValue(wc);
                Debug.Assert(response != null);

            } catch(WebException wex){
                response = wex.Response;
                Debug.Assert(response != null);
            }
            this.Dispose();
            return new ClientResponse((HttpWebResponse)response);
        }


        public void Dispose() {
            wc.Dispose();
            wc = null;
        }
    }
}
