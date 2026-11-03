using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        SpriteRenderer[] damageRenderers;
        KarmaWaveMotion damageMotion;
        internal bool PresentationActive { get { return Active && isActiveAndEnabled; } }
        // Only the guardian artwork contributes: attack VFX and shield halos are separate objects.
        public Bounds DamageBounds
        {
            get
            {
                if (damageRenderers == null || damageRenderers.Length == 0)
                {
                    damageRenderers = art.GetComponentsInChildren<SpriteRenderer>();
                    damageMotion = art.GetComponent<KarmaWaveMotion>();
                }
                Bounds bounds = default;
                bool found = false;
                foreach (var renderer in damageRenderers)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sprite == null) continue;
                    var body = damageMotion != null ? damageMotion.GetDamageBounds(renderer) : renderer.bounds;
                    if (!found) { bounds = body; found = true; }
                    else bounds.Encapsulate(body);
                }
                return found ? bounds : new Bounds(Position, KarmaSpace.Units(new Vector2(1.4f, 2.3f)));
            }
        }
        public bool OverlapsDamageArea(Vector2 center, Vector2 size)
        {
            var bounds = DamageBounds;
            return Mathf.Abs(center.x - bounds.center.x) <= size.x / 2 + bounds.extents.x
                && Mathf.Abs(center.y - bounds.center.y) <= size.y / 2 + bounds.extents.y;
        }
        public bool IntersectsDamagePath(Vector2 from, Vector2 to, float radius)
        {
            var bounds = DamageBounds;
            return KarmaBossGeometry.SweptBox(from.x, from.y, to.x, to.y, bounds.center.x, bounds.center.y,
                bounds.extents.x + radius, bounds.extents.y + radius);
        }
        public Vector2 ClosestDamagePoint(Vector2 source) { return DamageBounds.ClosestPoint(source); }
        public void TakeDamage(float amount) { TakeDamage(amount, game.Player.Position); }
        public void TakeDamage(float amount, Vector2 source)
        {
            if (!Alive || !game.IsCombat || amount <= 0 || stopped) return;
            engaged = true;
            if (fullShield) return;
            bool front = frontalGuard && (source.x - Position.x) * direction >= 0;
            bool reduced = front || swordGuard || FightSeconds < shieldUntil;
            behavior.OnDamaged();
            if (reduced)
            {
                amount *= Mathf.Clamp01(behavior.Balance.shieldDamageMultiplier);
                KarmaRemnantVFX.Impact(game, Position, Essence.Guard);
            }
            ReceiveDamage(amount);
        }
        void ReceiveDamage(float amount)
        {
            if (amount <= 0) return;
            animator.Hurt(); Health = Mathf.Max(0, Health - amount);
            KarmaVisuals.Flash(game.World, Position, Vector2.one * KarmaSpace.Units(0.7f), Color.white, 0.1f);
            if (Health <= 0)
            {
                CancelCombat(); art.SetParent(game.World, true); animator.Die();
                game.EnemyDefeated(this, art); Destroy(gameObject);
            }
        }
    }
}
