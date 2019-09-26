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
                    var state = Get(guild.Id);

                    // enforce refresh every 12 hours
                    var updateTime = DateTime.Now.AddHours(12);

                    // refresh at the earliest reset time
                    if (!state.CanClaim)
                        Min(ref updateTime, state.ClaimReset);

                    if (state.RollsLeft == 0)
                        Min(ref updateTime, state.RollsReset);

                    if (!state.CanKakera)
                        Min(ref updateTime, state.KakeraReset);

                    if (!state.CanKakeraDaily)
                        Min(ref updateTime, state.KakeraDailyReset);

                    // can we refresh?
                    if (now <= updateTime && !state.ForceNextRefresh)
                        continue;

                    // select a bot channel to send command in
                    var channel = guild.TextChannels.FirstOrDefault(c => _config.BotChannelIds.Contains(c.Id));

                    if (channel == null)
                        continue;

                    // send command
                    await channel.SendMessageAsync(_config.StateUpdateCommand);

                    state.ForceNextRefresh = false;
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }

        static void Min(ref DateTime a, DateTime? b)
        {
            if (b != null)
                a = a < b.Value ? a : b.Value;
        }

        // async with no await
#pragma warning disable 1998

        async Task HandleMessage(SocketMessage message)
        {
            if (!(message.Channel is IGuildChannel guildChannel))
                return;

            if (!MudaeInfo.IsMudae(message.Author))
                return;

            if (!_config.BotChannelIds.Contains(message.Channel.Id))
                return;

            if (!TimersUpParser.TryParse(_client, message, out var state))
                return;

            _states[guildChannel.GuildId] = state;

            Log.Debug($"Guild '{guildChannel.Guild}' state updated: {JsonConvert.SerializeObject(state)}");
        }

        public MudaeState Get(ulong guildId) => _states.TryGetValue(guildId, out var state)
            ? state
            : _states[guildId] = new MudaeState
            {
                ForceNextRefresh = true
            };
    }
}