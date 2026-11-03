using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class SWBasicPattern : SwordBossPattern
        {
            public SWBasicPattern(KarmaEnemy owner, SwordBossBehavior sword) : base(owner, sword) { }
            public override IEnumerator Execute()
            {
                owner.state = ActionState.Telegraph;
                Vector2 origin = owner.Position;
                Vector2 aim = SwordBossBehavior.SwordAim(origin, sword.SwordPlayerBounds.center, owner.direction);
                float warning = owner.Warning(0.4f);
                var mark = owner.PathMark(origin, origin + aim * sword.BasicSwordSpeed * sword.BasicSwordLife, sword.BasicSwordSize);
                owner.animator.PrepareWaveAttack(warning, false);
                yield return owner.Wait(warning);
                mark.gameObject.SetActive(false); Destroy(mark.gameObject);
                owner.state = ActionState.Attack; owner.animator.Attack(aim);
                owner.Attack(origin, aim * sword.BasicSwordSpeed, sword.BasicSwordSize, sword.BasicSwordLife, Settings.basicDamageMultiplier);
                sword.WaveLaunchVFX(origin, aim * sword.BasicSwordSpeed, sword.BasicSwordSize, false);
                yield return owner.Wait(sword.BasicSwordLife);
                yield return owner.Recovery(0.65f);
            }
            public override bool CanReach(Vector2 from, Vector2 target)
            {
                float facing = target.x < from.x ? -1 : 1;
                Vector2 aim = SwordBossBehavior.SwordAim(from, target + (Vector2)sword.SwordPlayerBounds.center - owner.game.Player.Position, facing);
                return Vector2.Distance(from, target) <= KarmaSpace.Units(5)
                    && sword.SwordShotCanReach(from, aim * sword.BasicSwordSpeed, sword.BasicSwordSize, sword.BasicSwordLife, target);
            }

        }
    }
}
