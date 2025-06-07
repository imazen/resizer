using System.Runtime.CompilerServices;

// Allow test assemblies to access internal types
[assembly: InternalsVisibleTo("ImageResizer.AllPlugins.Tests")]
[assembly: InternalsVisibleTo("ImageResizer.Plugins.Imageflow.Tests")]
[assembly: InternalsVisibleTo("ImageResizer.HttpTests")] 
[assembly: InternalsVisibleTo("ImageResizer.Core.Tests")]