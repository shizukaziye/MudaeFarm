using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class AutoRoller : IModule
    {
        readonly DiscordSocketClient _client;
        readonly ConfigManager _config;
        readonly MudaeStateManager _state;

        public AutoRoller(DiscordSocketClient client, ConfigManager config, MudaeStateManager state)
        {
            _client = client;
            _config = config;
            _state  = state;
        }

        // guildId - cancellationTokenSource
        readonly ConcurrentDictionary<ulong, CancellationTokenSource> _cancellations
            = new ConcurrentDictionary<ulong, CancellationTokenSource>();

        public void Initialize()
        {
            _client.JoinedGuild      += guild => ReloadWorkers();
            _client.LeftGuild        += guild => ReloadWorkers();
            _client.GuildAvailable   += guild => ReloadWorkers();
            _client.GuildUnavailable += guild => ReloadWorkers();
        }

        public Task RunAsync(CancellationToken cancellationToken = default) => ReloadWorkers();

        Task ReloadWorkers()
        {
            var guildIds = new HashSet<ulong>();

            // start worker for rolling in guilds, on separate threads
            foreach (var guild in _client.Guilds)
            {
                guildIds.Add(guild.Id);

                if (_cancellations.ContainsKey(guild.Id))
                    continue;

                var source = _cancellations[guild.Id] = new CancellationTokenSource();
                var token  = source.Token;

                _ = Task.Run(
                    async () =>
                    {
                        while (true)
                        {
                            try
                            {
                                await Task.WhenAll(
                                    RunRollAsync(guild, token),
                                    RunDailyKakeraAsync(guild, token));

                                return;
                            }
                            catch (TaskCanceledException)
                            {
                                return;
                            }
                            catch (Exception e)
                            {
                                Log.Warning($"Error while rolling in guild '{guild}'.", e);

                                await Task.Delay(TimeSpan.FromSeconds(1), token);
                            }
                        }
                    },
                    token);
            }

            // stop workers for unavailable guilds
            foreach (var id in _cancellations.Keys)
            {
                if (!guildIds.Remove(id) && _cancellations.TryRemove(id, out var source))
                {
                    source.Cancel();
                    source.Dispose();
                }
            }

            return Task.CompletedTask;
        }

        async Task RunRollAsync(SocketGuild guild, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var state = _state.Get(guild.Id);

                var interval = _config.RollIntervalOverride;

                if (interval == null)
                {
                    if (!_config.RollEnabled || state.RollsLeft <= 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                        continue;
                    }

                    interval = new TimeSpan((state.RollsReset - DateTime.Now).Ticks / state.RollsLeft);
                }

                foreach (var channel in guild.TextChannels)
                {
                    if (!_config.BotChannelIds.Contains(channel.Id))
                        continue;

                    using (channel.EnterTypingState())
                    {
                        await Task.Delay(_config.RollTypingDelay, cancellationToken);

                        try
                        {
                            await channel.SendMessageAsync(_config.RollCommand);

                            --state.RollsLeft;

                            Log.Debug($"{channel.Guild} #{channel}: Rolled '{_config.RollCommand}'.");
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"{channel.Guild} #{channel}: Could not send roll command '{_config.RollCommand}'.", e);
                        }
                    }

                    break;
                }

                if (interval < TimeSpan.FromSeconds(5))
                    interval = TimeSpan.FromSeconds(5);

                await Task.Delay(interval.Value, cancellationToken);
            }
        }

        async Task RunDailyKakeraAsync(SocketGuild guild, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var state = _state.Get(guild.Id);

                if (!_config.DailyKakeraEnabled || !state.CanKakeraDaily)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    continue;
                }

                foreach (var channel in guild.TextChannels)
                {
                    if (!_config.BotChannelIds.Contains(channel.Id))
                        continue;

                    using (channel.EnterTypingState())
                    {
                        await Task.Delay(_config.RollTypingDelay, cancellationToken);

                        try
                        {
                            await channel.SendMessageAsync(_config.DailyKakeraCommand);

                            state.CanKakeraDaily = false;

                            Log.Debug($"{channel.Guild} #{channel}: Sent daily kakera command '{_config.DailyKakeraCommand}'.");

                            if (_config.DailyKakeraStateUpdate && !string.IsNullOrWhiteSpace(_config.StateUpdateCommand))
                            {
                                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                                await channel.SendMessageAsync(_config.StateUpdateCommand);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"{channel.Guild} #{channel}: Could not send daily kakera command '{_config.DailyKakeraCommand}'.", e);
                        }
                    }

                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}