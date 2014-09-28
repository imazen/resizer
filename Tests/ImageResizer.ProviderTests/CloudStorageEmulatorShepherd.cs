using System;
using System.Diagnostics;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

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
                string executable = "WAStorageEmulator.exe";
                string path = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator";

                ProcessStartInfo processStartInfo = new ProcessStartInfo() {
                    FileName = Path.Combine(path, executable),
                    Arguments = @"start",
                };

                using (Process process = Process.Start(processStartInfo)) {
                    process.WaitForExit();
                }
            }
        }
    }
}
