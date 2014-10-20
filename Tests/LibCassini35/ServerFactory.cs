using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibCassini {
    public class ServerFactory {
        public ServerFactory(){
        }

        public Server CreateAndStart(string physicalPath, string virtualPath, int retryCount = 2){
            //49152 through 65535 are valid port numbers
            //Try 2 port numbers, then fail if we still can't start the server.
            for (int i = 0; i < retryCount; i++){
                int port = new Random().Next(49152, 65535);
                try {
                    Server server = new Server(port, virtualPath, physicalPath);
                    server.Start();
                    return server;
                }catch(Exception e){
                    if (i + 1 == retryCount) throw e;//On the last iteration, rethrow the exception.
                }
            }
            return null;
        }
  
    }
}
