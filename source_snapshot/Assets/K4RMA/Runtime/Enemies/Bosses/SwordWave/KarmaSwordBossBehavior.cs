using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class SwordBossBehavior : BossBehavior
        {
            public Vector2 WaveSize { get { return new Vector2(Mathf.Max(KarmaSpace.Units(0.2f), Settings.swordWidth), Mathf.Clamp(Settings.swordHeight, KarmaSpace.Units(0.2f), KarmaSpace.Units(0.8f))); } }
            public Vector2 SwordSize(EnemyAttack attack)
            {
                if (attack == EnemyAttack.SW02)
                {
                    // Bound the whole ordinary jump, including the player's real body.
                    // The head at its apex stays below the top of this single slash.
                    float gravity = Mathf.Max(0.01f, Mathf.Abs(Physics2D.gravity.y * owner.game.Config.gravityScale));
                    float jump = owner.game.Config.jumpSpeed * owner.game.Config.jumpSpeed / (2 * gravity);
                    float height = jump + SwordPlayerBounds.size.y + 0.6f - SW02Pattern.Floor;
                    return new Vector2(Mathf.Max(WaveSize.x * 1.35f * 1.7f, height * 0.6f), height);
                }
                return WaveSize * (attack == EnemyAttack.SW01 || attack == EnemyAttack.SW03 ? new Vector2(1.35f, 1.30f) : Vector2.one * 1.30f);
            }
            public Bounds SwordPlayerBounds
            {
                get
                {
                    var collider = owner.game.Player.GetComponent<Collider2D>();
                    return collider != null && collider.enabled ? collider.bounds
                    : new Bounds(owner.game.Player.Position, KarmaSpace.PlayerSize);
                }
            }
            public float WaveLife { get { return Mathf.Clamp(Settings.swordLifetime, 0.5f, 2); } }
            public Vector2 BasicSwordSize { get { return WaveSize * new Vector2(0.9f, 1); } }
            public float BasicSwordSpeed { get { return Mathf.Max(KarmaSpace.Units(5), Settings.swordSpeed * 0.8f); } }
            public float BasicSwordLife { get { return KarmaSpace.Units(5) / BasicSwordSpeed; } }
            public void WaveLaunchVFX(Vector2 origin, Vector2 velocity, Vector2 size, bool aimed)
            {
                var sprite = KarmaArtwork.Sprite("GuardianVFX", aimed ? "Beam" : "Cleave");
                if (sprite == null) return;
                Vector2 axis = velocity.normalized;
                var bounds = sprite.bounds.size;
                float width = Mathf.Abs(axis.x) * bounds.x + Mathf.Abs(axis.y) * bounds.y;
                float height = Mathf.Abs(axis.y) * bounds.x + Mathf.Abs(axis.x) * bounds.y;
                float fit = Mathf.Min(size.x / width, size.y / height) * 0.7f;
                // Fit the rotated core/echo to this shot's footprint. No collider or damage;
                // ownership keeps the brief swing and existing afterimages cancellable.
                KarmaCombatVFX.Burst(owner.effectsRoot, origin, sprite, "Wave sword swing",
                new Vector2(bounds.x, bounds.y) * fit, 0.18f,
                Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg, new Color(1, 0.38f, 0.12f), owner.WaveVisuals);
            }
            public static Vector2 SwordAim(Vector2 origin, Vector2 target, float facing)
            {
                Vector2 aim = (target - origin).normalized;
                return aim.sqrMagnitude < 0.01f ? Vector2.right * facing : aim;
            }
            public bool SwordShotCanReach(Vector2 origin, Vector2 velocity, Vector2 size, float life, Vector2 target)
            {
                Vector2 end = origin + velocity * KarmaBossProjectileGeometry.SwordShotLife(origin, velocity, life);
                var axis = velocity.sqrMagnitude > 0.000001f ? velocity.normalized : Vector2.right;
                var player = SwordPlayerBounds;
                target += (Vector2)player.center - owner.game.Player.Position;
                return KarmaBossGeometry.SweptOrientedBox(origin.x, origin.y, end.x, end.y, target.x, target.y,
                axis.x, axis.y, size.x, size.y, player.extents.x, player.extents.y);
            }
            bool SwordAttackCanReachFrom(EnemyAttack candidate, Vector2 from, Vector2 target)
            { return PatternCanReach(candidate, from, target); }
            bool TrySelectSwordAttack(float distance, bool sight, bool escape, out EnemyAttack next)
            {
                next = owner.previous;
                // SW05 is a once-per-health-gate set piece. Its two triggers
                // bypass ordinary cooldown/repeat rules after the current action.
                if (owner.attacks.Contains(EnemyAttack.SW05) && ultimate.ThresholdPending && !counter.Formed)
                { next = EnemyAttack.SW05; return true; }
                bool basic = owner.FightSeconds >= nextSwordBasic && sight && SwordAttackCanReach(EnemyAttack.SWBasic, owner.game.Player.Position);
                if (basic && (swordBasicDue || distance < KarmaSpace.Units(2.5f)))
                { next = EnemyAttack.SWBasic; return true; }
                bool repeatAvailable = false;
                for (int i = 0; i < owner.attacks.Count; i++)
                {
                    var candidate = owner.attacks[cycleIndex++ % owner.attacks.Count];
                    if (!SwordCandidateAvailable(candidate, owner.Position, distance, sight, escape)) continue;
                    // Keep the inherited cycle; repeat only when all other unlocked skills are ineligible.
                    if (candidate == owner.previous) { next = candidate; repeatAvailable = true; continue; }
                    next = candidate;
                    return true;
                }
                if (!repeatAvailable && basic) { next = EnemyAttack.SWBasic; return true; }
                return repeatAvailable;
            }
            bool SwordCandidateAvailable(EnemyAttack candidate, Vector2 from, float distance, bool sight, bool escape)
            {
                if (candidate == EnemyAttack.SW05) return ultimate.ThresholdPending && !counter.Formed;
                if (candidate == EnemyAttack.SW04 && (counter.Formed || owner.FightSeconds < counter.NextReadyAt)) return false;
                // Keep the defensive overlay readable: its four committed counters
                // can accompany ordinary waves, while larger set pieces wait for it.
                if (counter.Formed && candidate != EnemyAttack.SW01 && candidate != EnemyAttack.SW02) return false;
                return KarmaPatternCatalog.CanSelect(candidate, owner.previous, distance / KarmaSpace.Scale, owner.game.Player.Airborne,
                sight, owner.FightSeconds < owner.shieldUntil, escape, owner.FightSeconds >= owner.nextUltimate)
                && SwordAttackCanReachFrom(candidate, from, owner.game.Player.Position);
            }
            public bool HasSwordCandidate(Vector2 from)
            {
                float distance = Mathf.Abs(owner.game.Player.Position.x - from.x);
                bool escape = from.x > KarmaStage.ArenaLeft + 5 && from.x < KarmaStage.ArenaRight - 5;
                bool sight = owner.ClearShot(from, owner.game.Player.Position);
                if (owner.FightSeconds >= nextSwordBasic && sight && SwordAttackCanReachFrom(EnemyAttack.SWBasic, from, owner.game.Player.Position)) return true;
                foreach (var candidate in owner.attacks)
                if (SwordCandidateAvailable(candidate, from, distance, sight, escape)) return true;
                return false;
            }
            bool SwordAttackCanReach(EnemyAttack candidate, Vector2 target)
            { return SwordAttackCanReachFrom(candidate, owner.Position, target); }
            readonly SwordBossMovement movement;
            readonly SW04Pattern counter;
            readonly SW05Pattern ultimate;
            int swordSkillsSinceBasic;
            float nextSwordBasic;
            bool swordBasicDue;
            new KarmaSwordBossSettings Settings { get { return owner.Tuning.sword; } }
            public override KarmaBossBalance Balance { get { return Settings; } }
            public SwordBossBehavior(KarmaEnemy owner) : base(owner)
            {
                movement = new SwordBossMovement(owner, this);
                counter = new SW04Pattern(owner, this);
                Register(EnemyAttack.SWBasic, new SWBasicPattern(owner, this));
                Register(EnemyAttack.SW01, new SW01Pattern(owner, this));
                Register(EnemyAttack.SW02, new SW02Pattern(owner, this));
                Register(EnemyAttack.SW03, new SW03Pattern(owner, this));
                Register(EnemyAttack.SW04, counter);
                ultimate = new SW05Pattern(owner, this);
                Register(EnemyAttack.SW05, ultimate);
            }
            public override bool CanEngage { get { return base.CanEngage || Vector2.Distance(owner.Position, owner.game.Player.Position) <= KarmaSpace.Units(7); } }
            public override bool OrientedWarnings { get { return true; } }
            public override bool GroundWarning { get { return owner.selected == EnemyAttack.SW05; } }
            public override BossAttackVisual AttackVisual(EnemyAttack attack, Vector2 velocity)
            {
                return new BossAttackVisual { Sheet = "GuardianVFX", Color = new Color(1, 0.38f, 0.12f),
                    Restrained = true, SeparateLaunch = attack == EnemyAttack.SWBasic || attack == EnemyAttack.SW01 || attack == EnemyAttack.SW02,
                    ExtraPose = KarmaWaveArtwork.Pose(attack, velocity.sqrMagnitude <= 0.01f) };
            }
            public void RequestBasic() { swordBasicDue = true; }
            public override bool TrySelect(float distance, bool sight, bool escape, out EnemyAttack attack)
            { return TrySelectSwordAttack(distance, sight, escape, out attack); }
            public override bool TickMovement(float dt, float dx)
            {
                if (owner.attacks.Contains(EnemyAttack.SW05) && ultimate.ThresholdPending && !counter.Formed) return false;
                return movement.Tick(dt, dx);
            }
            public override void OnSelected()
            {
                movement.OnSelected();
                if (owner.selected == EnemyAttack.SWBasic)
                { nextSwordBasic = owner.FightSeconds + Mathf.Max(0.5f, Settings.swordBasicCooldown); swordBasicDue = false; swordSkillsSinceBasic = 0; }
            }
            public override void OnCompleted()
            {
                movement.OnCompleted();
                if (owner.selected != EnemyAttack.SWBasic)
                { swordSkillsSinceBasic++; swordBasicDue = swordSkillsSinceBasic >= Mathf.Clamp(Settings.swordSkillsBetweenBasics, 1, 6); }
            }
            public override float DecisionDelay { get { return KarmaPatternCatalog.IsUltimate(owner.selected) ? base.DecisionDelay : base.DecisionDelay * Mathf.Clamp(Settings.swordDecisionMultiplier, 0.25f, 1.5f); } }
            public override float RetryDelay { get { return Mathf.Max(0.05f, Settings.swordSelectionRetry); } }
            public override void Tick() { counter.Tick(); movement.ObservePressure(); }
            public override void OnDamaged() { movement.RememberSwordPressure(); counter.OnDamaged(); }
            public override Transform PersistentEffects { get { return counter.EffectsRoot; } }
            public override void Interrupt(bool preservePressure)
            { base.Interrupt(preservePressure); movement.Interrupt(preservePressure); }

        }
    }
}
