using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        bool WaveVisuals { get { return behavior.OrientedWarnings; } }
        SpriteRenderer Mark(string name, Vector2 center, Vector2 size, bool locked = true)
        {
            var color = locked ? new Color(1, 0.55f, 0.2f, 0.3f) : new Color(0.3f, 0.85f, 1, 0.22f);
            var marker = KarmaVisuals.Box(effectsRoot, name, center, size, color, 8);
            marker.gameObject.AddComponent<KarmaAttackWarning>().SetPath(marker, center, center, size, WaveVisuals);
            return marker;
        }
        SpriteRenderer PathMark(Vector2 from, Vector2 to, Vector2 size, bool locked = true)
        {
            if (WaveVisuals)
                to = from + (to - from) * KarmaBossProjectileGeometry.SwordShotLife(from, to - from, 1);
            var marker = Mark(locked ? "고정 공격 경로" : "추적 중인 경로", Vector2.zero, Vector2.one, locked);
            marker.GetComponent<KarmaAttackWarning>().SetPath(marker, from, to, size, false, WaveVisuals,
                behavior.GroundWarning);
            return marker;
        }
        void ClearPatternObjects()
        {
            if (effectsRoot == null) return;
            foreach (Transform child in effectsRoot)
            {
                if (child == behavior.PersistentEffects) continue;
                child.gameObject.SetActive(false); Destroy(child.gameObject);
            }
        }
        KarmaBossAttack Attack(Vector2 at, Vector2 velocity, Vector2 size, float life,
            float multiplier = 1, KarmaAttackContact contact = null, float stun = 0,
            EnemyAttack? pattern = null, Transform parent = null, string visualPose = null, bool parryStun = false)
        {
            var attack = pattern ?? selected;
            var visual = behavior.AttackVisual(attack, velocity);
            return KarmaBossAttack.Spawn(game, this, parent != null ? parent : effectsRoot, at, velocity, size, life,
                Damage * multiplier, visual.Color, contact, stun, visualPose ?? visual.Pose,
                visual.Restrained, visual.SeparateLaunch, visual.ExtraPose, visual.Sheet, parryStun, visual.ShieldBreak);
        }
        IEnumerator Motion(Vector2 target, float duration, Vector2 hitSize, KarmaAttackContact contact = null, Sprite sweepSprite = null)
        {
            Vector2 from = Position; float elapsed = 0;
            var ledger = contact ?? new KarmaAttackContact();
            Vector2 heading = target - from;
            if (Mathf.Abs(heading.x) > 0.01f) { direction = Mathf.Sign(heading.x); art.localScale = new Vector3(Facing * Mathf.Abs(art.localScale.x), art.localScale.y, art.localScale.z); }
            state = ActionState.Attack; animator.Attack(heading.normalized);
            SpriteRenderer sweep = null;
            if (hitSize != Vector2.zero)
            {
                sweep = KarmaCombatVFX.Burst(effectsRoot, from, (sweepSprite != null ? sweepSprite : KarmaArtwork.Sprite("GuardianVFX", "AirSlash")),
                    "Guardian motion slash", hitSize * (WaveVisuals ? 0.9f : 1.4f), duration, 0, new Color(1, 0.45f, 0.15f), WaveVisuals);
                // AirSlash is authored upright: mirror horizontally without turning rising art upside down.
                sweep.transform.parent.localScale = new Vector3(Facing, 1, 1);
            }
            while (elapsed < duration && Active)
            {
                if (game.Paused) { yield return null; continue; }
                elapsed += Time.deltaTime;
                Vector2 before = Position;
                Vector2 to = Vector2.Lerp(from, target, Mathf.Clamp01(elapsed / duration));
                transform.position = to;
                if (sweep != null) sweep.transform.parent.position = to;
                if (hitSize != Vector2.zero) TryAttackHit(before, to, hitSize, Damage, ledger, 0);
                yield return null;
            }
            if (sweep != null)
            {
                Destroy(sweep.transform.parent.gameObject);
                if (heading.y < -0.1f && target.y <= KarmaSpace.GuardianGroundY + 0.1f)
                    KarmaCombatVFX.Burst(effectsRoot, target, KarmaArtwork.Sprite("GuardianVFX", "Landing"),
                        "Guardian landing impact", hitSize * new Vector2(2.4f, 1.2f), 0.35f, 0, new Color(1, 0.55f, 0.1f), WaveVisuals);
            }
        }
        public void TryAttackHit(Vector2 from, Vector2 to, Vector2 size, float amount, KarmaAttackContact contact, float stun, Vector2? attackAxis = null, bool parryStun = false, bool shieldBreak = false)
        {
            if (!Active || game.Paused || contact.Consumed) return;
            var player = game.Player; var p = player.Position;
            var collider = player.GetComponent<Collider2D>();
            var body = collider != null && collider.enabled ? collider.bounds : new Bounds(p, KarmaSpace.PlayerSize);
            p = body.center;
            bool hit = attackAxis.HasValue
                ? KarmaBossGeometry.SweptOrientedBox(from.x, from.y, to.x, to.y, p.x, p.y, attackAxis.Value.x, attackAxis.Value.y,
                    size.x, size.y, body.extents.x, body.extents.y)
                : KarmaBossGeometry.SweptBox(from.x, from.y, to.x, to.y, p.x, p.y, size.x / 2 + body.extents.x, size.y / 2 + body.extents.y);
            if (!hit) return;
            if (!contact.TryConsume()) return;
            if (contact.BreaksShieldOnParry && state == ActionState.Attack && behavior is GuardBossBehavior guard && guard.Heavy.ConsumeQueuedParry())
                player.BeginCounter();
            int parries = player.ParryCount;
            float before = player.Health;
            player.TakeDamage(amount);
            if (player.ParryCount != parries)
            {
                // Projectile parries remain local; GD02 is the shield-breaking parry.
                if (contact.BreaksShieldOnParry || shieldBreak || parryStun) pendingParry = true;
                if (parryStun) KarmaRisingPolish.ParryBreak(effectsRoot, player.Position);
                return;
            }
            if (player.Health < before)
            { contact.RecordDamage(); trial.Hit(); if (stun > 0 && Active) player.Stun(stun); }
        }
    }
}
