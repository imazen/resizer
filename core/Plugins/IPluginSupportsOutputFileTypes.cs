using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    public interface IPluginSupportsOutputFileTypes
    {
        ImageFileType GuessOutputFileTypeIfSupported(Instructions commands, string virtualPath);
    }
}      