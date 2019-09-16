using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MudaeFarm
{
    public class CommandListener
    {
        readonly Config _config;
        readonly DiscordSocketClient _client;

        public CommandListener(Config config, DiscordSocketClient client)
        {
            _config = config;
            _client = client;
        }

        Dictionary<string, CommandDelegate> _commands;

        public void Initialize()
        {
            _commands = GetType()
                       .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                       .Where(m => m.GetCustomAttribute<CommandAttribute>() != null)
                       .ToDictionary<MethodInfo, string, CommandDelegate>(
                            m => m.GetCustomAttribute<CommandAttribute>().Name,
                            m =>
                            {
                                var parameters = m.GetParameters();

                                if (m.ReturnType.IsSubclassOf(typeof(Task)))
                                {
                                    if (parameters.Length == 0)
                                        return (msg, args) => (Task) m.Invoke(this, new object[0]);

                                    if (parameters[0].ParameterType == typeof(IUserMessage))
                                        switch (parameters.Length)
                                        {
                                            case 1:
                                                return (msg, args) => (Task) m.Invoke(this, new object[] { msg });

                                            case 2 when parameters[1].ParameterType == typeof(string):
                                                return (msg, args) => (Task) m.Invoke(this, new object[] { msg, args.Length == 0 ? null : string.Join(" ", args) });

                                            case 2 when parameters[1].ParameterType == typeof(string[]):
                                                return (msg, args) => (Task) m.Invoke(this, new object[] { msg, args });
                                        }
                                }

                                throw new NotSupportedException($"Unsupported command method: {m}");
                            });

            _client.MessageReceived += HandleMessageAsync;
        }

        async Task HandleMessageAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage))
                return;

            if (message.Author.Id != _client.CurrentUser.Id)
                return;

            try
            {
                await HandleCommandMessage(userMessage);
            }
            catch (Exception e)
            {
                Log.Warning($"Could not handle self command message {message.Id} '{message.Content}'.", e);
            }
        }

        async Task HandleCommandMessage(IUserMessage message)
        {
            var content = message.Content;

            if (!content.StartsWith("/"))
                return;

            var parts     = content.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var command   = parts[0];
            var arguments = parts.Skip(1).ToArray();

            if (_commands.TryGetValue(command, out var func))
                try
                {
                    await func(message, arguments);
                }
                catch (Exception e)
                {
                    throw new ApplicationException($"Exception while executing '{command}' with arguments: '{string.Join("', '", arguments)}'", e);
                }
        }

        [AttributeUsage(AttributeTargets.Method)]
        sealed class CommandAttribute : Attribute
        {
            public readonly string Name;

            public CommandAttribute(string name)
            {
                Name = name;
            }
        }

        delegate Task CommandDelegate(IUserMessage message, string[] args);

        [Command("wishlist")]
        public async Task WishlistAsync(IUserMessage message)
        {
            string characterWishlist;

            lock (_config.WishlistCharacters)
            {
                if (_config.WishlistCharacters.Count == 0)
                    characterWishlist = "No characters in wishlist.";
                else
                    characterWishlist = "Wished characters: " + string.Join(", ", _config.WishlistCharacters);
            }

            string animeWishlist;

            lock (_config.WishlistAnime)
            {
                if (_config.WishlistAnime.Count == 0)
                    animeWishlist = "No anime in wishlist.";
                else
                    animeWishlist = "Wished anime: " + string.Join(", ", _config.WishlistAnime);
            }

            await message.ModifyAsync(m => m.Content = characterWishlist);
            await message.Channel.SendMessageAsync(animeWishlist);
        }

        [Command("wish")]
        public async Task WishCharacterAsync(IUserMessage message, string character)
        {
            lock (_config.WishlistCharacters)
            {
                if (_config.WishlistCharacters.Add(character.ToLowerInvariant()))
                    Log.Info($"Added character '{character}' to the wishlist.");
            }

            await message.DeleteAsync();
        }

        [Command("unwish")]
        public async Task UnwishCharacterAsync(IUserMessage message, string character)
        {
            lock (_config.WishlistCharacters)
            {
                if (_config.WishlistCharacters.Remove(character.ToLowerInvariant()))
                    Log.Info($"Removed character '{character}' from the wishlist.");
            }

            await message.DeleteAsync();
        }

        [Command("wishani")]
        public async Task WishAnimeAsync(IUserMessage message, string anime)
        {
            lock (_config.WishlistAnime)
            {
                if (_config.WishlistAnime.Add(anime.ToLowerInvariant()))
                    Log.Info($"Added anime '{anime}' to the wishlist.");
            }

            await message.DeleteAsync();
        }

        [Command("unwishani")]
        public async Task UnwishAnimeAsync(IUserMessage message, string anime)
        {
            lock (_config.WishlistAnime)
            {
                if (_config.WishlistAnime.Remove(anime.ToLowerInvariant()))
                    Log.Info($"Removed anime '{anime}' from the wishlist.");
            }

            await message.DeleteAsync();
        }

        [Command("wishclear")]
        public async Task ClearWishlistAsync(IUserMessage message, string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                ClearWishlistInternal("characters");
                ClearWishlistInternal("anime");
            }
            else
            {
                ClearWishlistInternal(category);
            }

            await message.DeleteAsync();
        }

        void ClearWishlistInternal(string category)
        {
            switch (category.ToLowerInvariant())
            {
                case "characters":
                    lock (_config.WishlistCharacters)
                    {
                        _config.WishlistCharacters.Clear();
                        Log.Info("Cleared character wishlist.");
                    }

                    break;

                case "anime":
                    lock (_config.WishlistAnime)
                    {
                        _config.WishlistAnime.Clear();
                        Log.Info("Cleared anime wishlist.");
                    }

                    break;
            }
        }

        [Command("claimdelay")]
        public async Task ClaimDelayAsync(IUserMessage message, string arg)
        {
            if (!double.TryParse(arg, out var delay))
                return;

            // ReSharper disable once InconsistentlySynchronizedField
            _config.ClaimDelay = delay;

            Log.Info(delay <= 0
                         ? "Claiming delay disabled."
                         : $"Claiming delay set to {delay:F} seconds.");

            await message.DeleteAsync();
        }

        [Command("rollinterval")]
        public async Task RollIntervalAsync(IUserMessage message, string arg)
        {
            if (!double.TryParse(arg, out var interval))
                return;

            // ReSharper disable once InconsistentlySynchronizedField
            _config.RollInterval = interval;

            Log.Info(interval <= 0
                         ? "Rolling is disabled."
                         : $"Rolling interval set to {interval:F} minutes.");

            await message.DeleteAsync();
        }

        [Command("roll")]
        public async Task RollChannelAsync(IUserMessage message, string action)
        {
            lock (_config.RollChannels)
            {
                switch (action.ToLowerInvariant())
                {
                    default:
                        if (_config.RollChannels.Add(message.Channel.Id))
                            Log.Info($"Added roll channel '{message.Channel}'");

                        break;

                    case "disable":
                        if (_config.RollChannels.Remove(message.Channel.Id))
                            Log.Info($"Removed roll channel '{message.Channel}'");

                        break;
                }
            }

            // edit message first so the command is not saved in logs
            await message.ModifyAsync(m => m.Content = Utilities.RandString(3));
            await message.DeleteAsync();
        }

        [Command("marry")]
        public async Task MarryAsync(IUserMessage message, string command)
        {
            switch (command.ToLowerInvariant())
            {
                case "waifu":
                    command = "w";
                    break;

                case "husbando":
                    command = "h";
                    break;
            }

            // ReSharper disable once InconsistentlySynchronizedField
            _config.RollCommand = command;

            Log.Info($"Roll command set to '${command}'.");

            await message.DeleteAsync();
        }
    }
}