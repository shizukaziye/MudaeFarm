using System;
using System.Text.RegularExpressions;

namespace MudaeFarm
{
    public interface IMudaeOutputParser
    {
        bool TryParseTime(string s, out TimeSpan time);
    }

    public class EnglishMudaeOutputParser : IMudaeOutputParser
    {
        readonly Regex _timeRegex = new Regex(@"((?<hour>\d+)h\s*)?(?<minute>\d+)(\**)?\s*min", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public bool TryParseTime(string s, out TimeSpan time)
        {
            var match = _timeRegex.Match(s);

            if (int.TryParse(match.Groups["minute"].Value, out var minutes))
            {
                // hour is optional
                int.TryParse(match.Groups["hour"].Value, out var hours);

                time = new TimeSpan(0, hours, minutes);
                return true;
            }

            time = default;
            return false;
        }
    }
}