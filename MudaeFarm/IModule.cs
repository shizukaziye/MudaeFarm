using System.Threading;
using System.Threading.Tasks;

namespace MudaeFarm
{
    public interface IModule
    {
        Task RunAsync(CancellationToken cancellationToken = default);
    }
}