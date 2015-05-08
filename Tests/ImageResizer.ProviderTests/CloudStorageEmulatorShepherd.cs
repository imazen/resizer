// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Diagnostics;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System.Linq;

namespace ImageResizer.ProviderTests {
    public static class CloudStorageEmulatorShepherd {
        /// <summary>
        /// Start the developer azure service if it is not started already.
        /// </summary>
        public static void Start() {
            try {
                CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("image-resizer");
                container.CreateIfNotExists(
                    new BlobRequestOptions() {
                        RetryPolicy = new NoRetry(),
                        ServerTimeout = new TimeSpan(0, 0, 0, 1)
                    });
            }
            catch (Microsoft.WindowsAzure.Storage.StorageException) {
                
                string path = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator";
                var filenames = new string[] { "AzureStorageEmulator.exe", "WAStorageEmulator.exe" }.Select(name => Path.Combine(path, name));

                string filename = filenames.First(n => File.Exists(n));

                ProcessStartInfo processStartInfo = new ProcessStartInfo() {
                    FileName = filename,
                    Arguments = @"start",
                };

                using (Process process = Process.Start(processStartInfo)) {
                    process.WaitForExit();
                }
            }
        }
    }
}
