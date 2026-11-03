using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class UPBasicPattern : RisingBossPattern
        {
            public UPBasicPattern(KarmaEnemy owner) : base(owner) { }
            public override IEnumerator Execute()
            {
                float gap = owner.game.Player.Position.x - owner.Position.x;
                owner.direction = gap < 0 ? -1 : 1;
                owner.art.localScale = new Vector3(owner.direction * KarmaSpace.GuardianArtScale, KarmaSpace.GuardianArtScale, 1);
                if (Mathf.Abs(gap) > KarmaSpace.Units(2.45f))
                {
                    float step = Mathf.Clamp(Mathf.Abs(gap) - KarmaSpace.Units(1.9f), 0f, KarmaSpace.Units(2.6f));
                    Vector2 close = new Vector2(Mathf.Clamp(owner.Position.x + owner.direction * step, KarmaStage.BossLeftWall, KarmaStage.BossRightWall), KarmaSpace.GuardianGroundY);
                    owner.state = ActionState.Approach;
                    yield return owner.Motion(close, 0.16f, Vector2.zero);
                }
                Vector2 size = KarmaSpace.Units(new Vector2(3.05f, 1.55f));
                Vector2 center = new Vector2(owner.Position.x + owner.direction * KarmaSpace.Units(1.62f), KarmaSpace.GuardianGroundY + KarmaSpace.Units(0.72f));
                owner.state = ActionState.Telegraph;
                var mark = owner.Mark("기본 베기", center, size);

                yield return owner.Wait(owner.Warning(0.32f));
                if (mark != null) Destroy(mark.gameObject);
                owner.state = ActionState.Attack;
                var slash = owner.Attack(center, Vector2.zero, size, 0.16f, Settings.meleeDamageMultiplier, null, 0f, visualPose: "Cleave");
                if (slash != null) slash.Repaint(KarmaRisingArt.Get("Slash"), true);
                yield return owner.Wait(0.18f);
                yield return owner.Recovery(0.62f);
            }

        }
    }
}
