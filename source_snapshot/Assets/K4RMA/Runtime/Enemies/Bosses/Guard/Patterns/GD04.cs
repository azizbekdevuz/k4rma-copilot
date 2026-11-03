using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        sealed class GD04Pattern : GuardBossPattern
        {
            readonly GD02Pattern heavy;
            public GD04Pattern(KarmaEnemy owner, GD02Pattern heavy) : base(owner) { this.heavy = heavy; }

            public override IEnumerator Execute()
            {
                owner.frontalGuard = true; owner.state = ActionState.Telegraph;
                owner.direction = owner.game.Player.Position.x < owner.Position.x ? -1 : 1;
                int count = Mathf.Clamp(Settings.barrierSteps, 3, 8);
                float width = KarmaSpace.Units(1.65f);
                float room = Mathf.Max(0.1f, (owner.Facing > 0 ? KarmaStage.ArenaRight - owner.Position.x
                    : owner.Position.x - KarmaStage.ArenaLeft) - KarmaSpace.Units(0.15f));
                float gap = Mathf.Min(KarmaSpace.Units(1.2f), room * 0.2f);
                // Fit toward the player instead of flipping the attack behind the boss.
                // Keep the normal footprint where possible, and at least three steps if they fit.
                int fitting = Mathf.FloorToInt((room - gap - width) / (width * 0.92f)) + 1;
                count = Mathf.Min(count, Mathf.Max(3, fitting));
                while (count > 1 && (room - gap) / (1 + (count - 1) * 0.92f) < KarmaSpace.Units(0.75f)) count--;
                width = Mathf.Min(width, (room - gap) / (1 + (count - 1) * 0.92f));
                float stride = width * 0.92f, offset = gap + width / 2;
                owner.art.localScale = new Vector3(owner.Facing * KarmaSpace.GuardianArtScale, KarmaSpace.GuardianArtScale, 1);
                int terrainDirection = owner.Facing;
                bool sawPlayerInFront = (owner.game.Player.Position.x - owner.Position.x) * terrainDirection >= 0;
                var sizes = new Vector2[count]; var centers = new Vector2[count];
                var marks = new SpriteRenderer[count];
                for (int i = 0; i < count; i++)
                {
                    float height = KarmaSpace.Units(1.1f + i * 0.6f);
                    sizes[i] = new Vector2(width, height);
                    centers[i] = new Vector2(owner.Position.x + owner.direction * (offset + i * stride), height / 2);
                    marks[i] = owner.Mark("GD04 계단 상승 예고 " + (i + 1), centers[i], sizes[i]);
                }

                yield return owner.Wait(owner.Warning(0.8f));
                owner.state = ActionState.Attack; owner.animator.Attack(Vector2.down);
                bool finished = false;
                var wave = owner.StartCoroutine(RiseBarrierWave(centers, sizes, marks, () => finished = true));
                float held = 0;
                while (owner.Active && (held < 0.6f || !finished))
                {
                    if (owner.game.Paused) { yield return null; continue; }
                    float signedDistance = (owner.game.Player.Position.x - owner.Position.x) * terrainDirection;
                    if (signedDistance >= 0) sawPlayerInFront = true;
                    // One rear counter per GD04, while the terrain stays on its original side.
                    if ((owner.FightSeconds < owner.shieldUntil) && sawPlayerInFront && signedDistance < -KarmaSpace.PlayerHalfWidth
                        && -signedDistance <= KarmaSpace.Units(KarmaPatternCatalog.GuardHeavyReach) + KarmaSpace.PlayerHalfWidth)
                    {
                        owner.selected = owner.previous = EnemyAttack.GD02;
                        owner.direction = -terrainDirection;
                        owner.art.localScale = new Vector3(owner.Facing * KarmaSpace.GuardianArtScale, KarmaSpace.GuardianArtScale, 1);
                        owner.patternStarted = owner.FightSeconds;

                        yield return heavy.ExecuteWithTerrain(wave);
                        yield break;
                    }
                    if (finished)
                    {

                        held += Time.deltaTime;
                    }
                    yield return null;
                }
                yield return owner.Recovery(Mathf.Max(0.3f, Settings.barrierRecovery));
            }

            IEnumerator RiseBarrierWave(Vector2[] centers, Vector2[] sizes, SpriteRenderer[] marks, System.Action completed)
            {
                int count = centers.Length;
                for (int i = 0; i < count; i++)
                {
                    if (i == count - 1) yield return RiseBarrierStep(centers[i], sizes[i], marks[i], i + 1);
                    else
                    {
                        owner.StartCoroutine(RiseBarrierStep(centers[i], sizes[i], marks[i], i + 1));
                        yield return owner.Wait(Mathf.Max(0.12f, Settings.barrierStepGap));
                    }
                }
                completed();
            }

            IEnumerator RiseBarrierStep(Vector2 center, Vector2 size, SpriteRenderer mark, int number)
            {
                mark.gameObject.SetActive(false); Destroy(mark.gameObject);
                var visual = KarmaVisuals.Box(owner.effectsRoot, "GD04 상승 지면 " + number, center, Vector2.one, Color.white, 9);
                KarmaCombatVFX.ApplySprite(visual, "GuardBossVFX", "Pillar", size);
                Vector3 scale = visual.transform.localScale;
                float elapsed = 0;
                while (elapsed < 0.18f && owner.Active)
                {
                    if (owner.game.Paused) { yield return null; continue; }
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / 0.18f);
                    visual.transform.localScale = new Vector3(scale.x, scale.y * t, 1);
                    visual.transform.position = new Vector2(center.x, -visual.sprite.bounds.min.y * scale.y * t);
                    yield return null;
                }
                // The boss may already have turned into GD02 while this terrain is still rising.
                var attack = KarmaBossAttack.Spawn(owner.game, owner, owner.effectsRoot, center, Vector2.zero, size,
                    0.25f, owner.Damage, new Color(1, 0.7f, 0.12f), null, 0, "Pillar", visualSheet: "GuardBossVFX", renderArtwork: false);
                yield return owner.Wait(0.25f);
                if (attack != null) { attack.gameObject.SetActive(false); Destroy(attack.gameObject); }
                owner.game.Player.ResolveBarrier(center.x, size.x, size.y);
                var wall = KarmaVisuals.Box(owner.effectsRoot, "완성 방벽 · 계단 " + number,
                    center, size, new Color(0.3f, 0.85f, 1, 0.12f), 7);
                wall.gameObject.AddComponent<KarmaSurface>();
                wall.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
            }
        }
    }
}
