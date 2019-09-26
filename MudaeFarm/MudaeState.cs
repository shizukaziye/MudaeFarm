using System;
using Newtonsoft.Json;

namespace MudaeFarm
{
    public class MudaeState
    {
        [JsonProperty("claim_reset")]
        public DateTime ClaimReset { get; set; } = DateTime.MaxValue;

        [JsonProperty("claim_can")]
        public bool CanClaim { get; set; }

        [JsonProperty("rolls_left")]
        public int RollsLeft { get; set; }

        [JsonProperty("rolls_reset")]
        public DateTime RollsReset { get; set; } = DateTime.MaxValue;

        [JsonProperty("kakera_reset")]
        public DateTime KakeraReset { get; set; } = DateTime.MaxValue;

        [JsonProperty("kakera_power")]
        public double KakeraPower { get; set; }

        [JsonProperty("kakera_consumption")]
        public double KakeraConsumption { get; set; }

        [JsonProperty("kakera_stock")]
        public int KakeraStock { get; set; }

        [JsonProperty("kakera_reset_daily")]
        public DateTime KakeraDailyReset { get; set; } = DateTime.MaxValue;

        [JsonProperty("kakera_reset_daily_can")]
        public bool CanKakeraDailyReset { get; set; }

#region Meta

        [JsonIgnore]
        public bool ForceNextRefresh { get; set; }

#endregion
    }
}