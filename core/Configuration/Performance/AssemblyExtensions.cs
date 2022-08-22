using System.Linq;
using System.Reflection;
using ImageResizer.Util;
using Imazen.Common.Instrumentation.Support;

namespace ImageResizer.Configuration.Performance
{
    internal static class AssemblyExtensions
    {
       
        public static string GetShortCommit(this Assembly a) =>
            a.GetFirstAttribute<CommitAttribute>()?.Value.Take(8).IntoString();
        
        public static string GetEditionCode(this Assembly a) =>
            a.GetFirstAttribute<EditionAttribute>()?.Value;

    }
}