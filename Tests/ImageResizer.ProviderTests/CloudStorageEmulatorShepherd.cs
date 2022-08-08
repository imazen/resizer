// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace ImageResizer.ProviderTests
{
    internal sealed class CloudStorageEmulatorShepherd
    {
        private static Process cloudStorageEmulatorProcess;
        
        private static CloudStorageEmulatorShepherd keepAlive = new CloudStorageEmulatorShepherd(true);
        private readonly bool disposeShared = false;

        internal CloudStorageEmulatorShepherd(bool disposeShared = false)
        {
            this.disposeShared = disposeShared;
        }
        ~CloudStorageEmulatorShepherd()
        {
            if (!disposeShared) return;
            cloudStorageEmulatorProcess?.Kill();
            cloudStorageEmulatorProcess?.Dispose();
            cloudStorageEmulatorProcess = null;
        }
  
        /// <summary>
        ///     Start the developer azure service if it is not started already.
        /// </summary>
        public void Start()
        {
            try
            {
                var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("image-resizer");
                container.CreateIfNotExists(
                    new BlobRequestOptions()
                    {
                        RetryPolicy = new NoRetry(),
                        ServerTimeout = new TimeSpan(0, 0, 0, 1)
                    });
            }
            catch (StorageException)
            {
                var azuritePath = new string[]
                {
                    @"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\Extensions\Microsoft\Azure Storage Emulator\azurite.exe",
                    @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\Microsoft\Azure Storage Emulator\azurite.exe"
                }.FirstOrDefault(File.Exists);


                if (azuritePath != null)
                {
                    var storageDir = Path.Combine(Path.GetTempPath(), "azurite-resizer");
                    if (!Directory.Exists(storageDir)) Directory.CreateDirectory(storageDir);
                    var processStartInfo = new ProcessStartInfo()
                    {
                        FileName = azuritePath,
                        Arguments = $" --silent --location \"{storageDir}\" --debug \"{storageDir}\\debug.log\"",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false
                    };

                    cloudStorageEmulatorProcess = Process.Start(processStartInfo);
                    cloudStorageEmulatorProcess?.WaitForExit(100);
                }
                else
                {

                    var path = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator";
                    var filenames =
                        new string[] { "AzureStorageEmulator.exe", "WAStorageEmulator.exe" }.Select(name =>
                            Path.Combine(path, name));

                    var filename = filenames.First(n => File.Exists(n));

                    var processStartInfo = new ProcessStartInfo()
                    {
                        FileName = filename,
                        Arguments = @"start"
                    };

                    using (var process = Process.Start(processStartInfo))
                    {
                        process?.WaitForExit();
                    }
                }
            }
        }
    }
}