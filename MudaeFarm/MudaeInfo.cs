using System;
using System.Text.RegularExpressions;
using Discord;

namespace MudaeFarm
{
    public static class MudaeInfo
    {
        // Mudae bot IDs
        static readonly ulong[] _ids =
        {
            432610292342587392, // main Mudae bot
            479206206725160960  // the first maid "Mudamaid" which doesn't match _maidRegex
        };

        // Mudae's maid username regex
        static readonly Regex _maidRegex = new Regex(@"^Mudae?maid\s*\d+$", RegexOptions.Singleline | RegexOptions.Compiled);

        public static bool IsMudae(IUser user) => user.IsBot && (Array.IndexOf(_ids, user.Id) != -1 || _maidRegex.IsMatch(user.Username));
    }
}