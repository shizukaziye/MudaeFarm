using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MudaeFarm
{
    public static class Program
    {
        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                   .ConfigureLogging(l =>
                    {
                        if (args.Contains("--verbose"))
                            l.SetMinimumLevel(LogLevel.Trace);

                        l.AddFile("log_{Date}.txt");
                    })
                   .ConfigureServices((builder, services) =>
                    {
                        /*services.AddHostedService<Worker>()*/
                    });
    }
}