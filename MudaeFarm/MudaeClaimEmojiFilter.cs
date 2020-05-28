using System;
using System.Collections.Generic;
using Disqord;
using Microsoft.Extensions.Options;

namespace MudaeFarm
{
    public interface IMudaeClaimEmojiFilter
    {
        bool IsClaimEmoji(IEmoji emoji);
        bool IsKakeraEmoji(IEmoji emoji, out KakeraType kakera);
    }

    public class MudaeClaimEmojiFilter : IMudaeClaimEmojiFilter
    {
        readonly IOptionsMonitor<ClaimingOptions> _options;

        public MudaeClaimEmojiFilter(IOptionsMonitor<ClaimingOptions> options)
        {
            _options = options;
        }

        // https://emojipedia.org/hearts/
        static readonly IEmoji[] _heartEmojis =
        {
            new LocalEmoji("\uD83D\uDC98"), // cupid
            new LocalEmoji("\uD83D\uDC9D"), // gift_heart
            new LocalEmoji("\uD83D\uDC96"), // sparkling_heart
            new LocalEmoji("\uD83D\uDC97"), // heartpulse
            new LocalEmoji("\uD83D\uDC93"), // heartbeat
            new LocalEmoji("\uD83D\uDC9E"), // revolving_hearts
            new LocalEmoji("\uD83D\uDC95"), // two_hearts
            new LocalEmoji("\uD83D\uDC9F"), // heart_decoration
            new LocalEmoji("\u2764"),       // heart
            new LocalEmoji("\uD83E\uDDE1"), // heart (orange)
            new LocalEmoji("\uD83D\uDC9B"), // yellow_heart
            new LocalEmoji("\uD83D\uDC9A"), // green_heart
            new LocalEmoji("\uD83D\uDC99"), // blue_heart
            new LocalEmoji("\uD83D\uDC9C"), // purple_heart
            new LocalEmoji("\uD83E\uDD0E"), // heart (brown)
            new LocalEmoji("\uD83D\uDDA4"), // heart (black)
            new LocalEmoji("\uD83E\uDD0D"), // heart (white)
            new LocalEmoji("\u2665")        // hearts
        };

        public bool IsClaimEmoji(IEmoji emoji)
        {
            if (_options.CurrentValue.CustomEmotes)
                return true;

            return Array.IndexOf(_heartEmojis, emoji) != -1;
        }

        static readonly Dictionary<string, KakeraType> _kakeraMap = new Dictionary<string, KakeraType>(StringComparer.OrdinalIgnoreCase)
        {
            { "kakerap", KakeraType.Purple },
            { "kakera", KakeraType.Blue },
            { "kakerat", KakeraType.Teal },
            { "kakerag", KakeraType.Green },
            { "kakeray", KakeraType.Yellow },
            { "kakerao", KakeraType.Orange },
            { "kakerar", KakeraType.Red },
            { "kakeraw", KakeraType.Rainbow }
        };

        public bool IsKakeraEmoji(IEmoji emoji, out KakeraType kakera) => emoji is ICustomEmoji & _kakeraMap.TryGetValue(emoji.Name, out kakera);
    }
}