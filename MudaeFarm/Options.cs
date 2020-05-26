using System;
using System.Collections.Generic;
using System.Linq;
using Disqord;
using Newtonsoft.Json;

namespace MudaeFarm
{
    public class GeneralOptions
    {
        [JsonProperty("fallback_status")]
        public UserStatus FallbackStatus { get; set; } = UserStatus.Idle;

        [JsonProperty("state_update_command")]
        public string StateUpdateCommand { get; set; } = "$tu";

        [JsonProperty("state_update_auto")]
        public bool StateUpdateAuto { get; set; }

        [JsonProperty("auto_update")]
        public bool AutoUpdate { get; set; } = true;
    }

    public class ClaimingOptions
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("delay_seconds")]
        public double Delay { get; set; } = 0.2;

        [JsonProperty("kakera_delay_seconds")]
        public double KakeraDelay { get; set; } = 0.2;

        [JsonProperty("kakera_targets")]
        public HashSet<KakeraType> KakeraTargets { get; set; } = new HashSet<KakeraType>(Enum.GetValues(typeof(KakeraType)).Cast<KakeraType>());

        [JsonProperty("enable_custom_emotes")]
        public bool CustomEmotes { get; set; }
    }

    public class RollingOptions
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; } = "$w";

        [JsonProperty("roll_with_no_claim")]
        public bool RollWithNoClaim { get; set; }

        [JsonProperty("daily_kakera_enabled")]
        public bool DailyKakeraEnabled { get; set; }

        [JsonProperty("daily_kakera_command")]
        public string DailyKakeraCommand { get; set; } = "$dk";

        [JsonProperty("typing_delay_seconds")]
        public double TypingDelay { get; set; } = 0.3;

        [JsonProperty("interval_override_minutes")]
        public double? IntervalOverrideMinutes { get; set; }
    }

    public class CharacterWishlist
    {
        public class Item
        {
            [JsonProperty("name")]
            public string Name { get; set; }
        }

        [JsonProperty("items")]
        public List<Item> Items { get; set; }
    }

    public class AnimeWishlist
    {
        public class Item
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("excluding")]
            public CharacterWishlist Excluding { get; set; }
        }

        [JsonProperty("items")]
        public List<Item> Items { get; set; }
    }

    public class BotChannelList
    {
        public class Item
        {
            [JsonProperty("id")]
            public ulong Id { get; set; }
        }

        [JsonProperty("items")]
        public List<Item> Items { get; set; }
    }

    public class ClaimReplyList
    {
        public class Item
        {
            [JsonProperty("content")]
            public string Content { get; set; }
        }

        [JsonProperty("items")]
        public List<Item> Items { get; set; }
    }
}