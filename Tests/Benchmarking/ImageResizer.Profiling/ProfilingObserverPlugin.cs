using ImageResizer;
using ImageResizer.Plugins;
using ImageResizer.Resizing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bench
{


    public class ProfilingObserverPlugin : BuilderExtension, IPlugin
    {

        private const string Key = "ProfilingObserverPlugin_IProfiler";
        private IProfiler p
        {
            get
            {
                return CallContext.LogicalGetData(Key) as IProfiler;
            }
        }

        protected override RequestedAction BuildJob(ImageJob job)
        {
            if (job.Profiler != null && job.Profiler.Active){
                CallContext.LogicalSetData(Key, job.Profiler);
            }
            start("job [isolate]");
            return base.BuildJob(job);
        }

        protected override void PreLoadImage(ref object source, ref string path, ref bool disposeSource, ref ResizeSettings settings)
        {
            start("loadimage");
            start("decode");
            base.PreLoadImage(ref source, ref path, ref disposeSource, ref settings);
        }

        protected override RequestedAction PostDecodeStream(ref Bitmap img, ResizeSettings settings)
        {
            stop("decode");
            stop("loadimage");
            return base.PostDecodeStream(ref img, settings);
        }
        protected override void PreAcquireStream(ref object dest, ResizeSettings settings)
        {
            stop("decode",false);
            stop("loadimage", false);
            start("bit");
        }

        protected override RequestedAction OnProcess(ImageState s)
        {
            start("process");
            return base.OnProcess(s);
        }

        protected override RequestedAction PrepareSourceBitmap(ImageState s)
        {
            start("prepsource");
            return base.PrepareSourceBitmap(s);
        }
        protected override RequestedAction Layout(ImageState s)
        {
            stop("prepsource");
            start("layout");
            return base.Layout(s);
        }
        protected override RequestedAction EndLayout(ImageState s)
        {
            stop("layout");
            start("prepdest");
            return base.EndLayout(s);
        }

        protected override RequestedAction Render(ImageState s)
        {
            stop("prepdest");
            start("render");
            return base.Render(s);
        }

        protected override RequestedAction PostFlushChanges(ImageState s)
        {
            stop("render");
            return RequestedAction.None;
        }
        protected override RequestedAction EndProcess(ImageState s)
        {
            stop("process");
            return base.EndProcess(s);
        }

        protected override RequestedAction BeforeEncode(ImageJob job)
        {
            stop("bit");
            start("encode");
            return base.BeforeEncode(job);
        }

        protected override RequestedAction EndBuildJob(ImageJob job)
        {
            stop("bit", false);
            stop("encode", false);
            stop("job");

            if (job.Profiler != null && job.Profiler.Active)
            {
                CallContext.LogicalSetData(Key, null);
            }

            return base.EndBuildJob(job);
        }






        private bool IsRunning(string name)
        {
            return p != null && p.IsRunning(name);
        }
        private void start(string name, bool allowRecursion = false)
        {
            if (p != null && p.Active) p.Start(name, allowRecursion);
        }
        private void stop(string name, bool assertRunning = true, bool stopChildren = false)
        {
            if (p != null && p.Active) p.Stop(name, assertRunning, stopChildren);
        }
  
        public IPlugin Install(ImageResizer.Configuration.Config c)
        {
            if (c.Plugins.Has<ProfilingObserverPlugin>()) return null;
            c.Plugins.AllPlugins.AddFirst(this);
            c.Plugins.ImageBuilderExtensions.AddFirst(this);
            return this;
        }

        public bool Uninstall(ImageResizer.Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }

}
