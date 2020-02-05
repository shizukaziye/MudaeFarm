using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    /// <summary>
    /// Manages Discord authentication.
    /// </summary>
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
            var ready = new TaskCompletionSource<object>();

            Task handleReady()
            {
                ready.TrySetResult(null);
                return Task.CompletedTask;
            }

            _client.Ready += handleReady;

            try
            {
                await _client.LoginAsync(TokenType.User, _token.Token);
                await _client.StartAsync();

                await ready.Task;
            }
            catch (Exception e)
            {
                Log.Error("Error while authenticating to Discord.", e);

                // reset token if authentication failed
                _token.Reset();
            }
            finally
            {
                _client.Ready -= handleReady;
            }

            Log.Warning($"Logged in as: {_client.CurrentUser.Username} ({_client.CurrentUser.Id})");
        }
    }
}