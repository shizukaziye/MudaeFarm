using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class AutoRoller
    {
        readonly DiscordSocketClient _client;
        readonly ConfigManager _config;

        public AutoRoller(DiscordSocketClient client, ConfigManager config)
        {
            _client = client;
            _config = config;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var interval = 60; // todo: with kakera update

                if (interval <= 0)
                {
                    // rolling disabled
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }

                await SendRollsAsync();

                await Task.Delay(TimeSpan.FromMinutes(interval), cancellationToken);
            }
        }

        async Task SendRollsAsync()
        {
            foreach (var channelId in _config.RollChannelIds)
            {
                if (_client.GetChannel(channelId) is ITextChannel channel)
                    try
                    {
                        await channel.SendMessageAsync(_config.RollCommand);
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"Could not send roll to channel '#{channel.Name}' in server '{channel.Guild.Name}'.", e);
                    }

                // don't spam
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}