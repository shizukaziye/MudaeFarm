using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace MudaeFarm
{
    /// <summary>
    /// Responsible for periodically sending "$tu" to update Mudae state.
    /// </summary>
    public class MudaeStateManager
    {
        // guildId - state
        readonly ConcurrentDictionary<ulong, MudaeState> _states = new ConcurrentDictionary<ulong, MudaeState>();

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
                    if (!_config.Enabled)
                        continue;

                    var state = Get(guild.Id);

                    // if state_update_command is disabled, assume...
                    if (string.IsNullOrWhiteSpace(_config.StateUpdateCommand))
                    {
                        // we can always claim rolls or Kakera at any time
                        state.CanClaim    = true;
                        state.KakeraPower = double.MaxValue;

                        continue;
                    }

                    // enforce refresh every 12 hours
                    var updateTime = now.AddHours(12);

                    // refresh at the earliest reset time
                    if (!state.CanClaim)
                        Min(ref updateTime, state.ClaimReset);

                    if (state.RollsLeft == 0)
                        Min(ref updateTime, state.RollsReset);

                    if (!state.CanKakera)
                        Min(ref updateTime, state.KakeraReset);

                    // should we refresh?
                    if (now < updateTime && !state.ForceNextRefresh)
                        continue;

                    // don't spam refreshes
                    /*if (now < state.LastRefresh.AddMinutes(10))
                        continue;*/

                    // select a bot channel to send command in
                    var channel = guild.TextChannels.FirstOrDefault(c => _config.BotChannelIds.Contains(c.Id));

                    if (channel == null)
                        continue;

                    try
                    {
                        // send command
                        await channel.SendMessageAsync(_config.StateUpdateCommand);

                        Log.Debug($"Sent state update command '{_config.StateUpdateCommand}' in channel #{channel}.");

                        state.LastRefresh = now;

                        // force load
                        if (state.ForceNextRefresh)
                            await foreach (var messages in channel.GetMessagesAsync(5).WithCancellation(cancellationToken))
                            foreach (var message in messages)
                            {
                                if (await HandleMessageInternal(message))
                                    break;
                            }
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"Could not send state update command '{_config.StateUpdateCommand}' in channel #{channel}: {JsonConvert.SerializeObject(state)}", e);
                    }

                    state.ForceNextRefresh = false;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        static void Min(ref DateTime a, DateTime? b)
        {
            if (b != null)
                a = a < b.Value ? a : b.Value;
        }

        // async with no await
#pragma warning disable 1998

        Task HandleMessage(SocketMessage message) => HandleMessageInternal(message);

        async Task<bool> HandleMessageInternal(IMessage message)
        {
            if (!(message.Channel is IGuildChannel guildChannel))
                return false;

            if (!MudaeInfo.IsMudae(message.Author))
                return false;

            if (!_config.BotChannelIds.Contains(message.Channel.Id))
                return false;

            if (!TimersUpParser.TryParse(_client, message, out var state))
                return false;

            _states[guildChannel.GuildId] = state;

            Log.Debug($"Guild '{guildChannel.Guild}' state updated: {JsonConvert.SerializeObject(state)}");

            return true;
        }

        public MudaeState Get(ulong guildId) => _states.TryGetValue(guildId, out var state)
            ? state
            : _states[guildId] = new MudaeState
            {
                ForceNextRefresh = true
            };
    }
}