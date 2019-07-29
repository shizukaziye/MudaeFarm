using System;
using System.Collections.Generic;

namespace MudaeFarm
{
    public class Config
    {
        public string AuthToken { get; set; } = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

        public double? RollInterval { get; set; }
        public char RollCommand { get; set; } = 'w';

        public double ClaimDelay { get; set; }

        public HashSet<ulong> BotChannels { get; set; } = new HashSet<ulong>();

        public HashSet<string> WishlistCharacters { get; set; } = new HashSet<string>();
        public HashSet<string> WishlistAnime { get; set; } = new HashSet<string>();
    }
}