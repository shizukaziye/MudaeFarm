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

            Task handleReactionAdded(ReactionAddedEventArgs args)
            {
                // this needs to run in background because it waiting for Mudae output blocks message receive
                var _ = Task.Run(() => HandleReactionAdded(args), stoppingToken);
                return Task.CompletedTask;
            }

            client.MessageReceived += HandleMessageReceived;
            client.ReactionAdded   += handleReactionAdded;

            try
            {
                await Task.Delay(-1, stoppingToken);
            }
            finally
            {
                client.MessageReceived -= HandleMessageReceived;
                client.ReactionAdded   -= handleReactionAdded;
            }
        }

        readonly ConcurrentDictionary<ulong, ClaimState> _states = new ConcurrentDictionary<ulong, ClaimState>();

        sealed class ClaimState
        {
            public DateTime CooldownResetTime { get; set; }
            public DateTime KakeraResetTime { get; set; }
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

            var embed       = message.Embeds[0];
            var description = embed.Description.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var character   = new CharacterInfo(embed.Author.Name, description[0]);

            // ignore $im messages
            if (description.Any(l => l.StartsWith("claims:", StringComparison.OrdinalIgnoreCase) || l.StartsWith("likes:", StringComparison.OrdinalIgnoreCase)))
                return;

            _logger.LogDebug($"Detected character '{character}' in {logPlace}.");

            // if character belongs to another user, claim kakera
            if (embed.Footer?.Text.StartsWith("belongs", StringComparison.OrdinalIgnoreCase) == true)
            {
                // check cooldown
                var state = _states.GetOrAdd(channel.Id, new ClaimState());
                var now   = DateTime.Now;

                if (!options.KakeraIgnoreCooldown && now < state.KakeraResetTime)
                    return;

                _pendingClaims[message.Id] = new PendingClaim(logPlace, message, character, stopwatch, true);
            }

            else
            {
                var wishedBy = message.Content.StartsWith("wished by", StringComparison.OrdinalIgnoreCase) ? message.GetUserIds().ToArray() : null;

                // must be wished or included in a user wishlist
                if (!_characterFilter.IsWished(character, wishedBy))
                {
                    _logger.LogInformation($"Ignoring character '{character}' in {logPlace} because they are not wished.");
                    return;
                }

                // check cooldown
                var state = _states.GetOrAdd(channel.Id, new ClaimState());
                var now   = DateTime.Now;

                if (!options.IgnoreCooldown && now < state.CooldownResetTime)
                {
                    _logger.LogWarning($"Ignoring character '{character}' in {logPlace} because of cooldown. Cooldown finishes in {state.CooldownResetTime - now}.");
                    return;
                }

                _logger.LogWarning($"Attempting to claim character '{character}' in {logPlace}...");

                _pendingClaims[message.Id] = new PendingClaim(logPlace, message, character, stopwatch, false);
            }

            // purge old pending claims
            foreach (var id in _pendingClaims.Keys)
            {
                if (_pendingClaims.TryGetValue(id, out var claim) && claim.CreatedTime.AddMinutes(1) < DateTime.Now)
                    _pendingClaims.TryRemove(id, out _);
            }
        }

        readonly struct PendingClaim
        {
            public readonly DateTime CreatedTime;

            public readonly string LogPlace;
            public readonly IUserMessage Message;
            public readonly CharacterInfo Character;
            public readonly Stopwatch Stopwatch;
            public readonly bool OnlyKakera;

            public PendingClaim(string logPlace, IUserMessage message, CharacterInfo character, Stopwatch stopwatch, bool onlyKakera)
            {
                CreatedTime = DateTime.Now;

                LogPlace   = logPlace;
                Message    = message;
                Character  = character;
                Stopwatch  = stopwatch;
                OnlyKakera = onlyKakera;
            }
        }

        readonly ConcurrentDictionary<ulong, PendingClaim> _pendingClaims = new ConcurrentDictionary<ulong, PendingClaim>();

        async Task HandleReactionAdded(ReactionAddedEventArgs e)
        {
            var options = _options.CurrentValue;

            if (!options.Enabled || !_pendingClaims.TryGetValue(e.Message.Id, out var claim))
                return;

            var logPlace   = claim.LogPlace;
            var message    = claim.Message;
            var character  = claim.Character;
            var stopwatch  = claim.Stopwatch;
            var onlyKakera = claim.OnlyKakera;

            if (_claimEmojiFilter.IsKakeraEmoji(e.Emoji, out var kakera) && _pendingClaims.TryRemove(e.Message.Id, out claim))
            {
                if (!options.KakeraTargets.Contains(kakera))
                {
                    _logger.LogInformation($"Ignoring {kakera} kakera on character '{character}' in {logPlace} because it is not targeted.");
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(options.KakeraDelaySeconds));

                IUserMessage response;

                try
                {
                    response = await _commandHandler.ReactAsync(message, e.Emoji);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not claim {kakera} kakera on character '{character}' in {logPlace}.");
                    return;
                }

                if (_outputParser.TryParseKakeraSucceeded(response.Content, out var claimer, out _) && claimer.Equals(e.Client.CurrentUser.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Claimed {kakera} kakera on character '{character}' in {logPlace} in {stopwatch.Elapsed.TotalMilliseconds}ms.");
                    return;
                }

                if (_outputParser.TryParseKakeraFailed(response.Content, out var resetTime))
                {
                    _states.GetOrAdd(message.ChannelId, new ClaimState()).KakeraResetTime = DateTime.Now + resetTime;

                    _logger.LogWarning($"Could not claim {kakera} kakera on character '{character}' in {logPlace} due to cooldown. Kakera is reset in {resetTime}.");
                    return;
                }

                _logger.LogWarning($"Probably claimed {kakera} kakera on character '{character}' in {logPlace}, but result could not be determined. Channel is probably busy.");
            }

            else if (!onlyKakera && _claimEmojiFilter.IsClaimEmoji(e.Emoji) && _pendingClaims.TryRemove(e.Message.Id, out claim))
            {
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

                _logger.LogWarning($"Probably claimed character '{character}' in {logPlace}, but result could not be determined. Channel is probably busy. Not sending claim replies.");
            }
        }
    }
}