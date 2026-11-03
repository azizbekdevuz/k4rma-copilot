using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class UP02Pattern : RisingBossPattern
        {
            public UP02Pattern(KarmaEnemy owner) : base(owner) { }
            IEnumerator AirPursuit(bool recover)
            {
                Vector2 origin = owner.Position, target = origin;
                SpriteRenderer path = null;
                owner.state = ActionState.Telegraph;
                var charge = BeginRisingCharge(KarmaRisingCue.Tracking);
                float elapsed = 0;
                while (elapsed < 0.36f && owner.Active)
                {
                    if (owner.game.Paused) { yield return null; continue; }
                    target = owner.game.Player.Position;
                    target.x = Mathf.Clamp(target.x, Mathf.Max(KarmaStage.ArenaLeft + 2, origin.x - KarmaSpace.Units(7)), Mathf.Min(KarmaStage.ArenaRight - 2, origin.x + KarmaSpace.Units(7)));
                    target.y = Mathf.Clamp(target.y, KarmaSpace.Units(2.5f), KarmaSpace.Units(7));
                    if (path == null) path = RisingPath(origin, target, bodyAttackSize, KarmaRisingCue.Tracking);
                    else path.GetComponent<KarmaAttackWarning>().SetPath(path, origin, target, bodyAttackSize);
                    elapsed += Time.deltaTime; yield return null;
                }
                if (path != null) Destroy(path.gameObject);
                if (charge != null) charge.Lock();
                KarmaRisingPolish.LockPop(owner.effectsRoot, target);
                path = RisingPath(origin, target, bodyAttackSize, KarmaRisingCue.Locked);

                yield return owner.Wait(owner.Warning(0.36f));
                EndRisingCharge(charge);
                if (path != null) Destroy(path.gameObject);
                yield return owner.Motion(target, 0.28f, bodyAttackSize, null, KarmaRisingArt.Get("Slash"));
                yield return owner.Motion(new Vector2(target.x, KarmaSpace.GuardianGroundY), 0.32f, Vector2.zero);
                if (recover && owner.Enraged)
                {
                    yield return owner.Wait(Mathf.Max(0.45f, Settings.crossingReset * owner.Tempo));
                    yield return AirPursuit(false);
                }
                if (recover) yield return owner.Recovery(0.9f);
            }
            public override IEnumerator Execute() { return AirPursuit(true); }

        }
    }
}
