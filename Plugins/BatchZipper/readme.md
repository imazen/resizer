Tags: plugin
Edition: elite
Tagline: Asynchronously generated .zip archives of files and resized images. Great for providing customized downloads to customers.
Aliases: /plugins/batchzipper


# BatchZipper plugin

Provides batch resizing and zip file creation, with asynchronous support. Great for generating customized user downloads.

This plugin isn't actually a plugin - in fact, it wraps the ImageResizer instead of the other way around.

## Installation

Either run `Install-Package ImageResizer.Plugins.BatchZipper` in the NuGet package manager, or:

1. Add `ImageResizer.Plugins.BatchZipper.dll` to your project


## Usage

Reference `Image`

Create a job specification with the BatchResizeSettings class:

   var s = new BatchResizeSettings(string destinationFile, Guid jobId, IList<BatchResizeItem> files);

Start it with this:

   ImageResizer.Plugins.BatchZipper.BatchResizeManager.BeginBatchResize(s)

You can attach event handlers to s.ItemEvent and s.JobEvent to monitor progress and provide user notification when the process is complete.

Please refer to the excellent XML documentation for further details - it is very comprehensive.


## Example

    public partial class BatchResizeAndZip : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            Response.ContentType = "text/plain";
            Guid job = Guid.NewGuid();
            BatchResizeSettings s = new BatchResizeSettings(MapPath("~/Results/" + job.ToString() + ".zip"), job, new List<BatchResizeItem>());

            string[] sourceImages = System.IO.Directory.GetFiles(Path.GetFullPath(MapPath("~/").TrimEnd('\\') + "\\..\\Images"), "*");
            foreach (string img in sourceImages) {
                s.files.Add(new BatchResizeItem(img, null, "?width=100"));
            }
            s.ItemEvent += new ItemCallback(s_ItemEvent);
            s.JobEvent += new JobCallback(s_JobEvent);

            //Executes on a thread pool thread
            BatchResizeManager.BeginBatchResize(s);

            ///Executes synchronously. Use  BatchResizeManager.BeginBatchResize(s) for async execution.
            //new BatchResizeWorker(s).Work();
        }

        void s_JobEvent(JobEventArgs e) {
            //If you throw an exception here, you will kill the asp.net worker process and it will have to restart. Don't.
            try {
                if (HttpContext.Current == null) return; //When using async mode, there is no session, no request, nothing. You can send e-mails and hit the database using web.config settings.

                Response.Write("\n\n" + e.ToString());
                //This is normally where you would e-mail the user about the result of the job. Job stats and individual item results are available in the args.
            } catch (Exception ex) {
                //Log the exception somehow, but don't throw another exception.
            }
        }

        void s_ItemEvent(ItemEventArgs e) {
            //If you throw an exception here, the job will fail. It's probably better to set e.Cancel.

            if (HttpContext.Current == null) return; //When using async mode, there is no session, no request, nothing. You can send e-mails and hit the database using web.config settings.

            Response.Write("\n\n" + e.ToString());
            //This is normally where you would update the database on the job progress. You can also use this to cancel the job by setting e.Cancel= true;
            double percentComplete = (e.Stats.FailedItems + e.Stats.SuccessfulItems) / e.Stats.RequestedItems * 100;
            Response.Write("\n" + Math.Round(percentComplete) + "% complete");
            //Note, however, on a live server, that opening a file for writing may take much longer than reading, thus
            //even 90% of elapsed time could occur before the first item is resized. This is very much an I/O bound process, and 
            //calculating a percentage complete is nearly worthless on a live server.
        }
    }