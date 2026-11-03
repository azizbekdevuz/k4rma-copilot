using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaPlayer
    {
        internal bool SwordAreaOverlaps(KarmaEnemy enemy)
        {
            var tuning = game.Config.Techniques;
            float width = tuning.longSlashReach + KarmaSpace.PlayerHalfWidth;
            Vector2 center = Position + Vector2.right * Facing * (tuning.longSlashReach - KarmaSpace.PlayerHalfWidth) * 0.5f;
            return enemy.OverlapsDamageArea(center, new Vector2(width, tuning.longSlashHeight));
        }

        float normalSwordPressureUntil;
        internal bool SwordPressureActive { get { return !airSlashRunning && Time.time < normalSwordPressureUntil && Time.time < nextSword; } }
        bool CanSword() { return game.IsCombat && Health > 0 && Time.time >= nextSword && !airSlashRunning && !Stunned && !Shielded && !techniques.CounterReady(Time.time); }
        Vector2 AttackDirection()
        {
            float vertical = KarmaInput.Vertical;
            Vector2 direction = new Vector2(KarmaInput.Horizontal, vertical);
            return direction.sqrMagnitude > 0 ? direction.normalized : Vector2.right * Facing;
        }
        void ReadUniqueDirectionTap()
        {
            if (!Essences.HasRemnant(Essence.SwordWave) || !CanSword()) { techniques.ClearDirectionTap(); return; }
            int direction = 0, count = 0;
            if (KarmaInput.DirectionPressed(1)) { direction = 1; count++; }
            if (KarmaInput.DirectionPressed(2)) { direction = 2; count++; }
            if (KarmaInput.DirectionPressed(3)) { direction = 3; count++; }
            if (KarmaInput.DirectionPressed(4)) { direction = 4; count++; }
            if (count > 1) { techniques.ClearDirectionTap(); return; }
            if (count == 0 || !techniques.DirectionTap(direction, Time.time, game.Config.Techniques.doubleTapWindow)) return;
            Vector2 aim = direction == 1 ? Vector2.left : direction == 2 ? Vector2.right : direction == 3 ? Vector2.up : Vector2.down;
            BeginDirectionalSlash(aim, game.Config.Techniques.pierceDistance, Essence.SwordWave);
        }
        void Attack()
        {
            if (!CanSword()) return;
            // Preserve a simultaneous jump + attack press through the takeoff frame.
            if (Essences.HasRemnant(Essence.RisingSlash) && airSlashBufferedUntil > Time.time &&
                jumpBufferedUntil > Time.time && !Airborne) return;
            if (Essences.HasRemnant(Essence.RisingSlash) && Airborne && airSlashBufferedUntil > Time.time && techniques.TryAirSlash())
            {
                BeginAirSlash(AttackDirection());
                return;
            }
            animator.Attack();
            nextSword = Time.time + game.Config.swordCooldown;
            var swordTuning = game.Config.Techniques;
            normalSwordPressureUntil = Time.time + swordTuning.slashLife;
            KarmaRemnantVFX.SwordSlash(game.World, Position, Vector2.right * Facing,
                swordTuning.longSlashReach, swordTuning.longSlashHeight, swordTuning.slashLife);
            foreach (var enemy in game.Enemies.ToArray())
            {
                if (enemy == null || !enemy.Alive) continue;
                float width = swordTuning.longSlashReach + KarmaSpace.PlayerHalfWidth;
                Vector2 center = Position + Vector2.right * Facing * (swordTuning.longSlashReach - KarmaSpace.PlayerHalfWidth) * 0.5f;
                if (enemy.OverlapsDamageArea(center, new Vector2(width, swordTuning.longSlashHeight)))
                    SwordHit(enemy, Vector2.right * Facing);
            }
        }
        void BeginDirectionalSlash(Vector2 direction, float distance, Essence essence)
        {
            animator.Attack(direction); nextSword = Time.time + game.Config.swordCooldown;
            animator.PlayPose("Pierce", 0.22f);
            if (direction.x != 0) Facing = direction.x > 0 ? 1 : -1;
            Vector2 from = Position;
            Vector2 to = MoveSlash(direction, distance);
            // Pierce travel is atomic. Cover the execution frame and a brief arrival window;
            // blocked attempts grant nothing, and existing hit protection is never shortened.
            if (essence == Essence.SwordWave && (to - from).sqrMagnitude > 0.0001f)
                invulnerableUntil = Mathf.Max(invulnerableUntil,
                    Time.time + Mathf.Clamp(game.Config.Techniques.pierceInvulnerabilitySeconds, 0, 0.2f));
            Color color = game.Config.ColorOf(essence);
            var flash = KarmaVisuals.Box(game.World, "Directional slash", (from + to) * 0.5f,
                new Vector2(Mathf.Max(KarmaSpace.Units(0.2f), Vector2.Distance(from, to)), KarmaSpace.Units(0.3f)), color, 10);
            KarmaCombatVFX.ApplySprite(flash, "PlayerVFX", "PierceTrail",
                new Vector2(Mathf.Max(KarmaSpace.Units(0.2f), Vector2.Distance(from, to)), KarmaSpace.Units(0.3f)));
            flash.gameObject.AddComponent<KarmaEffect>().Initialize(0.2f);
            flash.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            KarmaRemnantVFX.SwordSlash(game.World, to, direction, KarmaSpace.Units(1.6f), KarmaSpace.Units(2.1f), game.Config.Techniques.slashLife);
            Essences.NotifyUnique(essence);
            foreach (var enemy in game.Enemies.ToArray())
            {
                if (enemy == null || !enemy.Alive) continue;
                if (enemy.IntersectsDamagePath(from, to, KarmaSpace.Units(1.3f)))
                SwordHit(enemy, direction);
            }
        }
        void SwordHit(KarmaEnemy enemy, Vector2 direction)
        {
            float before = enemy.Health;
            Vector2 point = enemy.ClosestDamagePoint(Position);
            enemy.TakeDamage(game.Config.swordDamage);
            if (enemy.Health >= before) return;
            KarmaRemnantVFX.SwordImpact(game, point, direction);
            game.SwordHitFeedback();
        }
        Vector2 MoveSlash(Vector2 direction, float distance)
        {
            Vector2 from = Position;
            Vector2 to = from + direction * Mathf.Max(0, distance);
            to.x = Mathf.Clamp(to.x, KarmaStage.ArenaLeft + KarmaSpace.PlayerHalfWidth, KarmaStage.ArenaRight - KarmaSpace.PlayerHalfWidth);
            float travel = Vector2.Distance(from, to);
            direction = travel > 0 ? (to - from).normalized : direction;
            var hits = Physics2D.BoxCastAll(from, KarmaSpace.PlayerSize, 0, direction, travel);
            foreach (var h in hits)
            {
                if (h.collider.isTrigger || h.collider.GetComponent<KarmaSurface>() == null) continue;
                // One-way platforms block only downward crossings from above.
                if (h.collider.GetComponent<PlatformEffector2D>() != null &&
                    (direction.y >= 0 || from.y - KarmaSpace.PlayerHalfHeight < h.collider.bounds.max.y - KarmaSpace.Units(0.05f))) continue;
                // Ignore contacts we are moving away from (e.g. jumping off the floor).
                if (Vector2.Dot(h.normal, direction) >= -0.001f) continue;
                travel = Mathf.Min(travel, Mathf.Max(0, h.distance - KarmaSpace.Units(0.05f)));
            }
            to = from + direction * travel;
            body.position = to; transform.position = to;
            return to;
        }
        public void BeginRisingSlash()
        {
            CancelAirSlash(); animator.Attack(Vector2.up); body.linearVelocity = new Vector2(body.linearVelocity.x, game.Config.Techniques.risingSpeed);
            KarmaCombatVFX.SpawnSprite(game.World, Position + Vector2.up * KarmaSpace.Scale, "PlayerVFX", "Rising",
                KarmaSpace.Units(new Vector2(1.8f, 3)), 0.25f);
            foreach (var enemy in game.Enemies.ToArray())
                if (enemy != null && enemy.Alive && enemy.OverlapsDamageArea(Position + Vector2.up * KarmaSpace.Scale, KarmaSpace.Units(new Vector2(4.2f, 4))))
                    enemy.TakeDamage(game.Config.Techniques.risingDamage);
        }
        public void BeginSwordWave() { animator.PlayPose("SwordWave", 0.25f); }
    }
}
