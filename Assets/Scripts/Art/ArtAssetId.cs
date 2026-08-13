using System.Text.RegularExpressions;

namespace YuanHaiLu.Art
{
    /// <summary>
    /// Shared stable-ID contract for authored art and its runtime catalogs.
    /// </summary>
    public static class ArtAssetId
    {
        private static readonly Regex Pattern = new Regex(
            "^[a-z][a-z0-9_]{2,63}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static bool IsValid(string id)
        {
            return !string.IsNullOrEmpty(id) && Pattern.IsMatch(id);
        }

        public static class CharacterCategory
        {
            public const string Player = "player";
            public const string Named = "named";
            public const string Npc = "npc";
            public const string Enemy = "enemy";
            public const string Boss = "boss";
        }
    }
}
