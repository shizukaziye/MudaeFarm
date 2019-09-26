using System.Threading;
using System.Threading.Tasks;

namespace MudaeFarm
{
    public interface IModule
    {
        void Initialize();

        Task RunAsync(CancellationToken cancellationToken = default);
    }
}