using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class UP01Pattern : RisingBossPattern
        {
            public UP01Pattern(KarmaEnemy owner) : base(owner) { }
            public override IEnumerator Execute()
            {
                Vector2 start = owner.Position;
                float apexY = KarmaSpace.GuardianGroundY + Mathf.Clamp(Settings.riseHeight, KarmaSpace.Units(2.4f), KarmaSpace.Units(5.2f));
                Vector2 apex = new Vector2(Mathf.Clamp(start.x + owner.direction * KarmaSpace.Units(1.4f), KarmaStage.ArenaLeft + 3, KarmaStage.ArenaRight - 3), apexY);
                owner.state = ActionState.Telegraph;
                var rise = RisingPath(start, apex, bodyAttackSize, KarmaRisingCue.Charging);
                var charge = BeginRisingCharge(KarmaRisingCue.Charging);

                yield return owner.Wait(owner.Warning(0.42f));
                EndRisingCharge(charge);
                if (rise != null) Destroy(rise.gameObject);
                yield return owner.Motion(apex, 0.32f, Vector2.zero);
                Vector2 landing = new Vector2(Mathf.Clamp(owner.game.Player.Position.x, KarmaStage.ArenaLeft + 4, KarmaStage.ArenaRight - 4), KarmaSpace.GuardianGroundY);
                landing.x = Mathf.Clamp(landing.x, apex.x - KarmaSpace.Units(4.5f), apex.x + KarmaSpace.Units(4.5f));
                Vector2 area = new Vector2(KarmaSpace.Units(6.5f), KarmaSpace.Units(1.25f));
                Vector2 impact = new Vector2(landing.x, KarmaSpace.Units(0.62f));
                var drop = RisingPath(apex, landing, bodyAttackSize, KarmaRisingCue.Locked);
                var zone = owner.Mark("UP01 착지 범위", impact, area);
                charge = BeginRisingCharge(KarmaRisingCue.Locked);
                KarmaRisingPolish.LockPop(owner.effectsRoot, impact);

                yield return owner.Wait(owner.Warning(0.7f));
                EndRisingCharge(charge);
                if (drop != null) Destroy(drop.gameObject);
                if (zone != null) Destroy(zone.gameObject);
                var contact = new KarmaAttackContact();
                yield return owner.Motion(landing, 0.26f, bodyAttackSize, contact, KarmaRisingArt.Get("Slash"));
                KarmaRisingPolish.AreaImpact(owner.game, owner.effectsRoot, impact, area * new Vector2(1.15f, 1.8f));
                var blast = owner.Attack(impact, Vector2.zero, area, 0.22f, Settings.landingDamageMultiplier, contact, 0f, visualPose: "GroundRing");
                if (blast != null) blast.Repaint(KarmaRisingArt.Get("Impact"), true);
                if (owner.Enraged)
                {
                    yield return owner.Wait(0.28f);
                    Vector2 echoSize = new Vector2(area.x * 0.72f, area.y);
                    var echo = owner.Mark("착지 여파", impact, echoSize);

                    yield return owner.Wait(owner.Warning(0.42f));
                    if (echo != null) Destroy(echo.gameObject);
                    var second = owner.Attack(impact, Vector2.zero, echoSize, 0.18f, 0.7f, null, 0f, visualPose: "GroundRing");
                    if (second != null) second.Repaint(KarmaRisingArt.Get("Impact"), true);
                    KarmaRisingPolish.AreaImpact(owner.game, owner.effectsRoot, impact, echoSize);
                    yield return owner.Wait(0.22f);
                }
                else yield return owner.Wait(0.34f);
                yield return owner.Recovery(1.05f);
            }

        }
    }
}
