using System.Linq;
using NUnit.Framework;
using YuanHaiLu.Core;

namespace YuanHaiLu.Tests.EditMode
{
    public class PlayerAppearanceTests
    {
        [Test]
        public void RosterContainsTwoGendersBySixProfessions()
        {
            Assert.That(PlayerAppearance.All.Count, Is.EqualTo(12));
            Assert.That(PlayerAppearance.All.Select(value => value.ArtId).Distinct().Count(), Is.EqualTo(12));

            foreach (PlayerGender gender in System.Enum.GetValues(typeof(PlayerGender)))
            foreach (PlayerProfession profession in System.Enum.GetValues(typeof(PlayerProfession)))
            {
                var appearance = new PlayerAppearance(gender, profession);
                Assert.That(PlayerAppearance.TryParse(appearance.ArtId, out var parsed), Is.True);
                Assert.That(parsed, Is.EqualTo(appearance));
            }
        }

        [TestCase("player_male_swordsman", PlayerGender.Male, PlayerProfession.Swordsman)]
        [TestCase("player_female_hidden_weapon", PlayerGender.Female, PlayerProfession.HiddenWeapon)]
        [TestCase("player_female_mystic", PlayerGender.Female, PlayerProfession.Mystic)]
        public void StableArtIdRoundTrips(
            string artId,
            PlayerGender expectedGender,
            PlayerProfession expectedProfession)
        {
            Assert.That(PlayerAppearance.TryParse(artId, out var appearance), Is.True);
            Assert.That(appearance.Gender, Is.EqualTo(expectedGender));
            Assert.That(appearance.Profession, Is.EqualTo(expectedProfession));
            Assert.That(appearance.ArtId, Is.EqualTo(artId));
        }

        [Test]
        public void InvalidArtIdIsRejectedAndDefaultIsFixedMaleSwordsman()
        {
            Assert.That(PlayerAppearance.TryParse("missing_actor", out _), Is.False);
            // docs/15：单主角 MVP 固定男性剑客身体；其余 11 套外观保留在库中。
            Assert.That(PlayerAppearance.Default.ArtId, Is.EqualTo("player_male_swordsman"));
        }
    }
}
