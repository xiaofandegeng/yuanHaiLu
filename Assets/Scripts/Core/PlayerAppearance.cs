using System;
using System.Collections.Generic;

namespace YuanHaiLu.Core
{
    public enum PlayerGender
    {
        Male,
        Female
    }

    public enum PlayerProfession
    {
        Swordsman,
        Boxer,
        HiddenWeapon,
        Healer,
        Scholar,
        Mystic
    }

    /// <summary>
    /// Immutable player appearance selection backed by a stable formal-art ID.
    /// </summary>
    public readonly struct PlayerAppearance : IEquatable<PlayerAppearance>
    {
        // docs/15：单主角 MVP 固定男性剑客身体；12 套外观资源仍在库中但不再对外选择。
        public const string DefaultArtId = "player_male_swordsman";

        private static readonly PlayerGender[] Genders =
            (PlayerGender[])Enum.GetValues(typeof(PlayerGender));
        private static readonly PlayerProfession[] Professions =
            (PlayerProfession[])Enum.GetValues(typeof(PlayerProfession));
        private static readonly IReadOnlyList<PlayerAppearance> AllValues = BuildAll();

        public PlayerGender Gender { get; }
        public PlayerProfession Profession { get; }
        public string ArtId => $"player_{GenderToken(Gender)}_{ProfessionToken(Profession)}";

        public static PlayerAppearance Default =>
            new PlayerAppearance(PlayerGender.Male, PlayerProfession.Swordsman);
        public static IReadOnlyList<PlayerAppearance> All => AllValues;

        public PlayerAppearance(PlayerGender gender, PlayerProfession profession)
        {
            if (!Enum.IsDefined(typeof(PlayerGender), gender))
                throw new ArgumentOutOfRangeException(nameof(gender));
            if (!Enum.IsDefined(typeof(PlayerProfession), profession))
                throw new ArgumentOutOfRangeException(nameof(profession));
            Gender = gender;
            Profession = profession;
        }

        public static bool TryParse(string artId, out PlayerAppearance appearance)
        {
            foreach (var value in AllValues)
            {
                if (!string.Equals(value.ArtId, artId, StringComparison.Ordinal))
                    continue;
                appearance = value;
                return true;
            }
            appearance = default;
            return false;
        }

        public static PlayerAppearance ParseOrDefault(string artId)
        {
            return TryParse(artId, out var appearance) ? appearance : Default;
        }

        public string DisplayName
        {
            get
            {
                string gender = Gender == PlayerGender.Male ? "男" : "女";
                string profession = Profession switch
                {
                    PlayerProfession.Swordsman => "剑客",
                    PlayerProfession.Boxer => "拳师",
                    PlayerProfession.HiddenWeapon => "暗器",
                    PlayerProfession.Healer => "医者",
                    PlayerProfession.Scholar => "儒生",
                    PlayerProfession.Mystic => "术士",
                    _ => throw new ArgumentOutOfRangeException()
                };
                return gender + " · " + profession;
            }
        }

        public bool Equals(PlayerAppearance other) =>
            Gender == other.Gender && Profession == other.Profession;
        public override bool Equals(object obj) => obj is PlayerAppearance other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((int)Gender, (int)Profession);
        public static bool operator ==(PlayerAppearance left, PlayerAppearance right) => left.Equals(right);
        public static bool operator !=(PlayerAppearance left, PlayerAppearance right) => !left.Equals(right);
        public override string ToString() => ArtId;

        private static IReadOnlyList<PlayerAppearance> BuildAll()
        {
            var values = new List<PlayerAppearance>(Genders.Length * Professions.Length);
            foreach (var gender in Genders)
            foreach (var profession in Professions)
                values.Add(new PlayerAppearance(gender, profession));
            return values.AsReadOnly();
        }

        private static string GenderToken(PlayerGender gender) =>
            gender == PlayerGender.Male ? "male" : "female";

        private static string ProfessionToken(PlayerProfession profession)
        {
            return profession switch
            {
                PlayerProfession.Swordsman => "swordsman",
                PlayerProfession.Boxer => "boxer",
                PlayerProfession.HiddenWeapon => "hidden_weapon",
                PlayerProfession.Healer => "healer",
                PlayerProfession.Scholar => "scholar",
                PlayerProfession.Mystic => "mystic",
                _ => throw new ArgumentOutOfRangeException(nameof(profession))
            };
        }
    }
}
