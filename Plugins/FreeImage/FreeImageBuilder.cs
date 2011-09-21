using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Encoding;
using ImageResizer.Configuration.Issues;
using FreeImageAPI;

namespace ImageResizer.Plugins.FreeImage {
    public class FreeImageBuilder :ImageBuilder, IPlugin, IIssueProvider {
         /// <summary>
        /// Creates a new FreeImageBuilder instance with no extensions.
        /// </summary>
        public FreeImageBuilder(IEncoderProvider encoderProvider): base(encoderProvider) {
        }

        /// <summary>
        /// Create a new instance of FreeImageBuilder using the specified extensions and encoder provider. Extension methods will be fired in the order they exist in the collection.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="encoderProvider"></param>
        public FreeImageBuilder(IEnumerable<BuilderExtension> extensions, IEncoderProvider encoderProvider)
            : base(extensions, encoderProvider) {
        }

        
        /// <summary>
        /// Creates another instance of the class using the specified extensions. Subclasses should override this and point to their own constructor.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        public virtual FreeImageBuilder Create(IEnumerable<BuilderExtension> extensions, IEncoderProvider writer) {
            return new FreeImageBuilder(extensions, writer);
        }
        /// <summary>
        /// Copies the instance along with extensions. Subclasses must override this.
        /// </summary>
        /// <returns></returns>
        public virtual FreeImageBuilder Copy() {
            return new FreeImageBuilder(this.exts, this._encoderProvider);
        }

        public IPlugin Install(Configuration.Config c) {
            c.UpgradeImageBuilder(new FreeImageBuilder(c.CurrentImageBuilder.EncoderProvider));
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            return false; // We can't uninstall this.
        }


        public override string Build(object source, object dest, ResizeSettings settings, bool disposeSource, bool addFileExtension) {
            return base.Build(source, dest, settings, disposeSource, addFileExtension);
        }

        protected virtual bool BuildUnmanaged(string source, string dest, ResizeSettings settings) {
            if (!FreeImageAPI.FreeImage.IsAvailable()) return false;

        }


        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (!FreeImageAPI.FreeImage.IsAvailable()) issues.Add(new Issue("The FreeImage library is not available! All FreeImage plugins will be disabled.", IssueSeverity.Error));
            return issues;
        }
    }
}
