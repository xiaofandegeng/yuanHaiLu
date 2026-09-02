using UnityEngine;
using UnityEditor;

namespace YuanHaiLu.Editor
{
    /// <summary>
    /// 编辑器工具 — 批量配置精灵导入设置
    /// 菜单: Tools/渊海录/
    /// </summary>
    public static class PixelArtImporter
    {
        private const int PIXELS_PER_UNIT = 16;
        private static readonly string[] SpriteFolders =
        {
            "Assets/Art",
            "Assets/Resources/Art"
        };

        [MenuItem("Tools/渊海录/配置所有精灵为像素模式")]
        public static void ConfigureAllSpritesAsPixelArt()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", SpriteFolders);
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ArtImportRules.Apply(path);
                count++;
            }

            AssetDatabase.Refresh();
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("完成",
                    $"已配置 {count} 个精灵为像素模式\n" +
                    $"PPU: {PIXELS_PER_UNIT}\n" +
                    $"过滤: Point (无模糊)\n" +
                    $"压缩: 无",
                    "确定");
            }
        }

        [MenuItem("Tools/渊海录/配置选中精灵为像素模式")]
        public static void ConfigureSelectedAsPixelArt()
        {
            Object[] selections = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            int count = 0;

            foreach (Object obj in selections)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                ArtImportRules.Apply(path);
                count++;
            }

            Debug.Log($"[PixelArtImporter] 配置了 {count} 个精灵");
        }

        [MenuItem("Tools/渊海录/切分角色精灵表 (32x32)")]
        public static void SliceCharacterSpriteSheet()
        {
            Object selected = Selection.activeObject;
            if (selected == null) return;

            string path = AssetDatabase.GetAssetPath(selected);
            ArtImportRules.Apply(path);
        }

        [MenuItem("Tools/渊海录/切分瓦片集 (16x16)")]
        public static void SliceTileset()
        {
            Object selected = Selection.activeObject;
            if (selected == null) return;

            string path = AssetDatabase.GetAssetPath(selected);
            ArtImportRules.Apply(path);
        }

        // === 图层和标签自动配置 ===

        [MenuItem("Tools/渊海录/初始化项目设置")]
        public static void InitializeProjectSettings()
        {
            ProjectInitializer.ConfigureProjectSettings();

            EditorUtility.DisplayDialog("完成", "项目设置已初始化！\n" +
                "- 图层: 6 Player, 7 Enemy, 8 NPC, 9 Environment\n" +
                "- 排序层: Ground, Environment, Character, Foreground, UI\n" +
                "- 标签: Player, Enemy, NPC, Item, Environment, Trigger", "确定");
        }
    }
}
