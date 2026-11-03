using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class SW01Pattern : SwordBossPattern
        {
            public SW01Pattern(KarmaEnemy owner, SwordBossBehavior sword) : base(owner, sword) { }
            public override IEnumerator Execute()
            { yield return SwordWaveVolley.Fire(owner, sword, false); yield return owner.Recovery(1); }
            public override bool CanReach(Vector2 from, Vector2 target)
            { return SwordWaveVolley.CanReach(owner, sword, EnemyAttack.SW01, from, target); }

        }
    }
}
