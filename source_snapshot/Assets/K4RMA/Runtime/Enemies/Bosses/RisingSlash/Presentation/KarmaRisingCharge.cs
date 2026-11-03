using UnityEngine;
namespace KarmaPrototype
{
    public sealed class KarmaRisingCharge : MonoBehaviour
    {
        Transform follow;
        SpriteRenderer core, glow, ring;
        KarmaRisingCue cue;
        float punch;

        public static KarmaRisingCharge Begin(Transform parent, Transform follow, KarmaRisingCue cue)
        {
            if (parent == null || follow == null) return null;
            var go = new GameObject(cue == KarmaRisingCue.Tracking ? "상승베기 추적 준비" : cue == KarmaRisingCue.Locked ? "상승베기 고정 준비" : "상승베기 준비");
            go.transform.SetParent(parent, false);
            var charge = go.AddComponent<KarmaRisingCharge>();
            charge.follow = follow;
            charge.cue = cue;
            charge.punch = 0.14f;
            charge.core = charge.Make("기둥", "GuardianVFX", "Rising", KarmaSpace.Units(new Vector2(2.4f, 4.1f)), 16);
            charge.glow = charge.Make("빛", "GuardianVFX", "Rising", KarmaSpace.Units(new Vector2(2.9f, 4.6f)), 15);
            charge.ring = charge.Make("발밑", "GuardianVFX", "GroundRing", KarmaSpace.Units(new Vector2(2.8f, 0.95f)), 14);
            if (KarmaCombatVFX.GlowMaterial != null && charge.glow != null) charge.glow.sharedMaterial = KarmaCombatVFX.GlowMaterial;
            return charge;
        }
        public void Lock()
        {
            cue = KarmaRisingCue.Locked;
            punch = 0.16f;
        }
        public void End()
        {
            if (this != null) Destroy(gameObject);
        }
        SpriteRenderer Make(string name, string sheet, string pose, Vector2 size, int order)
        {
            var sprite = KarmaVisuals.Box(transform, name, Vector2.zero, Vector2.one, Color.white, order);
            if (!KarmaCombatVFX.ApplySprite(sprite, sheet, pose, size, true))
                sprite.transform.localScale = new Vector3(size.x, size.y, 1f);
            return sprite;
        }
        void LateUpdate()
        {
            if (follow == null) { Destroy(gameObject); return; }
            float lift = KarmaSpace.Units(1.05f);
            transform.position = follow.position + Vector3.up * lift;
            if (ring != null) ring.transform.localPosition = Vector3.down * (lift + KarmaSpace.Units(0.2f));
            float speed = cue == KarmaRisingCue.Locked ? 28f : cue == KarmaRisingCue.Charging ? 14f : 7f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * speed);
            Color tint = cue == KarmaRisingCue.Locked
                ? new Color(1f, 0.36f, 0.05f, 1f)
                : cue == KarmaRisingCue.Charging
                    ? new Color(1f, 0.72f, 0.22f, 1f)
                    : new Color(0.25f, 0.9f, 1f, 1f);
            float alpha = cue == KarmaRisingCue.Tracking ? 0.42f + pulse * 0.28f : 0.72f + pulse * 0.28f;
            if (core != null) core.color = new Color(1f, 1f, 1f, alpha);
            if (glow != null)
            {
                var light = tint;
                light.a = alpha;
                glow.color = light;
            }
            if (ring != null)
            {
                var mark = tint;
                mark.a = alpha * 0.9f;
                ring.color = mark;
            }
            float pop = punch > 0f ? 1f + Mathf.Clamp01(punch / 0.16f) * 0.28f : 1f;
            if (punch > 0f) punch -= Time.deltaTime;
            transform.localScale = Vector3.one * (pop * (cue == KarmaRisingCue.Locked ? 1.08f : 1f));
        }
    }
}
