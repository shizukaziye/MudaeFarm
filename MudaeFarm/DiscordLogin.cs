using System;
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

        public async Task RunAsync()
        {
            var completionSource = new TaskCompletionSource<object>();

            Task handleReady()
            {
                completionSource.SetResult(null);
                return Task.CompletedTask;
            }

            _client.Ready += handleReady;

            try
            {
                await _client.LoginAsync(TokenType.User, _config.AuthToken);
                await _client.StartAsync();

                await _client.SetStatusAsync(_config.UserStatus);

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
                _client.Ready -= handleReady;
            }

            Log.Warning($"Logged in as: {_client.CurrentUser.Username} ({_client.CurrentUser.Id})");

            foreach (var guild in _client.Guilds)
                Log.Info($"Found server: {guild.Name} ({guild.Id})");
        }
    }
}