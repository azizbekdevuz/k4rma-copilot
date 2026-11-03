using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Owns ground spacing and pressure memory; never chooses or interrupts an attack.
        sealed class SwordBossMovement
        {
            readonly KarmaEnemy owner;
            readonly SwordBossBehavior behavior;
            KarmaSwordBossSettings Settings { get { return owner.Tuning.sword; } }
            float nextSwordRetreat, swordRetreatRemaining, swordRetreatUntil, swordRetreatDirection;
            float nextSwordApproach, swordApproachUntil, swordRetreatTarget, swordPressureTarget;
            bool swordAttackedSinceRetreat = true, swordApproaching, swordPressurePending;
            public SwordBossMovement(KarmaEnemy owner, SwordBossBehavior behavior) { this.owner = owner; this.behavior = behavior; }
            float SwordSpacingDistance()
            {
                // Use the incoming-hit silhouette, including its asymmetric pivot.
                // Movement/idle safety and basic-wave reach are different bands.
                var body = owner.DamageBounds;
                float front = owner.game.Player.Position.x < owner.Position.x ? owner.Position.x - body.min.x : body.max.x - owner.Position.x;
                return Mathf.Max(Mathf.Clamp(Settings.swordRetreatStopDistance, 9, 14),
                owner.game.Config.Techniques.longSlashReach + Mathf.Max(0, front)
                + Mathf.Clamp(Settings.swordMeleeSpacingMargin, 0.25f, 1.25f));
            }
            public void RememberSwordPressure()
            {
                if (owner.PrimaryType != Essence.SwordWave || !owner.Active) return;
                swordPressurePending = true;
                // Capture before Hurt changes the pose. A smaller next pose must
                // not erase a real hit or shrink its requested separation.
                swordPressureTarget = Mathf.Max(swordPressureTarget, SwordSpacingDistance());
            }
            bool RetreatSword(float dt, float dx)
            {
                if (swordRetreatRemaining <= 0)
                {
                    // SW01's existing minimum range is wider than a close SW02.
                    // After using SW02, make room once if that rule still excludes
                    // SW01; keep its attack range and the inherited cycle intact.
                    bool waveRoom = owner.previous == EnemyAttack.SW02 && owner.attacks.Contains(EnemyAttack.SW01)
                    && !KarmaPatternCatalog.CanSelect(EnemyAttack.SW01, owner.previous, Mathf.Abs(dx) / KarmaSpace.Scale,
                    owner.game.Player.Airborne, true, false, true, true);
                    float safeGap = Mathf.Max(swordPressureTarget, SwordSpacingDistance());
                    bool pressure = swordPressurePending && Mathf.Abs(dx) < safeGap;
                    // React before the player has reached the wide melee body.
                    // The completed-attack token and cooldown still limit escapes.
                    bool nearMelee = Mathf.Abs(dx) < Mathf.Max(Settings.swordRetreatStartDistance,
                    safeGap - Mathf.Clamp(Settings.swordMeleeSpacingMargin, 0.25f, 1.25f));
                    if ((!waveRoom && !pressure && !nearMelee)
                    || owner.FightSeconds < nextSwordRetreat || !swordAttackedSinceRetreat || owner.game.Player.ContinuousSwordAttack)
                    return false;
                    swordApproaching = false;
                    swordPressurePending = false;
                    swordPressureTarget = 0;
                    swordRetreatTarget = safeGap;
                    swordRetreatDirection = dx < 0 ? 1 : -1;
                    swordRetreatRemaining = Mathf.Clamp(Settings.swordRetreatDistance, 2, 18);
                    swordRetreatUntil = owner.FightSeconds + Mathf.Clamp(Settings.swordRetreatDuration, 0.35f, 0.75f);
                    nextSwordRetreat = owner.FightSeconds + Mathf.Max(4.5f, Settings.swordRetreatCooldown);
                    // Even a blocked attempt yields to an attack before retrying.
                    swordAttackedSinceRetreat = false;
                }
                float stop = swordRetreatTarget;
                if (owner.FightSeconds >= swordRetreatUntil || dx * swordRetreatDirection >= 0 || Mathf.Abs(dx) >= stop)
                { FinishSwordRetreat(); return false; }
                // A short burst can gain ground on pursuit; ordinary walking was
                // slower than the player and reduced the gap despite moving away.
                float speed = Mathf.Min(Mathf.Max(1, Settings.swordRetreatSpeed), owner.game.Config.moveSpeed * 2.5f);
                float step = swordRetreatDirection * Mathf.Min(Mathf.Min(swordRetreatRemaining, stop - Mathf.Abs(dx)), speed * dt);
                float travelled = MoveSwordOnGround(step);
                if (travelled <= 0.0001f) { FinishSwordRetreat(); return false; }
                swordRetreatRemaining = Mathf.Max(0, swordRetreatRemaining - travelled);
                if (swordRetreatRemaining <= 0.001f) FinishSwordRetreat();

                return true;
            }
            void FinishSwordRetreat()
            {
                swordRetreatRemaining = 0;
                swordRetreatTarget = 0;
                // A new hit during this move remains queued behind the cooldown;
                // starting the move already consumed the previous request.
                behavior.RequestBasic();
                owner.timer = Mathf.Max(owner.timer, 0.18f);
                nextSwordApproach = owner.FightSeconds + Mathf.Max(0.25f, owner.timer + 0.05f);
            }
            bool ApproachSword(float dt, float dx)
            {
                float distance = Mathf.Abs(dx), preferred = Mathf.Max(Mathf.Clamp(Settings.swordPreferredDistance, 9, 14), SwordSpacingDistance());
                if (distance <= preferred)
                {
                    if (swordApproaching) FinishSwordApproach();
                    return false;
                }
                if (!swordApproaching)
                {
                    if (owner.FightSeconds < nextSwordApproach) return false;
                    if (distance <= Mathf.Max(Settings.swordApproachStartDistance, preferred + 1) && behavior.HasSwordCandidate(owner.Position)) return false;
                    swordApproaching = true;
                    swordApproachUntil = owner.FightSeconds + 1.2f;
                }
                // A faster fleeing player must not postpone attacks indefinitely.
                // A reachable ranged attack gets a turn after this short approach.
                if (owner.FightSeconds >= swordApproachUntil && behavior.HasSwordCandidate(owner.Position))
                { FinishSwordApproach(); return false; }
                float speed = Mathf.Min(owner.game.Config.enemyMoveSpeed * owner.Tuning.moveMultiplier * Settings.movementScale, owner.game.Config.moveSpeed * 0.8f);
                float step = Mathf.Sign(dx) * Mathf.Min(distance - preferred, speed * dt);
                if (MoveSwordOnGround(step) <= 0.0001f)
                {
                    FinishSwordApproach();
                    return false;
                }

                return true;
            }
            void FinishSwordApproach()
            {
                swordApproaching = false;
                nextSwordApproach = owner.FightSeconds + Mathf.Max(0.25f, owner.timer + 0.05f);
            }
            float MoveSwordOnGround(float step)
            {
                float before = owner.Position.x;
                // A boss already inside the wall margin can walk back out of it,
                // but clamping must never reverse or enlarge the requested step.
                float left = Mathf.Min(before, KarmaStage.BossLeftWall + 0.5f);
                float right = Mathf.Max(before, KarmaStage.BossRightWall - 0.5f);
                float after = Mathf.Clamp(before + step, left, right);
                step = after - before;
                if (Mathf.Abs(step) <= 0.0001f) return 0;
                foreach (var hit in Physics2D.BoxCastAll(owner.Position, KarmaSpace.Units(new Vector2(0.8f, 1.6f)), 0,
                Vector2.right * Mathf.Sign(step), Mathf.Abs(step) + 0.1f))
                if (hit.collider.GetComponent<KarmaSurface>() != null && hit.collider.GetComponent<PlatformEffector2D>() == null)
                return 0;
                bool floor = false;
                foreach (var hit in Physics2D.RaycastAll(new Vector2(after, owner.Position.y), Vector2.down, KarmaSpace.GuardianGroundY + 0.5f))
                if (hit.collider.GetComponent<KarmaSurface>() != null) { floor = true; break; }
                if (!floor) return 0;
                owner.Move(step);
                return Mathf.Abs(owner.Position.x - before);
            }

            public bool Tick(float dt, float dx)
            {
                if (dt <= 0) return true;
                if (Mathf.Abs(dx) > 0.5f) owner.direction = Mathf.Sign(dx);
                if (owner.direction == 0) owner.direction = owner.art.localScale.x < 0 ? -1 : 1;
                owner.art.localScale = new Vector3(owner.direction * KarmaSpace.GuardianArtScale, KarmaSpace.GuardianArtScale, 1);
                if (!RetreatSword(dt, dx) && !ApproachSword(dt, dx)) return false;
                owner.animator.ShowGuardianSpacing(); return true;
            }
            public void ObservePressure()
            {
                if (owner.game.Player.SwordPressureActive && owner.game.Player.SwordAreaOverlaps(owner)) RememberSwordPressure();
                else if (Mathf.Abs(owner.game.Player.Position.x - owner.Position.x) > Mathf.Max(swordPressureTarget, SwordSpacingDistance()) + 0.25f)
                { swordPressurePending = false; swordPressureTarget = 0; }
            }
            public void OnSelected() { swordRetreatRemaining = 0; swordApproaching = false; }
            public void OnCompleted() { swordAttackedSinceRetreat = true; }
            public void Interrupt(bool preservePressure)
            {
                swordRetreatRemaining = 0; swordApproaching = false; nextSwordApproach = 0;
                swordRetreatTarget = 0;
                if (!preservePressure || !owner.Active) { swordPressurePending = false; swordPressureTarget = 0; }
            }
        }
    }
}
