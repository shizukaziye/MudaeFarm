using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class AutoRoller
    {
        readonly Config _config;
        readonly DiscordSocketClient _client;

        public AutoRoller(Config config, DiscordSocketClient client)
        {
            _config = config;
            _client = client;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var interval = _config.RollInterval;

                if (interval <= 0)
                {
                    // rolling disabled
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }

                await SendRollsAsync(cancellationToken);

                await Task.Delay(TimeSpan.FromMinutes(interval), cancellationToken);
            }
        }

        async Task SendRollsAsync(CancellationToken cancellationToken = default)
        {
            var channels = _config.RollChannels.Lock(x => x.ToArray());

            foreach (var channelId in channels)
            {
                if (_client.GetChannel(channelId) is ITextChannel channel)
                    try
                    {
                        await channel.SendMessageAsync("$" + _config.RollCommand, options: new RequestOptions { CancelToken = cancellationToken });
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"Could not send roll to channel '#{channel.Name}' in server '{channel.Guild.Name}'.", e);
                    }

                // don't spam the api
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}