using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MudaeFarm
{
    public class ConfigManager
    {
        readonly DiscordSocketClient _client;

        public ConfigManager(DiscordSocketClient client)
        {
            _client = client;
        }

        IGuild _guild;

        ITextChannel _generalConfigChannel;
        ITextChannel _wishedCharacterChannel;
        ITextChannel _wishedAnimeChannel;
        ITextChannel _botChannelChannel;

        public string StateUpdateCommand;

        public bool ClaimEnabled;
        public TimeSpan ClaimDelay;
        public TimeSpan KakeraClaimDelay;
        public HashSet<KakeraType> KakeraTargets;

        public bool RollEnabled;
        public string RollCommand;
        public string DailyKakeraCommand;
        public TimeSpan RollTypingDelay;
        public HashSet<ulong> BotChannelIds;

        public Regex WishedCharacterRegex;
        public Regex WishedAnimeRegex;

        public async Task InitializeAsync()
        {
            // find config guild
            var userId = _client.CurrentUser.Id;

            foreach (var guild in _client.Guilds)
            {
                if (guild.OwnerId == userId &&
                    guild.TextChannels.Any(c => c.Name == "information" && c.Topic.StartsWith($"MudaeFarm Configuration Server {userId}")))
                {
                    _guild = guild;
                    break;
                }
            }

            if (_guild == null)
                try
                {
                    Log.Warning("Initializing a new configuration server. This may take a while...");

                    _guild = await _client.CreateGuildAsync("MudaeFarm", await _client.GetOptimalVoiceRegionAsync());

                    // delete default channels
                    foreach (var c in await _guild.GetChannelsAsync())
                        await c.DeleteAsync();

                    var channel = await CreateChannelAsync("information", $"MudaeFarm Configuration Server {userId} - Do not delete this channel!");

                    var message = await channel.SendMessageAsync(
                        "This is your MudaeFarm server where you can configure the bot.\n" +
                        "\n" +
                        "Check <https://github.com/chiyadev/MudaeFarm> for detailed usage guidelines!");

                    await message.PinAsync();
                }
                catch (Exception e)
                {
                    Log.Warning("Could not initialize configuration server. Try creating it manually.", e);

                    throw new DummyRestartException();
                }

            // load channels
            foreach (var channel in await _guild.GetTextChannelsAsync())
            {
                void set(string name, ref ITextChannel c)
                {
                    if (channel.Name == name)
                        c = channel;
                }

                set("information", ref _generalConfigChannel);
                set("wished-characters", ref _wishedCharacterChannel);
                set("wished-anime", ref _wishedAnimeChannel);
                set("bot-channels", ref _botChannelChannel);
            }

            // create channel if not created already
            _wishedCharacterChannel = _wishedCharacterChannel ?? await CreateChannelAsync("wished-characters", "Configure your character wishlist here. Wildcards characters are supported.");
            _wishedAnimeChannel     = _wishedAnimeChannel ?? await CreateChannelAsync("wished-anime", "Configure your anime wishlist here. Wildcards characters are supported.");
            _botChannelChannel      = _botChannelChannel ?? await CreateChannelAsync("bot-channels", "Configure channels to enable MudaeFarm autorolling/claiming by sending the channel ID.");

            // initial load
            await ReloadChannelAsync(_generalConfigChannel);
            await ReloadChannelAsync(_wishedCharacterChannel);
            await ReloadChannelAsync(_wishedAnimeChannel);
            await ReloadChannelAsync(_botChannelChannel);

            Log.Info("Configuration loaded.");

            // import old configuration (config.json)
            var legacyCfg = LegacyConfig.Load();

            if (legacyCfg != null)
            {
                Log.Warning("Importing legacy wishlist configuration. This may take a while...");

                if (legacyCfg.WishlistCharacters != null)
                    foreach (var character in legacyCfg.WishlistCharacters)
                        await _wishedCharacterChannel.SendMessageAsync(character);

                if (legacyCfg.WishlistAnime != null)
                    foreach (var anime in legacyCfg.WishlistAnime)
                        await _wishedAnimeChannel.SendMessageAsync(anime);

                LegacyConfig.Delete();
            }

            // events
            _client.MessageReceived += message => ReloadChannelAsync(message.Channel);
            _client.MessageDeleted  += (cacheable, channel) => ReloadChannelAsync(channel);
            _client.MessageUpdated  += (cacheable, message, channel) => ReloadChannelAsync(channel);
        }

        async Task<ITextChannel> CreateChannelAsync(string name, string topic)
        {
            var channel = await _guild.CreateTextChannelAsync(name);

            if (!string.IsNullOrEmpty(topic))
                await channel.ModifyAsync(c => c.Topic = topic);

            Log.Debug($"Channel created: '#{name}' - '{topic ?? "<null>"}'");

            return channel;
        }

        async Task<List<IUserMessage>> LoadMessagesAsync(IMessageChannel channel)
        {
            var author = _client.CurrentUser.Id;

            var list = new List<IUserMessage>();

            await channel.GetMessagesAsync(int.MaxValue)
                         .ForEachAsync(messages =>
                          {
                              // ReSharper disable once LoopCanBeConvertedToQuery
                              foreach (var message in messages.Where(m => m.Author.Id == author).OfType<IUserMessage>())
                              {
                                  if (!string.IsNullOrWhiteSpace(message.Content))
                                      list.Add(message);
                              }
                          });

            // chronological
            list.Reverse();

            return list;
        }

        async Task ReloadChannelAsync(IMessageChannel channel)
        {
            if (channel.Id == _generalConfigChannel.Id)
            {
                var messages = await LoadMessagesAsync(channel);
                var dict     = new Dictionary<string, ConfigPart>();

                foreach (var message in messages)
                {
                    if (!message.Content.StartsWith("> "))
                        continue;

                    var lines = message.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var key   = lines[0].Substring(2).Trim().ToLowerInvariant();
                    var value = string.Join("\n", lines.Skip(1).Where(l => !l.StartsWith("```")));

                    if (dict.ContainsKey(key))
                    {
                        if (message.Reactions.Count == 0)
                            await message.AddReactionAsync(new Emoji("\u0032\u20E3"));

                        continue;
                    }

                    dict[key] = new ConfigPart(message, value);
                }

                // general
                var general = await LoadConfigPartAsync<GeneralConfig>(channel, "General", dict);

                await _client.SetStatusAsync(general.FallbackStatus);

                StateUpdateCommand = general.StateUpdateCommand;

                // claiming
                var claim = await LoadConfigPartAsync<ClaimConfig>(channel, "Claiming", dict);

                ClaimEnabled     = claim.Enabled;
                ClaimDelay       = TimeSpan.FromSeconds(claim.Delay);
                KakeraClaimDelay = TimeSpan.FromSeconds(claim.KakeraDelay);
                KakeraTargets    = claim.KakeraTargets;

                // rolling
                var roll = await LoadConfigPartAsync<RollConfig>(channel, "Rolling", dict);

                RollEnabled        = roll.Enabled;
                RollCommand        = roll.Command;
                DailyKakeraCommand = roll.KakeraCommand;
                RollTypingDelay    = TimeSpan.FromSeconds(roll.TypingDelay);
            }

            else if (channel.Id == _wishedCharacterChannel.Id)
            {
                WishedCharacterRegex = CreateWishRegex(await LoadMessagesAsync(channel));
            }

            else if (channel.Id == _wishedAnimeChannel.Id)
            {
                WishedAnimeRegex = CreateWishRegex(await LoadMessagesAsync(channel));
            }

            else if (channel.Id == _botChannelChannel.Id)
            {
                var messages   = await LoadMessagesAsync(channel);
                var channelIds = new HashSet<ulong>();

                foreach (var message in messages)
                {
                    var parts = message.Content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    ITextChannel chan;

                    if (!ulong.TryParse(parts[0], out var id) &&
                        !ulong.TryParse(_channelMentionRegex.Match(parts[0]).Groups["id"].Value, out id) ||
                        (chan = _client.GetChannel(id) as ITextChannel) == null)
                    {
                        if (message.Reactions.Count == 0)
                            await message.AddReactionAsync(new Emoji("\u274C"));

                        continue;
                    }

                    if (!channelIds.Add(id))
                    {
                        if (message.Reactions.Count == 0)
                            await message.AddReactionAsync(new Emoji("\u0032\u20E3"));

                        continue;
                    }

                    if (message.Reactions.Count != 0)
                        await message.RemoveAllReactionsAsync();

                    if (parts.Length == 1)
                        await message.ModifyAsync(m => m.Content = $"<#{chan.Id}> - **{chan.Guild.Name}**");
                }

                BotChannelIds = channelIds;
            }
            else
            {
                return;
            }

            Log.Debug($"Configuration channel '${channel.Name}' reloaded.");
        }

        static readonly Regex _channelMentionRegex = new Regex(@"^<#(?<id>\d+)>$", RegexOptions.Compiled | RegexOptions.Singleline);

        static async Task<T> LoadConfigPartAsync<T>(IMessageChannel channel, string key, IReadOnlyDictionary<string, ConfigPart> dict)
            where T : class, new()
        {
            if (dict.TryGetValue(key.ToLowerInvariant(), out var part))
            {
                var error = false;

                var config = JsonConvert.DeserializeObject<T>(
                                 part.Value,
                                 new JsonSerializerSettings
                                 {
                                     Error = (sender, args) =>
                                     {
                                         args.ErrorContext.Handled = true;
                                         error                     = true;
                                     }
                                 })
                          ?? new T();

                if (error && part.Message.Reactions.Count == 0)
                    await part.Message.AddReactionAsync(new Emoji("\u274C"));

                else if (!error && part.Message.Reactions.Count != 0)
                    await part.Message.RemoveAllReactionsAsync();

                return config;
            }

            else
            {
                var config = new T();

                await channel.SendMessageAsync(formatConfigMessage(config));

                return config;
            }

            string formatConfigMessage(T obj)
                => $"> {key}\n```json\n{JsonConvert.SerializeObject(obj, Formatting.Indented, new StringEnumConverter())}\n```";
        }

        static Regex CreateWishRegex(ICollection<IUserMessage> items)
        {
            if (items.Count == 0)
                return null;

            var s = new HashSet<string>(items.Select(x => x.Content.Trim().ToLowerInvariant())).Select(GlobToRegex);

            return new Regex($"({string.Join(")|(", s)})", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        static string GlobToRegex(string s)
            => $"^{Regex.Escape(s).Replace("\\*", ".*").Replace("\\?", ".")}$";

#region Partial configs

        struct ConfigPart
        {
            public readonly IUserMessage Message;
            public readonly string Value;

            public ConfigPart(IUserMessage message, string value)
            {
                Message = message;
                Value   = value;
            }
        }

        public class GeneralConfig
        {
            [JsonProperty("fallback_status")]
            public UserStatus FallbackStatus { get; set; } = UserStatus.Idle;

            [JsonProperty("state_update_command")]
            public string StateUpdateCommand { get; set; } = "$tu";
        }

        public class ClaimConfig
        {
            [JsonProperty("enabled")]
            public bool Enabled { get; set; } = true;

            [JsonProperty("delay_seconds")]
            public double Delay { get; set; } = 0.2;

            [JsonProperty("kakera_delay_seconds")]
            public double KakeraDelay { get; set; } = 0.2;

            [JsonProperty("kakera_targets")]
            public HashSet<KakeraType> KakeraTargets { get; set; } = new HashSet<KakeraType>(Enum.GetValues(typeof(KakeraType)).Cast<KakeraType>());
        }

        public class RollConfig
        {
            [JsonProperty("enabled")]
            public bool Enabled { get; set; } = true;

            [JsonProperty("command")]
            public string Command { get; set; } = "$w";

            [JsonProperty("kakera_command")]
            public string KakeraCommand { get; set; } = "$dk";

            [JsonProperty("typing_delay_seconds")]
            public double TypingDelay { get; set; } = 0.3;
        }

#endregion
    }
}