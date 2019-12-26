using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class ConnectionStabilizer
    {
        readonly DiscordSocketClient _client;
        readonly ConfigManager _config;

        public ConnectionStabilizer(DiscordSocketClient client, ConfigManager config)
        {
            _client = client;
            _config = config;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            Log.Warning("Waiting for connection to stabilize...");
            Log.Color = Log.DebugColor;

            var measure = new MeasureContext();

            var channel = await _config.GetOrCreateChannelAsync("connection-test");

            var i = 0;

            Task handleLog(LogMessage m)
            {
                if (m.Message.Contains("Failed to resume"))
                    i = 0;

                return Task.CompletedTask;
            }

            _client.Log += handleLog;

            try
            {
                const int iterations = 4;

                for (; i < iterations; i++)
                {
                    try
                    {
                        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        using var cts     = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

                        await TestReadAsync(channel, cts.Token);
                        await TestEventAsync(channel, cts.Token);

                        if (i < iterations)
                        {
                            Log.Debug($"nearly there... {iterations - i}");

                            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        i = 0;

                        Log.Debug("patience...");
                    }
                }
            }
            catch
            {
                Log.Color = null;
                throw;
            }
            finally
            {
                _client.Log -= handleLog;

                Log.Color = null;

                await channel.DeleteAsync();
            }

            Log.Info($"Connected stabilized in {measure}.");
        }

        static async Task TestReadAsync(IMessageChannel channel, CancellationToken cancellationToken = default)
        {
            await foreach (var _ in channel.GetMessagesAsync(5).WithCancellation(cancellationToken))
                return;
        }

        readonly Random _random = new Random();

        async Task TestEventAsync(IMessageChannel channel, CancellationToken cancellationToken = default)
        {
            var completion = new TaskCompletionSource<object>();

            var message = null as string;

            Task handleMessage(SocketMessage m)
            {
                // ReSharper disable once AccessToModifiedClosure
                if (m.Content == message)
                    completion.TrySetResult(null);

                return Task.CompletedTask;
            }

            _client.MessageReceived += handleMessage;

            try
            {
                var msg = await channel.SendMessageAsync(message = _random.NextDouble().ToString(CultureInfo.InvariantCulture));

                try
                {
                    using (cancellationToken.Register(() => completion.TrySetCanceled()))
                        await completion.Task;
                }
                finally
                {
                    await msg.DeleteAsync();
                }
            }
            finally
            {
                _client.MessageReceived -= handleMessage;
            }
        }
    }
}