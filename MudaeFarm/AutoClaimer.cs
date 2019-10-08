using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

// async methods with no await
#pragma warning disable 1998

namespace MudaeFarm
{
    public class AutoClaimer : IModule
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

        Task IModule.RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        async Task HandleMessageAsync(SocketMessage message)
        {
            if (!_config.ClaimEnabled)
                return;

            if (!(message is SocketUserMessage userMessage))
                return;

            if (!MudaeInfo.IsMudae(message.Author))
                return;

            // channel must be enabled for claiming
            if (!_config.BotChannelIds.Contains(message.Channel.Id))
                return;

            try
            {
                await HandleMudaeMessageAsync(userMessage);
            }
            catch (Exception e)
            {
                Log.Warning($"Could not handle Mudae message {message.Id} '{message.Content}'.", e);
            }
        }

        static readonly Regex _imFooterRegex = new Regex(@"^\s*\d+\s*\/\s*\d+\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        async Task HandleMudaeMessageAsync(SocketUserMessage message)
        {
            if (!message.Embeds.Any())
                return;

            var guild = ((IGuildChannel) message.Channel).Guild;
            var embed = message.Embeds.First();

            if (embed.Footer.HasValue)
            {
                // character must not belong to another user
                if (embed.Footer.Value.Text.StartsWith("Belongs to", StringComparison.OrdinalIgnoreCase))
                    return;

                // message must not be $im
                if (_imFooterRegex.IsMatch(embed.Footer.Value.Text))
                    return;
            }

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
                var state = _state.Get(guild.Id);

                // ensure we can claim right now
                if (!state.CanClaim && DateTime.Now < state.ClaimReset)
                {
                    Log.Warning($"{guild} #{message.Channel}: Found character '{character}' but cannot claim it due to cooldown.");
                    return;
                }

                Log.Warning($"{guild} #{message.Channel}: Found character '{character}', trying claim.");

                // reactions are not attached when we received this message
                // remember this message so we can attach an appropriate reaction later when we receive it
                _claimQueue[message.Id] = new ClaimQueueItem
                {
                    Message   = message,
                    Character = new CharacterInfo(character, anime),
                    Measure   = new MeasureContext()
                };
            }
            else
            {
                Log.Info($"{guild} #{message.Channel}: Ignored character '{character}', not wished.");
            }
        }

        static readonly ConcurrentDictionary<ulong, ClaimQueueItem> _claimQueue = new ConcurrentDictionary<ulong, ClaimQueueItem>();

        struct ClaimQueueItem
        {
            public IUserMessage Message;
            public CharacterInfo Character;
            public MeasureContext Measure;
        }

        async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!_claimQueue.TryGetValue(reaction.MessageId, out var x))
                return;

            var (message, character, measure) = (x.Message, x.Character, x.Measure);

            // reaction must be a heart emote
            if (Array.IndexOf(_heartEmotes, reaction.Emote) == -1)
                return;

            // claim the roll
            await Task.Delay(_config.ClaimDelay);

            try
            {
                await message.AddReactionAsync(reaction.Emote);

                Log.Warning($"Attempted claim on character '{character.Name}' in {measure}.");
            }
            catch (Exception e)
            {
                Log.Warning($"Could not react with heart emote on character '{character.Name}'.", e);
                return;
            }

            // update state
            _state.Get(((IGuildChannel) message.Channel).GuildId).CanClaim = false;

            // automated reply
            if (_config.ClaimReplies.Count != 0)
            {
                var random = new Random();
                var reply  = _config.ClaimReplies[random.Next(_config.ClaimReplies.Count)];

                if (!string.IsNullOrWhiteSpace(reply))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500 + random.NextDouble() * 1000));

                    using (channel.EnterTypingState())
                    {
                        // type for the length of the reply
                        await Task.Delay(TimeSpan.FromMilliseconds(reply.Length * 100));

                        await channel.SendMessageAsync(reply);
                    }
                }
            }
        }
    }
}