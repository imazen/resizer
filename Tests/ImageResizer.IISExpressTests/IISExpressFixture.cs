// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using Xunit;

namespace ImageResizer.IISExpressTests {

    [CollectionDefinition("IISExpress")]
    public class IISExpressCollection : ICollectionFixture<IISExpressFixture> { }

    public class IISExpressFixture : IDisposable {
        private Process _iisExpressProcess;
        private string _tempSiteDir;

        public HttpClient Client { get; private set; }
        public string BaseUrl { get; private set; }

        public IISExpressFixture() {
            // Build the temp site
            var site = new SiteCreator().Create();
            site.WriteWebConfig(new WebConfigBuilder().Build());
            _tempSiteDir = site.dir;

            int port = GetAvailablePort();
            BaseUrl = "http://localhost:" + port;

            Client = new HttpClient();
            Client.BaseAddress = new Uri(BaseUrl);
            Client.Timeout = TimeSpan.FromSeconds(30);

            StartIISExpress(site.dir, port);
            WaitForReady();
        }

        private static int GetAvailablePort() {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private void StartIISExpress(string sitePath, int port) {
            // IIS Express is pre-installed on GitHub Actions windows-latest
            string iisExpressPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "IIS Express", "iisexpress.exe");

            if (!File.Exists(iisExpressPath)) {
                // Try x86 Program Files
                iisExpressPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "IIS Express", "iisexpress.exe");
            }

            if (!File.Exists(iisExpressPath))
                throw new FileNotFoundException("IIS Express not found. These tests require IIS Express to be installed.");

            var psi = new ProcessStartInfo {
                FileName = iisExpressPath,
                Arguments = string.Format("/path:\"{0}\" /port:{1} /systray:false", sitePath, port),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _iisExpressProcess = Process.Start(psi);

            // Log IIS Express output asynchronously for diagnostics
            _iisExpressProcess.OutputDataReceived += (s, e) => {
                if (e.Data != null) Debug.WriteLine("[IISExpress] " + e.Data);
            };
            _iisExpressProcess.ErrorDataReceived += (s, e) => {
                if (e.Data != null) Debug.WriteLine("[IISExpress ERR] " + e.Data);
            };
            _iisExpressProcess.BeginOutputReadLine();
            _iisExpressProcess.BeginErrorReadLine();
        }

        private void WaitForReady() {
            var sw = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(15);
            Exception lastException = null;

            while (sw.Elapsed < timeout) {
                if (_iisExpressProcess.HasExited)
                    throw new Exception("IIS Express exited prematurely with code " + _iisExpressProcess.ExitCode);

                try {
                    var response = Client.GetAsync("/").Result;
                    // Any response (even 403/404) means the server is up
                    return;
                } catch (Exception ex) {
                    lastException = ex;
                    Thread.Sleep(250);
                }
            }

            throw new TimeoutException(
                "IIS Express did not become ready within " + timeout.TotalSeconds + " seconds.",
                lastException);
        }

        public void Dispose() {
            Client?.Dispose();

            if (_iisExpressProcess != null && !_iisExpressProcess.HasExited) {
                try {
                    _iisExpressProcess.Kill();
                    _iisExpressProcess.WaitForExit(5000);
                } catch (Exception ex) {
                    Debug.WriteLine("Failed to kill IIS Express: " + ex.Message);
                }
                _iisExpressProcess.Dispose();
            }

            if (_tempSiteDir != null && Directory.Exists(_tempSiteDir)) {
                try {
                    Directory.Delete(_tempSiteDir, true);
                } catch (Exception ex) {
                    Debug.WriteLine("Failed to clean up temp site: " + ex.Message);
                }
            }
        }
    }
}
