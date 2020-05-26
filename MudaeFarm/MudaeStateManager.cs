using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MudaeFarm
{
    public interface IMudaeState
    {
        /// <summary>
        /// Time when the claim cooldown resets.
        /// </summary>
        DateTime? ClaimReset { get; }

        /// <summary>
        /// Whether we can claim right now (cooldown).
        /// </summary>
        bool CanClaim { get; }

        /// <summary>
        /// Time when the roll count resets.
        /// </summary>
        DateTime? RollsReset { get; }

        /// <summary>
        /// Number of rolls left until the next roll count reset.
        /// </summary>
        int RollsLeft { get; }

        /// <summary>
        /// Time when the kakera claim cooldown resets.
        /// </summary>
        DateTime? KakeraReset { get; }

        /// <summary>
        /// Remaining kakera power.
        /// </summary>
        double KakeraPower { get; }

        /// <summary>
        /// Power consumed when a kakera is claimed.
        /// </summary>
        double KakeraConsumption { get; }

        /// <summary>
        /// Whether we can claim kakera right now (power).
        /// </summary>
        bool CanKakera => KakeraPower > 0 && KakeraPower - KakeraConsumption >= 0;

        /// <summary>
        /// Amount of kakera in stock.
        /// </summary>
        int KakeraStock { get; }

        /// <summary>
        /// Time when $dailykakera command resets.
        /// </summary>
        DateTime? KakeraDailyReset { get; }

        /// <summary>
        /// Whether we can do $dailykakera right now (cooldown).
        /// </summary>
        bool CanKakeraDaily { get; }
    }

    public class MudaeStateManager : BackgroundService, IMudaeState
    {
        public DateTime? ClaimReset { get; set; }
        public bool CanClaim { get; set; }
        public DateTime? RollsReset { get; set; }
        public int RollsLeft { get; set; }
        public DateTime? KakeraReset { get; set; }
        public double KakeraPower { get; set; }
        public double KakeraConsumption { get; set; }
        public int KakeraStock { get; set; }
        public DateTime? KakeraDailyReset { get; set; }
        public bool CanKakeraDaily { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) { }
    }
}