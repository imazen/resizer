using System;
using System.Threading;

namespace ImageResizer.Plugins.LicenseVerifier.Async {
    public abstract class BackgroundAction : IBackgroundAction {
        protected volatile bool cancel;
        object stateLock = new object();
        ActionState state = ActionState.None;
        public event EventHandler<ActionFinishedEventArgs> Finished;

        public void Begin() {
            if (state != ActionState.None)
                throw new InvalidOperationException("Begin should only be called once.");

            state = ActionState.InProgress;
            ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProcess));
        }

        void ThreadProcess(object state) {
            lock (stateLock) {
                try {
                    PerformBackgroundAction();
                    this.state = cancel ? ActionState.Cancelled : ActionState.Success;
                    ReportFinished();
                }
                catch (Exception e) {
                    this.state = ActionState.Error;
                    if (Finished != null)
                        Finished(this, new ActionFinishedEventArgs() { State = State, Exception = e });
                }
            }
        }

        protected abstract void PerformBackgroundAction();

        public void Cancel() {
            cancel = true;
        }

        public ActionState State {
            get { return state; }
        }

        private void ReportFinished() {
            if (Finished != null)
                Finished(this, new ActionFinishedEventArgs() { State = state });
        }
    }
}
