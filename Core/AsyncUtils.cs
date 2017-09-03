using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer
{
    public abstract class AsyncHttpHandlerBase : IHttpAsyncHandler
    {
        public abstract Task ProcessRequestAsync(HttpContext context);
        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            Task task = ProcessRequestAsync(context);
            if (task == null)
            {
                return null;
            }

            var retVal = new AsyncUtils.TaskWrapperAsyncResult(task, extraData);

            if (cb != null)
            {
                // The callback needs the same argument that the Begin method returns, which is our special wrapper, not the original Task.
                task.ContinueWith(_ => cb(retVal));
            }

            return retVal;
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            // The End* method doesn't actually perform any actual work, but we do need to maintain two invariants:
            // 1. Make sure the underlying Task actually *is* complete.
            // 2. If the Task encountered an exception, observe it here.
            // (The Wait method handles both of those.)
            var castResult = (AsyncUtils.TaskWrapperAsyncResult)result;
            castResult.Task.Wait();
        }


        public virtual bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            AsyncUtils.RunSync(() => ProcessRequestAsync(context));
        }
    }

}
