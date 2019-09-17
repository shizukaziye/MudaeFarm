using System.Text.RegularExpressions;
using Discord;

namespace MudaeFarm
{
    public static class MudaeInfo
    {
        // main Mudae bot ID
        const ulong _id = 432610292342587392;

        // Mudae's maid username regex
        static readonly Regex _maidRegex = new Regex(@"^Mudamaid\s*\d+$", RegexOptions.Singleline | RegexOptions.Compiled);

        public static bool IsMudae(IUser user) => user.IsBot && (user.Id == _id || _maidRegex.IsMatch(user.Username));
    }
}
