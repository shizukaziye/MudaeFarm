using System;
using Newtonsoft.Json;

namespace MudaeFarm
{
    public class MudaeState
    {
        [JsonProperty("claim_reset")]
        public DateTime? ClaimReset { get; set; }

        [JsonProperty("rolls_left")]
        public int RollsLeft { get; set; }

        [JsonProperty("rolls_reset")]
        public DateTime? RollsReset { get; set; }

        [JsonProperty("roll_interval_avg")]
        public TimeSpan? AverageRollInterval { get; set; }

        [JsonProperty("kakera_reset")]
        public DateTime? KakeraReset { get; set; }

        [JsonProperty("kakera_power")]
        public double KakeraPower { get; set; }

        [JsonProperty("kakera_power_consumption")]
        public double KakeraPowerConsumption { get; set; }

        [JsonProperty("kakera_stock")]
        public int KakeraStock { get; set; }

        [JsonProperty("kakera_reset_daily")]
        public DateTime? KakeraDailyReset { get; set; }

#region Meta

        [JsonIgnore]
        public DateTime LastUpdatedTime { get; set; }

#endregion
    }
}