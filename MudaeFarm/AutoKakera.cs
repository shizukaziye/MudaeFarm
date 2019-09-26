using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

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

        public void Initialize() => _client.ReactionAdded += HandleReactionAsync;

        async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!(channel is SocketGuildChannel guildChannel))
                return;

            // channel must be enabled for claiming
            if (!_config.BotChannelIds.Contains(channel.Id))
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
            var state = _state.Get(guildChannel.Guild);

            if (state.KakeraReset == null || state.KakeraReset <= DateTime.Now)
                state = await _state.RefreshAsync(guildChannel.Guild);

            if (state.KakeraPower - state.KakeraPowerConsumption < 0)
                return;

            state.KakeraPower -= state.KakeraPowerConsumption;

            // retrieve message
            IUserMessage message;

            try
            {
                message = await channel.GetMessageAsync(reaction.MessageId) as IUserMessage;

                if (message == null)
                    return;
            }
            catch (Exception e)
            {
                Log.Warning($"Could not get message {reaction.MessageId}.", e);
                return;
            }

            // claim kakera
            await Task.Delay(_config.KakeraClaimDelay);

            await message.AddReactionAsync(reaction.Emote);
        }
    }
}