using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KarmaPrototype
{
    // Only copied static terrain and a copied player body enter the local scene.
    // The real player, boss position, colliders and physics scene are untouched.
    public sealed class KarmaAerialSwordPlanner : MonoBehaviour
    {
        public const int VolleyCount = 3;
        public sealed class Shot
        {
            public Vector2 origin, orbit, velocity, acceleration, size;
            public float life, delay, depth = 0.5f;
            public Vector2 PositionAt(float seconds)
            { return origin + velocity * seconds + acceleration * (seconds * seconds * 0.5f); }
        }
        public sealed class Group
        {
            public Shot[] shots;
            public int escapeInput;
            public float escapeJumpAt = -1;
        }
        sealed class Route
        {
            public Vector2[] centers;
            public int input;
            public float jumpAt = -1;
        }

        KarmaGame game;
        System.Random random;
        Scene simulationScene;
        PhysicsScene2D physics;
        GameObject simulationRoot;
        Rigidbody2D probe;
        BoxCollider2D probeShape;
        readonly ContactPoint2D[] contacts = new ContactPoint2D[8];
        readonly List<Transform> live = new List<Transform>(54);
        public int[] GroupSizes { get; private set; }
        public int Seed { get; private set; }
        public Group LatestGroup { get; private set; }
        public float LatestGroupAt { get; private set; }
        public bool HasLiveShots
        {
            get
            {
                foreach (var shot in live)
                    if (shot != null && shot.gameObject.activeInHierarchy) return true;
                return false;
            }
        }

        public static int ResolveSeed(int configuredSeed)
        { return configuredSeed != 0 ? configuredSeed : UnityEngine.Random.Range(1, int.MaxValue); }

        public static KarmaAerialSwordPlanner Create(Transform lifetimeRoot, KarmaGame game, int seed)
        {
            var go = new GameObject("Aerial sword escape planner");
            go.transform.SetParent(lifetimeRoot, false);
            var planner = go.AddComponent<KarmaAerialSwordPlanner>();
            planner.game = game; planner.Seed = seed; planner.random = new System.Random(seed);
            planner.GroupSizes = CreateGroupSizes(game.Config.BossTuning.sword, planner.random);
            planner.CopyPhysicsWorld();
            return planner;
        }

        public static int[] CreateGroupSizes(KarmaSwordBossSettings tuning, System.Random random)
        {
            int low = Mathf.Clamp(tuning.aerialSwordVolleyMin, 1, 18);
            int high = Mathf.Clamp(tuning.aerialSwordVolleyMax, low, 18);
            var sizes = new int[VolleyCount];
            for (int i = 0; i < sizes.Length; i++) sizes[i] = random.Next(low, high + 1);
            return sizes;
        }

        public Group TryBuildGroup(Vector2 elevated, int index, float warning,
            Vector2 size, float speed, float fightSeconds)
        {
            var player = game.Player;
            var sourceBody = player.GetComponent<Rigidbody2D>();
            var sourceShape = player.GetComponent<BoxCollider2D>();
            Vector2 target = sourceShape.bounds.center;
            Vector2 half = sourceShape.bounds.extents;
            float step = Mathf.Clamp(Time.fixedDeltaTime, 0.005f, 0.05f);
            float horizon = warning + 1 + TimingAllowance(step) + step * 2;
            live.RemoveAll(shot => shot == null || !shot.gameObject.activeInHierarchy);
            if (HasLiveShots) return null;

            int count = GroupSizes[index];
            var dimensions = new Vector2[count];
            var formation = new Vector2[count];
            var orbits = new Vector2[count];
            var depths = new float[count];
            float ceiling = KarmaSpace.Units(12);
            if (game.GameCamera != null && game.GameCamera.orthographic)
                ceiling = Mathf.Min(ceiling, game.GameCamera.transform.position.y + game.GameCamera.orthographicSize);
            for (int i = 0; i < count; i++)
            {
                float sample = (float)random.NextDouble();
                float x = count > 1 ? i * 2f / (count - 1) - 1 : 0;
                float arc = Mathf.Sqrt(Mathf.Max(0, 1 - x*x));
                bool foreground = i % 2 == 0;
                depths[i] = 0.5f + (foreground ? 1 : -1) * arc * 0.5f;
                // Near and far semicircles have real, proportionate hit shapes.
                // The same silhouette is used for both art and dodge planning.
                dimensions[i] = size * Mathf.Lerp(0.74f, 1.3f, depths[i]) * (1 + sample * 0.035f);
                float crownHeight = elevated.y + 6 + (foreground ? -1 : 1) * arc * 3.8f;
                float height = Mathf.Clamp(crownHeight,
                    dimensions[i].x / 2 + 3.8f, ceiling - dimensions[i].x / 2 - 0.75f);
                orbits[i] = new Vector2(KarmaStage.ArenaCenter + x * KarmaStage.ArenaWidth * 0.36f, height);
                // Conjure around the midair boss, then raise the real release
                // pose to the top of the viewport. Hilts may extend off screen;
                // the visible cutting tips hang in its upper quarter.
                float sky = ceiling + dimensions[i].x / 2 - Mathf.Lerp(2.5f, 4.5f, depths[i]);
                formation[i] = new Vector2(orbits[i].x, sky);
            }
            float left = KarmaStage.ArenaLeft + 0.25f;
            float right = KarmaStage.ArenaRight - 0.25f;
            var slots = new float[count];
            // Random gap weights create different clusters each slam, while
            // retaining broad coverage instead of leaving a permanent safe lane.
            float total = 0;
            var gaps = new float[Mathf.Max(0, count - 1)];
            for (int i = 0; i < gaps.Length; i++) { gaps[i] = Mathf.Lerp(0.55f, 1.45f, (float)random.NextDouble()); total += gaps[i]; }
            float cursor = left;
            for (int i = 0; i < count; i++)
            {
                slots[i] = count == 1 ? (left + right) / 2 : cursor;
                if (i < gaps.Length) cursor += (right - left) * gaps[i] / total;
            }
            // One committed blade targets the player's position at warning start.
            // It never tracks afterward and may not be moved to make standing safe.
            float focus = Mathf.Clamp(target.x, left, right);
            int aimFirst = count >= 3 ? 1 : 0, aimLast = count >= 3 ? count - 2 : count - 1;
            int aimedSlot = aimFirst;
            for (int i = aimFirst + 1; i <= aimLast; i++)
                if (Mathf.Abs(slots[i] - focus) < Mathf.Abs(slots[aimedSlot] - focus)) aimedSlot = i;
            slots[aimedSlot] = focus;
            Route standing = SimulateRoute(sourceBody, sourceShape, 0, false, step, horizon);
            int inward = target.x < KarmaStage.ArenaCenter ? 1 : -1;
            Route selected = null;
            Shot[] shots = null;
            // Slams do not overlap. Reserve the complete upcoming flight and
            // require both a moving escape and a real threat to standing still.
            foreach (bool jump in new[] { false, true })
            {
                foreach (int input in new[] { inward, -inward })
                {
                    var route = SimulateRoute(sourceBody, sourceShape, input, jump, step, horizon);
                    var candidate = new Shot[count];
                    bool allSafe = true;
                    for (int member = 0; member < count; member++)
                    {
                        float slot = slots[member];
                        bool edge = count >= 3 && (member == 0 || member == count - 1);
                        float low = member == aimedSlot ? focus : edge ? slot : member == 0 ? left : (slots[member - 1] + slot) / 2;
                        float high = member == aimedSlot ? focus : edge ? slot : member == count - 1 ? right : (slots[member + 1] + slot) / 2;
                        float desired = slot;
                        candidate[member] = FindSafeShot(desired, low, high,
                            speed, dimensions[member], route, step, half, warning, formation[member], orbits[member], depths[member]);
                        if (candidate[member] == null) { allSafe = false; break; }
                    }
                    if (!allSafe) continue;
                    bool threatensStanding = false;
                    foreach (var shot in candidate)
                        if (!SafeAtPhase(standing, step, half, shot, warning + shot.delay, shot.size, 0))
                        { threatensStanding = true; break; }
                    if (!threatensStanding) continue;
                    shots = candidate; selected = route; break;
                }
                if (selected != null) break;
            }
            if (selected == null) return null;
            LatestGroupAt = fightSeconds;
            LatestGroup = new Group { shots = shots, escapeInput = selected.input, escapeJumpAt = selected.jumpAt };
            return LatestGroup;
        }

        public void RegisterLaunch(Transform instance) { live.Add(instance); }

        static Shot MakeShot(float landing, float speed, Vector2 size, Vector2 formation, Vector2 orbit, float depth)
        {
            speed = Mathf.Max(0.01f, speed);
            Vector2 ray = new Vector2(landing, 0.05f) - formation;
            Vector2 axis = ray.normalized;
            float distance = ray.magnitude - size.x / 2;
            const float acceleration = 2200;
            float duration = (Mathf.Sqrt(speed * speed + 2 * acceleration * distance) - speed) / acceleration;
            return new Shot { origin = formation, orbit = orbit, velocity = axis * speed, acceleration = axis * acceleration,
                size = size, life = duration, delay = Mathf.Abs(Mathf.Sin(landing * 0.71f)) * 0.035f, depth = depth };
        }

        Shot FindSafeShot(float desired, float left, float right, float speed, Vector2 size,
            Route route, float step, Vector2 half, float warning, Vector2 formation, Vector2 orbit, float depth)
        {
            // Search only this sword's randomized landing region. Every accepted
            // placement remains a visible, full-size, physically falling sword.
            int attempts = right - left < 0.001f ? 0 : 24;
            for (int attempt = 0; attempt <= attempts; attempt++)
            {
                float x = attempt == 0 ? desired : Mathf.Lerp(left, right, (attempt - 1) / 23f);
                var shot = MakeShot(x, speed, size, formation, orbit, depth);
                if (shot.life > 0.05f && Safe(route, step, half, shot, warning + shot.delay, size)) return shot;
            }
            return null;
        }

        static bool Safe(Route route, float step, Vector2 half, Shot shot, float begins, Vector2 size)
        {
            // Sweep the whole early/late time window of the exact accelerated
            // path. Clamping to its true start/end avoids falsely extending a
            // very fast sword through the floor and closing every escape lane.
            return SafeAtPhase(route, step, half, shot, begins, size, TimingAllowance(step));
        }

        static float TimingAllowance(float step)
        { return Mathf.Max(step * 2, Mathf.Min(0.05f, Time.deltaTime)); }

        static bool SafeAtPhase(Route route, float step, Vector2 half, Shot shot, float begins, Vector2 size, float allowance)
        {
            Vector2 axis = shot.velocity.normalized;
            float activeFrom = begins - allowance;
            float until = begins + shot.life + allowance;
            for (int i = 1; i < route.centers.Length; i++)
            {
                float a = (i - 1) * step, b = i * step;
                if (b < activeFrom || a > until) continue;
                float start = Mathf.Max(a, activeFrom), end = Mathf.Min(b, until);
                if (end < start) continue;
                Vector2 p = Vector2.Lerp(route.centers[i - 1], route.centers[i], (start - a) / step);
                Vector2 q = Vector2.Lerp(route.centers[i - 1], route.centers[i], (end - a) / step);
                Vector2 from = shot.PositionAt(Mathf.Clamp(start - begins - allowance, 0, shot.life)) - p;
                Vector2 to = shot.PositionAt(Mathf.Clamp(end - begins + allowance, 0, shot.life)) - q;
                // Reserve a small allowance for render/fixed update phase offsets.
                if (KarmaBossGeometry.SweptOrientedBox(from.x, from.y, to.x, to.y, 0, 0,
                    axis.x, axis.y, size.x, size.y, half.x + 0.15f + Mathf.Abs(q.x - p.x),
                    half.y + 0.15f + Mathf.Abs(q.y - p.y))) return false;
            }
            return true;
        }

        Route SimulateRoute(Rigidbody2D sourceBody, BoxCollider2D sourceShape,
            int input, bool jump, float step, float horizon)
        {
            probe.simulated = false;
            probe.transform.SetPositionAndRotation(sourceBody.position, sourceBody.transform.rotation);
            probeShape.size = sourceShape.size; probeShape.offset = sourceShape.offset;
            probeShape.edgeRadius = sourceShape.edgeRadius;
            probe.transform.localScale = sourceBody.transform.lossyScale;
            probe.simulated = true;
            // Re-enabling simulation restores the native body from its Transform.
            // Set its pose and velocity afterwards; setters while disabled can
            // otherwise leave the previous candidate's endpoint in the new route.
            probe.position = sourceBody.position; probe.rotation = sourceBody.rotation;
            probe.linearVelocity = sourceBody.linearVelocity; probe.angularVelocity = 0;
            probe.gravityScale = game.Config.gravityScale;
            Physics2D.SyncTransforms();
            probe.WakeUp();
            int steps = Mathf.CeilToInt(horizon / step);
            var route = new Route { input = input, centers = new Vector2[steps + 1] };
            route.centers[0] = ProbeCenter();
            bool issued = false, grounded = !game.Player.Airborne;
            for (int i = 0; i < steps; i++)
            {
                float vertical = probe.linearVelocity.y;
                if (jump && !issued && grounded && vertical <= 0.1f)
                { vertical = game.Config.jumpSpeed; route.jumpAt = i * step; issued = true; }
                probe.linearVelocity = new Vector2(input * game.Config.moveSpeed, vertical);
                physics.Simulate(step);
                route.centers[i + 1] = ProbeCenter();
                grounded = false;
                int touching = probeShape.GetContacts(contacts);
                for (int c = 0; c < touching; c++) grounded |= contacts[c].normal.y > 0.5f;
            }
            return route;
        }

        Vector2 ProbeCenter()
        {
            Vector2 offset = Vector2.Scale(probeShape.offset, probe.transform.lossyScale);
            float angle = probe.rotation * Mathf.Deg2Rad;
            return probe.position + new Vector2(offset.x * Mathf.Cos(angle) - offset.y * Mathf.Sin(angle),
                offset.x * Mathf.Sin(angle) + offset.y * Mathf.Cos(angle));
        }

        void CopyPhysicsWorld()
        {
            simulationScene = SceneManager.CreateScene("SW05 escape physics " + Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            physics = simulationScene.GetPhysicsScene2D();
            simulationRoot = new GameObject("SW05 copied collision world");
            simulationRoot.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(simulationRoot, simulationScene);
            foreach (var surface in game.World.GetComponentsInChildren<KarmaSurface>())
                foreach (var source in surface.GetComponents<Collider2D>())
                {
                    if (!source.enabled || source.isTrigger) continue;
                    var go = new GameObject("Copied " + surface.name);
                    go.transform.SetParent(simulationRoot.transform, false);
                    go.transform.position = source.transform.position;
                    go.transform.rotation = source.transform.rotation;
                    go.transform.localScale = source.transform.lossyScale;
                    var box = source as BoxCollider2D;
                    if (box == null) { Destroy(go); continue; }
                    var copy = go.AddComponent<BoxCollider2D>(); copy.size = box.size; copy.offset = box.offset;
                    copy.edgeRadius = box.edgeRadius; copy.sharedMaterial = source.sharedMaterial;
                }
            var player = new GameObject("Copied real player collider");
            player.transform.SetParent(simulationRoot.transform, false);
            probeShape = player.AddComponent<BoxCollider2D>();
            probe = player.AddComponent<Rigidbody2D>(); probe.freezeRotation = true;
            probe.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            probe.sleepMode = RigidbodySleepMode2D.NeverSleep;
            probe.linearDamping = game.Player.GetComponent<Rigidbody2D>().linearDamping;
            probeShape.sharedMaterial = game.Player.GetComponent<BoxCollider2D>().sharedMaterial;
        }

        void OnDisable()
        {
            live.Clear();
            if (simulationRoot != null)
            { simulationRoot.SetActive(false); Destroy(simulationRoot); simulationRoot = null; }
            if (simulationScene.IsValid() && simulationScene.isLoaded) SceneManager.UnloadSceneAsync(simulationScene);
            simulationScene = default(Scene);
        }
    }
}
