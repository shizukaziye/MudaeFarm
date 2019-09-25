using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using Newtonsoft.Json;

namespace MudaeFarm
{
    /// <remarks>
    /// Always lock collection properties before accessing them!!
    /// </remarks>
    public class Config : ICloneable
    {
        // store at %LocalAppData%/MudaeFarm/config.json
        static readonly string _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MudaeFarm", "config.json");

        [JsonProperty("cmd_server_id")]
        public ulong CommandServerId { get; set; }

        [JsonProperty("auth_token")]
        public string AuthToken { get; set; } = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

        [JsonProperty("roll_interval")]
        public double RollInterval { get; set; }

        [JsonProperty("roll_command")]
        public string RollCommand { get; set; } = "w";

        [JsonProperty("roll_channels")]
        public HashSet<ulong> RollChannels { get; set; } = new HashSet<ulong>();

        [JsonProperty("claim_delay")]
        public double ClaimDelay { get; set; }

        [JsonProperty("claim_servers_blacklist")]
        public HashSet<ulong> ClaimServersBlacklist { get; set; } = new HashSet<ulong>();

        [JsonProperty("wish_chars")]
        public HashSet<string> WishlistCharacters { get; set; } = new HashSet<string>();

        [JsonProperty("wish_anime")]
        public HashSet<string> WishlistAnime { get; set; } = new HashSet<string>();

        [JsonProperty("user_status")]
        public UserStatus UserStatus { get; set; } = UserStatus.Idle;

        public object Clone() => new Config
        {
            CommandServerId       = CommandServerId,
            AuthToken             = AuthToken,
            RollInterval          = RollInterval,
            RollCommand           = RollCommand,
            RollChannels          = RollChannels.Lock(x => new HashSet<ulong>(x)),
            ClaimDelay            = ClaimDelay,
            ClaimServersBlacklist = ClaimServersBlacklist,
            WishlistCharacters    = WishlistCharacters.Lock(x => new HashSet<string>(x)),
            WishlistAnime         = WishlistAnime.Lock(x => new HashSet<string>(x)),
            UserStatus            = UserStatus
        };

        public static Config Load()
        {
            try
            {
                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(_configPath));
            }
            catch (FileNotFoundException)
            {
                Log.Debug($"Initializing new configuration not found at: {_configPath}");
                return new Config();
            }
            catch (DirectoryNotFoundException)
            {
                Log.Debug($"Initializing new configuration not found at: {_configPath}");
                return new Config();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath));

            // clone first for thread safety
            var config = Clone();

            File.WriteAllText(_configPath, JsonConvert.SerializeObject(config, Formatting.Indented));

            Log.Debug($"Configuration saved at: {_configPath}");
        }
    }
}