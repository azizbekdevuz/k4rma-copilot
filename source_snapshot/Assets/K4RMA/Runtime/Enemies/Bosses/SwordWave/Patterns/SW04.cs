using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class SW04Pattern : SwordBossPattern
        {
            public SW04Pattern(KarmaEnemy owner, SwordBossBehavior sword) : base(owner, sword) { }
            float swordGuardUntil, nextGuardResponse;
            bool guardResponsePending, guardResponseRunning;
            Transform swordGuardRoot;
            Coroutine swordGuardRoutine;
            float nextSwordGuard;
            float GuardTrackingTime { get { return 0.35f; } }
            float GuardLockTime { get { return Mathf.Max(0.3f, Settings.counterWarning); } }
            float GuardShotGap { get { return Mathf.Max(0.16f, Settings.swordGuardResponseCooldown - GuardTrackingTime - GuardLockTime); } }
            public override IEnumerator Execute()
            {
                if (swordGuardRoot != null) yield break;
                owner.state = ActionState.Telegraph;
                owner.animator.PlayWavePhase("Windup");
                var formation = owner.Mark("검막 형성", owner.Position, KarmaSpace.Units(new Vector2(3, 3)));
                yield return owner.Wait(owner.Warning(0.7f)); Destroy(formation.gameObject);
                owner.state = ActionState.Attack; owner.swordGuard = true; guardResponsePending = guardResponseRunning = false;
                owner.animator.PlayWavePhase("Guard");
                swordGuardUntil = owner.FightSeconds + Mathf.Max(0.6f, Settings.swordGuardDuration);
                nextGuardResponse = owner.FightSeconds;
                nextSwordGuard = owner.FightSeconds + Mathf.Max(8, Settings.swordGuardDuration + 4);
                swordGuardRoot = new GameObject("Independent sword guard").transform;
                swordGuardRoot.SetParent(owner.effectsRoot, false);
                var blades = new SpriteRenderer[4];
                for (int i = 0; i < blades.Length; i++)
                {
                    blades[i] = KarmaVisuals.Box(swordGuardRoot, "회전 검막 · 접촉 피해 없음", owner.Position, Vector2.one, Color.white, 13);
                    KarmaWaveArtwork.Apply(blades[i], "SW04_CounterWave", sword.SwordSize(EnemyAttack.SW04), Vector2.right);
                }
                swordGuardRoutine = owner.StartCoroutine(MaintainSwordGuard(blades));
                // Only formation occupies the action slot. The buff, aim warnings
                // and counter projectiles own a separate lifetime and object root.
                yield return owner.Recovery(0.45f);
            }
            IEnumerator MaintainSwordGuard(SpriteRenderer[] blades)
            {
                while ((owner.swordGuard && owner.FightSeconds < swordGuardUntil || guardResponsePending) && owner.Active)
                {
                    if (owner.game.Paused) { yield return null; continue; }
                    UpdateSwordGuardBlades(blades);
                    if (guardResponsePending)
                    {
                        guardResponsePending = false; guardResponseRunning = true;
                        var shots = new KarmaBossAttack[blades.Length];
                        var size = sword.SwordSize(EnemyAttack.SW04);
                        for (int i = 0; i < blades.Length && owner.Active; i++)
                        {
                            Vector2 origin = blades[i].transform.position, aim = Vector2.right * owner.direction;
                            var mark = SwordGuardPathMark(origin, origin, size, false);
                            float trackingUntil = owner.FightSeconds + GuardTrackingTime;
                            while (owner.Active && owner.FightSeconds < trackingUntil)
                            {
                                if (owner.game.Paused) { yield return null; continue; }
                                UpdateSwordGuardBlades(blades, i);
                                origin = blades[i].transform.position;
                                aim = ((Vector2)sword.SwordPlayerBounds.center - origin).normalized;
                                if (aim.sqrMagnitude < 0.01f) aim = Vector2.right * owner.direction;
                                blades[i].transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
                                mark.GetComponent<KarmaAttackWarning>().SetPath(mark, origin,
                                origin + aim * Settings.swordSpeed * KarmaBossProjectileGeometry.SwordShotLife(origin, aim * Settings.swordSpeed, sword.WaveLife), size, false, true);
                                yield return null;
                            }
                            if (!owner.Active) yield break;
                            mark.gameObject.SetActive(false); Destroy(mark.gameObject);
                            mark = SwordGuardPathMark(origin, origin + aim * Settings.swordSpeed * sword.WaveLife, size);
                            float lockedUntil = owner.FightSeconds + GuardLockTime;
                            while (owner.Active && owner.FightSeconds < lockedUntil)
                            {
                                if (!owner.game.Paused) UpdateSwordGuardBlades(blades, i);
                                yield return null;
                            }
                            if (!owner.Active) yield break;
                            mark.gameObject.SetActive(false); Destroy(mark.gameObject);
                            blades[i].gameObject.SetActive(false); Destroy(blades[i].gameObject); blades[i] = null;
                            shots[i] = owner.Attack(origin, aim * Settings.swordSpeed, size, sword.WaveLife,
                            pattern: EnemyAttack.SW04, parent: swordGuardRoot);
                            if (i + 1 < blades.Length)
                            {
                                float nextAt = owner.FightSeconds + GuardShotGap;
                                while (owner.Active && owner.FightSeconds < nextAt)
                                {
                                    if (!owner.game.Paused) UpdateSwordGuardBlades(blades);
                                    yield return null;
                                }
                            }
                        }
                        if (!owner.Active) yield break;
                        owner.swordGuard = false;
                        // Committed counters finish independently without changing
                        // the current attack's animation, state or recovery window.
                        bool flying;
                        do
                        {
                            flying = false;
                            foreach (var shot in shots) flying |= shot != null && shot.gameObject.activeInHierarchy;
                            if (flying) yield return null;
                        } while (owner.Active && flying);
                        ClearSwordGuard(false);
                        yield break;
                    }
                    yield return null;
                }
                ClearSwordGuard(false);
            }
            SpriteRenderer SwordGuardPathMark(Vector2 from, Vector2 to, Vector2 size, bool locked = true)
            {
                var mark = KarmaVisuals.Box(swordGuardRoot, locked ? "고정 공격 경로" : "추적 중인 경로",
                Vector2.zero, Vector2.one, locked ? new Color(1, 0.55f, 0.2f, 0.3f) : new Color(0.3f, 0.85f, 1, 0.22f), 8);
                to = from + (to - from) * KarmaBossProjectileGeometry.SwordShotLife(from, to - from, 1);
                mark.gameObject.AddComponent<KarmaAttackWarning>().SetPath(mark, from, to, size, true, true);
                return mark;
            }
            void ClearSwordGuard(bool stopRoutine = true)
            {
                if (stopRoutine && swordGuardRoutine != null) owner.StopCoroutine(swordGuardRoutine);
                swordGuardRoutine = null;
                if (swordGuardRoot != null)
                { swordGuardRoot.gameObject.SetActive(false); Destroy(swordGuardRoot.gameObject); swordGuardRoot = null; }
                owner.swordGuard = guardResponsePending = guardResponseRunning = false;
                swordGuardUntil = nextGuardResponse = 0;
            }
            void UpdateSwordGuardBlades(SpriteRenderer[] blades, int aiming = -1)
            {
                // Defense expires on its original deadline, even if the committed
                // four-shot execution continues without armor after that deadline.
                if (owner.FightSeconds >= swordGuardUntil) owner.swordGuard = false;
                for (int i = 0; i < blades.Length; i++)
                {
                    if (blades[i] == null || i == aiming) continue;
                    float angle = owner.FightSeconds * 3 + i * Mathf.PI / 2;
                    Vector2 at = owner.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * KarmaSpace.Units(1.6f);
                    float margin = sword.SwordSize(EnemyAttack.SW04).magnitude / 2 + 0.05f;
                    at.x = Mathf.Clamp(at.x, KarmaStage.ArenaLeft + margin, KarmaStage.ArenaRight - margin);
                    at.y = Mathf.Clamp(at.y, margin, KarmaSpace.Units(12) - margin);
                    blades[i].transform.position = at;
                    blades[i].transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                }
            }
            public Transform EffectsRoot { get { return swordGuardRoot; } }
            public bool Formed { get { return swordGuardRoot != null; } }
            public float NextReadyAt { get { return nextSwordGuard; } }
            public void Tick() { if (owner.swordGuard && owner.FightSeconds >= swordGuardUntil) owner.swordGuard = false; }
            public void OnDamaged()
            {
                if (owner.swordGuard && owner.FightSeconds < swordGuardUntil && owner.FightSeconds >= nextGuardResponse && !guardResponsePending && !guardResponseRunning)
                { guardResponsePending = true; nextGuardResponse = owner.FightSeconds + Mathf.Max(0.5f, Settings.swordGuardResponseCooldown); }
            }
            public override void Interrupt() { ClearSwordGuard(); }
            public override bool CanReach(Vector2 from, Vector2 target)
            {
                float facing = target.x < from.x ? -1 : 1;
                return sword.SwordShotCanReach(from, SwordBossBehavior.SwordAim(from, target, facing) * Settings.swordSpeed,
                    sword.SwordSize(EnemyAttack.SW04), sword.WaveLife, target);
            }

        }
    }
}
