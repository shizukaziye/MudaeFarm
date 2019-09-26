using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class DiscordLogin
    {
        readonly DiscordSocketClient _client;
        readonly AuthTokenManager _token;

        public DiscordLogin(DiscordSocketClient client, AuthTokenManager token)
        {
            _client = client;
            _token  = token;
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
                await _client.LoginAsync(TokenType.User, _token.Value);
                await _client.StartAsync();

                await completionSource.Task;
            }
            catch (Exception e)
            {
                Log.Error("Error while authenticating to Discord.", e);

                _token.Reset();
            }
            finally
            {
                _client.Ready -= handleReady;
            }

            Log.Warning($"Logged in as: {_client.CurrentUser.Username} ({_client.CurrentUser.Id})");

            foreach (var guild in _client.Guilds)
                Log.Debug($"Found server: {guild.Name} ({guild.Id})");
        }
    }
}