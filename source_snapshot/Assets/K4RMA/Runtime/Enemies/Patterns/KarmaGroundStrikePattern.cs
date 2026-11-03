using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class GroundStrikePattern : BossPattern
        {
            public GroundStrikePattern(KarmaEnemy owner) : base(owner) { }
            public override IEnumerator Execute()
            {
                int count = owner.Enraged ? 3 : 2;
                for (int i = 0; i < count && owner.Active; i++)
                {
                    // Sample again BETWEEN swings; each orange warning locks its target.
                    float dx = owner.game.Player.Position.x - owner.Position.x;
                    owner.direction = dx < 0 ? -1 : 1;
                    owner.art.localScale = new Vector3(owner.direction * KarmaSpace.GuardianArtScale, KarmaSpace.GuardianArtScale, 1);
                    bool finish = i == count - 1;
                    float step = Mathf.Clamp(Mathf.Abs(dx) - KarmaSpace.Units(1.8f), 0, KarmaSpace.Units(finish ? 3 : 2));
                    Vector2 target = new Vector2(Mathf.Clamp(owner.Position.x + owner.direction * step, KarmaStage.BossLeftWall, KarmaStage.BossRightWall), owner.Position.y);
                    Vector2 size = KarmaSpace.Units(finish ? new Vector2(4.2f, 2.1f) : new Vector2(3.3f, 1.8f));
                    Vector2 center = target + Vector2.right * owner.direction * KarmaSpace.Units(1.6f);
                    owner.state = ActionState.Telegraph;
                    var mark = owner.Mark(finish ? "연속 베기 · 마무리" : "연속 베기 · 첫 타", center, size);

                    yield return owner.Wait(owner.Warning(i == 0 ? owner.game.Config.enemyTelegraphTime : 0.55f));
                    yield return owner.Motion(target, 0.16f, Vector2.zero);
                    Destroy(mark.gameObject); owner.animator.Attack(); owner.state = ActionState.Attack;
                    owner.Attack(center, Vector2.zero, size, 0.18f, finish ? 1.25f : 0.85f);
                    yield return owner.Wait(0.2f);
                }
                yield return owner.Recovery(0.85f);
            }
        }
    }
}
