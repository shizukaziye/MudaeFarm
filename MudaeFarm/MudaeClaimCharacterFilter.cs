using System;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MudaeFarm
{
    public readonly struct CharacterInfo
    {
        public readonly string Name;
        public readonly string Anime;

        public CharacterInfo(string name, string anime)
        {
            Name  = name?.Trim() ?? "";
            Anime = anime?.Trim() ?? "";
        }
    }

    public interface IMudaeClaimCharacterFilter
    {
        bool IsWished(CharacterInfo character);
    }

    public class MudaeClaimCharacterFilter : IMudaeClaimCharacterFilter
    {
        readonly ILogger<MudaeClaimCharacterFilter> _logger;

        public MudaeClaimCharacterFilter(IOptionsMonitor<CharacterWishlist> characterWishlist, IOptionsMonitor<AnimeWishlist> animeWishlist, ILogger<MudaeClaimCharacterFilter> logger)
        {
            _logger = logger;

            ResetNameMatch(characterWishlist.CurrentValue);
            ResetAnimeMatch(animeWishlist.CurrentValue);

            characterWishlist.OnChange(ResetNameMatch);
            animeWishlist.OnChange(ResetAnimeMatch);
        }

        NameMatch _name;
        AnimeMatch _anime;

        void ResetNameMatch(CharacterWishlist wishlist)
        {
            try
            {
                _name = new NameMatch(wishlist);
            }
            catch (Exception e)
            {
                _name = default;
                _logger.LogWarning(e, "Could not build regex for character wishlist.");
            }
        }

        void ResetAnimeMatch(AnimeWishlist wishlist)
        {
            try
            {
                _anime = new AnimeMatch(wishlist);
            }
            catch (Exception e)
            {
                _anime = default;
                _logger.LogWarning(e, "Could not build regex for anime wishlist.");
            }
        }

        readonly struct NameMatch
        {
            readonly Regex _name;

            public NameMatch(CharacterWishlist wishlist)
            {
                if (wishlist.Items.Count == 0)
                {
                    _name = null;
                    return;
                }

                var builder = new StringBuilder();
                var first   = true;

                foreach (var item in wishlist.Items)
                {
                    if (first)
                        first = false;
                    else
                        builder.Append('|');

                    builder.Append('(')
                           .Append(item.Name)
                           .Append(')');
                }

                _name = new Regex(builder.ToString(), RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            }

            public bool IsMatch(CharacterInfo character) => _name?.IsMatch(character.Name ?? "") == true;
        }

        readonly struct AnimeMatch
        {
            readonly struct Item
            {
                readonly Regex _anime;
                readonly NameMatch _excluding;

                public Item(AnimeWishlist.Item item)
                {
                    _anime     = new Regex(item.Name, RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    _excluding = item.Excluding == null ? default : new NameMatch(item.Excluding);
                }

                public bool IsMatch(CharacterInfo character) => _anime?.IsMatch(character.Anime ?? "") == true && !_excluding.IsMatch(character);
            }

            readonly Item[] _items;

            public AnimeMatch(AnimeWishlist wishlist)
            {
                _items = new Item[wishlist.Items.Count];

                for (var i = 0; i < _items.Length; i++)
                    _items[i] = new Item(wishlist.Items[i]);
            }

            public bool IsMatch(CharacterInfo character)
            {
                for (var i = 0; i < _items.Length; i++)
                {
                    if (_items[i].IsMatch(character))
                        return true;
                }

                return false;
            }
        }

        public bool IsWished(CharacterInfo character) => _name.IsMatch(character) || _anime.IsMatch(character);
    }
}