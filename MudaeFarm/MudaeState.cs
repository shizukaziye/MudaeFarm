using System;
using Newtonsoft.Json;

namespace MudaeFarm
{
    public class MudaeState
    {
        /// <summary>
        /// Time when the claim cooldown resets.
        /// </summary>
        [JsonProperty("claim_reset")]
        public DateTime ClaimReset { get; set; } = DateTime.MaxValue;

        /// <summary>
        /// Whether we can claim right now (cooldown).
        /// </summary>
        [JsonProperty("claim_can")]
        public bool CanClaim { get; set; }

        /// <summary>
        /// Number of rolls left until the next roll count reset.
        /// </summary>
        [JsonProperty("rolls_left")]
        public int RollsLeft { get; set; }

        /// <summary>
        /// Time when the roll count resets.
        /// </summary>
        [JsonProperty("rolls_reset")]
        public DateTime RollsReset { get; set; } = DateTime.MaxValue;

        [JsonProperty("daily_reset")]
        public DateTime DailyReset { get; set; } = DateTime.MaxValue;

        [JsonProperty("daily_reset_can")]
        public bool CanDaily { get; set; }

        /// <summary>
        /// Time when the kakera claim cooldown resets.
        /// </summary>
        [JsonProperty("kakera_reset")]
        public DateTime KakeraReset { get; set; } = DateTime.MaxValue;

        /// <summary>
        /// Remaining kakera power.
        /// </summary>
        [JsonProperty("kakera_power")]
        public double KakeraPower { get; set; }

        /// <summary>
        /// Power consumed when a kakera is claimed.
        /// </summary>
        [JsonProperty("kakera_consumption")]
        public double KakeraConsumption { get; set; }

        /// <summary>
        /// Whether we can claim kakera right now (power).
        /// </summary>
        [JsonProperty("kakera_can")]
        public bool CanKakera => KakeraPower > 0 && KakeraPower - KakeraConsumption >= 0;

        /// <summary>
        /// Amount of kakera in stock.
        /// </summary>
        [JsonProperty("kakera_stock")]
        public int KakeraStock { get; set; }

        /// <summary>
        /// Time when $dailykakera command resets.
        /// </summary>
        [JsonProperty("kakera_reset_daily")]
        public DateTime KakeraDailyReset { get; set; } = DateTime.MaxValue;

        /// <summary>
        /// Whether we can do $dailykakera right now (cooldown).
        /// </summary>
        [JsonProperty("kakera_reset_daily_can")]
        public bool CanKakeraDaily { get; set; }

#region Meta

        /// <summary>
        /// Force a state refresh?
        /// </summary>
        [JsonIgnore]
        public bool ForceNextRefresh { get; set; }

        /// <summary>
        /// Last state refresh time.
        /// </summary>
        [JsonIgnore]
        public DateTime LastRefresh { get; set; }

#endregion

        public MudaeState Clone() => JsonConvert.DeserializeObject<MudaeState>(JsonConvert.SerializeObject(this));
    }
}