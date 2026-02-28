// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
using System;
using System.Diagnostics;
using Azure;
using Azure.Storage.Blobs;

namespace ImageResizer.ProviderTests {
    public static class CloudStorageEmulatorShepherd {
        /// <summary>
        /// Ensure Azurite (or legacy Azure Storage Emulator) is running.
        /// Tries to connect; if that fails, attempts to start Azurite.
        /// </summary>
        public static void Start() {
            try {
                var client = new BlobServiceClient("UseDevelopmentStorage=true");
                var container = client.GetBlobContainerClient("image-resizer");
                container.CreateIfNotExists();
            }
            catch (RequestFailedException) {
                // Azurite not running — try to start it
                try {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo() {
                        FileName = "azurite",
                        Arguments = "--silent",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };

                    Process.Start(processStartInfo);

                    // Give Azurite a moment to start
                    System.Threading.Thread.Sleep(2000);
                }
                catch (Exception ex) {
                    throw new InvalidOperationException(
                        "Could not connect to Azurite and failed to start it. " +
                        "Install Azurite with: npm install -g azurite", ex);
                }
            }
        }
    }
}
