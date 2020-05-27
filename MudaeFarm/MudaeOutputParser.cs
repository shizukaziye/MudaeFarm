using System;
using System.Text.RegularExpressions;

namespace MudaeFarm
{
    public interface IMudaeOutputParser
    {
        bool TryParseTime(string s, out TimeSpan time);
        bool TryParseRollLimited(string s, out TimeSpan resetTime);
    }

    public class EnglishMudaeOutputParser : IMudaeOutputParser
    {
        const RegexOptions _regexOptions = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase;

        static readonly Regex _timeRegex = new Regex(@"((?<hour>\d+)h\s*)?(?<minute>\d+)(\**)?\s*min", _regexOptions);

        public bool TryParseTime(string s, out TimeSpan time)
        {
            var match = _timeRegex.Match(s);

            if (int.TryParse(match.Groups["minute"].Value, out var minutes))
            {
                // hour is optional
                int.TryParse(match.Groups["hour"].Value, out var hours);

                time = new TimeSpan(hours, minutes, 0);
                return true;
            }

            time = default;
            return false;
        }

        static readonly Regex _rollLimitedRegex = new Regex(@"roulette\s+is\s+limited", _regexOptions);

        public bool TryParseRollLimited(string s, out TimeSpan resetTime) => _rollLimitedRegex.IsMatch(s) & TryParseTime(s, out resetTime);
    }
}