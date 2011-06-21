using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using ImageResizer.Util;
using System.Drawing;
using System.Diagnostics;

namespace ImageResizer.Plugins.SeamCarving {
    public class CairManager {
        public CairManager() {
        }

        protected string cairPath = null;
        protected object cairLock = new object();
        public string GetCair(){
            if (cairPath != null) return cairPath;
            lock(cairLock){
                if (cairPath != null) return cairPath;
                //Copy to temporary file 

                string dllPath = Path.Combine(Path.GetTempPath(), "pthreadVSE2.dll");

                using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("ImageResizer.Plugins.SeamCarving.pthreadVSE2.dll"))
                using (Stream output = File.Create(dllPath))
                {
                    StreamUtils.CopyTo(input, output);
                }

                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".exe");

                using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("ImageResizer.Plugins.SeamCarving.CAIR.exe"))
                using (Stream output = File.Create(tempPath)) {
                    StreamUtils.CopyTo(input, output);
                }
                //Save the path.
                cairPath = tempPath;
            }
            
            return cairPath;
        }
        public void CairMissing(){
            cairPath = null;
            GetCair();
        }

        public bool CairyIt(string sourcePath, string destPath, Size size, SeamCarving.SeamCarvingPlugin.FilterType type, int msToWait){

            //Make sure CAIR.exe still exists. If not, recreate it
            if (!File.Exists(GetCair())) CairMissing();

            string args = "";
            args += " -I \"" + sourcePath + "\"";
            args += " -O \"" + destPath + "\"";
            args += " -T 1";
            args += " -C " + type.ToString();
			args += " -X " + size.Width;
			args += " -Y " + size.Height;

            ProcessStartInfo info = new ProcessStartInfo(GetCair(), args);
            info.UseShellExecute = false;
            info.RedirectStandardError = true;
            info.RedirectStandardOutput= true;
            info.CreateNoWindow = true;
            
            using (Process p = Process.Start(info)) {
                bool result = p.WaitForExit(msToWait);
                if (!result) p.Kill();
                string messages = p.StandardError.ReadToEnd() + p.StandardOutput.ReadToEnd();
                if (p.ExitCode != 0)
                    throw new ImageProcessingException("Content-aware image processing failed: " + messages);
                return result;
            }

        }



		
    }
}
