using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

// async methods with no await
#pragma warning disable 1998

namespace MudaeFarm
{
    public class AutoClaimer
    {
        // https://emojipedia.org/hearts/
        static readonly IEmote[] _heartEmotes =
        {
            new Emoji("\uD83D\uDC98"), // cupid
            new Emoji("\uD83D\uDC9D"), // gift_heart
            new Emoji("\uD83D\uDC96"), // sparkling_heart
            new Emoji("\uD83D\uDC97"), // heartpulse
            new Emoji("\uD83D\uDC93"), // heartbeat
            new Emoji("\uD83D\uDC9E"), // revolving_hearts
            new Emoji("\uD83D\uDC95"), // two_hearts
            new Emoji("\uD83D\uDC9F"), // heart_decoration
            new Emoji("\u2764"),       // heart
            new Emoji("\uD83E\uDDE1"), // heart (orange)
            new Emoji("\uD83D\uDC9B"), // yellow_heart
            new Emoji("\uD83D\uDC9A"), // green_heart
            new Emoji("\uD83D\uDC99"), // blue_heart
            new Emoji("\uD83D\uDC9C"), // purple_heart
            new Emoji("\uD83E\uDD0E"), // heart (brown)
            new Emoji("\uD83D\uDDA4"), // heart (black)
            new Emoji("\uD83E\uDD0D"), // heart (white)
            new Emoji("\u2665")        // hearts
        };

        readonly DiscordSocketClient _client;
        readonly ConfigManager _config;
        readonly MudaeStateManager _state;

        public AutoClaimer(DiscordSocketClient client, ConfigManager config, MudaeStateManager state)
        {
            _client = client;
            _config = config;
            _state  = state;
        }

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

            // channel must be enabled for claiming
            if (!_config.BotChannelIds.Contains(message.Channel.Id))
                return;

            var guild = (message.Channel as SocketGuildChannel)?.Guild;

            // must be able to claim right now
            var state = _state.Get(guild);

            if (state.ClaimReset == null || state.ClaimReset <= DateTime.Now)
                state = await _state.RefreshAsync(guild);

            if (state.ClaimReset == null || DateTime.Now < state.ClaimReset)
                return;

            try
            {
                HandleMudaeMessage(guild, userMessage);
            }
            catch (Exception e)
            {
                Log.Warning($"Could not handle Mudae message {message.Id} '{message.Content}'.", e);
            }
        }

        void HandleMudaeMessage(IGuild guild, SocketUserMessage message)
        {
            if (!message.Embeds.Any())
                return;

            var embed = message.Embeds.First();

            // character must not belong to another user
            if (embed.Footer.HasValue && embed.Footer.Value.Text.StartsWith("Belongs to", StringComparison.OrdinalIgnoreCase))
                return;

            //
            if (!embed.Author.HasValue || embed.Author.Value.IconUrl != null)
                return;

            var character = embed.Author.Value.Name.Trim().ToLowerInvariant();
            var anime     = embed.Description.Split('\n')[0].Trim().ToLowerInvariant();

            // matching
            var matched = false;

            matched |= _config.WishedCharacterRegex?.IsMatch(character) ?? false;
            matched |= _config.WishedAnimeRegex?.IsMatch(anime) ?? false;

            if (matched)
            {
                Log.Warning($"{guild} {message.Channel}: Found character '{character}', trying marriage.");

                // reactions may not have been attached when we received this message
                // remember this message so we can attach an appropriate reaction later when we receive it
                _claimQueue[message.Id] = message;
            }
            else
            {
                Log.Info($"{guild} {message.Channel}: Ignored character '{character}', not wished.");
            }
        }

        static readonly ConcurrentDictionary<ulong, IUserMessage> _claimQueue = new ConcurrentDictionary<ulong, IUserMessage>();

        async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!_claimQueue.TryRemove(reaction.MessageId, out var message))
                return;

            // reaction must be a heart emote
            if (Array.IndexOf(_heartEmotes, reaction.Emote) == -1)
                return;

            // claim the roll
            await Task.Delay(_config.ClaimDelay);

            await message.AddReactionAsync(reaction.Emote);

            // refresh state
            if (channel is SocketGuildChannel guildChannel)
                await _state.RefreshAsync(guildChannel.Guild);
        }
    }
}
