using UnityEngine;

namespace KarmaPrototype
{
    // A field-space ceremony attached to the real actor, with no hit boxes or damage.
    public sealed class KarmaPersonalizationVFX : MonoBehaviour
    {
        Transform actor;
        SpriteRenderer halo, sigil, orb;
        readonly SpriteRenderer[] motes = new SpriteRenderer[20];
        readonly SpriteRenderer[] waves = new SpriteRenderer[3];
        Color color;
        float age, duration, radius;
        bool guardian;

        public static void Play(KarmaGame game, Transform actor, Essence essence, float seconds,
            bool guardian, Sprite previousAppearance = null, string previousForm = null)
        {
            var root = new GameObject(guardian ? "Guardian inheritance ceremony" : "Player personalization ceremony");
            root.transform.SetParent(game.World, false);
            var effect = root.AddComponent<KarmaPersonalizationVFX>();
            effect.actor = actor;
            effect.duration = seconds;
            effect.guardian = guardian;
            effect.color = essence == Essence.Guard ? new Color(1, 0.85f, 0.2f) : game.Config.ColorOf(essence);
            effect.radius = guardian ? 5.5f : 3.5f;
            effect.halo = effect.Layer("Gathering halo", KarmaArtwork.Sprite("PlayerVFX", "Barrier"), 4);
            effect.sigil = effect.Layer("Absorbed sigil", KarmaArtwork.Sprite("PersonalizationVFX", "Awaken"), 16);
            var icons = KarmaArtwork.Settings.skillIcons;
            int icon = (int)essence + (guardian ? 0 : 3);
            var iconSprite = icons != null && icons.Length > icon ? icons[icon] : null;
            effect.orb = effect.Layer("Inherited power", iconSprite, 25);
            for (int i = 0; i < effect.motes.Length; i++)
                effect.motes[i] = effect.Layer("Absorbing fragment", KarmaArtwork.Sprite("PersonalizationVFX", "Core"), 23);
            for (int i = 0; i < effect.waves.Length; i++)
                effect.waves[i] = effect.Layer("Awakening pulse", KarmaArtwork.Sprite("PersonalizationVFX", "Awaken"), 17);
            var animator = actor.GetComponentInChildren<KarmaCharacterAnimator>(true);
            if (animator != null) animator.Empower(essence, seconds, previousAppearance,
                guardian && game.Player.Essences.Count == 1, previousForm);
            effect.Draw();
        }

        SpriteRenderer Layer(string name, Sprite sprite, int order)
        {
            var child = new GameObject(name); child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sortingOrder = order;
            KarmaVisuals.ApplyMaterial(renderer);
            if (name != "Inherited power" && KarmaCombatVFX.GlowMaterial != null)
                renderer.sharedMaterial = KarmaCombatVFX.GlowMaterial;
            renderer.color = Color.clear;
            return renderer;
        }

        static void Show(SpriteRenderer renderer, Vector2 point, float size, float angle, Color color, float alpha)
        {
            if (renderer.sprite == null) return;
            renderer.transform.localPosition = point;
            renderer.transform.localRotation = Quaternion.Euler(0, 0, angle);
            float fit = size / Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
            renderer.transform.localScale = new Vector3(fit, fit, 1);
            color.a = Mathf.Clamp01(alpha); renderer.color = color;
        }

        void Draw()
        {
            if (actor == null) return;
            transform.position = actor.position + Vector3.up * (guardian ? 1.6f : 0.5f);
            float t = Mathf.Clamp01(age / duration);
            float gather = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.48f));
            float release = Mathf.InverseLerp(0.45f, 1, t);
            float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.82f, 1, t));
            float pulse = Mathf.Sin(t * Mathf.PI);
            Show(halo, Vector2.zero, radius * Mathf.Lerp(2, 0.65f, gather), age * 35,
                color, (0.3f + pulse * 0.3f) * fade);
            Show(sigil, Vector2.zero, radius * Mathf.Lerp(0.5f, 1.5f, pulse), -age * 22,
                color, pulse * 0.35f * fade);
            for (int i = 0; i < motes.Length; i++)
            {
                float angle = i * 2.39996f + age * 1.3f + gather * 3;
                float distance = radius * (1 - gather) * (0.7f + (i % 4) * 0.15f);
                var point = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance * 0.8f);
                Show(motes[i], point, 0.28f + pulse * 0.2f, angle * Mathf.Rad2Deg,
                    color, Mathf.Min(1, t * 8) * (1 - release) * fade);
            }
            for (int i = 0; i < waves.Length; i++)
            {
                float wave = Mathf.InverseLerp(0.46f + i * 0.08f, 0.85f + i * 0.06f, t);
                Show(waves[i], Vector2.zero, radius * Mathf.Lerp(0.5f, 3.2f, Mathf.Sqrt(wave)), i * 55,
                    Color.Lerp(color, Color.white, 0.35f), wave > 0 ? (1 - wave) * 0.75f * fade : 0);
            }
            // The boss visibly receives the original power from offscreen before transforming.
            var entry = guardian ? new Vector2(0, radius * 1.8f) : new Vector2(0, radius * 0.85f);
            var orbPoint = Vector2.Lerp(entry, Vector2.zero, gather);
            Show(orb, orbPoint, Mathf.Lerp(1.2f, 0.35f, gather) * (guardian ? 1.5f : 1), age * 12,
                Color.white, (1 - Mathf.InverseLerp(0.45f, 0.6f, t)) * Mathf.Min(1, t * 8));
        }

        void LateUpdate()
        {
            if (actor == null) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            if (age >= duration)
            {
                Destroy(gameObject);
                return;
            }
            Draw();
        }
    }
}
