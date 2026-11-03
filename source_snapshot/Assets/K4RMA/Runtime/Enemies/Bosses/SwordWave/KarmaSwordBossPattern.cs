using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        abstract class SwordBossPattern : BossPattern
        {
            protected readonly SwordBossBehavior sword;
            protected KarmaSwordBossSettings Settings { get { return owner.Tuning.sword; } }
            protected SwordBossPattern(KarmaEnemy owner, SwordBossBehavior sword) : base(owner) { this.sword = sword; }
        }
    }
}
