using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        sealed class GD03Pattern : GuardBossPattern
        {
            public GD03Pattern(KarmaEnemy owner) : base(owner) { }

            const float FragmentBaseChaseSeconds = 0.65f, FragmentMaxChaseSeconds = 5;
            public override IEnumerator Execute()
            {
                owner.state = ActionState.Telegraph;
                Vector2 size = KarmaSpace.Units(Vector2.one * 1.8f);
                Vector2 origin = owner.Position + KarmaSpace.Units(new Vector2(owner.direction * 1.05f, 0.9f));
                Vector2 impact = new Vector2(Mathf.Clamp(owner.game.Player.Position.x, KarmaStage.ArenaLeft + 4,
                    KarmaStage.ArenaRight - 4), size.y / 2 + KarmaSpace.Units(0.1f));
                var mark = owner.PathMark(origin, impact, size);
                yield return owner.Wait(owner.Warning(0.8f));
                mark.gameObject.SetActive(false); Destroy(mark.gameObject);
                owner.state = ActionState.Attack; owner.animator.Attack();

                var shield = KarmaVisuals.Box(owner.effectsRoot, "GD03 큰 방패 투척", origin, Vector2.one, Color.white, 26);
                KarmaCombatVFX.ApplySprite(shield, "GuardBossVFX", "WholeHot", size, true);
                shield.sharedMaterial = KarmaVisuals.DefaultMaterial;
                var flightTrail = SetShieldTrail(shield, true); flightTrail.emitting = true;
                var contact = new KarmaAttackContact();
                float elapsed = 0;
                while (elapsed < 0.5f && owner.Active)
                {
                    if (owner.game.Paused) { yield return null; continue; }
                    elapsed += Mathf.Min(Time.deltaTime, 0.5f - elapsed);
                    Vector2 before = shield.transform.position;
                    Vector2 to = Vector2.Lerp(origin, impact, elapsed / 0.5f);
                    shield.transform.position = to;
                    shield.transform.rotation = Quaternion.Euler(0, 0, -owner.direction * 720 * elapsed);
                    owner.TryAttackHit(before, to, size, owner.Damage * Settings.shieldThrowDamageMultiplier, contact, 0);
                    yield return null;
                }
                if (!owner.Active) yield break;
                // The actual moving shield is the origin of every shard; there is no despawn/spawn gap.
                impact = shield.transform.position;
                flightTrail.emitting = false;


                int count = Mathf.Clamp(Settings.shieldFragments, 2, 5);
                var pieces = new SpriteRenderer[count];
                var spread = new Vector2[count];
                var scales = new Vector3[count];
                for (int i = 0; i < count; i++)
                {
                    float angle = Mathf.Lerp(155, 25, i / (float)(count - 1)) * Mathf.Deg2Rad;
                    spread[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * KarmaSpace.Units(1.5f);
                    pieces[i] = KarmaVisuals.Box(owner.effectsRoot, "GD03 방패 " + (i + 1) + " 분열", impact, Vector2.one, Color.white, 27);
                    SetFragmentHeat(pieces[i], true);
                    scales[i] = pieces[i].transform.localScale;
                    pieces[i].transform.localScale = scales[i] * 0.25f;
                    pieces[i].GetComponent<TrailRenderer>().emitting = true;
                }
                Vector3 shieldScale = shield.transform.localScale;
                elapsed = 0;
                while (elapsed < 0.28f && owner.Active)
                {
                    if (owner.game.Paused) { yield return null; continue; }
                    elapsed += Mathf.Min(Time.deltaTime, 0.28f - elapsed);
                    float t = elapsed / 0.28f, spreadT = 1 - (1 - t) * (1 - t);
                    shield.color = new Color(1, 1, 1, Mathf.Clamp01(1 - elapsed / 0.12f));
                    shield.transform.localScale = shieldScale * (1 + t * 0.06f);
                    for (int i = 0; i < count; i++)
                    {
                        pieces[i].transform.position = impact + spread[i] * spreadT;
                        pieces[i].transform.localScale = scales[i] * Mathf.Lerp(0.25f, 1, spreadT);
                        TurnFragment(pieces[i], spread[i]);
                    }
                    yield return null;
                }
                if (!owner.Active) yield break;
                shield.gameObject.SetActive(false); Destroy(shield.gameObject);
                foreach (var piece in pieces)
                { piece.name = piece.name.Replace("분열", "준비"); piece.GetComponent<TrailRenderer>().emitting = false; }

                var flights = new Coroutine[count];
                for (int i = 0; i < count; i++)
                {
                    flights[i] = owner.StartCoroutine(ShieldFragment(pieces[i], i + 1));
                    if (i < count - 1) yield return owner.Wait(Mathf.Max(0.15f, Settings.fragmentLaunchGap));
                }
                // Distance-based chases finish in different orders; wait for every shard.
                foreach (var flight in flights) yield return flight;
                yield return owner.Recovery(1.2f);
            }

            IEnumerator ShieldFragment(SpriteRenderer piece, int number)
            {
                SetFragmentHeat(piece, true);
                Vector2 size = KarmaSpace.Units(Vector2.one * 0.55f);
                Vector2 from = piece.transform.position;
                var mark = owner.PathMark(from, owner.game.Player.Position, size, false);
                float elapsed = 0, warning = owner.Warning(Settings.fragmentAimWarning);
                while (elapsed < warning && owner.Active)
                {
                    if (owner.game.Paused) { yield return null; continue; }
                    mark.GetComponent<KarmaAttackWarning>().SetPath(mark, from, owner.game.Player.Position, size);
                    elapsed += Time.deltaTime; yield return null;
                }
                mark.gameObject.SetActive(false); Destroy(mark.gameObject);
                piece.name = "GD03 방패 " + number + " 추적";
                var trail = piece.GetComponent<TrailRenderer>(); trail.emitting = true;
                var outgoing = new KarmaAttackContact();
                float speed = Mathf.Max(KarmaSpace.Units(1), Settings.fragmentChaseSpeed);
                float closingSpeed = Mathf.Max(KarmaSpace.Units(1), speed - owner.game.Config.moveSpeed);
                float chaseSeconds = Mathf.Clamp(Vector2.Distance(from, owner.game.Player.Position) / closingSpeed + 0.15f,
                    FragmentBaseChaseSeconds, FragmentMaxChaseSeconds);
                elapsed = 0;
                while (elapsed < chaseSeconds && owner.Active)
                {
                    if (owner.game.Paused) { yield return null; continue; }
                    float dt = Mathf.Min(Time.deltaTime, chaseSeconds - elapsed);
                    Vector2 before = piece.transform.position;
                    Vector2 target = owner.game.Player.Position;
                    target.x = Mathf.Clamp(target.x, KarmaStage.ArenaLeft + size.x, KarmaStage.ArenaRight - size.x);
                    target.y = Mathf.Clamp(target.y, size.y / 2, KarmaSpace.Units(9));
                    Vector2 to = Vector2.MoveTowards(before, target, speed * dt);
                    piece.transform.position = to;
                    TurnFragment(piece, to - before);
                    owner.TryAttackHit(before, to, size, owner.Damage * Settings.shieldFragmentDamageMultiplier, outgoing, Settings.fragmentStunSeconds);
                    elapsed += dt; yield return null;
                    if (elapsed >= FragmentBaseChaseSeconds && outgoing.Consumed) break;
                }
                // Neither endpoint follows the player during the stop or the return.
                Vector2 stopped = piece.transform.position, home = owner.Position;
                piece.name = "GD03 방패 " + number + " 정지";
                SetFragmentHeat(piece, false);
                var returnMark = owner.PathMark(stopped, home, size);
                yield return owner.Wait(0.18f);
                returnMark.gameObject.SetActive(false); Destroy(returnMark.gameObject);
                piece.name = "GD03 방패 " + number + " 복귀";

                var returning = new KarmaAttackContact();
                TurnFragment(piece, home - stopped);
                float returnSeconds = Mathf.Max(0.1f, Settings.fragmentReturnSeconds);
                trail.time = Mathf.Min(trail.time, returnSeconds * KarmaSpace.Units(0.65f) / Mathf.Max(0.01f, Vector2.Distance(stopped, home)));
                trail.emitting = true;
                elapsed = 0;
                while (elapsed < returnSeconds && owner.Active)
                {
                    if (owner.game.Paused) { yield return null; continue; }
                    elapsed += Mathf.Min(Time.deltaTime, returnSeconds - elapsed);
                    Vector2 before = piece.transform.position;
                    Vector2 to = Vector2.Lerp(stopped, home, elapsed / returnSeconds);
                    piece.transform.position = to;
                    owner.TryAttackHit(before, to, size, owner.Damage * Settings.shieldFragmentDamageMultiplier, returning, 0);
                    yield return null;
                }
                piece.gameObject.SetActive(false); Destroy(piece.gameObject);
            }

            static void SetFragmentHeat(SpriteRenderer piece, bool hot)
            {
                KarmaCombatVFX.ApplySprite(piece, "GuardBossVFX", hot ? "ShardHot" : "ShardCooled",
                    KarmaSpace.Units(Vector2.one * 0.65f), true);
                piece.sharedMaterial = KarmaVisuals.DefaultMaterial; piece.color = Color.white;
                SetShieldTrail(piece, hot);
            }

            static TrailRenderer SetShieldTrail(SpriteRenderer piece, bool hot)
            {
                var trail = piece.GetComponent<TrailRenderer>();
                if (trail == null)
                {
                    trail = piece.gameObject.AddComponent<TrailRenderer>();
                    trail.sharedMaterial = KarmaVisuals.DefaultMaterial;
                    trail.sortingOrder = piece.sortingOrder - 1;
                    trail.minVertexDistance = KarmaSpace.Units(0.04f); trail.endWidth = 0;
                    trail.numCapVertices = 2; trail.numCornerVertices = 2;
                }
                trail.emitting = false; trail.Clear();
                trail.time = hot ? 0.1f : 0.055f;
                trail.startWidth = KarmaSpace.Units(hot ? 0.1f : 0.07f);
                trail.startColor = hot ? new Color(1, 0.12f, 0.025f, 0.7f) : new Color(0.65f, 0.7f, 0.75f, 0.45f);
                trail.endColor = hot ? new Color(1, 0.3f, 0.04f, 0) : new Color(0.65f, 0.7f, 0.75f, 0);
                return trail;
            }

            static void TurnFragment(SpriteRenderer piece, Vector2 heading)
            {
                if (heading.sqrMagnitude > 0.001f)
                    piece.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
            }
        }
    }
}
