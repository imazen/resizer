using ImageResizer.Plugins.LicenseVerifier.Async;
using Should;
using System;
using System.Threading;

namespace ImageResizer.Plugins.LicenseVerifier.Tests.Unit.Async {
    public class BackgroundActionTests {

        public BackgroundActionTests() {
        }

        public void Should_only_be_allowed_to_call_begin_once() {
            var stubAction = new StubAction();
            try {
                stubAction.Begin();
                stubAction.Begin();
            }
            catch (InvalidOperationException e) {
                e.Message.ShouldEqual("Begin should only be called once.");
            }
        }

        public class SuccessfulBackgroundActionTests {
            StubAction stubAction;
            ActionFinishedEventArgs finishedEventArgs;

            public SuccessfulBackgroundActionTests() {
                stubAction = new StubAction();
            }

            public void Should_mark_state_to_in_progress() {
                stubAction.Begin();
                stubAction.State.ShouldEqual(ActionState.InProgress);
            }

            public void Should_successfully_complete_background_action() {
                stubAction.Finished += stubAction_Finished;
                stubAction.Begin();

                Helpers.WaitForCondition(() => { return stubAction.State == ActionState.Success; });

                stubAction.PerformBackgroundActionWasCalled.ShouldBeTrue();
                finishedEventArgs.State.ShouldEqual(ActionState.Success);
                stubAction.Finished -= stubAction_Finished;
            }

            private void stubAction_Finished(object sender, ActionFinishedEventArgs eventArgs) {
                finishedEventArgs = eventArgs;
            }
        }
        
        public class FailedBackgroundActionTests {
            StubExceptionAction stubExceptionAction;
            ActionFinishedEventArgs finishedEventArgs;

            public FailedBackgroundActionTests() {
                stubExceptionAction = new StubExceptionAction();
            }

            public void Should_report_error_and_exception() {
                stubExceptionAction.Finished += stubExceptionAction_Finished;
                stubExceptionAction.Begin();

                Helpers.WaitForCondition(() => { return stubExceptionAction.State == ActionState.Error; });

                finishedEventArgs.State.ShouldEqual(ActionState.Error);
                finishedEventArgs.Exception.ShouldNotBeNull();
                finishedEventArgs.Exception.ShouldBeType(typeof(InvalidOperationException));
                finishedEventArgs.Exception.Message.ShouldEqual("Exception message.");

                stubExceptionAction.Finished -= stubExceptionAction_Finished;
            }

            private void stubExceptionAction_Finished(object sender, ActionFinishedEventArgs eventArgs) {
                finishedEventArgs = eventArgs;
            }
        }

        public class CancelBackgroundActionTests {
            StubCancelAction stubCancelAction;
            ActionFinishedEventArgs finishedEventArgs;

            public CancelBackgroundActionTests() {
                stubCancelAction = new StubCancelAction();
            }

            public void Should_cancel_background_action() {
                stubCancelAction.Finished += stubAction_Finished;
                stubCancelAction.Begin();

                Thread.Sleep(500);

                stubCancelAction.Cancel();

                Helpers.WaitForCondition(() => { return stubCancelAction.State == ActionState.Cancelled; });

                finishedEventArgs.State.ShouldEqual(ActionState.Cancelled);

                stubCancelAction.Finished -= stubAction_Finished;
            }

            private void stubAction_Finished(object sender, ActionFinishedEventArgs eventArgs) {
                finishedEventArgs = eventArgs;
            }
        }
    }

    public class StubAction : BackgroundAction {
        public bool PerformBackgroundActionWasCalled { get; set; }
        
        public StubAction() {
        }

        protected override void PerformBackgroundAction() {
            PerformBackgroundActionWasCalled = true;
        }
    }

    public class StubExceptionAction : BackgroundAction {
        public StubExceptionAction() {
        }

        protected override void PerformBackgroundAction()
        {
            throw new InvalidOperationException("Exception message.");
        }
    }

    public class StubCancelAction : BackgroundAction {
        public StubCancelAction() {
        }

        protected override void PerformBackgroundAction() {
            Thread.Sleep(2000);
        }
    }
}
