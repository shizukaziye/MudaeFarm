using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MudaeFarm
{
    public interface IMudaeClaimer : IHostedService { }

    public class MudaeClaimer : BackgroundService, IMudaeClaimer
    {
        readonly IDiscordClientService _discord;
        readonly IMudaeUserFilter _userFilter;
        readonly IMudaeClaimCharacterFilter _characterFilter;
        readonly IMudaeClaimEmojiFilter _claimEmojiFilter;
        readonly IOptionsMonitor<ClaimingOptions> _options;
        readonly IOptionsMonitor<BotChannelList> _channelList;
        readonly IMudaeCommandHandler _commandHandler;
        readonly IMudaeOutputParser _outputParser;
        readonly ILogger<MudaeClaimer> _logger;

        public MudaeClaimer(IDiscordClientService discord, IMudaeUserFilter userFilter, IMudaeClaimCharacterFilter characterFilter, IMudaeClaimEmojiFilter claimEmojiFilter, IOptionsMonitor<ClaimingOptions> options, IOptionsMonitor<BotChannelList> channelList, IMudaeCommandHandler commandHandler, IMudaeOutputParser outputParser, ILogger<MudaeClaimer> logger)
        {
            _discord          = discord;
            _userFilter       = userFilter;
            _characterFilter  = characterFilter;
            _claimEmojiFilter = claimEmojiFilter;
            _options          = options;
            _channelList      = channelList;
            _commandHandler   = commandHandler;
            _outputParser     = outputParser;
            _logger           = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var client = await _discord.GetClientAsync();

            client.MessageReceived += HandleMessageReceived;
            client.ReactionAdded   += HandleReactionAdded;

            try
            {
                await Task.Delay(-1, stoppingToken);
            }
            finally
            {
                client.MessageReceived -= HandleMessageReceived;
                client.ReactionAdded   -= HandleReactionAdded;
            }
        }

        readonly ConcurrentDictionary<ulong, ClaimState> _states = new ConcurrentDictionary<ulong, ClaimState>();

        sealed class ClaimState
        {
            public DateTime CooldownResetTime { get; set; }
        }

#pragma warning disable 1998
        async Task HandleMessageReceived(MessageReceivedEventArgs e)
#pragma warning restore 1998
        {
            var options = _options.CurrentValue;

            // enabled, message author is mudae, channel is configured, embed exists
            if (!options.Enabled || !(e.Message is IUserMessage message && _userFilter.IsMudae(message.Author)) || _channelList.CurrentValue.Items.All(x => x.Id != message.ChannelId) || message.Embeds.Count == 0)
                return;

            var stopwatch = Stopwatch.StartNew();

            var channel  = e.Client.GetChannel(message.ChannelId);
            var guild    = channel is IGuildChannel gc ? e.Client.GetGuild(gc.GuildId) : null;
            var logPlace = $"channel '{channel.Name}' ({channel.Id}, server '{guild?.Name}')";

            var embed = message.Embeds[0];

            // character must not belong to another user
            if (embed.Footer?.Text.StartsWith("belongs", StringComparison.OrdinalIgnoreCase) == true)
                return;

            // i forgot what this was about, but it existed in legacy MudaeFarm
            if (embed.Author != null)
                return;

            var description = embed.Description.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // ignore $im messages
            if (description.Any(l => l.StartsWith("claims:", StringComparison.OrdinalIgnoreCase) || l.StartsWith("likes:", StringComparison.OrdinalIgnoreCase)))
                return;

            var character = new CharacterInfo(embed.Title, description[0]);
            var wishedBy  = message.Content.StartsWith("wished by", StringComparison.OrdinalIgnoreCase) ? message.GetUserIds().ToArray() : null;

            // must be wished or included in a user wishlist
            if (!_characterFilter.IsWished(character, wishedBy))
            {
                _logger.LogInformation($"Ignoring character '{character}' in {logPlace} because not wished.");
                return;
            }

            // check cooldown
            var state = _states.GetOrAdd(channel.Id, new ClaimState());
            var now   = DateTime.Now;

            if (now < state.CooldownResetTime)
            {
                _logger.LogWarning($"Ignoring character '{character}' in {logPlace} because of cooldown. Cooldown finishes in {state.CooldownResetTime - now}.");
                return;
            }

            _logger.LogWarning($"Attempting to claim character '{character}' in {logPlace}...");

            // reactions are not attached when we receive this message
            _pendingClaims[message.Id] = new PendingClaim(logPlace, message, character, stopwatch);
        }

        readonly struct PendingClaim
        {
            public readonly string LogPlace;
            public readonly IUserMessage Message;
            public readonly CharacterInfo Character;
            public readonly Stopwatch Stopwatch;

            public PendingClaim(string logPlace, IUserMessage message, CharacterInfo character, Stopwatch stopwatch)
            {
                LogPlace  = logPlace;
                Message   = message;
                Character = character;
                Stopwatch = stopwatch;
            }
        }

        readonly ConcurrentDictionary<ulong, PendingClaim> _pendingClaims = new ConcurrentDictionary<ulong, PendingClaim>();

        async Task HandleReactionAdded(ReactionAddedEventArgs e)
        {
            var options = _options.CurrentValue;

            if (!options.Enabled || !_pendingClaims.TryGetValue(e.Message.Id, out var claim))
                return;

            var logPlace  = claim.LogPlace;
            var message   = claim.Message;
            var character = claim.Character;
            var stopwatch = claim.Stopwatch;

            if (!_claimEmojiFilter.IsClaimEmoji(e.Emoji) || !_pendingClaims.TryRemove(e.Message.Id, out claim))
                return;

            await Task.Delay(TimeSpan.FromSeconds(options.DelaySeconds));

            IUserMessage response;

            try
            {
                response = await _commandHandler.ReactAsync(message, e.Emoji);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not claim character '{character}' in {logPlace}.");
                return;
            }

            if (_outputParser.TryParseClaimSucceeded(response.Content, out var claimer, out _) && claimer.Equals(e.Client.CurrentUser.Name, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Claimed character '{character}' in {logPlace} in {stopwatch.Elapsed.TotalMilliseconds}ms.");
                return;
            }

            if (_outputParser.TryParseClaimFailed(response.Content, out var resetTime))
            {
                _states.GetOrAdd(message.ChannelId, new ClaimState()).CooldownResetTime = DateTime.Now + resetTime;

                _logger.LogWarning($"Could not claim character '{character}' in {logPlace} due to cooldown. Cooldown finishes in {resetTime}.");
                return;
            }

            _logger.LogWarning($"Probably claimed character '{character}' in {logPlace}, but result could not be determined. Not sending claim replies.");
        }
    }
}