using UnityEngine;

namespace KarmaPrototype
{
    // Accelerated SW05 flight only. The identical equation is evaluated by the
    // escape planner; ascent, warning and planted pictures never cause damage.
    public sealed class KarmaSW05RainBlade : MonoBehaviour
    {
        KarmaGame game;
        KarmaEnemy owner;
        KarmaAerialSwordPlanner.Shot shot;
        SpriteRenderer warning;
        readonly KarmaAttackContact contact = new KarmaAttackContact();
        float age, damage;
        public SpriteRenderer Visual { get; private set; }
        public static KarmaSW05RainBlade Spawn(KarmaGame game, KarmaEnemy owner, Transform root,
            KarmaAerialSwordPlanner.Shot shot, float damage, SpriteRenderer warning)
        {
            var go = new GameObject("SW05 descending judgement / active damage");
            go.transform.SetParent(root, false); go.transform.position = shot.origin;
            var blade = go.AddComponent<KarmaSW05RainBlade>();
            blade.game = game; blade.owner = owner; blade.shot = shot;
            blade.damage = damage; blade.warning = warning;
            blade.Visual = KarmaVisuals.Box(go.transform, "SW05 crimson sword", Vector2.zero, Vector2.one, Color.white, 26);
            KarmaSW05VFX.ApplySword(blade.Visual, shot.size, shot.velocity);
            KarmaSW05VFX.ApplyDepth(blade.Visual, shot.depth);
            KarmaSW05VFX.Falling(root, game, blade.transform, blade.Visual, shot.size, shot.delay + shot.life, shot.velocity.normalized);
            return blade;
        }
        void Update()
        {
            if (game == null || owner == null || !owner.Alive || game.Phase != RunPhase.Combat)
            { gameObject.SetActive(false); Destroy(gameObject); return; }
            if (game.Paused) return;
            float previous = age; age += Time.deltaTime;
            if (age < shot.delay) return;
            float fromTime = Mathf.Clamp(previous - shot.delay, 0, shot.life);
            float toTime = Mathf.Clamp(age - shot.delay, 0, shot.life);
            Vector2 from = shot.PositionAt(fromTime), to = shot.PositionAt(toTime);
            transform.position = to;
            owner.TryAttackHit(from, to, shot.size, damage, contact, 0, shot.velocity.normalized);
            if (toTime < shot.life) return;
            if (warning != null) { warning.gameObject.SetActive(false); Destroy(warning.gameObject); }
            KarmaSW05VFX.Strike(transform.parent, game, to, shot.size, shot.velocity.normalized, shot.depth);
            gameObject.SetActive(false); Destroy(gameObject);
        }
    }
}
