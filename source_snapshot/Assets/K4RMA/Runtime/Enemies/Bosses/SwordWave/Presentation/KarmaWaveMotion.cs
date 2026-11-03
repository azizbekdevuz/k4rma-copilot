using UnityEngine;

namespace KarmaPrototype
{
    /// <summary>
    /// Visual-only secondary motion for the Wave guardian.
    /// Uses the sprites already chosen by KarmaCharacterAnimator; does not
    /// select different sprites, spawn projectiles, or modify the boss root.
    ///
    /// Attach to the Wave guardian's "Artwork" object in KarmaArtwork.Guardian().
    /// </summary>
    [DefaultExecutionOrder(300)]
    [DisallowMultipleComponent]
    public sealed class KarmaWaveMotion : MonoBehaviour
    {
        [Header("Existing-sprite motion (local units / degrees)")]
        [SerializeField, Range(0f, 0.08f)] float walkingSway = 0.018f;
        [SerializeField, Range(0f, 5f)] float walkingTilt = 1.5f;
        [SerializeField, Range(0f, 0.12f)] float attackReach = 0.060f;
        [SerializeField, Range(0f, 10f)] float attackTilt = 5f;
        [SerializeField, Range(0f, 0.06f)] float hoveringBob = 0.025f;

        KarmaCharacterAnimator animator;
        KarmaEnemy enemy;
        KarmaGame game;
        Transform spriteTransform;
        SpriteRenderer spriteRenderer;
        Vector3 spriteHome;
        Quaternion spriteHomeRotation;
        string previousPose = "";
        float visualClock;
        float poseStartedAt;
        // GuardianPose restores the renderer's authored position in Update. Keep
        // the displayed motion here instead of reading that reset transform.
        Vector3 motionOffset, motionVelocity;
        float motionAngle, angularVelocity;
        Vector2 previousRootPosition;
        float gaitPhase, locomotionSpeed;
        int previousFacing;
        bool initialized;
        int motionRevision;
        internal bool HoverActive { get; private set; }

        void Start()
        {
            if (initialized) return;
            animator = GetComponent<KarmaCharacterAnimator>();
            enemy = GetComponentInParent<KarmaEnemy>();
            game = GetComponentInParent<KarmaGame>();
            if (game == null) game = FindAnyObjectByType<KarmaGame>();

            Transform child = transform.Find("Guardian sprite");
            if (child == null || child.GetComponent<SpriteRenderer>() == null ||
                animator == null || enemy == null)
            {
                Debug.LogWarning("KarmaWaveMotion: Wave guardian sprite/animator not found.", this);
                enabled = false;
                return;
            }

            spriteTransform = child;
            spriteRenderer = child.GetComponent<SpriteRenderer>();
            spriteHome = child.localPosition;
            spriteHomeRotation = child.localRotation;
            previousRootPosition = enemy.Position;
            previousFacing = transform.lossyScale.x < 0 ? -1 : 1;
            initialized = true;
        }

        void LateUpdate()
        {
            AdvanceMotion(Time.deltaTime);
        }

        // Explicit delta lets the same visual path be checked at low/high FPS.
        // Root movement is only sampled here; this component never writes it.
        void AdvanceMotion(float dt)
        {
            if (!initialized) return;
            // Artwork survives briefly after its owner dies and is detached.
            // Reset before checking the owner so no last-frame lean is retained.
            if (animator == null || enemy == null || !enemy.PresentationActive ||
                enemy.Stunned || animator.CurrentPose == "Death")
            {
                ResetVisualTransform();
                return;
            }
            if (game != null && game.Paused) return;
            if (dt <= 0) return;

            // The existing character animator selects sprites in Update().
            // Only add subtle offsets AFTER it finishes; no root translation.
            visualClock += dt;
            string pose = animator.CurrentPose ?? "Idle";
            int facing = transform.lossyScale.x < 0 ? -1 : 1;
            if (facing != previousFacing)
            {
                // Artwork flips immediately to face the player. Carry the
                // displayed displacement/lean in world space while it settles.
                motionOffset.x = -motionOffset.x;
                motionVelocity.x = -motionVelocity.x;
                motionAngle = -motionAngle;
                angularVelocity = -angularVelocity;
                locomotionSpeed = -locomotionSpeed;
                previousFacing = facing;
            }
            float travel = enemy.Position.x - previousRootPosition.x;
            previousRootPosition = enemy.Position;
            bool locomotionPose = pose == "Idle" || pose == "Move";
            bool grounded = enemy.Position.y <= KarmaSpace.GuardianGroundY + 0.1f;
            float nominalSpeed = game != null
                ? Mathf.Max(0.1f, game.Config.enemyMoveSpeed * game.Config.BossTuning.moveMultiplier)
                : KarmaSpace.Units(2.4f) * 1.7f;
            float actualSpeed = locomotionPose && grounded ? travel / dt * facing : 0;
            locomotionSpeed = Mathf.Lerp(locomotionSpeed, actualSpeed, 1 - Mathf.Exp(-dt / 0.07f));
            if (locomotionPose && grounded)
                gaitPhase += Mathf.Abs(travel) * 11f / nominalSpeed;
            // Queuing the following warning must not restart a release that is
            // still being displayed. Only a real release can repeat one pose.
            if (pose != previousPose || pose == "Wave" && motionRevision != animator.WaveMotionRevision)
            {
                previousPose = pose;
                poseStartedAt = visualClock;
            }
            motionRevision = animator.WaveMotionRevision;

            float elapsed = visualClock - poseStartedAt;
            int current = enemy.CurrentPattern;
            bool aimed = current == (int)EnemyAttack.SW02;
            bool scars = current == (int)EnemyAttack.SW03;
            bool counter = current == (int)EnemyAttack.SW04;
            bool aerial = current == (int)EnemyAttack.SW05;
            float relax = Mathf.Exp(-8f * elapsed);
            Vector3 delta = Vector3.zero;
            float angle = 0f;

            switch (pose)
            {
                case "Idle":
                case "Move":
                    // One existing Move image: transfer weight without lifting
                    // the feet. Actual travel advances the gait, so a blocked
                    // root cannot walk in place. Idle also accepts this sample
                    // because GuardianPose observes movement one Update earlier.
                    float movement = Mathf.Clamp(locomotionSpeed / nominalSpeed, -1f, 1f);
                    float gait = Mathf.Sin(gaitPhase) * movement;
                    delta.x = walkingSway * gait;
                    // Positive local X travel needs a clockwise lean. Keep the
                    // gait smaller than that lean so retreat steps cannot tip
                    // the upper body against their measured travel direction.
                    angle = -walkingTilt * (0.8f * movement + 0.2f * gait);
                    if (pose == "Idle") angle += 0.35f * Mathf.Sin(visualClock * 1.2f) * (1 - Mathf.Abs(movement));
                    break;

                case "Windup":
                    // Lean backwards while concentrating power.
                    float anticipation = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.12f));
                    delta.x = -0.025f * anticipation;
                    angle = (aimed ? -2.2f : scars ? -3.5f : -4.2f) * anticipation;
                    break;

                case "Guard":
                    // SW02 aiming lock; SW04 ward; SW05 aerial shield.
                    delta.x = aimed ? -0.015f : counter ? -0.006f : 0f;
                    angle = (counter ? -1.5f : aimed ? -2f : -0.5f)
                        + 0.18f * Mathf.Sin(visualClock * 2.5f);
                    break;

                case "Slash":
                    // The swing leads into the actual release instead of first
                    // resetting to neutral when the Slash image appears.
                    float swing = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.10f));
                    delta.x = Mathf.Lerp(-0.025f, attackReach * (aimed ? 1.2f : 1f), swing);
                    angle = Mathf.Lerp(aimed ? -2.2f : -4.2f, attackTilt * (aimed ? 1.3f : 1f), swing);
                    break;

                case "Wave":
                    // Follow-through/recoil when the projectile is released.
                    delta.x = attackReach * (aimed ? 1.2f : 1f) * relax;
                    angle = attackTilt * (aimed ? 1.3f : 1f) * relax;
                    break;

                case "Rising":
                    // The root already travels during SW05. SW03 stays planted
                    // while its remote ground scars rise.
                    delta.x = scars ? attackReach * 0.35f * relax : 0f;
                    angle = scars ? 3.2f * relax : 1.1f * Mathf.Sin(visualClock * 4f);
                    break;

                case "FollowThrough":
                    delta.x = 0.030f * relax;
                    angle = 3f * relax;
                    break;

                case "Hurt":
                    delta.x = -0.015f * relax;
                    angle = -2f * relax;
                    break;
            }

            // Hover belongs only to the existing airborne full-shield phase.
            // Grounded poses (including Guard) never add a vertical bob.
            HoverActive = aerial && enemy.Invulnerable && pose != "Hurt"
                && enemy.Position.y > KarmaSpace.GuardianGroundY + 0.1f;
            if (HoverActive) delta.y = hoveringBob * Mathf.Sin(visualClock * 5f);
            else { motionOffset.y = 0; motionVelocity.y = 0; }

            // Carry the displayed offset and its velocity across sprite changes.
            // A new pose changes the target, never teleports to its first sample.
            float blend = pose == "Slash" || pose == "Wave" || pose == "Hurt" ? 0.035f : 0.07f;
            motionOffset = Vector3.SmoothDamp(motionOffset, delta, ref motionVelocity, blend, Mathf.Infinity, dt);
            motionAngle = Mathf.SmoothDampAngle(motionAngle, angle, ref angularVelocity, blend, Mathf.Infinity, dt);
            Quaternion rotation = Quaternion.Euler(0f, 0f, motionAngle);
            // The supplied pivots are just above the feet. Rotate around that
            // foot anchor, preserving its authored height through the lean.
            Vector3 foot = spriteRenderer.sprite != null
                ? new Vector3(0, spriteRenderer.sprite.bounds.min.y * spriteTransform.localScale.y, 0) : Vector3.zero;
            Vector3 planted = spriteHomeRotation * (foot - rotation * foot);
            spriteTransform.localPosition = spriteHome + motionOffset + planted;
            spriteTransform.localRotation = spriteHomeRotation * rotation;
        }

        internal Bounds GetDamageBounds(SpriteRenderer renderer)
        {
            // Damage can be queried in the spawn frame, before Unity calls Start.
            // Capture the authored foot transform before any visual motion runs.
            if (!initialized) Start();
            if (!initialized || renderer == null || renderer.transform != spriteTransform)
                return renderer != null ? renderer.bounds : default;
            // The visible renderer may lean, bob, or use a new phase key pose.
            // Incoming hits keep the original animator pose, pivot and scale.
            // Rebuild its world AABB on demand so root travel/facing and inherited
            // appearances remain current even before this frame's LateUpdate.
            var sprite = animator.GuardianPhaseOverridesPose ? animator.GuardianDamageSprite : null;
            var local = sprite != null ? sprite.bounds : renderer.localBounds;
            var scale = sprite != null ? animator.GuardianDamageScale : spriteTransform.localScale;
            var matrix = spriteTransform.parent.localToWorldMatrix *
                Matrix4x4.TRS(spriteHome, spriteHomeRotation, scale);
            var e = local.extents;
            var x = matrix.MultiplyVector(new Vector3(e.x, 0, 0));
            var y = matrix.MultiplyVector(new Vector3(0, e.y, 0));
            var z = matrix.MultiplyVector(new Vector3(0, 0, e.z));
            var extents = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            return new Bounds(matrix.MultiplyPoint3x4(local.center), extents * 2);
        }

        void ResetVisualTransform()
        {
            if (spriteTransform == null) return;
            motionOffset = motionVelocity = Vector3.zero;
            motionAngle = angularVelocity = 0;
            locomotionSpeed = 0;
            if (enemy != null) previousRootPosition = enemy.Position;
            previousFacing = transform.lossyScale.x < 0 ? -1 : 1;
            HoverActive = false;
            spriteTransform.localPosition = spriteHome;
            spriteTransform.localRotation = spriteHomeRotation;
        }

        void OnDisable()
        {
            if (initialized) ResetVisualTransform();
            previousPose = "";
        }
    }
}
