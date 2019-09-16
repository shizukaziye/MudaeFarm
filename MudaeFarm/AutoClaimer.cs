using System;
using System.Collections.Generic;
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
        readonly Config _config;
        readonly DiscordSocketClient _client;

        public AutoClaimer(Config config, DiscordSocketClient client)
        {
            _config = config;
            _client = client;
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

            try
            {
                HandleMudaeMessage(userMessage);
            }
            catch (Exception e)
            {
                Log.Warning($"Could not handle Mudae message {message.Id} '{message.Content}'.", e);
            }
        }

        void HandleMudaeMessage(SocketUserMessage message)
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

            var name  = embed.Author.Value.Name.Trim().ToLowerInvariant();
            var anime = embed.Description.Split('\n')[0].Trim().ToLowerInvariant();

            if (_config.WishlistCharacters.Contains(name) || _config.WishlistAnime.Contains(anime))
            {
                Log.Warning($"Found character '{name}', trying marriage.");

                // reactions may not have been attached when we received this message
                // remember this message so we can attach an appropriate reaction later when we receive it
                lock (_claimQueue)
                    _claimQueue.Add(message.Id, message);
            }
            else
            {
                Log.Info($"Ignored character '{name}', not wished.");
            }
        }

        static readonly Dictionary<ulong, IUserMessage> _claimQueue = new Dictionary<ulong, IUserMessage>();

        async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage message;

            lock (_claimQueue)
            {
                if (!_claimQueue.TryGetValue(reaction.MessageId, out message))
                    return;

                _claimQueue.Remove(reaction.MessageId);
            }

            // claim delay
            var delay = _config.ClaimDelay;

            if (delay > 0)
                await Task.Delay(TimeSpan.FromSeconds(delay));

            await message.AddReactionAsync(reaction.Emote);
        }
    }
}
