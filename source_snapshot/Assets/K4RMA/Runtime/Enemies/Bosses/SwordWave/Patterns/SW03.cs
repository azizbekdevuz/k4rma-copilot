using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class SW03Pattern : SwordBossPattern
        {
            public SW03Pattern(KarmaEnemy owner, SwordBossBehavior sword) : base(owner, sword) { }
            Vector2 ScarSize { get { return KarmaSpace.Units(new Vector2(1.8f * 1.25f, 5 * 1.35f)); } }
            bool PlaceSwordScars(float lowX, float highX, out Vector2 a, out Vector2 b)
            {
                var size = ScarSize;
                var player = sword.SwordPlayerBounds;
                // Measure the corridor between damage edges, in Unity world units.
                float gap = Mathf.Max(3, player.size.x + 0.5f);
                float halfSeparation = (size.x + gap) / 2;
                float inset = halfSeparation + size.x / 2;
                bool pair = highX - lowX >= inset * 2;
                float x = pair ? Mathf.Clamp(player.center.x, lowX + inset, highX - inset) : (lowX + highX) / 2;
                a = new Vector2(x - (pair ? halfSeparation : 0), size.y / 2);
                b = new Vector2(x + (pair ? halfSeparation : 0), size.y / 2);
                return pair;
            }
            public override IEnumerator Execute()
            {
                yield return SwordWaveVolley.Fire(owner, sword, true);
                {
                    // Shift the whole pair inside the arena, independently of the
                    // preceding volley's shorter travelled lane. Keep the gap intact.
                    var size = ScarSize;
                    Vector2 a, b;
                    bool pair = PlaceSwordScars(KarmaStage.ArenaLeft, KarmaStage.ArenaRight, out a, out b);
                    owner.Mark("검흔 상승 범위", a, size);
                    if (pair) owner.Mark("검흔 상승 범위", b, size);

                    owner.animator.PlayWavePhase("Windup");
                    yield return owner.Wait(owner.Warning(0.75f)); owner.ClearPatternObjects();
                    // One attack/contact per scar survives the original rise and
                    // three planted seconds, so touching it cannot deal frame damage.
                    owner.Attack(a, Vector2.zero, size, 3.25f);
                    if (pair) owner.Attack(b, Vector2.zero, size, 3.25f);
                    foreach (var point in pair ? new[] { a, b } : new[] { a })
                    {
                        var impact = KarmaVisuals.Box(owner.effectsRoot, "Wave ground impact / no independent damage",
                        new Vector2(point.x, size.x * 0.3f), Vector2.one, Color.white, 24);
                        KarmaWaveArtwork.Apply(impact, "SW03_GroundImpact", new Vector2(size.x, size.x * 0.6f), Vector2.zero);
                        impact.gameObject.AddComponent<KarmaEffect>().Initialize(0.25f);
                    }
                    owner.animator.PlayWavePhase("Rising");
                    yield return owner.Wait(0.25f);

                    yield return owner.Wait(3);
                }
                yield return owner.Recovery(1);
            }
            public override bool CanReach(Vector2 from, Vector2 target)
            { return SwordWaveVolley.CanReach(owner, sword, EnemyAttack.SW03, from, target); }

        }
    }
}
