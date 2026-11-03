using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Private collaborators can use the combat host without exposing mutable fields to game code.
        // Selection and movement are overridable; hit resolution and lifecycle remain in KarmaEnemy.
        struct BossAttackVisual
        {
            public string Pose, Sheet, ExtraPose;
            public bool Restrained, SeparateLaunch, ShieldBreak;
            public Color Color;
        }
        class BossBehavior
        {
            protected readonly KarmaEnemy owner;
            protected int cycleIndex;
            readonly Dictionary<EnemyAttack, BossPattern> patterns = new Dictionary<EnemyAttack, BossPattern>();
            public BossBehavior(KarmaEnemy owner)
            {
                this.owner = owner;
                Register(EnemyAttack.GroundStrike, new GroundStrikePattern(owner));
            }
            protected void Register(EnemyAttack attack, BossPattern pattern) { patterns[attack] = pattern; }
            protected bool PatternCanReach(EnemyAttack attack, Vector2 from, Vector2 target)
            { return patterns.TryGetValue(attack, out var pattern) && pattern.CanReach(from, target); }
            public virtual bool CanEngage { get { return owner.game.Player.Position.x >= 12; } }
            public virtual float ParryStunSeconds { get { return owner.Tuning.guard.parryStunSeconds; } }
            public virtual bool OrientedWarnings { get { return false; } }
            public virtual bool GroundWarning { get { return false; } }
            public virtual BossAttackVisual AttackVisual(EnemyAttack attack, Vector2 velocity)
            { return new BossAttackVisual { Sheet = "GuardianVFX", Color = new Color(1, 0.38f, 0.12f) }; }
            public IEnumerator Execute(EnemyAttack attack)
            {
                if (!patterns.TryGetValue(attack, out var pattern)) throw new System.ArgumentOutOfRangeException(nameof(attack));
                return pattern.Execute();
            }
            public virtual bool TrySelect(float distance, bool sight, bool escape, out EnemyAttack attack)
            {
                for (int i = 0; i < owner.attacks.Count; i++)
                {
                    var candidate = owner.attacks[cycleIndex++ % owner.attacks.Count];
                    if (!Eligible(candidate, distance, sight, escape)) continue;
                    attack = candidate; return true;
                }
                attack = default; return false;
            }
            protected virtual bool Eligible(EnemyAttack attack, float distance, bool sight, bool escape)
            {
                return KarmaPatternCatalog.CanSelect(attack, owner.previous, distance / KarmaSpace.Scale,
                owner.game.Player.Airborne, sight, owner.FightSeconds < owner.shieldUntil, escape,
                owner.FightSeconds >= owner.nextUltimate);
            }
            protected void Face(float dx)
            {
                owner.direction = dx < 0 ? -1 : 1;
                owner.art.localScale = new Vector3(owner.direction * KarmaSpace.GuardianArtScale, KarmaSpace.GuardianArtScale, 1);
            }
            public virtual bool TickMovement(float dt, float dx)
            {
                Face(dx);
                if (Mathf.Abs(dx) > KarmaSpace.Units(3))
                owner.Move(Mathf.Sign(dx) * owner.game.Config.enemyMoveSpeed * owner.Tuning.moveMultiplier * Balance.movementScale * dt);
                return false;
            }
            public virtual void Tick() { }
            public virtual void OnDamaged() { }
            public virtual void OnSelected() { }
            public virtual void OnCompleted() { }
            public virtual void Interrupt(bool preservePressure)
            { foreach (var pattern in patterns.Values) pattern.Interrupt(); }
            public virtual Transform PersistentEffects { get { return null; } }
            protected KarmaBossBalance Settings { get { return Balance; } }
            static readonly KarmaBossBalance neutralBalance = new KarmaBossBalance();
            public virtual KarmaBossBalance Balance { get { return neutralBalance; } }
            public virtual float DecisionDelay { get { return owner.game.Config.enemyDecisionDelay * owner.Tuning.decisionMultiplier * owner.Tempo * Balance.decisionScale; } }
            public virtual float RetryDelay { get { return 0.2f; } }
        }
        BossBehavior CreateBehavior()
        {
            switch (PrimaryType)
            {
                case Essence.SwordWave: return new SwordBossBehavior(this);
                case Essence.RisingSlash: return new RisingBossBehavior(this);
                case Essence.Guard: return new GuardBossBehavior(this);
                default: return new BossBehavior(this);
            }
        }
    }
}
