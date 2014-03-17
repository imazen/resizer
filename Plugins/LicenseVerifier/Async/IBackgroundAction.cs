using System;

namespace ImageResizer.Plugins.LicenseVerifier.Async {
    public interface IBackgroundAction {
        void Begin();
        event EventHandler<ActionFinishedEventArgs> Finished;
        void Cancel();
        ActionState State { get; }
    }
}
