using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        sealed class GD05Pattern : GuardBossPattern
        {
            public GD05Pattern(KarmaEnemy owner) : base(owner) { }

            public const float TrialCenter = 20;
            public const float TrialRadius = 10 * KarmaSpace.Scale;
            const float TrialAttackScale = 1.3f;
            const float TrialWaveSpacing = 2.6f * KarmaSpace.Scale;

            public override IEnumerator Execute()
            {
                owner.fullShield = true; owner.state = ActionState.Telegraph;

                yield return owner.Wait(1);
                float limit = TrialRadius - KarmaSpace.Units(0.6f);
                owner.game.Player.ConfineArena(TrialCenter - limit, TrialCenter + limit);
                yield return owner.Motion(new Vector2(TrialCenter, KarmaSpace.GuardianGroundY), 0.5f, Vector2.zero);
                var dome = KarmaGuardDome.Create(owner.game, owner.effectsRoot, new Vector2(TrialCenter, 0), TrialRadius);
                GuardDecoration("GuardBossVFX", "Seal", new Vector2(TrialCenter, KarmaSpace.Units(0.25f)),
                    new Vector2(TrialRadius * 2, KarmaSpace.Units(2)), new Color(1, 1, 1, 0.5f), 3);
                owner.trial.Begin(Mathf.Clamp(Settings.trialAttackCount, 3, 8), Settings.trialAllowedHits);
                for (int i = 0; i < owner.trial.Total; i++)
                {
                    var contact = new KarmaAttackContact(); // Simultaneous hazards share one contact per beat.
                    owner.state = ActionState.Telegraph;
                    bool left = i % 2 == 0;
                    switch (i % 5)
                    {
                        case 0: yield return TrialHorizontal(left, contact); break;
                        case 1: yield return TrialRain(contact); break;
                        case 2: yield return TrialPincer(contact); break;
                        case 3: yield return TrialPillars(contact); break;
                        default: yield return TrialWeave(left, contact); break;
                    }
                    owner.trial.CompleteAttack();
                    yield return owner.Wait(Mathf.Max(0.4f, Settings.trialAttackGap * owner.Tempo));
                }
                bool passed = owner.trial.Passed;
                owner.trial.Clear();
                if (!passed)
                {
                    // Keep the room intact while cracks and light announce its own detonation.

                    dome.Charge(0.95f);
                    yield return owner.Wait(0.95f);
                    dome.Explode();

                    var contact = new KarmaAttackContact();
                    float active = 0;
                    while (active < 0.18f && owner.Active)
                    {
                        if (!owner.game.Paused)
                        {
                            var player = owner.game.Player;
                            if (dome.Overlaps(player.Position, new Vector2(KarmaSpace.PlayerHalfWidth, KarmaSpace.PlayerHalfHeight)))
                                owner.TryAttackHit(player.Position, player.Position, Vector2.zero, owner.Damage * Settings.trialExplosionMultiplier, contact, 0);
                            active += Time.deltaTime;
                        }
                        yield return null;
                    }
                    yield return owner.Wait(0.57f);
                    owner.ClearPatternObjects(); owner.game.Player.ReleaseArena();
                    yield return owner.Recovery(1.5f);
                }
                else
                {
                    BreakShield();
                    owner.game.Player.ReleaseArena(); dome.Dissolve();
                    yield return owner.Wait(0.4f);

                    yield return owner.Recovery(Mathf.Max(1, Settings.trialStunSeconds),
                        scaleDuration: false);
                }
            }
            Vector2 TrialWaveSize { get { return WaveSize * TrialAttackScale; } }
            Vector2 TrialSide(bool left, float y)
            {
                // Keep the enlarged projectile's outer top corner below the curved roof.
                float top = y + TrialWaveSize.y / 2 + KarmaSpace.Units(0.1f);
                float edge = Mathf.Sqrt(Mathf.Max(0, TrialRadius * TrialRadius - top * top))
                    - TrialWaveSize.x / 2 - KarmaSpace.Units(0.1f);
                return new Vector2(TrialCenter + (left ? -edge : edge), y);
            }
            Vector2 TrialWaveStart(bool left, float y, int leading)
            { return TrialSide(left, y) + Vector2.right * (left ? 1 : -1) * leading * TrialWaveSpacing; }
            float TrialWaveLife(float y, int leading = 0)
            { return (Vector2.Distance(TrialSide(true, y), TrialSide(false, y)) - leading * TrialWaveSpacing) / KarmaSpace.Units(10); }
            Vector2 TrialVelocity(bool left) { return Vector2.right * KarmaSpace.Units(left ? 10 : -10); }
            KarmaBossAttack TrialShot(Vector2 at, Vector2 velocity, Vector2 size, float life, KarmaAttackContact contact, string pose = "Wave")
            {
                string sheet = "GuardBossVFX";
                if (pose == "Ward") pose = "Shard";
                return KarmaBossAttack.Spawn(owner.game, owner, owner.effectsRoot, at, velocity, size, life, owner.Damage * Settings.trialDamageMultiplier,
                    new Color(1, 0.75f, 0.15f), contact, 0, pose, visualSheet: sheet);
            }
            SpriteRenderer[] TrialWaveMarks(bool left, float y)
            {
                var marks = new SpriteRenderer[2];
                for (int i = 0; i < marks.Length; i++)
                    marks[i] = owner.PathMark(TrialWaveStart(left, y, i), TrialSide(!left, y), TrialWaveSize);
                return marks;
            }
            void LaunchTrialWaves(bool left, float y, SpriteRenderer[] marks, KarmaAttackContact contact)
            {
                for (int i = 0; i < marks.Length; i++)
                {
                    marks[i].gameObject.SetActive(false); Destroy(marks[i].gameObject);
                    TrialShot(TrialWaveStart(left, y, i), TrialVelocity(left), TrialWaveSize, TrialWaveLife(y, i), contact);
                }
            }
            IEnumerator TrialHorizontal(bool left, KarmaAttackContact contact)
            {
                float y = KarmaSpace.Units(0.9f);
                var marks = TrialWaveMarks(left, y);

                yield return owner.Wait(owner.Warning(0.75f)); owner.state = ActionState.Attack;
                LaunchTrialWaves(left, y, marks, contact);
                yield return owner.Wait(TrialWaveLife(y) + 0.05f);
            }
            IEnumerator TrialRain(KarmaAttackContact contact)
            {
                var starts = new Vector2[5]; var lives = new float[5]; var marks = new SpriteRenderer[5];
                Vector2 size = KarmaSpace.Units(new Vector2(0.65f, 0.85f)) * TrialAttackScale;
                float aim = Mathf.Clamp(owner.game.Player.Position.x, TrialCenter - KarmaSpace.Units(3.4f), TrialCenter + KarmaSpace.Units(3.4f));
                float longest = 0;
                for (int i = 0; i < starts.Length; i++)
                {
                    float x = aim + KarmaSpace.Units((i - 2) * 2.6f);
                    float dx = Mathf.Abs(x - TrialCenter) + size.x / 2;
                    float roof = Mathf.Sqrt(TrialRadius * TrialRadius - dx * dx);
                    starts[i] = new Vector2(x, roof - size.y / 2 - KarmaSpace.Units(0.25f));
                    lives[i] = (starts[i].y - size.y / 2 - KarmaSpace.Units(0.1f)) / KarmaSpace.Units(12);
                    longest = Mathf.Max(longest, lives[i]);
                    marks[i] = owner.PathMark(starts[i], starts[i] + Vector2.down * KarmaSpace.Units(12) * lives[i], size);
                }

                yield return owner.Wait(owner.Warning(0.9f)); owner.state = ActionState.Attack;
                for (int i = 0; i < starts.Length; i++)
                {
                    Destroy(marks[i].gameObject);
                    TrialShot(starts[i], Vector2.down * KarmaSpace.Units(12), size, lives[i], contact, "Ward");
                }
                yield return owner.Wait(longest + 0.05f);
            }
            IEnumerator TrialPincer(KarmaAttackContact contact)
            {
                float y = KarmaSpace.Units(0.9f);
                var left = TrialWaveMarks(true, y); var right = TrialWaveMarks(false, y);

                yield return owner.Wait(owner.Warning(0.85f));
                owner.state = ActionState.Attack;
                LaunchTrialWaves(true, y, left, contact);
                LaunchTrialWaves(false, y, right, contact);
                yield return owner.Wait(TrialWaveLife(y) + 0.05f);
            }
            IEnumerator TrialPillars(KarmaAttackContact contact)
            {
                Vector2 size = KarmaSpace.Units(new Vector2(0.9f, 3.8f)) * TrialAttackScale;
                var centers = new Vector2[5]; var marks = new SpriteRenderer[5];
                for (int i = 0; i < centers.Length; i++)
                {
                    centers[i] = new Vector2(TrialCenter + KarmaSpace.Units((i - 2) * 3.4f), size.y / 2);
                    marks[i] = owner.Mark("바닥 봉인 · 기둥 사이가 안전", centers[i], size);
                }

                yield return owner.Wait(owner.Warning(0.85f)); owner.state = ActionState.Attack;
                for (int i = 0; i < centers.Length; i++)
                {
                    Destroy(marks[i].gameObject);
                    TrialShot(centers[i], Vector2.zero, size, 0.3f, contact, "Pillar");
                }
                yield return owner.Wait(0.35f);
            }
            IEnumerator TrialWeave(bool left, KarmaAttackContact contact)
            {
                float high = KarmaSpace.Units(2.7f), low = KarmaSpace.Units(0.9f);
                var highMarks = TrialWaveMarks(left, high); var lowMarks = TrialWaveMarks(!left, low);

                yield return owner.Wait(owner.Warning(0.85f)); owner.state = ActionState.Attack;
                LaunchTrialWaves(left, high, highMarks, contact);
                yield return owner.Wait(0.85f);
                LaunchTrialWaves(!left, low, lowMarks, contact);
                yield return owner.Wait(TrialWaveLife(low) + 0.05f);
            }
        }
    }
}
