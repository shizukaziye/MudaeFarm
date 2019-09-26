using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class MudaeFarm : IDisposable
    {
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

            // retrieve auth token
            var token = new AuthTokenManager();

            // discord login
            await new DiscordLogin(_client, token).RunAsync();

            try
            {
                // configuration manager
                var config = new ConfigManager(_client);

                await config.InitializeAsync();

                // state management
                var state = new MudaeStateManager(_client, config);

                state.Initialize();

                // auto-claiming
                new AutoClaimer(_client, config).Initialize();

                // auto-rolling
                await new AutoRoller(_client, config, state).InitializeAsync();

                Log.Warning("Ready!!");

                // keep the bot running
                await Task.Delay(-1, cancellationToken);
            }
            finally
            {
                await _client.StopAsync();
            }
        }

        public void Dispose() => _client.Dispose();

        static Task HandleLogAsync(LogMessage message)
        {
            // these errors occur from using an old version of Discord.Net
            // they should not affect any functionality
            if (message.Message.Contains("Error handling Dispatch (TYPING_START)") ||
                message.Message.Contains("Unknown Dispatch (SESSIONS_REPLACE)") ||
                message.Message.Contains("Preemptive Rate limit"))
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