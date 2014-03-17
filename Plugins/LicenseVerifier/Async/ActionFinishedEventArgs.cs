using System;

namespace ImageResizer.Plugins.LicenseVerifier.Async {
    public class ActionFinishedEventArgs : EventArgs {
        public ActionState State { get; set; }
        public Exception Exception { get; set; }
    }
}
