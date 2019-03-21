using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MudaeFarm
{
    class Program
    {
        public static ulong[] MudaeIds =
        {
            432610292342587392,
            522749851922989068
        };

        static ILogger _logger;
        static DiscordSocketClient _discord;

        static async Task Main()
        {
            // Configure services
            var services = ConfigureServices(new ServiceCollection()).BuildServiceProvider();

            using (services.CreateScope())
            {
                _logger = services.GetService<ILogger<Program>>();
                _discord = services.GetService<DiscordSocketClient>();

                // Load configuration
                await LoadConfigAsync();

                // Register events
                _discord.Log += HandleLogAsync;
                _discord.MessageReceived += HandleMessageAsync;
                _discord.ReactionAdded += HandleReactionAsync;

                // Login
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

                // Keep the bot running
                // TODO: graceful shutdown?
                while (true)
                {
                    if (_config.AutoRollInterval.HasValue)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(_config.AutoRollInterval.Value));

                        await SendRollAsync();
                    }
                    else
                        await Task.Delay(1000);
                }

                // Unregister events
                _discord.Log -= HandleLogAsync;
                _discord.MessageReceived -= HandleMessageAsync;
                _discord.ReactionAdded -= HandleReactionAsync;

                // Logout
                await _discord.StopAsync();
                await _discord.LogoutAsync();
            }
        }

        static async Task SendRollAsync()
        {
            foreach (var channelId in _config.BotChannels)
            {
                var channel = _discord.GetChannel(channelId) as ITextChannel;

                await channel.SendMessageAsync("$" + _config.AutoRollGender);

                // Cooldown to not spam the API
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        static IServiceCollection ConfigureServices(IServiceCollection services) => services
            .AddSingleton<DiscordSocketClient>()
            .AddLogging(l => l.AddConsole());

        static Task HandleLogAsync(LogMessage m)
        {
            var level = LogLevel.None;

            switch (m.Severity)
            {
                case LogSeverity.Verbose:
                    level = LogLevel.Trace;
                    break;
                case LogSeverity.Debug:
                    level = LogLevel.Debug;
                    break;
                case LogSeverity.Info:
                    level = LogLevel.Information;
                    break;
                case LogSeverity.Warning:
                    level = LogLevel.Warning;
                    break;
                case LogSeverity.Error:
                    level = LogLevel.Error;
                    break;
                case LogSeverity.Critical:
                    level = LogLevel.Critical;
                    break;
            }

            if (m.Exception == null)
                _logger.Log(level, m.Message);
            else
                _logger.Log(level, m.Exception, m.Exception.Message);

            return Task.CompletedTask;
        }

        static async Task HandleMessageAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage))
                return;

            var author = message.Author.Id;

            if (author == _discord.CurrentUser.Id)
                await HandleSelfCommandAsync(userMessage);

            else if (Array.IndexOf(MudaeIds, author) != -1)
                await HandleMudaeMessageAsync(userMessage);
        }

        static async Task HandleSelfCommandAsync(SocketUserMessage message)
        {
            var content = message.Content;

            if (!content.StartsWith('/'))
                return;

            content = content.Substring(1);

            var delimitor = content.IndexOf(' ');
            var command = delimitor == -1 ? content : content.Substring(0, delimitor);
            var argument = delimitor == -1 ? null : content.Substring(delimitor + 1);

            if (string.IsNullOrWhiteSpace(command))
                return;

            switch (command)
            {
                case "wishlist":
                    await message.ModifyAsync(m =>
                    {
                        m.Content = $"Character wishlist: {string.Join(", ", _config.WishlistCharacters)}";
                    });
                    return;
                case "wishlistani":
                    await message.ModifyAsync(m =>
                    {
                        m.Content = $"Anime wishlist: {string.Join(", ", _config.WishlistAnimes)}";
                    });
                    return;
                case "setchannel":
                    if (_config.BotChannels.Add(message.Channel.Id))
                        _logger.LogInformation($"Added bot channel '{message.Channel.Id}'.");
                    await message.DeleteAsync();
                    return;
                case "unsetchannel":
                    if (_config.BotChannels.Remove(message.Channel.Id))
                        _logger.LogInformation($"Removed bot channel '{message.Channel.Id}'.");
                    await message.DeleteAsync();
                    return;
            }

            if (string.IsNullOrWhiteSpace(argument))
                return;

            switch (command.ToLowerInvariant())
            {
                case "wish":
                    _config.WishlistCharacters.Add(argument.ToLowerInvariant());
                    _logger.LogInformation($"Added character '{argument}' to the wishlist.");
                    break;
                case "unwish":
                    _config.WishlistCharacters.Remove(argument.ToLowerInvariant());
                    _logger.LogInformation($"Removed character '{argument}' from the wishlist.");
                    break;
                case "wishani":
                    _config.WishlistAnimes.Add(argument.ToLowerInvariant());
                    _logger.LogInformation($"Added anime '{argument}' to the wishlist.");
                    break;
                case "unwishani":
                    _config.WishlistAnimes.Remove(argument.ToLowerInvariant());
                    _logger.LogInformation($"Removed anime '{argument}' from the wishlist.");
                    break;
                case "rollinterval":
                    if (double.TryParse(argument, out var rollInterval))
                    {
                        _config.AutoRollInterval = rollInterval == -1 ? (double?) null : rollInterval;
                        _logger.LogInformation($"Set autoroll interval to every '{rollInterval}' minutes.");
                    }

                    break;
                case "marry":
                    if (argument.ToLowerInvariant() == "waifu")
                        _config.AutoRollGender = 'w';
                    else if (argument.ToLowerInvariant() == "husbando")
                        _config.AutoRollGender = 'h';
                    else
                        break;

                    _logger.LogInformation($"Set marry target gender to '{argument}'.");
                    break;
                default:
                    return;
            }

            await message.DeleteAsync();
            await SaveConfigAsync();
        }

        static readonly Dictionary<ulong, IUserMessage> _claimQueue = new Dictionary<ulong, IUserMessage>();

        static async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel,
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

            var name = embed.Author.Value.Name.Trim().ToLowerInvariant();
            var anime = embed.Description.Trim().ToLowerInvariant();

            if (anime.Contains('\n'))
                return;

            if (_config.WishlistCharacters.Contains(name) ||
                _config.WishlistAnimes.Contains(anime))
            {
                _logger.LogInformation($"Found character '{name}', trying marriage.");

                lock (_claimQueue)
                    _claimQueue.Add(message.Id, message);

                foreach (var emote in message.Reactions.Keys)
                    await message.AddReactionAsync(emote);
            }
            else
                _logger.LogInformation($"Ignored character '{name}', not wished.");
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

        static async Task SaveConfigAsync()
        {
            await File.WriteAllTextAsync("config.json", JsonConvert.SerializeObject(_config));
        }
    }
}