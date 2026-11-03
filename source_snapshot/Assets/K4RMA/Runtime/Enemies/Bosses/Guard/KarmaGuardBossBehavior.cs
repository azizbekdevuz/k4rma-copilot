using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class GuardBossBehavior : BossBehavior
        {
            new KarmaGuardBossSettings Settings { get { return owner.Tuning.guard; } }
            public override KarmaBossBalance Balance { get { return Settings; } }
            readonly GD01Pattern shield;
            public readonly GD02Pattern Heavy;
            public GuardBossBehavior(KarmaEnemy owner) : base(owner)
            {
                shield = new GD01Pattern(owner);
                Heavy = new GD02Pattern(owner);
                Register(EnemyAttack.GD02, Heavy);
                Register(EnemyAttack.GD03, new GD03Pattern(owner));
                Register(EnemyAttack.GD04, new GD04Pattern(owner, Heavy));
                Register(EnemyAttack.GD05, new GD05Pattern(owner));
            }
            public void Initialize() { shield.RefreshAutoShield(); owner.shieldHalo.enabled = true; owner.animator.ConfigureHeavyStrike(owner.game.Player.Essences.PersonalizationOrder); }
            public override void Tick() { shield.Tick(); }
            public override void OnCompleted() { Heavy.Interrupt(); }
            public override BossAttackVisual AttackVisual(EnemyAttack attack, Vector2 velocity)
            {
                string pose = attack == EnemyAttack.GD03 ? "Shard" : attack == EnemyAttack.GD04 ? "Pillar"
                    : velocity.sqrMagnitude > 0.01f ? "Wave" : attack == EnemyAttack.GD02 ? "Bash" : null;
                return new BossAttackVisual { Color = new Color(1, 0.7f, 0.12f),
                    Pose = pose, Sheet = pose != null ? "GuardBossVFX" : "GuardianVFX" };
            }
        }
    }
}
