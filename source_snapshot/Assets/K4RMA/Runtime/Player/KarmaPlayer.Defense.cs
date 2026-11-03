using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaPlayer
    {
        public void BeginCounter()
        {
            techniques.ArmCounter(Time.time, game.Config.Techniques.counterWindow);
            animator.PlayPose("Parry", game.Config.Techniques.counterWindow);
        }
        public void BeginGuard(float seconds)
        {
            guardUntil = Time.time + seconds; animator.PlayPose("Guard", seconds);
        }
        public void TakeDamage(float damage)
        {
            if (!game.IsCombat || Health <= 0 || damage <= 0 || Time.time < invulnerableUntil) return;
            if (techniques.ConsumeCounter(Time.time))
            {
                ParryCount++;
                animator.PlayPose("Riposte", 0.3f);
                invulnerableUntil = Time.time + 0.12f;
                Essences.NotifyUnique(Essence.Guard);
                foreach (var enemy in game.Enemies.ToArray())
                {
                    if (enemy == null || !enemy.Alive) continue;
                    if (Vector2.Distance(enemy.ClosestDamagePoint(Position), Position) < KarmaSpace.Units(3.5f)) enemy.TakeDamage(game.Config.Techniques.counterDamage);
                    else
                    {
                        Vector2 aim = ((Vector2)enemy.DamageBounds.center - Position).normalized;
                        KarmaProjectile.Spawn(game, Position + aim * KarmaSpace.Units(0.7f), aim * KarmaSpace.Units(14),
                            game.Config.Techniques.counterDamage, game.Config.guardColor, KarmaSpace.Units(0.25f));
                    }
                }
                return;
            }
            if (Shielded)
            {
                KarmaCombatVFX.SpawnSprite(game.World, Position, "PlayerVFX", "Barrier", KarmaSpace.Units(new Vector2(1.6f, 2.1f)), 0.2f);
                return;
            }
            if (TestInvincible) return;
            float actual = Mathf.Min(Health, damage);
            CancelAirSlash();
            animator.Hurt();
            Health -= actual; game.DamageTaken += actual;
            invulnerableUntil = Time.time + game.Config.hitInvulnerability;
            KarmaVisuals.Flash(game.World, Position, Vector2.one * KarmaSpace.Scale, new Color(1, 0.2f, 0.3f));
            if (Health <= 0) game.PlayerDied();
        }
        public void Heal(float amount) { Health = Mathf.Min(game.Config.playerHealth, Health + amount); }
    }
}
