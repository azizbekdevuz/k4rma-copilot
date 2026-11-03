using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class UP04Pattern : RisingBossPattern
        {
            public UP04Pattern(KarmaEnemy owner) : base(owner) { }
            public override IEnumerator Execute()
            {
                Vector2 start = owner.Position;
                owner.fullShield = true;
                owner.state = ActionState.Approach;
                KarmaRisingPolish.ShieldPlant(owner.effectsRoot, start, KarmaSpace.Units(new Vector2(2.2f, 2.6f)));
                float apexY = KarmaSpace.GuardianGroundY + Mathf.Clamp(Settings.riseHeight * 0.55f, KarmaSpace.Units(1.6f), KarmaSpace.Units(3.2f));
                Vector2 apex = new Vector2(Mathf.Clamp(start.x, KarmaStage.ArenaLeft + 3, KarmaStage.ArenaRight - 3), apexY);
                yield return owner.Motion(apex, 0.2f, Vector2.zero);
                Vector2 landing = new Vector2(Mathf.Clamp(start.x + owner.direction * KarmaSpace.Units(1.2f), KarmaStage.ArenaLeft + 3, KarmaStage.ArenaRight - 3), KarmaSpace.GuardianGroundY);
                yield return owner.Motion(landing, 0.16f, bodyAttackSize);
                Vector2 origin = new Vector2(landing.x, KarmaSpace.Units(2.35f));
                float life = Settings.fragmentLifetime;
                int count = FragmentDegrees.Length;
                var warnings = new SpriteRenderer[count];
                var ends = new Vector2[count];
                var velocities = new Vector2[count];
                var launches = new Vector2[count];
                Vector2 shard = KarmaSpace.Units(new Vector2(1.15f, 0.78f));
                for (int i = 0; i < count; i++)
                {
                    float rad = FragmentDegrees[i] * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                    launches[i] = origin + dir * KarmaSpace.Units(0.9f);
                    AimFragment(launches[i], FragmentDegrees[i], life, out ends[i], out velocities[i]);
                    warnings[i] = owner.PathMark(launches[i], ends[i], shard);
                }
                owner.state = ActionState.Telegraph;

                yield return owner.Wait(owner.Warning(0.48f));
                for (int i = 0; i < warnings.Length; i++) if (warnings[i] != null) Destroy(warnings[i].gameObject);
                owner.fullShield = false;
                KarmaRisingPolish.ShieldPlant(owner.effectsRoot, landing, KarmaSpace.Units(new Vector2(2.4f, 2.4f)));
                for (int i = 0; i < count && owner.Active; i++)
                {
                    var shot = owner.Attack(launches[i], velocities[i], shard, life, Settings.fragmentDamageMultiplier, new KarmaAttackContact(), 0f, visualPose: "Blade", parryStun: true);
                    if (shot == null) continue;
                    shot.name = "방패 파편";
                    shot.Repaint(KarmaRisingArt.Get("Fragment" + (i % 8)), false);
                    if (shot.Visual != null) shot.Visual.color = Color.white;
                }
                yield return owner.Wait(life);
                yield return owner.Recovery(1.15f);
            }
            static readonly float[] FragmentDegrees = { -6f, 12f, 30f, 48f, 66f, 84f, 102f, 120f, 138f, 156f, 174f, 192f, 208f, 222f };
            void AimFragment(Vector2 origin, float degrees, float life, out Vector2 end, out Vector2 velocity)
            {
                var dir = new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
                dir.Normalize();
                float limit = KarmaSpace.Units(14f);
                if (Mathf.Abs(dir.x) > 0.02f)
                {
                    float wall = dir.x > 0 ? KarmaStage.ArenaRight - 0.8f : KarmaStage.ArenaLeft + 0.8f;
                    limit = Mathf.Min(limit, (wall - origin.x) / dir.x);
                }
                if (dir.y > 0.02f) limit = Mathf.Min(limit, (KarmaSpace.Units(7.4f) - origin.y) / dir.y);
                else if (dir.y < -0.02f) limit = Mathf.Min(limit, (KarmaSpace.Units(0.45f) - origin.y) / dir.y);
                limit = Mathf.Max(KarmaSpace.Units(3.5f), limit);
                end = origin + dir * limit;
                velocity = dir * (limit / Mathf.Max(0.35f, life));
            }

        }
    }
}
