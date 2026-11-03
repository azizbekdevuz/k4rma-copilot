using System.Collections.Generic;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaPlayer
    {
        readonly KarmaAirSlashMotion airSlashMotion = new KarmaAirSlashMotion();
        readonly HashSet<KarmaEnemy> airSlashHits = new HashSet<KarmaEnemy>();
        internal bool ContinuousSwordAttack { get { return airSlashRunning; } }
        bool airSlashRunning;
        Vector2 airSlashDirection, airSlashPrevious;
        float airSlashBufferedUntil, airRecoveryLeft, airLaunchX;
        void BeginAirSlash(Vector2 direction)
        {
            var tuning = game.Config.Techniques;
            airSlashDirection = direction;
            airSlashPrevious = Position;
            airSlashMotion.Begin(tuning.airSlashDuration);
            airSlashRunning = true;
            airSlashHits.Clear(); airSlashBufferedUntil = 0; airRecoveryLeft = 0;
            jumpBufferedUntil = 0; lastGrounded = -10;
            nextSword = Time.time + Mathf.Max(game.Config.swordCooldown, tuning.airSlashDuration);
            if (direction.x != 0) Facing = direction.x > 0 ? 1 : -1;
            animator.Attack(direction);
            animator.PlayPose("AirSlash", tuning.airSlashDuration + 0.04f);
            Essences.NotifyUnique(Essence.RisingSlash);
            KarmaRemnantVFX.SwordSlash(transform, Vector2.zero, direction, KarmaSpace.Units(1.6f), KarmaSpace.Units(1.8f), tuning.airSlashDuration + 0.04f);
        }
        void DamageAirSlash()
        {
            Vector2 from = airSlashPrevious + airSlashDirection * KarmaSpace.Units(0.35f);
            Vector2 to = Position + airSlashDirection * KarmaSpace.Units(0.85f);
            airSlashPrevious = Position;
            foreach (var enemy in game.Enemies.ToArray())
            {
                if (enemy == null || !enemy.Alive || airSlashHits.Contains(enemy)) continue;
                if (!enemy.IntersectsDamagePath(from, to, KarmaSpace.Units(0.75f))) continue;
                airSlashHits.Add(enemy);
                SwordHit(enemy, airSlashDirection);
            }
        }
        void AdvanceAirSlash()
        {
            var tuning = game.Config.Techniques;
            if (airSlashMotion.Active)
            {
                // Rigidbody movement keeps interpolation and terrain collision active.
                body.gravityScale = 0;
                float travel = airSlashMotion.Advance(Time.fixedDeltaTime) * tuning.airSlashDistance;
                body.linearVelocity = airSlashDirection * (travel / Time.fixedDeltaTime);
            }
            else
            {
                airSlashRunning = false; airSlashHits.Clear();
                body.gravityScale = game.Config.gravityScale;
                airRecoveryLeft = Mathf.Max(0.02f, tuning.airSlashRecovery);
                airLaunchX = airSlashDirection.x * game.Config.moveSpeed * tuning.airSlashHorizontalBoost;
                body.linearVelocity = new Vector2(airLaunchX, game.Config.jumpSpeed * tuning.airSlashJumpScale);
            }
        }
        void CancelAirSlash()
        {
            airSlashRunning = false; airSlashMotion.Reset(); airSlashHits.Clear();
            airSlashBufferedUntil = airRecoveryLeft = 0;
        }
    }
}
