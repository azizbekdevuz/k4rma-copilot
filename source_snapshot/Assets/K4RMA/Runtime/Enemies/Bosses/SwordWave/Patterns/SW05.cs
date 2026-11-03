using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class SW05Pattern : SwordBossPattern
        {
            public SW05Pattern(KarmaEnemy owner, SwordBossBehavior sword) : base(owner, sword) { }
            Vector2 AerialSwordCastPosition { get { return new Vector2(KarmaStage.ArenaCenter, KarmaSpace.Units(3.75f)); } }
            int thresholdsUsed;
            public bool ThresholdPending
            {
                get { return thresholdsUsed < 2 && owner.Health <= owner.MaxHealth * (thresholdsUsed == 0 ? 0.5f : 0.25f); }
            }
            Vector2 RainSize { get { return sword.SwordSize(EnemyAttack.SW05) * new Vector2(2.45f, 1.6f); } }
            const float FormationTime = 1.8f;
            IEnumerator FlyBoss(Vector2 target, float seconds)
            {
                owner.state = ActionState.Attack;
                Vector2 from = owner.Position;
                float elapsed = 0;
                while (elapsed < seconds && owner.Active)
                {
                    if (!owner.game.Paused)
                    {
                        elapsed += Time.deltaTime;
                        owner.transform.position = Vector2.Lerp(from, target, Mathf.SmoothStep(0, 1, elapsed / seconds));
                    }
                    yield return null;
                }
            }
            IEnumerator FormAerialSword(SpriteRenderer[] blades, KarmaAerialSwordPlanner.Group plan, float seconds)
            {
                // Harmless ascent: the same picture becomes the real projectile only
                // after every blade has reached its reserved, visibly locked sky slot.
                var scales = new Vector3[blades.Length];
                for (int i = 0; i < blades.Length; i++) scales[i] = blades[i].transform.localScale / 0.22f;
                Vector2 source = owner.Position + Vector2.up * 0.5f;
                float elapsed = 0;
                while (elapsed < seconds && owner.Active)
                {
                    if (owner.game.Paused) { yield return null; continue; }
                    elapsed += Time.deltaTime;
                    for (int i = 0; i < blades.Length; i++)
                    {
                        float delay = 0.3f * i / Mathf.Max(1, blades.Length - 1);
                        float t = Mathf.Clamp01((elapsed - delay) / (seconds - 0.42f - delay));
                        // Cubic ease-out comes to rest at the apex. The control points
                        // give the blade an upward launch before it fans sideways.
                        float circle = Mathf.Clamp01(t / 0.65f);
                        float u = 1 - Mathf.Pow(1 - circle, 3);
                        Vector2 end = plan.shots[i].origin;
                        Vector2 orbit = plan.shots[i].orbit;
                        Vector2 p1 = source + new Vector2((end.x - source.x) * 0.12f, 4.5f);
                        Vector2 orbitTangent = new Vector2(-(orbit.y - (source.y + 5.5f)) * 0.85f, (orbit.x - source.x) * 0.11f);
                        Vector2 p2 = orbit - orbitTangent + Vector2.down * 0.6f;
                        float v = 1 - u;
                        Vector2 point = v*v*v*source + 3*v*v*u*p1 + 3*v*u*u*p2 + u*u*u*orbit;
                        float lift = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.6f, 1, t));
                        point = Vector2.Lerp(point, end, lift);
                        Vector2 tangent = 3*v*v*(p1-source) + 6*v*u*(p2-p1) + 3*u*u*(orbit-p2);
                        if (lift > 0.1f) tangent = Vector2.up;
                        float turn = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.45f, 1, t));
                        Vector2 fall = plan.shots[i].velocity.normalized;
                        float fallAngle = Mathf.Atan2(fall.y, fall.x) * Mathf.Rad2Deg;
                        float angle = Mathf.LerpAngle(Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg, fallAngle, turn);
                        var blade = blades[i];
                        blade.transform.position = point;
                        blade.transform.localScale = scales[i] * Mathf.Lerp(0.22f, 1, Mathf.SmoothStep(0, 1, t));
                        blade.transform.rotation = Quaternion.Euler(0, 0, angle + 90);
                        KarmaSW05VFX.ApplyDepth(blade, plan.shots[i].depth, Mathf.SmoothStep(0, 1, t * 6));
                    }
                    yield return null;
                }
            }
            public override IEnumerator Execute()
            {
                // Consume at cast start, never at completion: an interruption or
                // healing cannot re-arm an already used 50% / 25% ultimate.
                if (ThresholdPending) thresholdsUsed++;
                owner.state = ActionState.Telegraph;
                owner.animator.PlayWavePhase("Windup");
                KarmaSW05VFX.Warning(owner.effectsRoot, owner.game, owner.Position, KarmaSpace.Units(new Vector2(2, 2)));
                yield return owner.Wait(0.8f); owner.ClearPatternObjects();
                Vector2 ground = new Vector2(KarmaStage.ArenaCenter, KarmaSpace.GuardianGroundY);
                owner.animator.PlayWavePhase("Rising");

                yield return FlyBoss(AerialSwordCastPosition, 0.7f);
                if (!owner.Active) yield break;
                owner.transform.position = AerialSwordCastPosition;
                owner.fullShield = true;
                owner.animator.PlayWavePhase("Guard");
                KarmaSW05VFX.Corona(owner.effectsRoot, owner.game, owner.transform);
                int seed = KarmaAerialSwordPlanner.ResolveSeed(Settings.aerialSwordSeed);
                var planner = KarmaAerialSwordPlanner.Create(owner.effectsRoot, owner.game, seed);
                int groups = KarmaAerialSwordPlanner.VolleyCount;
                Vector2 size = RainSize;
                float warning = Mathf.Max(FormationTime, owner.Warning(FormationTime));
                for (int group = 0; group < groups && owner.Active; group++)
                {
                    // Dense randomized lanes and a fresh physical escape reservation for every
                    // slam. Finish the previous damage before planning the next one.
                    KarmaSW05VFX.CastPillar(owner.effectsRoot, owner.game, owner.Position, warning);
                    KarmaAerialSwordPlanner.Group plan = null;
                    while (owner.Active && plan == null)
                    {
                        plan = planner.TryBuildGroup(owner.Position, group, warning, size,
                            Settings.swordSpeed * 1.7f, owner.FightSeconds);
                        // Never launch an unsafe group. Live swords keep moving;
                        // re-plan positions/timing without omitting any promised blade.
                        if (plan == null) yield return owner.Wait(0.15f);
                    }
                    if (!owner.Active) yield break;
                    // Reveal in angular order so the blades sweep around the
                    // ellipse, rather than materializing as two horizontal rows.
                    System.Array.Sort(plan.shots, (a, b) =>
                        Mathf.Atan2((a.orbit.y - (owner.Position.y + 6)) / 3.8f,
                            (a.orbit.x - KarmaStage.ArenaCenter) / (KarmaStage.ArenaWidth * 0.36f)).CompareTo(
                        Mathf.Atan2((b.orbit.y - (owner.Position.y + 6)) / 3.8f,
                            (b.orbit.x - KarmaStage.ArenaCenter) / (KarmaStage.ArenaWidth * 0.36f))));
                    var marks = new SpriteRenderer[plan.shots.Length];
                    var blades = new SpriteRenderer[plan.shots.Length];
                    for (int member = 0; member < plan.shots.Length; member++)
                    {
                        var shot = plan.shots[member];
                        // The blade's transverse width is also its landing footprint.
                        // This fixed ground mark never tracks after the sky layout locks.
                        Vector2 axis = shot.velocity.normalized;
                        Vector2 tip = shot.PositionAt(shot.life) + axis * (shot.size.x / 2);
                        marks[member] = KarmaSW05VFX.Warning(owner.effectsRoot, owner.game, new Vector2(tip.x, 0.18f),
                            new Vector2(shot.size.y / Mathf.Abs(axis.y), 0.34f));
                        marks[member].name = "SW05 낙하 지면 전조";
                        blades[member] = KarmaVisuals.Box(owner.effectsRoot, "Forming aerial sword / no damage",
                        shot.origin, Vector2.one, Color.white, 25);
                        KarmaSW05VFX.ApplySword(blades[member], shot.size * 0.22f, Vector2.up);
                        KarmaSW05VFX.ApplyDepth(blades[member], shot.depth);
                        blades[member].transform.position = owner.Position;
                        blades[member].color = Color.clear;
                        KarmaSW05VFX.Ascent(blades[member], shot.size);
                    }

                    yield return FormAerialSword(blades, plan, warning);
                    if (!owner.Active) yield break;
                    for (int member = 0; member < plan.shots.Length; member++)
                    {
                        blades[member].gameObject.SetActive(false); Destroy(blades[member].gameObject);
                        var shot = plan.shots[member];
                        var attack = KarmaSW05RainBlade.Spawn(owner.game, owner, owner.effectsRoot,
                            shot, owner.Damage * Settings.aerialSwordDamageMultiplier, marks[member]);
                        planner.RegisterLaunch(attack.transform);
                    }
                    owner.animator.ReleaseWavePhase();
                    while (owner.Active && planner.HasLiveShots) yield return null;
                    if (!owner.Active) yield break;
                    // Three distinct heavy impacts, with a readable breath and
                    // harmless planted swords before the next summon.
                    yield return owner.Wait(0.7f);
                }
                if (!owner.Active) yield break;
                owner.fullShield = false;
                owner.animator.PlayWavePhase("Rising");
                yield return FlyBoss(ground, 0.7f);
                // Let the last harmless impact finish during descent rather than
                // clearing it in the same frame as the last blade's ground contact.
                yield return owner.Recovery(2);
            }
            public override bool CanReach(Vector2 from, Vector2 target)
            {
                // This set piece relocates to center and covers the arena. Its
                // eligibility is the two health gates, not a ranged attack ray.
                return target.x >= KarmaStage.ArenaLeft && target.x <= KarmaStage.ArenaRight
                    && 0.8f + 1.4f + KarmaAerialSwordPlanner.VolleyCount * (Mathf.Max(FormationTime, owner.Warning(FormationTime)) + 1 + 0.7f)
                        + 2 < Mathf.Max(5, owner.Tuning.patternTimeout);
            }

        }
    }
}
