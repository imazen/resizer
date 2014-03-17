using System;

namespace ImageResizer.Plugins.LicenseVerifier.Async {
    public class ProgressEventArgs : EventArgs {
        public string Message { get; set; }
    }
}
