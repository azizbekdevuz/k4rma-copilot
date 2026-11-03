using UnityEngine;
namespace KarmaPrototype
{
    // Boss-owned lifetime. Directional artwork is independent of the unrotated swept hit box.
    public sealed class KarmaBossAttack : MonoBehaviour
    {
        KarmaGame game;
        KarmaEnemy owner;
        Vector2 velocity, size;
        float remaining, damage, stun;
        KarmaAttackContact contact;
        bool oriented, aimedSlash, aerialSword, parryStun, shieldBreak;
        Vector2 axis = Vector2.right;
        public static KarmaBossAttack Spawn(KarmaGame game, KarmaEnemy owner, Transform root, Vector2 at,
            Vector2 velocity, Vector2 size, float life, float damage, Color color, KarmaAttackContact contact, float stun, string visualPose = null,
            bool restrainedVisual = false, bool separateLaunch = false, string extraWavePose = null, string visualSheet = "GuardianVFX", bool parryStun = false, bool shieldBreak = false, bool renderArtwork = true)
        {
            var go = new GameObject("유효 보스 공격"); go.transform.SetParent(root, false); go.transform.localPosition = at;
            var attack = go.AddComponent<KarmaBossAttack>();
            var picture = new GameObject("Directional attack artwork"); picture.transform.SetParent(go.transform, false);
            var sr = picture.AddComponent<SpriteRenderer>(); KarmaVisuals.ApplyMaterial(sr); sr.sortingOrder = 26;
            string pose = visualPose ?? VisualPose(velocity, size);
            bool aerialSword = extraWavePose == "SW05_AerialSword";
            attack.aerialSword = aerialSword; attack.parryStun = parryStun; attack.shieldBreak = shieldBreak;
            attack.aimedSlash = extraWavePose == "SW02_AimedSlash";
            attack.oriented = extraWavePose != null && velocity.sqrMagnitude > 0.01f;
            attack.axis = attack.oriented ? velocity.normalized : Vector2.right;
            if (extraWavePose != null) KarmaWaveArtwork.Apply(sr, extraWavePose, size, velocity);
            else ConfigureVisual(sr, velocity, size, owner.Facing, pose, restrainedVisual, visualSheet);
            sr.color = aerialSword ? Color.white : restrainedVisual ? new Color(1, 0.94f, 0.8f, 0.86f) : Color.Lerp(Color.white, color, 0.18f);
            attack.Visual = sr;
            picture.SetActive(renderArtwork);
            // The supplied aerial sword is the released visual. Orange radiance,
            // residual copies and a second launch sprite obscure its cutting edge.
            if (renderArtwork && !aerialSword && !attack.aimedSlash) KarmaCombatVFX.AttachAttack(sr, velocity, color, root, restrainedVisual);
            float angle = sr.transform.localEulerAngles.z;
            // SW01/02 already have their authored Cleave/Beam swing at this instant.
            // Avoid stacking another full-size white copy on top of that release.
            if (renderArtwork && !separateLaunch && !aerialSword && !attack.aimedSlash)
            {
                var burst = KarmaCombatVFX.Burst(root, at, sr.sprite, "Guardian launch/" + pose,
                    new Vector2(sr.sprite.bounds.size.x * sr.transform.localScale.x, sr.sprite.bounds.size.y * sr.transform.localScale.y)
                        * (restrainedVisual ? 0.8f : 1.15f), 0.24f, angle, color, restrainedVisual);
                burst.transform.parent.localScale = new Vector3(sr.flipX ? -1 : 1, 1, 1);
            }
            attack.game = game; attack.owner = owner; attack.velocity = velocity; attack.size = size;
            attack.remaining = life; attack.damage = damage; attack.contact = contact ?? new KarmaAttackContact(); attack.stun = stun;
            return attack;
        }
        public SpriteRenderer Visual { get; private set; }
        public static string VisualPose(Vector2 velocity, Vector2 size)
        {
            if (velocity.sqrMagnitude > 0.01f) return size.magnitude < KarmaSpace.Units(1) ? "Blade" : "Wave";
            return size.x > size.y * 2 ? "GroundRing" : size.y > size.x * 1.5f ? "Pillar" : "Cleave";
        }
        // Shared attacks use axis-aligned hit boxes; SwordBossVFX uses its own oriented path.
        public static void ConfigureVisual(SpriteRenderer sr, Vector2 velocity, Vector2 size, int facing, string pose, bool restrainedVisual = false, string visualSheet = "GuardianVFX")
        {
            bool moving = velocity.sqrMagnitude > 0.01f;
            float visualScale = restrainedVisual ? (moving ? 1 : 0.9f) : 1.15f;
            KarmaCombatVFX.ApplySprite(sr, visualSheet, pose, size * visualScale, pose != "Pillar");
            if (moving)
            {
                // Fit the rotated sprite to the world-space footprint, including diagonal shots.
                Vector2 axis = velocity.normalized;
                var artSize = sr.sprite.bounds.size;
                float width = Mathf.Abs(axis.x) * artSize.x + Mathf.Abs(axis.y) * artSize.y;
                float height = Mathf.Abs(axis.y) * artSize.x + Mathf.Abs(axis.x) * artSize.y;
                float fit = Mathf.Min(size.x / width, size.y / height) * visualScale;
                sr.transform.localScale = new Vector3(fit, fit, 1);
            }
            sr.flipX = !moving && facing < 0;
            sr.transform.localRotation = Quaternion.Euler(0, 0,
                moving ? Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg : 0);
        }
        // Presentation replacement never changes the shared hit shape or contact ledger.
        public void Repaint(Sprite sprite, bool upright)
        {
            if (sprite == null || Visual == null) return;
            Visual.sprite = sprite;
            Visual.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            if (upright) Visual.transform.localRotation = Quaternion.identity;
        }
        void Update()
        {
            if (game == null || owner == null || !owner.Alive || game.Phase != RunPhase.Combat)
            { gameObject.SetActive(false); Destroy(gameObject); return; }
            if (game.Paused) return;
            float dt = Mathf.Min(Time.deltaTime, Mathf.Max(0, remaining));
            Vector2 from = transform.position, to = from + velocity * dt;
            if (oriented)
            {
                dt = KarmaBossProjectileGeometry.SwordShotLife(from, velocity, dt);
                if (aimedSlash) dt = KarmaBossProjectileGeometry.AimedSlashBoundaryLife(from, velocity, size, dt);
                if (aerialSword && velocity.y < 0)
                    dt = Mathf.Min(dt, Mathf.Max(0, (from.y - (0.05f + size.x * 0.5f)) / -velocity.y));
                to = from + velocity * dt;
            }
            transform.position = to;
            owner.TryAttackHit(from, to, size, damage, contact, stun, oriented ? axis : (Vector2?)null, parryStun, shieldBreak);
            remaining -= Time.deltaTime;
            if (aerialSword && velocity.y < 0 && to.y <= 0.05f + size.x * 0.5f + 0.0001f)
            {
                // Cosmetic only: the projectile's final swept hit above is the
                // sole damage source. Reuse authored impact art and spark motion.
                var impact = KarmaArtwork.Sprite("SwordBossVFX", "SW03_GroundImpact");
                if (impact != null)
                    KarmaCombatVFX.Burst(transform.parent, new Vector2(to.x, 0.9f), impact,
                        "SW05 Ground Impact / no damage", new Vector2(Mathf.Max(2.2f, size.y * 2), 1.7f),
                        0.24f, 0, new Color(0.55f, 0.85f, 1), true);
                var ring = KarmaArtwork.Sprite("GuardianVFX", "GroundRing");
                if (ring != null)
                    KarmaCombatVFX.Burst(transform.parent, new Vector2(to.x, 0.12f), ring,
                        "SW05 Ground shock / no damage", new Vector2(Mathf.Max(2.8f, size.y * 2.6f), 0.8f),
                        0.18f, 0, new Color(0.7f, 0.92f, 1));
                gameObject.SetActive(false); Destroy(gameObject); return;
            }
            if (remaining <= 0 || oriented && dt < Time.deltaTime - 0.000001f || to.x < KarmaStage.ArenaLeft || to.x > KarmaStage.ArenaRight || to.y < 0 || to.y > KarmaSpace.Units(12))
            { gameObject.SetActive(false); Destroy(gameObject); }
        }
    }
}
