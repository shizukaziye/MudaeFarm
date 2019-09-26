using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

// async methods with no await
#pragma warning disable 1998

namespace MudaeFarm
{
    public class AutoKakera
    {
        readonly DiscordSocketClient _client;
        readonly ConfigManager _config;
        readonly MudaeStateManager _state;

        public AutoKakera(DiscordSocketClient client, ConfigManager config, MudaeStateManager state)
        {
            _client = client;
            _config = config;
            _state  = state;
        }

        static readonly Dictionary<string, KakeraType> _kakeraMap = new Dictionary<string, KakeraType>
        {
            { "kakeraP", KakeraType.Purple },
            { "kakera", KakeraType.Blue },
            { "kakeraT", KakeraType.Teal },
            { "kakeraG", KakeraType.Green },
            { "kakeraY", KakeraType.Yellow },
            { "kakeraO", KakeraType.Orange },
            { "KakeraR", KakeraType.Red },
            { "kakeraW", KakeraType.Rainbow }
        };

        public void Initialize()
        {
            _client.MessageReceived += HandleMessageAsync;
            _client.ReactionAdded   += HandleReactionAsync;
        }

        async Task HandleMessageAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage))
                return;

            if (!MudaeInfo.IsMudae(message.Author))
                return;

            if (!message.Embeds.Any())
                return;

            // channel must be enabled for claiming
            if (!_config.BotChannelIds.Contains(message.Channel.Id))
                return;

            // remember message
            _messageCache[message.Id] = userMessage;

            foreach (var m in _messageCache.Values)
            {
                if ((DateTimeOffset.Now - m.Timestamp).TotalMinutes >= 5)
                    _messageCache.TryRemove(m.Id, out _);
            }
        }

        readonly ConcurrentDictionary<ulong, IUserMessage> _messageCache = new ConcurrentDictionary<ulong, IUserMessage>();

        async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!_config.ClaimEnabled)
                return;

            if (!(channel is IGuildChannel guildChannel))
                return;

            // channel must be enabled for claiming
            if (!_config.BotChannelIds.Contains(channel.Id))
                return;

            // retrieve message
            if (!_messageCache.TryGetValue(reaction.MessageId, out var message))
                return;

            // reactor must be mudae
            var user = reaction.User.IsSpecified ? reaction.User.Value : _client.GetUser(reaction.UserId);

            if (user == null || !MudaeInfo.IsMudae(user))
                return;

            // reaction must be kakera
            if (!_kakeraMap.TryGetValue(reaction.Emote.Name, out var kakera))
                return;

            // kakera must be configured to be claimed
            if (!_config.KakeraTargets.Contains(kakera))
                return;

            // must have enough kakera power to claim this kakera
            var state = _state.Get(guildChannel.GuildId);

            if (state.KakeraPower - state.KakeraConsumption < 0)
                return;

            // claim kakera
            await Task.Delay(_config.KakeraClaimDelay);

            await message.AddReactionAsync(reaction.Emote);

            // update state
            state.KakeraPower -= state.KakeraConsumption;
        }
    }
}