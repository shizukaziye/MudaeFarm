using System;
using System.Text.RegularExpressions;
using Discord;

namespace MudaeFarm
{
    public static class TimersUpParser
    {
        static readonly Regex _timeRegex = new Regex(@"((?<hour>\d\d?)h\s*)?(?<minute>\d\d?)\**\s*min", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        static bool TryParseTime(string str, out TimeSpan time)
        {
            var match = _timeRegex.Match(str);

            if (!match.Success)
            {
                time = TimeSpan.Zero;
                return false;
            }

            int.TryParse(match.Groups["hour"].Value, out var hours);
            int.TryParse(match.Groups["minute"].Value, out var minutes);

            time = new TimeSpan(0, hours, minutes);
            return true;
        }

        static readonly Regex _intRegex = new Regex(@"\d+", RegexOptions.Compiled | RegexOptions.Singleline);

        static bool TryParseInt(string str, out int value)
            => int.TryParse(_intRegex.Match(str).Value, out value);

        public static bool TryParse(IDiscordClient client, IMessage message, MudaeState state)
        {
            if (!message.Content.StartsWith($"**{client.CurrentUser.Username}**"))
                return false;

            var now    = DateTime.Now;
            var failed = 0;

            var lines = message.Content.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line1 in lines)
            {
                var line = line1.ToLowerInvariant();

                if (line.Contains("claim") && line.Contains("reset"))
                {
                    if (TryParseTime(line, out var time))
                        state.ClaimReset = now + time;
                    else
                        state.ClaimReset = null;
                }

                else if (line.Contains("rolls") && line.Contains("left"))
                {
                    TryParseInt(line, out var value);

                    state.RollsLeft = value;
                }

                else if (line.Contains("rolls") && line.Contains("reset"))
                {
                    if (TryParseTime(line, out var time))
                        state.RollsReset = now + time;
                    else
                        state.RollsReset = null;
                }

                else if (line.Contains("react") && line.Contains("kakera"))
                {
                    if (TryParseTime(line, out var time))
                        state.KakeraReset = now + time;
                    else if (line.Contains("now"))
                        state.KakeraReset = now;
                    else
                        state.KakeraReset = null;
                }

                else if (line.Contains("power") && line.Contains("kakera"))
                {
                    var parts = line.Split(new[] { '(' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var part in parts)
                    {
                        if (part.Contains("power"))
                        {
                            if (TryParseInt(part, out var value))
                                state.KakeraPower = value / 100.0;
                            else
                                state.KakeraPower = 0;
                        }

                        else if (part.Contains("consume"))
                        {
                            if (TryParseInt(part, out var value))
                                state.KakeraPowerConsumption = value / 100.0;
                            else
                                state.KakeraPowerConsumption = 0;
                        }
                    }
                }

                else if (line.Contains("stock") && line.Contains("kakera"))
                {
                    TryParseInt(line, out var value);

                    state.KakeraStock = value;
                }

                else if (line.Contains("$dk"))
                {
                    if (TryParseTime(line, out var time))
                        state.KakeraDailyReset = now + time;
                    else if (line.Contains("ready"))
                        state.KakeraDailyReset = now;
                    else
                        state.KakeraDailyReset = null;
                }

                else
                {
                    ++failed;
                }
            }

            if (state.RollsLeft != 0 && state.RollsReset != null)
                state.AverageRollInterval = new TimeSpan(state.RollsReset.Value.Ticks / state.RollsLeft);

            var success = failed < lines.Length / 2;

            if (success)
                state.LastUpdatedTime = now;

            return success;
        }
    }
}