using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class UP03Pattern : RisingBossPattern
        {
            public UP03Pattern(KarmaEnemy owner) : base(owner) { }
            public override IEnumerator Execute()
            {
                Vector2 start = new Vector2(owner.Position.x, KarmaSpace.GuardianGroundY);
                yield return owner.Motion(start, 0.14f, Vector2.zero);
                float room = owner.direction > 0 ? KarmaStage.ArenaRight - 1.2f - start.x : start.x - (KarmaStage.ArenaLeft + 1.2f);
                float height = Mathf.Clamp(Settings.groundBladeHeight, KarmaSpace.Units(4.4f), KarmaSpace.Units(6.4f));
                float gap = KarmaSpace.Units(0.45f);
                float reach = Mathf.Max(KarmaSpace.Units(8f), room * 0.78f);
                Vector2 size = new Vector2(Mathf.Max(KarmaSpace.Units(4f), reach - gap), height);
                Vector2 center = new Vector2(start.x + owner.direction * (gap + size.x * 0.5f), height * 0.5f);
                var mark = owner.Mark("전방 대검기 · 등 뒤로", center, size);
                owner.state = ActionState.Telegraph;

                var preview = KarmaRisingArt.Get("WaveEcho");
                if (preview != null)
                {
                    var ghost = KarmaCombatVFX.Burst(owner.effectsRoot, new Vector2(start.x + owner.direction * (gap + size.x * 0.22f), height * 0.45f),
                    preview, "검기 예고", new Vector2(size.x * 0.42f, height * 0.55f), 0.7f, 0f, new Color(0.72f, 0.86f, 1f));
                    if (ghost != null) ghost.transform.parent.localScale = new Vector3(owner.direction, 1, 1);
                }
                yield return owner.Wait(owner.Warning(0.72f));
                if (mark != null) Destroy(mark.gameObject);
                var wave = owner.Attack(center, Vector2.zero, size, 0.42f, Settings.waveDamageMultiplier, null, 0f, visualPose: "Wave");
                if (wave != null)
                {
                    wave.Repaint(KarmaRisingArt.Get("Wave"), true);
                    if (wave.Visual != null) wave.Visual.flipX = owner.direction < 0;
                }
                yield return owner.Wait(0.48f);
                yield return owner.Recovery(1.15f);
            }

        }
    }
}
