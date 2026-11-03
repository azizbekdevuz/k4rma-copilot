using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class UP05Pattern : RisingBossPattern
        {
            public UP05Pattern(KarmaEnemy owner) : base(owner) { }
            public override IEnumerator Execute()
            {
                owner.state = ActionState.Telegraph;
                var charge = BeginRisingCharge(KarmaRisingCue.Charging);

                yield return owner.Wait(owner.Warning(0.62f));
                EndRisingCharge(charge);
                float left = KarmaStage.ArenaLeft + 4f, right = KarmaStage.ArenaRight - 4f;
                float ground = KarmaSpace.GuardianGroundY, mid = KarmaSpace.Units(3.15f), high = KarmaSpace.Units(5.35f);
                Vector2[] points =
                {
                    new Vector2(left, ground), new Vector2(right, mid),
                    new Vector2(left, high), new Vector2(right, ground),
                    new Vector2(left, mid), new Vector2(right, high),
                    new Vector2(left, ground), new Vector2(right, high),
                    new Vector2(left, mid)
                };
                yield return owner.Motion(points[0], 0.24f, Vector2.zero);
                Vector2 slash = KarmaSpace.Units(new Vector2(1.85f, 1.15f));
                var trail = KarmaRisingArt.Get("Slash");
                for (int i = 0; i < 8 && owner.Active; i++)
                {
                    var mark = owner.PathMark(points[i], points[i + 1], slash);

                    yield return owner.Wait(owner.Warning(Mathf.Min(0.46f, Settings.crossingWarning)));
                    if (mark != null) Destroy(mark.gameObject);
                    float duration = Mathf.Clamp(Vector2.Distance(points[i], points[i + 1]) / KarmaSpace.Units(16f), 0.22f, 0.4f);
                    yield return owner.Motion(points[i + 1], duration, slash, null, trail);
                }
                yield return owner.Motion(new Vector2(Mathf.Clamp(owner.Position.x, KarmaStage.BossLeftWall, KarmaStage.BossRightWall), ground), 0.26f, Vector2.zero);
                yield return owner.Recovery(2.5f);
            }

        }
    }
}
