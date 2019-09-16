using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MudaeFarm
{
    /// <remarks>
    /// Always lock collection properties before accessing them!!
    /// </remarks>
    public class Config
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

        [JsonProperty("wish_chars")]
        public HashSet<string> WishlistCharacters { get; set; } = new HashSet<string>();

        [JsonProperty("wish_anime")]
        public HashSet<string> WishlistAnime { get; set; } = new HashSet<string>();

        public static Config Load()
        {
            try
            {
                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(_configPath));
            }
            catch (FileNotFoundException)
            {
                return new Config();
            }
            catch (DirectoryNotFoundException)
            {
                return new Config();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath));

            File.WriteAllText(_configPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}