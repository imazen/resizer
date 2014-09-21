using System;
using System.Diagnostics;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ImageResizer.ProviderTests {
    public class CloudStorageEmulatorShepherd {
        public void Start() {
            try {
                CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("test");
                container.CreateIfNotExists(
                    new BlobRequestOptions() {
                        RetryPolicy = new Microsoft.WindowsAzure.Storage.RetryPolicies.NoRetry(),
                        ServerTimeout = new TimeSpan(0, 0, 0, 1)
                    });
            }
            catch (TimeoutException) {
                ProcessStartInfo processStartInfo = new ProcessStartInfo() {
                    FileName = Path.Combine(
                        @"C:\Program Files\Microsoft SDKs\Windows Azure\Emulator",
                        "csrun.exe"),
                    Arguments = @"/devstore",
                };

                using (Process process = Process.Start(processStartInfo)) {
                    process.WaitForExit();
                }
            }
        }
    }
}
