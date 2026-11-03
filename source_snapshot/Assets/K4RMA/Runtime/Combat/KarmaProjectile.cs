using UnityEngine;

namespace KarmaPrototype
{
    // Swept segment collision prevents fast projectiles skipping a target.
    // Projectiles intentionally pass through the small arena platforms.
    public sealed class KarmaProjectile : MonoBehaviour
    {
        KarmaGame game;
        Vector2 velocity;
        float damage, radius, expiresAt;
        public static void Spawn(KarmaGame game, Vector2 position, Vector2 velocity, float damage,
            Color color, float radius = 0.2f * KarmaSpace.Scale)
        {
            var sr = KarmaVisuals.Box(game.World, "Player projectile",
                position, Vector2.one * radius * 2, color, 7);
            var shot = sr.gameObject.AddComponent<KarmaProjectile>();
            shot.game = game; shot.velocity = velocity; shot.damage = damage;
            shot.radius = radius; shot.expiresAt = Time.time + 3;
            KarmaRemnantVFX.ProjectileLook(sr, velocity);
            KarmaCombatVFX.AttachAttack(sr, velocity, new Color(0.35f, 0.75f, 1));
            game.Projectiles.Add(shot);
        }
        void Update()
        {
            if (game == null) { Destroy(gameObject); return; }
            if (game.Paused) return;
            if (!game.IsCombat || Time.time > expiresAt) { Destroy(gameObject); return; }
            Vector2 from = transform.position;
            Vector2 to = from + velocity * Time.deltaTime;
            transform.position = to;
            if (to.x < KarmaStage.ArenaLeft || to.x > KarmaStage.ArenaRight || to.y < 0 || to.y > KarmaSpace.Units(12)) { Destroy(gameObject); return; }
            // Damage can remove the hit enemy; this path returns immediately afterward.
            for (int i = 0; i < game.Enemies.Count; i++)
            {
                var enemy = game.Enemies[i];
                if (enemy == null || !enemy.Alive || !enemy.IntersectsDamagePath(from, to, radius)) continue;
                enemy.TakeDamage(damage, from);
                // Current player projectiles stop at their first target; piercing is a melee move.
                enabled = false; Destroy(gameObject); return;
            }
        }
        void OnDestroy() { if (game != null) game.Projectiles.Remove(this); }
    }
}
