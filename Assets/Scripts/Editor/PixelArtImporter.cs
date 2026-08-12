using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

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
            "Assets/Sprites",
            "Assets/Art/Tilesets"
        };

        [MenuItem("Tools/渊海录/配置所有精灵为像素模式")]
        public static void ConfigureAllSpritesAsPixelArt()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", SpriteFolders);
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null) continue;

                // 像素艺术设置
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple; // 多数是精灵表
                importer.filterMode = FilterMode.Point;                // 最近邻过滤（锐利像素）
                importer.textureCompression = TextureImporterCompression.Uncompressed; // 不压缩
                importer.maxTextureSize = 2048;
                importer.spritePixelsPerUnit = PIXELS_PER_UNIT;

                // 像素完美相机兼容
                importer.spritePivot = new Vector2(0.5f, 0f); // 底部中心（角色）
                importer.mipmapEnabled = false;

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
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
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = PIXELS_PER_UNIT;
                importer.mipmapEnabled = false;

                importer.SaveAndReimport();
                count++;
            }

            Debug.Log($"[PixelArtImporter] 配置了 {count} 个精灵");
        }

        [MenuItem("Tools/渊海录/切分角色精灵表 (48x48)")]
        public static void SliceCharacterSpriteSheet()
        {
            Object selected = Selection.activeObject;
            if (selected == null) return;

            string path = AssetDatabase.GetAssetPath(selected);
            SliceSpriteSheet(path, 48, 48, Vector2.zero);
        }

        [MenuItem("Tools/渊海录/切分瓦片集 (16x16)")]
        public static void SliceTileset()
        {
            Object selected = Selection.activeObject;
            if (selected == null) return;

            string path = AssetDatabase.GetAssetPath(selected);
            SliceSpriteSheet(path, 16, 16, new Vector2(0.5f, 0.5f));
        }

        private static void SliceSpriteSheet(string path, int cellW, int cellH, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            // 获取纹理尺寸
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int cols = texture.width / cellW;
            int rows = texture.height / cellH;

            // 生成切片数据
            var metaData = new SpriteMetaData[cols * rows];
            int index = 0;

            for (int row = rows - 1; row >= 0; row--) // 从上到下
            {
                for (int col = 0; col < cols; col++)
                {
                    metaData[index] = new SpriteMetaData
                    {
                        name = $"sprite_{row}_{col}",
                        rect = new Rect(col * cellW, row * cellH, cellW, cellH),
                        pivot = pivot,
                        alignment = (int)SpriteAlignment.Center,
                    };
                    index++;
                }
            }

            importer.spritesheet = metaData.Select(m => new SpriteMetaData
            {
                name = m.name,
                rect = m.rect,
                pivot = m.pivot,
                alignment = m.alignment,
            }).ToArray();

            importer.SaveAndReimport();

            Debug.Log($"[PixelArtImporter] 切分完成: {path}\n" +
                      $"尺寸: {texture.width}x{texture.height}\n" +
                      $"格子: {cellW}x{cellH}\n" +
                      $"总数: {cols}x{rows} = {cols * rows} 帧");
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
