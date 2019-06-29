using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace MudaeFarm
{
    static class Program
    {
        static DiscordSocketClient _discord = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel         = LogSeverity.Info,
            MessageCacheSize = 5
        });

        static async Task Main()
        {
            // load config
            await LoadConfigAsync();

            // events
            _discord.Log             += HandleLogAsync;
            _discord.MessageReceived += HandleMessageAsync;
            _discord.ReactionAdded   += HandleReactionAsync;

            // login and wait for ready
            var connectionSource = new TaskCompletionSource<object>();

            _discord.Connected += handleConnect;

            Task handleConnect()
            {
                connectionSource.SetResult(null);
                return Task.CompletedTask;
            }

            await _discord.LoginAsync(TokenType.User, _config.AuthToken);
            await _discord.StartAsync();

            await connectionSource.Task;

            _discord.Connected -= handleConnect;

            // keep the bot running
            while (true)
            {
                // autorolling
                if (_config.AutoRollInterval.HasValue)
                {
                    await Task.Delay(TimeSpan.FromMinutes(_config.AutoRollInterval.Value));

                    await SendRollsAsync();
                }

                // else sleep
                else
                {
                    await Task.Delay(1000);
                }
            }
        }

        static async Task SendRollsAsync()
        {
            foreach (var channelId in _config.BotChannels)
            {
                if (_discord.GetChannel(channelId) is ITextChannel channel)
                    await channel.SendMessageAsync("$" + _config.AutoRollGender);
                else
                    Log(LogSeverity.Warning, $"Channel {channelId} is unavailable.");

                // don't spam the api
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        static Task HandleLogAsync(LogMessage m)
        {
            var message = m.Exception == null
                ? m.Message
                : $"{m.Message}: {m.Exception}";

            Log(m.Severity, message);

            return Task.CompletedTask;
        }

        // mudamaid username regex
        static readonly Regex _maidUsernameRegex = new Regex(
            @"^Mudamaid\s\d+$",
            RegexOptions.Singleline | RegexOptions.Compiled);

        static async Task HandleMessageAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage))
                return;

            var author = message.Author;

            // author is ourselves
            if (author.Id == _discord.CurrentUser.Id)
                await HandleSelfCommandAsync(userMessage);

            // author is mudae bot or its maid
            else if (author.Id == 432610292342587392 ||
                     _maidUsernameRegex.IsMatch(author.Username))
                await HandleMudaeMessageAsync(userMessage);
        }

        static async Task HandleSelfCommandAsync(SocketUserMessage message)
        {
            var content = message.Content;

            if (!content.StartsWith('/'))
                return;

            content = content.Substring(1);

            var delimitor = content.IndexOf(' ');
            var command   = delimitor == -1 ? content : content.Substring(0, delimitor);
            var argument  = delimitor == -1 ? null : content.Substring(delimitor + 1);

            if (string.IsNullOrWhiteSpace(command))
                return;

            switch (command)
            {
                case "wishlist":
                    await message.ModifyAsync(m =>
                    {
                        m.Content =
                            "Character wishlist: \n" +
                            $"- {string.Join("\n- ", _config.WishlistCharacters)}";
                    });

                    return;

                case "wishlistani":
                    await message.ModifyAsync(m =>
                    {
                        m.Content =
                            "Anime wishlist: \n" +
                            $"- {string.Join("\n- ", _config.WishlistAnimes)}";
                    });

                    return;

                case "setchannel":
                    if (_config.BotChannels.Add(message.Channel.Id))
                        Log(LogSeverity.Info, $"Added bot channel '{message.Channel.Id}'.");

                    await message.DeleteAsync();
                    return;

                case "unsetchannel":
                    if (_config.BotChannels.Remove(message.Channel.Id))
                        Log(LogSeverity.Info, $"Removed bot channel '{message.Channel.Id}'.");

                    await message.DeleteAsync();
                    return;
            }

            if (string.IsNullOrWhiteSpace(argument))
                return;

            switch (command.ToLowerInvariant())
            {
                case "wish":
                    _config.WishlistCharacters.Add(argument.ToLowerInvariant());

                    Log(LogSeverity.Info, $"Added character '{argument}' to the wishlist.");
                    break;

                case "unwish":
                    _config.WishlistCharacters.Remove(argument.ToLowerInvariant());

                    Log(LogSeverity.Info, $"Removed character '{argument}' from the wishlist.");
                    break;

                case "wishani":
                    _config.WishlistAnimes.Add(argument.ToLowerInvariant());

                    Log(LogSeverity.Info, $"Added anime '{argument}' to the wishlist.");
                    break;

                case "unwishani":
                    _config.WishlistAnimes.Remove(argument.ToLowerInvariant());

                    Log(LogSeverity.Info, $"Removed anime '{argument}' from the wishlist.");
                    break;

                case "rollinterval":
                    if (double.TryParse(argument, out var rollInterval))
                    {
                        _config.AutoRollInterval = rollInterval < 0 ? (double?) null : rollInterval;

                        Log(LogSeverity.Info,
                            $"Set roll interval to every '{_config.AutoRollInterval?.ToString() ?? "<null>"}' minutes.");
                    }

                    break;
                case "marry":
                    switch (argument.ToLowerInvariant())
                    {
                        case "waifu":
                            _config.AutoRollGender = 'w';
                            break;
                        case "husbando":
                            _config.AutoRollGender = 'h';
                            break;

                        default: return;
                    }

                    Log(LogSeverity.Info, $"Set marry command to '${argument}'.");
                    break;

                default: return;
            }

            await message.DeleteAsync();
            await SaveConfigAsync();
        }

        static readonly Dictionary<ulong, IUserMessage> _claimQueue = new Dictionary<ulong, IUserMessage>();

        static async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable,
                                              ISocketMessageChannel channel,
                                              SocketReaction reaction)
        {
            IUserMessage message;

            lock (_claimQueue)
            {
                if (!_claimQueue.TryGetValue(reaction.MessageId, out message))
                    return;

                _claimQueue.Remove(reaction.MessageId);
            }

            await message.AddReactionAsync(reaction.Emote);
        }

        static async Task HandleMudaeMessageAsync(SocketUserMessage message)
        {
            if (!message.Embeds.Any())
                return;

            var embed = message.Embeds.First();

            if (embed.Footer.HasValue &&
                embed.Footer.Value.Text.StartsWith("Belongs to", StringComparison.OrdinalIgnoreCase))
                return;

            if (!embed.Author.HasValue ||
                embed.Author.Value.IconUrl != null)
                return;

            var name  = embed.Author.Value.Name.Trim().ToLowerInvariant();
            var anime = embed.Description.Trim().ToLowerInvariant();

            if (anime.Contains('\n'))
                return;

            if (_config.WishlistCharacters.Contains(name) ||
                _config.WishlistAnimes.Contains(anime))
            {
                Log(LogSeverity.Info, $"Found character '{name}', trying marriage.");

                lock (_claimQueue)
                    _claimQueue.Add(message.Id, message);

                foreach (var emote in message.Reactions.Keys)
                    await message.AddReactionAsync(emote);
            }
            else
            {
                Log(LogSeverity.Info, $"Ignored character '{name}', not wished.");
            }
        }

        static Config _config;

        static async Task LoadConfigAsync()
        {
            try
            {
                _config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync("config.json"));
            }
            catch (FileNotFoundException)
            {
                _config = new Config();
            }
        }

        static Task SaveConfigAsync() => File.WriteAllTextAsync("config.json", JsonConvert.SerializeObject(_config));

        static void Log(LogSeverity severity,
                        string message) => Console.WriteLine($"{$"[{severity}]".PadRight(10, ' ')} {message}");
    }
}
