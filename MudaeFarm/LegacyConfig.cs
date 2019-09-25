using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using Newtonsoft.Json;

namespace MudaeFarm
{
    /// <summary>
    /// Deprecated in v2.1: Configuration is entirely stored on Discord itself. This class exists to import existing configuration onto the configuration server.
    /// </summary>
    public class LegacyConfig
    {
        // store at %LocalAppData%/MudaeFarm/config.json
        static readonly string _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MudaeFarm", "config.json");

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

        public static LegacyConfig Load()
        {
            try
            {
                return JsonConvert.DeserializeObject<LegacyConfig>(File.ReadAllText(_configPath));
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        public static void Delete() => File.Delete(_configPath);
    }
}