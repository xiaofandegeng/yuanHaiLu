using UnityEngine;
using System.Collections.Generic;

namespace YuanHaiLu.Effects
{
    /// <summary>
    /// 特效管理器 — 管理粒子特效池、全局特效播放
    /// 单例，挂载到 EffectsManager 空物体上
    /// </summary>
    public class EffectsManager : MonoBehaviour
    {
        public static EffectsManager Instance { get; private set; }

        [Header("特效池设置")]
        [SerializeField] private int defaultPoolSize = 5;

        // 特效预制体池
        private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> _prefabRegistry = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 注册特效预制体
        /// </summary>
        public void RegisterEffect(string effectId, GameObject prefab, int poolSize = 0)
        {
            if (poolSize <= 0) poolSize = defaultPoolSize;

            _prefabRegistry[effectId] = prefab;
            _pools[effectId] = new Queue<GameObject>();

            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                _pools[effectId].Enqueue(obj);
            }
        }

        /// <summary>
        /// 播放特效
        /// </summary>
        public GameObject PlayEffect(string effectId, Vector3 position, float autoDestroy = 2f)
        {
            GameObject obj = GetFromPool(effectId);
            if (obj == null) return null;

            obj.transform.position = position;
            obj.SetActive(true);

            // 粒子自动停止后回收
            var particleSystem = obj.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Clear();
                particleSystem.Play();
            }

            StartCoroutine(ReturnToPoolAfterDelay(effectId, obj, autoDestroy));
            return obj;
        }

        /// <summary>
        /// 播放特效（带方向）
        /// </summary>
        public GameObject PlayEffect(string effectId, Vector3 position, Vector2 direction, float autoDestroy = 2f)
        {
            GameObject obj = PlayEffect(effectId, position, autoDestroy);
            if (obj != null)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                obj.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            return obj;
        }

        /// <summary>
        /// 在两个点之间画一条剑气轨迹
        /// </summary>
        public void PlaySlashTrail(Vector2 start, Vector2 end, Color color, float duration = 0.3f)
        {
            StartCoroutine(DrawSlashTrail(start, end, color, duration));
        }

        private System.Collections.IEnumerator DrawSlashTrail(Vector2 start, Vector2 end, Color color, float duration)
        {
            GameObject trailObj = new GameObject("SlashTrail");
            trailObj.transform.SetParent(transform);

            var lineRenderer = trailObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startWidth = 0.15f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.startColor = color;
            lineRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.sortingLayerName = "Foreground";
            lineRenderer.sortingOrder = 100;

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                Color fadeColor = new Color(color.r, color.g, color.b, 1f - t);
                lineRenderer.startColor = fadeColor;
                lineRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
                yield return null;
            }

            Destroy(trailObj);
        }

        /// <summary>
        /// 屏幕闪烁效果（受伤红闪、升级金闪等）
        /// </summary>
        public void ScreenFlash(Color color, float duration = 0.2f)
        {
            StartCoroutine(DoScreenFlash(color, duration));
        }

        private System.Collections.IEnumerator DoScreenFlash(Color color, float duration)
        {
            // 创建全屏覆盖
            GameObject flashObj = new GameObject("ScreenFlash");
            flashObj.transform.SetParent(transform);

            var canvas = flashObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var image = flashObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(color.r, color.g, color.b, 0.5f);
            image.raycastTarget = false;

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float alpha = 0.5f * (1f - timer / duration);
                image.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            Destroy(flashObj);
        }

        /// <summary>
        /// 数字弹出效果（伤害数字等）
        /// </summary>
        public void SpawnDamageNumber(Vector3 position, int amount, bool isCrit = false)
        {
            StartCoroutine(AnimateDamageNumber(position, amount, isCrit));
        }

        private System.Collections.IEnumerator AnimateDamageNumber(Vector3 pos, int amount, bool isCrit)
        {
            // 创建世界空间文字
            GameObject textObj = new GameObject("DmgNumber");
            textObj.transform.position = pos + Vector3.up * 0.5f;

            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = amount.ToString();
            textMesh.fontSize = isCrit ? 24 : 16;
            textMesh.color = isCrit ? Color.yellow : Color.white;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            var renderer = textObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = "UI";
                renderer.sortingOrder = 200;
            }

            // 字体描边效果（简化处理）
            if (isCrit)
            {
                textMesh.text = $"暴击 {amount}!";
                textMesh.color = new Color(1f, 0.3f, 0.1f);
            }

            float duration = 1f;
            float timer = 0f;
            Vector3 startPos = textObj.transform.position;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;

                // 上浮
                textObj.transform.position = startPos + Vector3.up * (t * 1.5f);
                // 淡出
                Color c = textMesh.color;
                c.a = 1f - t;
                textMesh.color = c;

                yield return null;
            }

            Destroy(textObj);
        }

        // === 池管理 ===
        private GameObject GetFromPool(string effectId)
        {
            if (!_pools.ContainsKey(effectId)) return null;

            var pool = _pools[effectId];
            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                return obj;
            }

            // 池空了，创建新的
            if (_prefabRegistry.TryGetValue(effectId, out GameObject prefab))
            {
                return Instantiate(prefab, transform);
            }

            return null;
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay(string effectId, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(false);
            if (_pools.ContainsKey(effectId))
            {
                _pools[effectId].Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        // === 快捷静态方法 ===
        // 注意：Instance 必须用显式 == null 判断（Unity fake-null），
        // 禁止 ?. / ??，否则场景卸载后残留引用会在协程/实例方法上抛 MissingReferenceException。

        /// <summary>播放受击火花</summary>
        public static void HitSpark(Vector3 pos, Vector2 dir)
        {
            if (Instance == null) return;
            Instance.PlayEffect("hit_spark", pos, dir, 0.5f);
        }

        /// <summary>播放暴击特效</summary>
        public static void CritEffect(Vector3 pos)
        {
            if (Instance == null) return;
            Instance.PlayEffect("crit_burst", pos, 0.8f);
            Instance.ScreenFlash(new Color(1f, 0.3f, 0f), 0.15f);
        }

        /// <summary>播放升级光效</summary>
        public static void LevelUpEffect(Vector3 pos)
        {
            if (Instance == null) return;
            Instance.PlayEffect("levelup_ring", pos, 2f);
            Instance.ScreenFlash(new Color(1f, 0.85f, 0.2f), 0.3f);
        }

        /// <summary>播放治疗光效</summary>
        public static void HealEffect(Vector3 pos)
        {
            if (Instance == null) return;
            Instance.PlayEffect("heal_green", pos, 1f);
        }

        /// <summary>显示伤害数字</summary>
        public static void DamageNumber(Vector3 pos, int dmg, bool crit)
        {
            if (Instance == null) return;
            Instance.SpawnDamageNumber(pos, dmg, crit);
        }
    }
}
