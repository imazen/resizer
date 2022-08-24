using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace ImageResizer.Plugins.PdfiumRenderer.Web
{
    public class Global : System.Web.HttpApplication
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        protected void Application_Start(object sender, EventArgs e)
        {
            string path = Path.Combine(HttpRuntime.AppDomainAppPath, "bin");

            if (IntPtr.Size == 4)
                path = Path.Combine(path, "x86");
            else
                path = Path.Combine(path, "x64");

            path = Path.Combine(path, "pdfium.dll");

            if (File.Exists(path))
                LoadLibrary(path);
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}