using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using ImageResizer.Util;
using System.Drawing;
using System.Diagnostics;
using System.Web;
using System.Web.Hosting;
using System.Threading;
using System.Globalization;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.SeamCarving {
    public class CairManager {
        public CairManager() {
        }

        private int _maxConcurrentExecutions = 0;
        /// <summary>
        /// The maximum number of CAIR.exe instances to allow at the same time. After this limit is reached, 
        /// requests will wait until requests will MaxConcurrentWaitingThreads is reached, at which point 
        /// requests will be denied so the thread pool doesn't become exhausted.
        /// Set this value to at least CPU cores * 2, as the proccess is also I/O bound. Set to 0 for no limit (default).
        /// </summary>
        public int MaxConcurrentExecutions {
            get { return _maxConcurrentExecutions; }
            set { _maxConcurrentExecutions = value; }
        }

        private int _maxConcurrentWaitingThreads = 0;
        /// <summary>
        /// The maximum number of waiting threads (for a CAIR.exe instance) to permit before denying requests. Set to 0 (default) to permit an endless number of threads to wait (although request timeout will still be effect).
        /// Set this value to at least 30 
        /// </summary>
        public int MaxConcurrentWaitingThreads {
            get { return _maxConcurrentWaitingThreads; }
            set { _maxConcurrentWaitingThreads = value; }
        }

        protected string cairPath = null;
        protected object cairLock = new object();
        public string GetCair(){
            if (cairPath != null) return cairPath;
            lock(cairLock){
                if (cairPath != null) return cairPath;
                
                //In ASP.NET, use ~/App_Data/cair, otherwise use a temp folder.
                string cairDir = HttpContext.Current == null ? Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) : HostingEnvironment.MapPath("~/App_Data/cair/");
                if (!Directory.Exists(cairDir)) Directory.CreateDirectory(cairDir);

                string dllPath = Path.Combine(cairDir, "pthreadVSE2.dll");

                using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("ImageResizer.Plugins.SeamCarving.pthreadVSE2.dll"))
                using (Stream output = File.Create(dllPath))
                {
                    input.CopyToStream(output);
                }

                string tempPath = Path.Combine(cairDir, "cair.exe");

                using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("ImageResizer.Plugins.SeamCarving.CAIR.exe"))
                using (Stream output = File.Create(tempPath)) {
                    input.CopyToStream(output);
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
        /// <summary>
        /// Number of executing CAIR.exe processes
        /// </summary>
        private int _concurrentExecutions = 0;
        /// <summary>
        /// Number of threads waiting for a CAIR.exe process.
        /// </summary>
        private int _concurrentWaitingThreads = 0;
        /// <summary>
        /// Used for efficient thread waiting
        /// </summary>
        private AutoResetEvent turnstile = new AutoResetEvent(true);

        public bool CairyIt(CairJob job) {

            //If we have too many threads waiting to run CAIR, just kill the request.
            if (_maxConcurrentWaitingThreads > 0 &&
                _concurrentWaitingThreads > _maxConcurrentWaitingThreads)
                throw new ImageProcessingException("Content-aware image processing failed - too many threads waiting. Try again later.");

            //If there are any threads waiting in line, or if the permitted number of CAIR.exe instances has been reached, get in line
            if (_concurrentWaitingThreads > 0 || (_maxConcurrentExecutions > 0 &&
                    _concurrentExecutions > _maxConcurrentExecutions)) {
                try {
                    Interlocked.Increment(ref _concurrentWaitingThreads);
                    //Wait for a free slot
                    while (_maxConcurrentExecutions > 0 &&
                        _concurrentExecutions > _maxConcurrentExecutions) {
                        turnstile.WaitOne(1000);
                    }
                } finally {
                    Interlocked.Decrement(ref _concurrentWaitingThreads);
                }
            }
            //Ok, there should be a free slot now.
            try {
                //Register, we have our own process slot now.
                Interlocked.Increment(ref _concurrentExecutions);

                //Make sure CAIR.exe exists. If not, recreate it
                if (!File.Exists(GetCair())) CairMissing();

                /*CAIR CLI Usage: cair -I <input_file>
                    Other options:
                      -O <output_file>
                          Default: Dependent on operation
                      -W <weight_file>
                          Bitmap with: Black- no weight
                                       Green- Protect weight
                                       Red- Remove weight
                          Default: Weights are all zero
                      -S <weight_scale>
                          Default: 100,000
                      -X <goal_x>
                          Default: Source image width
                      -Y <goal_y>
                          Default: Source image height
                      -R <expected_result>
                          CAIR: 0
                          Grayscale: 1
                          Edge: 2
                          Vertical Energy: 3
                          Horizontal Energy: 4
                          Removal: 5
                          CAIR_HD: 6
                          Default: CAIR
                      -C <convoluton_type>
                          Prewitt: 0
                          V1: 1
                          V_SQUARE: 2
                          Sobel: 3
                          Laplacian: 4
                          Default: Prewitt

                      -E <energy_type>
                          Backward: 0
                          Forward: 1
                          Default: Backward
                      -T <thread_count>
                          Default : CAIR_NUM_THREADS (4)
                    http://sourceforge.net/projects/c-a-i-r/*/

                string args = "";
                args += " -I \"" + job.SourcePath + "\"";
                args += " -O \"" + job.DestPath + "\"";
                if (job.WeightPath != null) args += " -W \"" + job.WeightPath + "\"";
                args += " -T " + job.Threads;
                args += " -R " + ((int)job.Output).ToString(NumberFormatInfo.InvariantInfo);
                args += " -C " + ((int)job.Filter).ToString(NumberFormatInfo.InvariantInfo);
                args += " -E " + ((int)job.Energy).ToString(NumberFormatInfo.InvariantInfo);
                args += " -X " + job.Size.Width;
                args += " -Y " + job.Size.Height;

                ProcessStartInfo info = new ProcessStartInfo(GetCair(), args);
                info.UseShellExecute = false;
                info.RedirectStandardError = true;
                info.RedirectStandardOutput = true;
                info.CreateNoWindow = true;

                using (Process p = Process.Start(info)) {
                    bool result = p.WaitForExit(job.Timeout);
                    if (!result) {
                        p.Kill(); //Kill the process if it times out.
                        throw new ImageProcessingException("Content-aware image processing failed due to timeout.");
                    }
                    string messages = p.StandardError.ReadToEnd() + p.StandardOutput.ReadToEnd();
                    if (p.ExitCode != 0)
                        throw new ImageProcessingException("Content-aware image processing failed: " + messages);
                    return result;
                }
            } finally {
                Interlocked.Decrement(ref _concurrentExecutions);
                turnstile.Set();
            }

        }



        
    }
}
