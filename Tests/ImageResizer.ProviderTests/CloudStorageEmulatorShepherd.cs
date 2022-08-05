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
    public static class CloudStorageEmulatorShepherd
    {
        /// <summary>
        ///     Start the developer azure service if it is not started already.
        /// </summary>
        public static void Start()
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
                    process.WaitForExit();
                }
            }
        }
    }
}