using System;
using System.Diagnostics;
using System.Threading;
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

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.LoginAsync(TokenType.User, _token.Value);
                await _client.StartAsync();
            }
            catch (Exception e)
            {
                Log.Error("Error while authenticating to Discord.", e);

                // reset token if authentication failed
                _token.Reset();
            }

            await StabilizeConnectionAsync(cancellationToken);

            Log.Warning($"Logged in as: {_client.CurrentUser.Username} ({_client.CurrentUser.Id})");
        }

        /// <remarks>
        /// Client may disconnect multiple times when trying to connect to Discord. Wait for that to stop.
        /// </remarks>
        async Task StabilizeConnectionAsync(CancellationToken cancellationToken = default)
        {
            var waitTime   = TimeSpan.FromSeconds(10);
            var completion = new TaskCompletionSource<object>();

            Log.Error($"Waiting for connection to stabilize. This will take around {waitTime.Seconds} seconds.");
            Log.Color = Log.DebugColor;

            var countdown = new Countdown(waitTime, completion);
            countdown.Reset();

            Task handle1()
            {
                countdown.Reset();
                return Task.CompletedTask;
            }

            Task handle2(Exception _)
            {
                countdown.Reset();
                return Task.CompletedTask;
            }

            Task handle3(SocketGuild guild)
            {
                countdown.Reset();
                return Task.CompletedTask;
            }

            _client.Connected        += handle1;
            _client.Disconnected     += handle2;
            _client.LoggedIn         += handle1;
            _client.LoggedOut        += handle1;
            _client.GuildAvailable   += handle3;
            _client.GuildUnavailable += handle3;
            _client.Ready            += handle1;

            try
            {
                using (cancellationToken.Register(() => completion.TrySetCanceled()))
                    await completion.Task;
            }
            catch
            {
                Log.Color = null;
                throw;
            }
            finally
            {
                _client.Connected        -= handle1;
                _client.Disconnected     -= handle2;
                _client.LoggedIn         -= handle1;
                _client.LoggedOut        -= handle1;
                _client.GuildAvailable   -= handle3;
                _client.GuildUnavailable -= handle3;
                _client.Ready            -= handle1;

                Log.Color = null;

                countdown.Reset(false);
            }
        }

        class Countdown
        {
            readonly TimeSpan _length;
            readonly TaskCompletionSource<object> _completion;

            public Countdown(TimeSpan length, TaskCompletionSource<object> completion)
            {
                _length     = length;
                _completion = completion;

                Reset();
            }

            CancellationTokenSource _cts;

            public void Reset(bool restart = true)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;

                if (!restart)
                    return;

                _cts = new CancellationTokenSource();

                Task.Run(async () =>
                {
                    try
                    {
                        var watch = Stopwatch.StartNew();

                        while (true)
                        {
                            const double interval = 2;
                            await Task.Delay(TimeSpan.FromSeconds(interval), _cts.Token);

                            var elapsed = watch.Elapsed;

                            if (elapsed >= _length)
                                break;

                            Log.Debug("patience...");
                        }

                        _completion.TrySetResult(null);
                    }
                    catch
                    {
                        // canceled
                    }
                });
            }
        }
    }
}