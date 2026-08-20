using System;
using System.Collections.Generic;
using UnityEngine;

namespace YuanHaiLu.Core
{
    public enum WeaponStyleKind
    {
        Sword,      // 长剑：中距均衡三连击 + 前冲剑气
        Gauntlets,  // 拳套：短距快连击 + 短距冲拳
        Dart        // 飞镖：远程扇形三镖 + 弱近战
    }

    /// <summary>
    /// 单主角 MVP 的武器流派选择（docs/15）。
    /// 三种流派共用同一副男性身体（player_male_swordsman），
    /// 只有普攻判定、连击节奏、一个主动技能与数值不同。
    /// 复审 P2：全部数值收敛到一张不可变配置表，新增流派只改表不改 switch。
    /// </summary>
    public readonly struct WeaponStyle : IEquatable<WeaponStyle>
    {
        public const string DefaultStyleId = "sword";

        /// <summary>流派档案：ID、显示文案、主动技、武器小图与全部普攻数值。
        /// 复审 P2：私有 set 只在本类 BuildTable 的对象初始化器里赋值，构建后对所有外部代码（含包含类型）只读。
        /// 表必须由本类构建：C# 包含类型访问不到嵌套类型的私有 set。</summary>
        private sealed class StyleProfile
        {
            public string Id { get; private set; }
            public string DisplayName { get; private set; }
            public string Description { get; private set; }
            public string ActiveSkillId { get; private set; }
            public string WeaponSpriteId { get; private set; }
            public float MeleeRange { get; private set; }
            public Vector2 MeleeBoxSize { get; private set; }
            public int MaxCombo { get; private set; }
            public float AttackDuration { get; private set; }
            public float MeleeDamageMultiplier { get; private set; }
            public Color SlashColor { get; private set; }

            public static Dictionary<WeaponStyleKind, StyleProfile> BuildTable()
            {
                return new Dictionary<WeaponStyleKind, StyleProfile>
                {
                    {
                        WeaponStyleKind.Sword, new StyleProfile
                        {
                            Id = "sword",
                            WeaponSpriteId = "weapon_sword",
                            DisplayName = "长剑",
                            Description = "中距均衡，三段连斩，剑气前冲破敌",
                            ActiveSkillId = "sword_qi_wave",
                            MeleeRange = 1.2f,
                            MeleeBoxSize = new Vector2(1.0f, 0.8f),
                            MaxCombo = 3,
                            AttackDuration = 0.5f,
                            MeleeDamageMultiplier = 1f,
                            SlashColor = new Color(0.7f, 0.9f, 1f, 0.8f),
                        }
                    },
                    {
                        WeaponStyleKind.Gauntlets, new StyleProfile
                        {
                            Id = "gauntlets",
                            WeaponSpriteId = "weapon_gauntlets",
                            DisplayName = "拳套",
                            Description = "短距疾风连拳，一记冲拳贴身突进",
                            ActiveSkillId = "fist_dash_punch",
                            MeleeRange = 0.8f,
                            MeleeBoxSize = new Vector2(0.8f, 0.7f),
                            MaxCombo = 5,
                            AttackDuration = 0.32f,
                            MeleeDamageMultiplier = 0.65f,
                            SlashColor = new Color(1f, 0.7f, 0.3f, 0.8f),
                        }
                    },
                    {
                        WeaponStyleKind.Dart, new StyleProfile
                        {
                            Id = "dart",
                            WeaponSpriteId = "weapon_dart",
                            DisplayName = "飞镖",
                            Description = "远程点杀，扇形三镖齐发，近身较弱",
                            ActiveSkillId = "dart_fan_throw",
                            MeleeRange = 0.9f,
                            MeleeBoxSize = new Vector2(0.7f, 0.6f),
                            MaxCombo = 3,
                            AttackDuration = 0.45f,
                            MeleeDamageMultiplier = 0.55f,
                            SlashColor = new Color(0.85f, 0.8f, 1f, 0.8f),
                        }
                    },
                };
            }
        }

        private static readonly Dictionary<WeaponStyleKind, StyleProfile> ProfilesByKind =
            StyleProfile.BuildTable();

        private static readonly WeaponStyleKind[] Kinds =
            (WeaponStyleKind[])Enum.GetValues(typeof(WeaponStyleKind));
        private static readonly IReadOnlyList<WeaponStyle> AllValues = BuildAll();

        public WeaponStyleKind Kind { get; }

        public string StyleId => Profile.Id;
        public string DisplayName => Profile.DisplayName;
        public string Description => Profile.Description;

        /// <summary>新游戏随流派直接学会的唯一主动技能。</summary>
        public string ActiveSkillId => Profile.ActiveSkillId;

        /// <summary>流派武器小图（菜单图标与玩家武器层共用，MvpArtCatalog 持久精灵）。</summary>
        public string WeaponSpriteId => Profile.WeaponSpriteId;

        // === 普攻档案（PlayerCombat 消费） ===
        public float MeleeRange => Profile.MeleeRange;
        public Vector2 MeleeBoxSize => Profile.MeleeBoxSize;
        public int MaxCombo => Profile.MaxCombo;
        public float AttackDuration => Profile.AttackDuration;
        public float MeleeDamageMultiplier => Profile.MeleeDamageMultiplier;
        public Color SlashColor => Profile.SlashColor;

        public static WeaponStyle Default => new WeaponStyle(WeaponStyleKind.Sword);
        public static IReadOnlyList<WeaponStyle> All => AllValues;

        public WeaponStyle(WeaponStyleKind kind)
        {
            if (!Enum.IsDefined(typeof(WeaponStyleKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
        }

        private StyleProfile Profile =>
            ProfilesByKind.TryGetValue(Kind, out var profile)
                ? profile
                : throw new ArgumentOutOfRangeException(
                      nameof(Kind), Kind, "weapon style profile is missing");

        public static bool TryParse(string styleId, out WeaponStyle style)
        {
            foreach (var value in AllValues)
            {
                if (!string.Equals(value.StyleId, styleId, StringComparison.Ordinal))
                    continue;
                style = value;
                return true;
            }
            style = default;
            return false;
        }

        /// <summary>非法或缺失的流派 ID 一律回退长剑。</summary>
        public static WeaponStyle ParseOrDefault(string styleId)
        {
            return TryParse(styleId, out var style) ? style : Default;
        }

        public bool Equals(WeaponStyle other) => Kind == other.Kind;
        public override bool Equals(object obj) => obj is WeaponStyle other && Equals(other);
        public override int GetHashCode() => (int)Kind;
        public static bool operator ==(WeaponStyle left, WeaponStyle right) => left.Equals(right);
        public static bool operator !=(WeaponStyle left, WeaponStyle right) => !left.Equals(right);
        public override string ToString() => StyleId;

        private static IReadOnlyList<WeaponStyle> BuildAll()
        {
            var values = new List<WeaponStyle>(Kinds.Length);
            foreach (var kind in Kinds)
                values.Add(new WeaponStyle(kind));
            return values.AsReadOnly();
        }
    }
}
