using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace MudaeFarm
{
    public class MudaeStateManager
    {
        // guildId - state
        readonly ConcurrentDictionary<ulong, MudaeState> _states = new ConcurrentDictionary<ulong, MudaeState>();

        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        readonly DiscordSocketClient _client;
        readonly ConfigManager _config;

        public MudaeStateManager(DiscordSocketClient client, ConfigManager config)
        {
            _client = client;
            _config = config;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            _client.MessageReceived += HandleMessage;

            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                foreach (var guild in _client.Guilds)
                {
                    var state = Get(guild.Id);

                    var updateTime = DateTime.MaxValue;

                    if (!state.CanClaim)
                        Min(ref updateTime, state.ClaimReset);

                    if (state.RollsLeft == 0)
                        Min(ref updateTime, state.RollsReset);

                    if (state.KakeraPower - state.KakeraConsumption < 0)
                        Min(ref updateTime, state.KakeraReset);

                    if (!state.CanKakeraDailyReset)
                        Min(ref updateTime, state.KakeraDailyReset);

                    if (now >= updateTime || state.ForceNextRefresh)
                    {
                        var refreshed = await RefreshAsync(guild);

                        if (refreshed)
                            state.ForceNextRefresh = false;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        static void Min(ref DateTime a, DateTime? b)
        {
            if (b != null)
                a = a < b.Value ? a : b.Value;
        }

        // channelId - completionSource
        readonly ConcurrentDictionary<ulong, TaskCompletionSource<MudaeState>> _stateSources
            = new ConcurrentDictionary<ulong, TaskCompletionSource<MudaeState>>();

        Task HandleMessage(SocketMessage message)
        {
            // ReSharper disable once RemoveRedundantBraces
            if (MudaeInfo.IsMudae(message.Author) && _stateSources.ContainsKey(message.Channel.Id))
            {
                if (TimersUpParser.TryParse(_client, message, out var state) && _stateSources.TryRemove(message.Channel.Id, out var completionSource))
                    completionSource.TrySetResult(state);
            }

            return Task.CompletedTask;
        }

        public MudaeState Get(ulong guildId)
            => _states.TryGetValue(guildId, out var state)
                ? state
                : _states[guildId] = new MudaeState
                {
                    ForceNextRefresh = true
                };

        async Task<bool> RefreshAsync(SocketGuild guild)
        {
            await _semaphore.WaitAsync();
            try
            {
                MudaeState newState;

                // select a bot channel to send command in
                var channel = guild.TextChannels.FirstOrDefault(c => _config.BotChannelIds.Contains(c.Id));

                if (channel == null)
                    return false;

                var completionSource = new TaskCompletionSource<MudaeState>();

                _stateSources[channel.Id] = completionSource;

                try
                {
                    // send command
                    await channel.SendMessageAsync(_config.StateUpdateCommand);

                    // wait with timeout
                    using (var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    using (cancellationSource.Token.Register(completionSource.SetCanceled))
                        newState = await completionSource.Task;
                }
                catch (TaskCanceledException)
                {
                    Log.Warning("Expected Mudae `$tu` response but received nothing.");

                    return false;
                }
                finally
                {
                    _stateSources.TryRemove(channel.Id, out _);
                }

                _states[guild.Id] = newState;

                Log.Debug($"Guild '{guild}' state updated: {JsonConvert.SerializeObject(newState)}");

                return true;
            }
            catch (Exception e)
            {
                Log.Warning($"Could not refresh state for guild '{guild}'.", e);

                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
