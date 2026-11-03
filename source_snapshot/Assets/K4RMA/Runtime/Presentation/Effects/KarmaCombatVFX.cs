using System;
using System.Collections.Generic;
using UnityEngine;

namespace KarmaPrototype
{
    // Layered sprite animation, additive light and moving fragments. No damage logic.
    public sealed class KarmaCombatVFX : MonoBehaviour
    {
        struct Mote
        {
            public SpriteRenderer renderer;
            public Vector3 velocity;
        }
        static Material glowMaterial;
        public static Material GlowMaterial
        {
            get { if (glowMaterial == null) glowMaterial = Resources.Load<Material>("KarmaCombatGlow"); return glowMaterial; }
        }
        SpriteRenderer core, glow, echo;
        Mote[] motes = Array.Empty<Mote>();
        Vector3 scale;
        Color tint;
        float age, life, trailAt;
        bool sustained, moving, restrainedVisual;
        Transform trailRoot;
        readonly List<GameObject> ownedAfterimages = new List<GameObject>();

        public static bool ApplySprite(SpriteRenderer renderer, string sheet, string pose, Vector2 size, bool preserveAspect = false)
        {
            var sprite = KarmaArtwork.Sprite(sheet, pose);
            if (sprite == null) return false;
            renderer.sprite = sprite;
            float x = size.x / sprite.bounds.size.x, y = size.y / sprite.bounds.size.y;
            if (preserveAspect) x = y = Mathf.Min(x, y);
            renderer.transform.localScale = new Vector3(x, y, 1);
            return true;
        }
        public static SpriteRenderer SpawnSprite(Transform parent, Vector2 at, string sheet, string pose,
            Vector2 size, float seconds, float angle = 0)
        {
            var sprite = KarmaArtwork.Sprite(sheet, pose);
            if (sprite == null) return null;
            Color tint = sheet == "HitVFX" ? new Color(1, 0.75f, 0.32f)
                : sheet == "PersonalizationVFX" ? new Color(0.58f, 0.85f, 1)
                : new Color(0.35f, 0.7f, 1);
            return Burst(parent, at, sprite, sheet + "/" + pose, size, seconds, angle, tint);
        }

        static SpriteRenderer Layer(Transform parent, string name, Sprite sprite, int order, Color color, bool additive)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = order; sr.color = color;
            KarmaVisuals.ApplyMaterial(sr);
            if (additive && GlowMaterial != null) sr.sharedMaterial = GlowMaterial;
            return sr;
        }
        public static SpriteRenderer Burst(Transform parent, Vector2 at, Sprite sprite, string name, Vector2 size, float seconds, float angle, Color color, bool restrained = false)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            root.transform.localPosition = at; root.transform.localRotation = Quaternion.Euler(0, 0, angle);
            var fx = root.AddComponent<KarmaCombatVFX>(); fx.life = Mathf.Max(0.08f, seconds); fx.tint = color; fx.restrainedVisual = restrained;
            fx.core = Layer(root.transform, "Ivory edge", sprite, 22, restrained ? new Color(1, 0.96f, 0.84f, 0.78f) : Color.white, false);
            float fit = Mathf.Min(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
            fx.scale = new Vector3(fit, fit, 1);
            fx.core.transform.localScale = fx.scale;
            var light = color; if (restrained) light.a = 0.18f;
            fx.glow = Layer(root.transform, "Colored light", sprite, 21, light, true);
            fx.glow.transform.localScale = fx.scale * (restrained ? 1.04f : 1.16f);
            fx.echo = Layer(root.transform, "Sweeping echo", sprite, 20, new Color(color.r, color.g, color.b, restrained ? 0.1f : 0.6f), true);
            fx.echo.transform.localScale = fx.scale * (restrained ? 1.06f : 1.28f);
            int particles = restrained ? 2 : name.Contains("Hit") || name.Contains("Impact") ? 9 : 5;
            fx.motes = new Mote[particles];
            for (int i = 0; i < particles; i++)
            {
                float radians = (i * 137.508f + 19) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                var sr = KarmaVisuals.Box(root.transform, "Spark", direction * size.magnitude * 0.04f,
                    new Vector2(Mathf.Clamp(size.magnitude * 0.065f, 0.2f, 0.8f), 0.045f * KarmaSpace.Scale), color, 24);
                sr.transform.localRotation = Quaternion.Euler(0, 0, radians * Mathf.Rad2Deg);
                if (GlowMaterial != null) sr.sharedMaterial = GlowMaterial;
                fx.motes[i] = new Mote { renderer = sr, velocity = direction * Mathf.Min(13, size.magnitude * 2) };
            }
            return fx.core;
        }
        public static void AttachAttack(SpriteRenderer renderer, Vector2 velocity, Color color, Transform trailParent = null, bool restrained = false)
        {
            var fx = renderer.gameObject.AddComponent<KarmaCombatVFX>();
            fx.sustained = true; fx.moving = velocity.sqrMagnitude > 0.01f; fx.core = renderer; fx.tint = color; fx.restrainedVisual = restrained;
            fx.scale = renderer.transform.localScale; fx.trailRoot = trailParent != null ? trailParent : renderer.transform.parent;
            var light = color; if (restrained) light.a = 0.18f;
            fx.glow = Layer(renderer.transform, "Attack radiance", renderer.sprite, renderer.sortingOrder - 1, light, true);
            fx.glow.transform.localScale = Vector3.one * (restrained ? 1.04f : 1.25f);
            fx.glow.flipX = renderer.flipX; fx.glow.flipY = renderer.flipY;
        }
        void Update()
        {
            if (Time.deltaTime <= 0) return;
            age += Time.deltaTime;
            if (sustained)
            {
                var light = tint;
                light.a = restrainedVisual ? 0.16f + 0.035f * Mathf.Sin(age * 20) : 0.65f + 0.22f * Mathf.Sin(age * 35);
                glow.color = light;
                glow.transform.localScale = Vector3.one * (restrainedVisual ? 1.035f + Mathf.Sin(age * 20) * 0.015f : 1.18f + Mathf.Sin(age * 24) * 0.09f);
                if (moving && age >= trailAt)
                {
                    trailAt = age + (restrainedVisual ? 0.075f : 0.045f);
                    var ghost = Layer(trailRoot, "Attack afterimage", core.sprite, core.sortingOrder - 2,
                        new Color(tint.r, tint.g, tint.b, restrainedVisual ? 0.16f : 0.5f), true);
                    ghost.transform.position = transform.position; ghost.transform.rotation = transform.rotation;
                    ghost.transform.localScale = scale * (restrainedVisual ? 0.96f : 1.05f);
                    ghost.flipX = core.flipX; ghost.flipY = core.flipY;
                    ghost.gameObject.AddComponent<KarmaEffect>().Initialize(restrainedVisual ? 0.11f : 0.19f);
                    if (restrainedVisual) ownedAfterimages.Add(ghost.gameObject);
                }
                return;
            }
            float t = Mathf.Clamp01(age / life);
            if (t >= 1) { Destroy(gameObject); return; }
            float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.24f) / 0.76f));
            core.color = restrainedVisual ? new Color(1, 0.96f, 0.84f, fade * 0.78f) : new Color(1, 1, 1, fade);
            core.transform.localScale = scale * (restrainedVisual ? Mathf.Lerp(0.88f, 1.02f, Mathf.Sqrt(t)) : Mathf.Lerp(0.78f, 1.12f, Mathf.Sqrt(t)));
            var lightColor = tint; lightColor.a = fade * (restrainedVisual ? 0.18f - t * 0.08f : 1.15f - t * 0.4f); glow.color = lightColor;
            glow.transform.localScale = core.transform.localScale * (restrainedVisual ? 1.04f : Mathf.Lerp(1.1f, 1.25f, t));
            echo.color = new Color(tint.r, tint.g, tint.b, fade * (restrainedVisual ? 0.1f : 0.45f));
            echo.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-15, 10, t));
            echo.transform.localScale = scale * (restrainedVisual ? Mathf.Lerp(0.94f, 1.08f, t) : Mathf.Lerp(0.9f, 1.35f, t));
            for (int i = 0; i < motes.Length; i++)
            {
                ref var mote = ref motes[i];
                mote.renderer.transform.localPosition += mote.velocity * Time.deltaTime;
                mote.velocity *= Mathf.Exp(-5 * Time.deltaTime);
                mote.renderer.color = new Color(tint.r, tint.g, tint.b, fade * (1 - t));
            }
        }
        void OnDisable()
        {
            // Wave echoes stay in world space, but never suggest danger after
            // their owning projectile is cancelled, expires, or is cleaned up.
            foreach (var ghost in ownedAfterimages)
                if (ghost != null) { ghost.SetActive(false); Destroy(ghost); }
            ownedAfterimages.Clear();
        }
    }
}
