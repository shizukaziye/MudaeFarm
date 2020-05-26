using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Octokit;

namespace MudaeFarm
{
    public static class Program
    {
        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                   .ConfigureLogging(logger =>
                    {
                        if (args.Contains("-v") || args.Contains("--verbose"))
                            logger.SetMinimumLevel(LogLevel.Trace);

                        logger.AddFile("log_{Date}.txt");
                    })
                   .ConfigureAppConfiguration(config => config.Add(new DiscordConfigurationSource()))
                   .ConfigureServices((builder, services) =>
                    {
                        // configuration
                        services.AddSingleton((IConfigurationRoot) builder.Configuration)
                                .Configure<GeneralOptions>(builder.Configuration.GetSection("General"))
                                .Configure<ClaimingOptions>(builder.Configuration.GetSection("Claiming"))
                                .Configure<RollingOptions>(builder.Configuration.GetSection("Rolling"))
                                .Configure<CharacterWishlist>(builder.Configuration.GetSection("Character wishlist"))
                                .Configure<AnimeWishlist>(builder.Configuration.GetSection("Anime wishlist"))
                                .Configure<BotChannelList>(builder.Configuration.GetSection("Bot channels"));

                        // discord client
                        services.AddSingleton<IDiscordClientService, DiscordClientService>()
                                .AddTransient<IHostedService>(s => s.GetService<IDiscordClientService>());

                        services.AddSingleton<ICredentialManager, CredentialManager>();

                        // state manager
                        //services.AddSingleton<ITimersUpParser>();

                        // auto updater
                        services.AddHostedService<Updater>()
                                .AddSingleton<HttpClient>()
                                .AddSingleton(s => new GitHubClient(new ProductHeaderValue("MudaeFarm")));
                    });
    }
}