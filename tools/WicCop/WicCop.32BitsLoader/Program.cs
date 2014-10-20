//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace Microsoft.Test.Tools.WicCop
{
    class Program
    {
        static void Main(string[] args)
        {
            ChannelServices.RegisterChannel(new IpcChannel(args[0]), false);

            Remote rt = new Remote();
            ObjRef r = RemotingServices.Marshal(rt, Remote.ObjectName);

            using (Process p = Process.GetProcessById(int.Parse(args[1], CultureInfo.InvariantCulture)))
            {
                p.EnableRaisingEvents = true;
                p.Exited += delegate(object sender, EventArgs e) { rt.Exit(); };

                rt.Wait();
            }
        }
    }
}
