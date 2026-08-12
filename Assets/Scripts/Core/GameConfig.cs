using UnityEngine;

namespace YuanHaiLu.Core
{
    /// <summary>
    /// 游戏全局常量与配置
    /// </summary>
    public static class GameConfig
    {
        // === 分辨率 ===
        public const int NATIVE_WIDTH = 480;     // 像素内部分辨率
        public const int NATIVE_HEIGHT = 270;
        public const int PIXELS_PER_UNIT = 16;   // 每单位像素数（匹配16x16瓦片）
        public const float SPRITE_PPU = 16f;     // 瓦片/环境 PPU
        public const float CHARACTER_PPU = 16f;  // 角色 PPU（48x48角色 = 3单位高）

        // === 移动 ===
        public const float MOVE_SPEED = 3.5f;         // 基础移动速度（单位/秒）
        public const float DASH_SPEED = 8f;            // 冲刺速度
        public const float DASH_DURATION = 0.2f;       // 冲刺持续（秒）
        public const float DASH_COOLDOWN = 1.0f;       // 冲刺冷却

        // === 战斗 ===
        public const float ATTACK_DURATION = 0.5f;     // 普攻动画时长
        public const float HEAVY_ATTACK_DURATION = 0.8f; // 重击动画时长
        public const float ATTACK_COMBO_WINDOW = 0.3f;  // 连击输入窗口
        public const int MAX_COMBO = 3;                  // 最大连击数

        // === 世界 ===
        public const int TILE_SIZE = 16;                // 瓦片尺寸（像素）
        public const float INTERACT_RANGE = 1.5f;       // 交互距离

        // === 图层名称 ===
        public const string LAYER_GROUND = "Ground";
        public const string LAYER_ENVIRONMENT = "Environment";
        public const string LAYER_CHARACTER = "Character";
        public const string LAYER_INTERACTABLE = "Interactable";
        public const string LAYER_UI = "UI";

        // === 排序层 ===
        public const string SORTING_GROUND = "Ground";
        public const string SORTING_ENVIRONMENT = "Environment";
        public const string SORTING_CHARACTER = "Character";
        public const string SORTING_FOREGROUND = "Foreground";
        public const string SORTING_UI = "UI";

        // === 标签 ===
        public const string TAG_PLAYER = "Player";
        public const string TAG_ENEMY = "Enemy";
        public const string TAG_NPC = "NPC";
        public const string TAG_INTERACTABLE = "Interactable";
    }
}
