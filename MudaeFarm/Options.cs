using System;
using System.Collections.Generic;
using System.Linq;
using Disqord;
using Newtonsoft.Json;

namespace MudaeFarm
{
    public class GeneralOptions
    {
        public const string Section = "General";

        [JsonProperty("fallback_status")]
        public UserStatus FallbackStatus { get; set; } = UserStatus.Idle;

        [JsonProperty("auto_update")]
        public bool AutoUpdate { get; set; } = true;
    }

    public class ClaimingOptions
    {
        public const string Section = "Claiming";

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("delay_seconds")]
        public double DelaySeconds { get; set; } = 0.2;

        [JsonProperty("ignore_cooldown")]
        public bool IgnoreCooldown { get; set; }

        [JsonProperty("kakera_delay_seconds")]
        public double KakeraDelaySeconds { get; set; } = 0.2;

        [JsonProperty("kakera_ignore_cooldown")]
        public bool KakeraIgnoreCooldown { get; set; }

        [JsonProperty("kakera_targets")]
        public HashSet<KakeraType> KakeraTargets { get; set; } = new HashSet<KakeraType>(Enum.GetValues(typeof(KakeraType)).Cast<KakeraType>());

        [JsonProperty("enable_custom_emotes")]
        public bool CustomEmotes { get; set; }
    }

    public class RollingOptions
    {
        public const string Section = "Rolling";

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; } = "$w";

        [JsonProperty("daily_kakera_enabled")]
        public bool DailyKakeraEnabled { get; set; }

        [JsonProperty("daily_kakera_command")]
        public string DailyKakeraCommand { get; set; } = "$dk";

        [JsonProperty("typing_delay_seconds")]
        public double TypingDelaySeconds { get; set; } = 0.3;

        [JsonProperty("interval_seconds")]
        public double IntervalSeconds { get; set; } = 0.5;
    }

    public class CharacterWishlist
    {
        public const string Section = "Wished characters";

        public class Item
        {
            [JsonProperty("name")]
            public string Name { get; set; }
        }

        [JsonProperty("items")]
        public List<Item> Items { get; set; } = new List<Item>();
    }

    public class AnimeWishlist
    {
        public const string Section = "Wished anime";

        public class Item
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("excluding")]
            public CharacterWishlist Excluding { get; set; }
        }

        [JsonProperty("items")]
        public List<Item> Items { get; set; } = new List<Item>();
    }

    public class BotChannelList
    {
        public const string Section = "Bot channels";

        public class Item : IEquatable<Item>
        {
            [JsonProperty("id")]
            public ulong Id { get; set; }

            public bool Equals(Item other) => other != null && Id == other.Id;
        }

        [JsonProperty("items")]
        public List<Item> Items { get; set; } = new List<Item>();
    }

    public enum ClaimReplyTiming
    {
        After = 0,
        Before = 1
    }

    public class ClaimReplyList
    {
        public const string Section = "Claim replies";

        public class Item
        {
            [JsonProperty("content")]
            public string Content { get; set; }

            [JsonProperty("timing")]
            public ClaimReplyTiming Timing { get; set; }

            [JsonProperty("weight")]
            public double Weight { get; set; } = 1;
        }

        [JsonProperty("items")]
        public List<Item> Items { get; set; } = new List<Item>();
    }

    public class UserWishlistList
    {
        public const string Section = "User wishlists";

        public class Item
        {
            [JsonProperty("id")]
            public ulong Id { get; set; }

            [JsonProperty("excluding")]
            public CharacterWishlist ExcludingCharacters { get; set; } = new CharacterWishlist();

            [JsonProperty("excluding_anime")]
            public AnimeWishlist ExcludingAnime { get; set; } = new AnimeWishlist();
        }

        [JsonProperty("items")]
        public List<Item> Items { get; set; } = new List<Item>();
    }
}