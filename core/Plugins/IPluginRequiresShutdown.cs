using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    public interface IPluginRequiresShutdown
    {
        Task StopAsync(CancellationToken cancellationToken);
    }
}