using UnityEngine;

namespace KarmaPrototype
{
    // Visuals only: never owns damage, proc timing, or collision radius.
    // Textures are generated once; no external sprite/material setup required.
    public sealed class KarmaRemnantVFX : MonoBehaviour
    {
        static Sprite ring, crescent, ember;
        SpriteRenderer visual;
        Vector3 originalSize;
        Vector2 drift;
        float age, duration, expand, alpha;

        static Sprite Shape(int kind)
        {
            Sprite cached = kind == 0 ? ring : kind == 1 ? crescent : ember;
            if (cached != null) return cached;
            const int n = 96;
            var texture = new Texture2D(n, n);
            texture.name = "RemnantShape_" + kind;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2 - 1, v = (y + 0.5f) / n * 2 - 1;
                float r = Mathf.Sqrt(u * u + v * v);
                float a;
                if (kind == 0) a = Mathf.Clamp01((0.10f - Mathf.Abs(r - 0.82f)) * 35);
                else if (kind == 1)
                {
                    // A bright curved blade, opening toward the direction of travel.
                    a = Mathf.Clamp01((0.16f - Mathf.Abs(r - 0.78f)) * 25)
                        * Mathf.Clamp01((u + 0.1f) * 4);
                }
                else
                {
                    float width = 0.12f + (u + 1) * 0.3f;
                    a = Mathf.Clamp01((1 - Mathf.Abs(v) / width) * 3)
                        * Mathf.Clamp01((1 - r) * 10);
                }
                pixels[y * n + x] = new Color(1, 1, 1, a);
            }
            texture.SetPixels(pixels); texture.Apply();
            cached = Sprite.Create(texture, new Rect(0, 0, n, n), Vector2.one * 0.5f, n);
            if (kind == 0) ring = cached; else if (kind == 1) crescent = cached; else ember = cached;
            return cached;
        }
        static SpriteRenderer Draw(Transform parent, Vector2 p, Vector2 size, Color color, int kind)
        {
            var sr = KarmaVisuals.Box(parent, "Remnant VFX", p, size, color, 14);
            string pose = kind == 0 ? "Barrier" : kind == 1 ? "Slash" : "PierceTrail";
            if (KarmaCombatVFX.ApplySprite(sr, "PlayerVFX", pose, size)) sr.color = new Color(1, 1, 1, color.a);
            else sr.sprite = Shape(kind);
            return sr;
        }
        static void Animate(SpriteRenderer sr, float seconds, float growth, Vector2 drift)
        {
            var fx = sr.gameObject.AddComponent<KarmaRemnantVFX>();
            fx.visual = sr; fx.duration = seconds; fx.expand = growth; fx.drift = drift;
            fx.originalSize = sr.transform.localScale; fx.alpha = sr.color.a;
        }
        public static SpriteRenderer CreateGuardHalo(Transform player, Color color)
        {
            color.a = 0.35f;
            var sr = Draw(player, Vector2.zero, KarmaSpace.Units(new Vector2(1.5f, 2.15f)), color, 0);
            sr.sortingOrder = 2;
            return sr;
        }
        public static void ProjectileLook(SpriteRenderer sr, Vector2 velocity)
        {
            sr.sprite = Shape(1);
            sr.sortingOrder = 14;
            sr.transform.localScale = new Vector3(KarmaSpace.Units(1.25f), KarmaSpace.Units(2.3f), 1);
            if (KarmaCombatVFX.ApplySprite(sr, "PlayerVFX", "Wave", KarmaSpace.Units(new Vector2(2.3f, 1.25f)), true)) sr.color = Color.white;
            sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        }
        public static void Proc(KarmaGame game, Vector2 p, int facing, Essence essence)
        {
            string pose = essence == Essence.Guard ? "Counter" : essence == Essence.SwordWave ? "Pierce" : "AirSlash";
            KarmaCombatVFX.SpawnSprite(game.World, p, "PersonalizationVFX", pose,
                KarmaSpace.Units(new Vector2(2.5f, 2)), 0.35f, facing > 0 ? 0 : 180);
            Color color = game.Config.ColorOf(essence);
            if (essence == Essence.Guard)
            {
                // A local parry pulse.
                float diameter = KarmaSpace.Units(2.2f);
                var sr = Draw(game.World, p, Vector2.one * diameter, color, 0);
                Animate(sr, 0.65f, 1, Vector2.zero);
            }
            else
            {
                var sr = Draw(game.World, p + Vector2.right * facing * KarmaSpace.Units(0.8f),
                    KarmaSpace.Units(essence == Essence.SwordWave ? new Vector2(2.2f, 2.2f) : new Vector2(1.8f, 2.8f)), color, 1);
                sr.transform.localRotation = Quaternion.Euler(0, 0, facing > 0 ? 0 : 180);
                Animate(sr, 0.35f, 0.35f, Vector2.right * facing * KarmaSpace.Units(0.5f));
            }
        }
        public static void SwordSlash(Transform parent, Vector2 origin, Vector2 direction,
            float reach, float height, float seconds)
        {
            direction.Normalize();
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            string pose = direction.y > 0.25f ? "Rising" : direction.y < -0.25f ? "AirSlash" : "FollowThrough";
            Vector2 effectSize = pose == "Rising" ? new Vector2(height, reach * 1.3f) : new Vector2(reach * 1.3f, height);
            float effectAngle = angle - (pose == "Rising" ? 90 : 0);
            if (KarmaCombatVFX.SpawnSprite(parent, origin + direction * (reach * 0.48f), "PlayerVFX", pose,
                effectSize, seconds, effectAngle) != null) return;
            // Broad cool silhouette under a crisp ivory blade; visible against dark wood.
            var rim = Draw(parent, origin + direction * (reach * 0.48f), new Vector2(reach * 1.3f, height),
                new Color(0.4f, 0.76f, 1, 0.7f), 1);
            rim.sortingOrder = 15;
            rim.transform.localRotation = Quaternion.Euler(0, 0, angle);
            Animate(rim, seconds * 1.2f, 0, direction * KarmaSpace.Units(0.35f));
            var blade = Draw(parent, origin + direction * (reach * 0.48f), new Vector2(reach * 1.22f, height * 0.92f),
                new Color(1, 0.97f, 0.84f, 1), 1);
            blade.sortingOrder = 16;
            blade.transform.localRotation = Quaternion.Euler(0, 0, angle);
            Animate(blade, seconds, 0, direction * KarmaSpace.Units(0.2f));
            var line = KarmaVisuals.Box(parent, "Sword core", origin + direction * (reach * 0.5f),
                new Vector2(reach, KarmaSpace.Units(0.065f)), Color.white, 17);
            line.transform.localRotation = Quaternion.Euler(0, 0, angle);
            line.gameObject.AddComponent<KarmaEffect>().Initialize(seconds * 0.6f);
        }
        public static void SwordImpact(KarmaGame game, Vector2 point, Vector2 direction)
        {
            string pose = Mathf.Abs(direction.y) > 0.5f ? "Cross" : "Burst";
            if (KarmaCombatVFX.SpawnSprite(game.World, point, "HitVFX", pose,
                Vector2.one * KarmaSpace.Units(1.1f), 0.18f) != null) return;
            var ring = Draw(game.World, point, Vector2.one * KarmaSpace.Units(0.95f), new Color(1, 0.93f, 0.7f), 0);
            ring.sortingOrder = 20; Animate(ring, 0.16f, 0.45f, Vector2.zero);
            var artwork = KarmaArtwork.Settings.hitEffect;
            if (artwork != null)
            {
                var hit = new GameObject("Artwork sword impact"); hit.transform.SetParent(game.World, false);
                hit.transform.position = point;
                hit.transform.localScale = Vector3.one * KarmaSpace.Scale;
                var sr = hit.AddComponent<SpriteRenderer>(); sr.sprite = artwork; sr.sortingOrder = 22;
                KarmaVisuals.ApplyMaterial(sr);
                hit.AddComponent<KarmaEffect>().Initialize(0.16f);
            }
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            for (int i = 0; i < 5; i++)
            {
                float radians = (angle + (i - 2) * 32) * Mathf.Deg2Rad;
                Vector2 ray = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                var spark = Draw(game.World, point + ray * KarmaSpace.Units(0.1f), KarmaSpace.Units(new Vector2(0.5f, 0.06f)), Color.white, 2);
                spark.sortingOrder = 21;
                spark.transform.localRotation = Quaternion.Euler(0, 0, radians * Mathf.Rad2Deg);
                Animate(spark, 0.14f, 0, ray * KarmaSpace.Units(2.8f));
            }
        }
        public static void Impact(KarmaGame game, Vector2 p, Essence essence)
        {
            KarmaCombatVFX.SpawnSprite(game.World, p, "HitVFX", essence == Essence.Guard ? "Ring" : essence == Essence.RisingSlash ? "Cross" : "Crescent",
                Vector2.one * KarmaSpace.Units(1.5f), 0.25f);
            Color color = game.Config.ColorOf(essence);
            var sr = Draw(game.World, p, Vector2.one * KarmaSpace.Units(essence == Essence.RisingSlash ? 2.2f : 1.35f), color, 0);
            Animate(sr, 0.4f, 1, Vector2.zero);
        }
        void Update()
        {
            age += Time.deltaTime;
            if (age >= duration) { Destroy(gameObject); return; }
            float t = Mathf.Clamp01(age / duration);
            // Hold opacity briefly before fading, rather than disappearing in one frame.
            float a = 1 - Mathf.Clamp01((t - 0.3f) / 0.7f);
            var c = visual.color; c.a = alpha * a; visual.color = c;
            transform.localScale = originalSize * Mathf.Lerp(1 - expand * 0.75f, 1, Mathf.Sqrt(t));
            transform.position += (Vector3)(drift * Time.deltaTime);
        }
    }
}
