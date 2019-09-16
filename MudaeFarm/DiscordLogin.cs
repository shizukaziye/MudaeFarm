using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class DiscordLogin
    {
        readonly Config _config;
        readonly DiscordSocketClient _client;

        public DiscordLogin(Config config, DiscordSocketClient client)
        {
            _config = config;
            _client = client;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var completionSource = new TaskCompletionSource<object>();

            Task handleConnect()
            {
                completionSource.SetResult(null);
                return Task.CompletedTask;
            }

            _client.Connected += handleConnect;

            try
            {
                await _client.LoginAsync(TokenType.User, _config.AuthToken);
                await _client.StartAsync();

                using (cancellationToken.Register(completionSource.SetCanceled))
                    await completionSource.Task;
            }
            catch (Exception e)
            {
                Log.Error("Error while authenticating to Discord.", e);

                _config.AuthToken = null;
                _config.Save();

                Log.Info("User token has been erased due to an error while authenticating to Discord.");

                throw new DummyRestartException();
            }
            finally
            {
                _client.Connected -= handleConnect;
            }

            Log.Warning($"Logged in as {_client.CurrentUser.Username} ({_client.CurrentUser.Id}).");
        }
    }
}
