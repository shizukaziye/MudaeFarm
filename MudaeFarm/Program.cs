using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    static class Program
    {
        static async Task Main()
        {
            while (true)
            {
                try
                {
                    await RunAsync();
                    return;
                }
                catch (Exception e)
                {
                    // fatal error recovery
                    Log(LogSeverity.Critical, e.ToString());
                    Log(LogSeverity.Warning, "Restarting in 10 seconds...");

                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }
        }

        static Config _config;

        static DiscordSocketClient _discord;

        static async Task RunAsync()
        {
            _config = Config.Load();

            // reinitialize Discord client
            _discord?.Dispose();
            _discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel         = LogSeverity.Info,
                MessageCacheSize = 5
            });

            // ask for token if not set
            if (string.IsNullOrEmpty(_config.AuthToken))
            {
                Console.Write("MudaeFarm requires your user token in order to proceed.\n" +
                              "A user token is a long piece of text that is synonymous to your Discord password.\n" +
                              "\n" +
                              "What happens when you enter your token:\n" +
                              "- MudaeFarm will save this token to the disk UNENCRYPTED.\n" +
                              "- MudaeFarm will authenticate to Discord using this token, ACTING AS YOU.\n" +
                              "\n" +
                              "MudaeFarm makes no guarantee regarding your account's privacy nor safety.\n" +
                              "If you are concerned, you may inspect MudaeFarm's complete source code at https://github.com/chiyadev/MudaeFarm.\n" +
                              "\n" +
                              "MudaeFarm is licensed under the MIT License. The authors of MudaeFarm shall not be held liable for any claim, damage or liability.\n" +
                              "You can read the license terms at https://github.com/chiyadev/MudaeFarm/blob/master/LICENSE.\n" +
                              "\n" +
                              "Proceed? (y/n) ");

                if (!Console.ReadKey().KeyChar.ToString().Equals("y", StringComparison.OrdinalIgnoreCase))
                    Environment.Exit(1);

                Console.Write("\nToken: ");

                _config.AuthToken = Console.ReadLine();

                Console.Clear();

                _config.Save();
            }

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

            try
            {
                await _discord.LoginAsync(TokenType.User, _config.AuthToken);
            }
            catch (Exception e)
            {
                Log(LogSeverity.Error, e.ToString());

                _config.AuthToken = null;
                _config.Save();

                Log(LogSeverity.Warning,
                    "User token has been forgotten due to an error while authenticating to Discord.");

                return;
            }

            await _discord.StartAsync();

            await connectionSource.Task;

            _discord.Connected -= handleConnect;

            // keep the bot running
            while (true)
            {
                // autorolling
                if (_config.RollInterval.HasValue)
                {
                    await Task.Delay(TimeSpan.FromMinutes(_config.RollInterval.Value));

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
                    await channel.SendMessageAsync("$" + _config.RollCommand);
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
            @"^Mudamaid\s*\d+$",
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
            else if (author.IsBot && (author.Id == 432610292342587392 ||
                                      _maidUsernameRegex.IsMatch(author.Username)))
                await HandleMudaeMessageAsync(userMessage);
        }

        static async Task HandleSelfCommandAsync(SocketUserMessage message)
        {
            var content = message.Content;

            if (!content.StartsWith("/"))
                return;

            content = content.Substring(1);

            var delimitor = content.IndexOf(' ');
            var command   = delimitor == -1 ? content : content.Substring(0, delimitor);
            var argument  = delimitor == -1 ? null : content.Substring(delimitor).Trim();

            if (string.IsNullOrWhiteSpace(command))
                return;

            switch (command)
            {
                case "wishlist":
                    await message.ModifyAsync(m =>
                    {
                        m.Content =
                            "Character wishlist: \n" +
                            (_config.WishlistCharacters.Count == 0
                                ? "Empty"
                                : $"- `{string.Join("`\n- `", _config.WishlistCharacters)}`") +
                            "\n\n" +
                            "Anime wishlist: \n" +
                            (_config.WishlistAnime.Count == 0
                                ? "Empty"
                                : $"- `{string.Join("`\n- `", _config.WishlistAnime)}`");
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
                    if (_config.WishlistCharacters.Add(argument.ToLowerInvariant()))
                        Log(LogSeverity.Info, $"Added character '{argument}' to the wishlist.");

                    break;

                case "unwish":
                    if (_config.WishlistCharacters.Remove(argument.ToLowerInvariant()))
                        Log(LogSeverity.Info, $"Removed character '{argument}' from the wishlist.");

                    break;

                case "wishani":
                    if (_config.WishlistAnime.Add(argument.ToLowerInvariant()))
                        Log(LogSeverity.Info, $"Added anime '{argument}' to the wishlist.");

                    break;

                case "unwishani":
                    if (_config.WishlistAnime.Remove(argument.ToLowerInvariant()))
                        Log(LogSeverity.Info, $"Removed anime '{argument}' from the wishlist.");

                    break;

                case "clearwishes":
                    _config.WishlistCharacters.Clear();
                    _config.WishlistAnime.Clear();

                    Log(LogSeverity.Warning, "Cleared character and anime wishlist.");
                    break;

                case "rollinterval" when double.TryParse(argument, out var rollInterval):
                    _config.RollInterval = rollInterval < 0 ? (double?) null : rollInterval;

                    Log(LogSeverity.Info,
                        $"Set roll interval to every '{_config.RollInterval?.ToString() ?? "<null>"}' minutes.");

                    break;

                case "claimdelay" when double.TryParse(argument, out var claimDelay):
                    _config.ClaimDelay = Math.Max(claimDelay, 0);

                    Log(LogSeverity.Info, $"Set claim delay to `{claimDelay}` seconds.");
                    break;

                case "marry":
                    switch (argument.ToLowerInvariant())
                    {
                        case "waifu":
                            _config.RollCommand = 'w';
                            break;

                        case "husbando":
                            _config.RollCommand = 'h';
                            break;

                        default: return;
                    }

                    Log(LogSeverity.Info, $"Set marry command to '${argument}'.");
                    break;

                default: return;
            }

            await message.DeleteAsync();
            _config.Save();
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

            // claim delay
            if (_config.ClaimDelay > 0)
                await Task.Delay(TimeSpan.FromSeconds(_config.ClaimDelay));

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
            var anime = embed.Description.Split('\n')[0].Trim().ToLowerInvariant();

            if (anime.Contains('\n'))
                return;

            if (_config.WishlistCharacters.Contains(name) ||
                _config.WishlistAnime.Contains(anime))
            {
                Log(LogSeverity.Warning, $"Found character '{name}', trying marriage.");

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

        static readonly object _logLock = new object();

        static void Log(LogSeverity severity,
                        string message)
        {
            lock (_logLock)
            {
                var oldColor = Console.ForegroundColor;

                if (_severityColors.TryGetValue(severity, out var newColor))
                    Console.ForegroundColor = newColor;

                Console.WriteLine($"{$"[{severity}]".PadRight(10, ' ')} {message.Trim()}");

                Console.ForegroundColor = oldColor;
            }
        }

        static readonly Dictionary<LogSeverity, ConsoleColor> _severityColors =
            new Dictionary<LogSeverity, ConsoleColor>
            {
                { LogSeverity.Debug, ConsoleColor.Gray },
                { LogSeverity.Verbose, ConsoleColor.Gray },
                { LogSeverity.Warning, ConsoleColor.Yellow },
                { LogSeverity.Error, ConsoleColor.Red },
                { LogSeverity.Critical, ConsoleColor.Red }
            };
    }
}
