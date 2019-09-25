using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class MudaeFarm : IDisposable
    {
        readonly Config _config = Config.Load();

        readonly DiscordSocketClient _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel         = LogSeverity.Info,
            MessageCacheSize = 0
        });

        public MudaeFarm()
        {
            _client.Log += HandleLogAsync;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // check version
            await new UpdateChecker().RunAsync();

            // ask user for their auth token
            new AuthToken(_config).EnsureInitialized();

            // discord login
            await new DiscordLogin(_config, _client).RunAsync();

            try
            {
                // command server
                await new CommandServer(_config, _client).EnsureCreatedAsync();

                // command handling
                new CommandListener(_config, _client).Initialize();

                // auto-claiming
                new AutoClaimer(_config, _client).Initialize();

                // auto-rolling
                var roller = new AutoRoller(_config, _client).RunAsync(cancellationToken);

                // keep the bot running
                await Task.WhenAll(roller, Task.Delay(-1, cancellationToken));
            }
            finally
            {
                await _client.StopAsync();
            }
        }

        public void Dispose()
        {
            _config.Save();
            _client.Dispose();
        }

        static Task HandleLogAsync(LogMessage message)
        {
            // these errors occur from using an old version of Discord.Net
            // they should not affect any functionality
            if (message.Message.Contains("Error handling Dispatch (TYPING_START)") ||
                message.Message.Contains("Unknown Dispatch (SESSIONS_REPLACE)"))
                return Task.CompletedTask;

            var text = message.Exception == null
                ? message.Message
                : $"{message.Message}: {message.Exception}";

            switch (message.Severity)
            {
                case LogSeverity.Debug:
                    Log.Debug(text);
                    break;
                case LogSeverity.Verbose:
                    Log.Debug(text);
                    break;
                case LogSeverity.Info:
                    Log.Info(text);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(text);
                    break;
                case LogSeverity.Error:
                    Log.Error(text);
                    break;
                case LogSeverity.Critical:
                    Log.Error(text);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}