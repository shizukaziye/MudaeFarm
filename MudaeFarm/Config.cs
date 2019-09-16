using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MudaeFarm
{
    public class Config
    {
        [JsonProperty("auth_token")]
        public string AuthToken { get; set; } = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

        [JsonProperty("roll_interval")]
        public double? RollInterval { get; set; }

        [JsonProperty("roll_command")]
        public char RollCommand { get; set; } = 'w';

        [JsonProperty("claim_delay")]
        public double ClaimDelay { get; set; }

        [JsonProperty("bot_channels")]
        public HashSet<ulong> BotChannels { get; set; } = new HashSet<ulong>();

        [JsonProperty("wish_chars")]
        public HashSet<string> WishlistCharacters { get; set; } = new HashSet<string>();

        [JsonProperty("wish_anime")]
        public HashSet<string> WishlistAnime { get; set; } = new HashSet<string>();
    }
}