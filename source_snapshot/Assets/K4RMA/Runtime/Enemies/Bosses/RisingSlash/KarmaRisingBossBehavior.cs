using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class RisingBossBehavior : BossBehavior
        {
            new KarmaRisingBossSettings Settings { get { return owner.Tuning.rising; } }
            public override KarmaBossBalance Balance { get { return Settings; } }
            float nextStride;
            public RisingBossBehavior(KarmaEnemy owner) : base(owner)
            {
                Register(EnemyAttack.GroundStrike, new UPBasicPattern(owner));
                Register(EnemyAttack.UP01, new UP01Pattern(owner));
                Register(EnemyAttack.UP02, new UP02Pattern(owner));
                Register(EnemyAttack.UP03, new UP03Pattern(owner));
                Register(EnemyAttack.UP04, new UP04Pattern(owner));
                Register(EnemyAttack.UP05, new UP05Pattern(owner));
            }
            public override float ParryStunSeconds { get { return Settings.fragmentParryStunSeconds; } }
            public override bool TickMovement(float dt, float dx)
            {
                Face(dx);
                if (Mathf.Abs(dx) > Settings.preferredDistance)
                {
                    owner.Move(Mathf.Sign(dx) * owner.game.Config.enemyMoveSpeed * owner.Tuning.moveMultiplier * Settings.movementScale * Settings.approachSpeedMultiplier * dt);
                    if (owner.FightSeconds >= nextStride)
                    { nextStride = owner.FightSeconds + 0.12f; KarmaRisingPolish.Stride(owner.effectsRoot, owner.Position, Mathf.Sign(dx)); }
                }
                return false;
            }
            protected override bool Eligible(EnemyAttack attack, float distance, bool sight, bool escape)
            { return (attack != EnemyAttack.GroundStrike || distance <= Settings.meleeReach) && base.Eligible(attack, distance, sight, escape); }
            public override float DecisionDelay { get { return Mathf.Max(0.26f, base.DecisionDelay * Settings.attackDecisionMultiplier); } }

        }
    }
}
