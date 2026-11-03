#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using KarmaPrototype;

// Runs the real pattern and damage projectiles, with an advancing player loop.
[InitializeOnLoad]
public static class KarmaSW05PlayChecks
{
    const string Key = "K4RMA.SW05Checks";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static bool started;
    static KarmaGame game;
    static KarmaEnemy enemy;
    static int assertions;
    static readonly HashSet<KarmaSW05RainBlade> launches = new HashSet<KarmaSW05RainBlade>();
    static string runtimeError;
    static readonly string Output = "output/validation";
    static KarmaSW05PlayChecks() { EditorApplication.update += Pump; }
    static void Pump()
    {
        if (!SessionState.GetBool(Key, false)) return;
        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.isPlaying) EditorApplication.isPaused = false;
        if (started && EditorApplication.isPlaying && enemy != null)
            foreach (var attack in Root.GetComponentsInChildren<KarmaSW05RainBlade>()) launches.Add(attack);
        if (!started && EditorApplication.isPlaying)
        {
            EditorApplication.isPaused = false;
            if (Time.frameCount > 2) { started = true; Run(); }
        }
    }
    public static void RunBatch()
    {
        SessionState.SetBool(Key, true);
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Show();
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/K4RMA/Scenes/KarmaPrototype.unity");
        EditorApplication.EnterPlaymode();
    }
    static void Set(object target, string name, object value) { target.GetType().GetField(name, Private).SetValue(target, value); }
    static T Get<T>(object target, string name) { return (T)target.GetType().GetField(name, Private).GetValue(target); }
    static object Call(object target, string name, params object[] args) { return target.GetType().GetMethod(name, Private).Invoke(target, args); }
    static void Check(bool condition, string label) { assertions++; if (!condition) throw new Exception(label); }
    static Transform Root => Get<Transform>(enemy, "effectsRoot");
    static void Reset()
    {
        game.StartRun();
        foreach (var e in new[] { Essence.SwordWave, Essence.Guard, Essence.RisingSlash })
        { game.Player.Essences.Preview(e); game.Player.Essences.Confirm(out _); }
        typeof(KarmaGame).GetProperty("StageIndex").SetValue(game, 3);
        Call(game, "LoadRoom", true); enemy = game.Enemies[0];
        enemy.transform.position = new Vector3(20, KarmaSpace.GuardianGroundY, 0);
        Set(enemy, "engaged", true); Set(enemy, "timer", float.MaxValue);
        Set(game.Player, "invulnerableUntil", Time.time + 1000);
        var body = game.Player.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.position = new Vector2(13, 2); body.linearVelocity = Vector2.zero;
    }
    static void Force()
    {
        Call(enemy, "InterruptPattern"); Set(enemy, "selected", EnemyAttack.SW05);
        Set(enemy, "running", true); Set(enemy, "patternStarted", enemy.FightSeconds);
        enemy.StartCoroutine((IEnumerator)Call(enemy, "RunPattern"));
    }
    static async Task Until(Func<bool> condition, float seconds)
    {
        double limit = EditorApplication.timeSinceStartup + seconds;
        while (!condition()) { if (EditorApplication.timeSinceStartup > limit) throw new Exception("SW05 timeout"); await Task.Delay(5); }
    }
    static async Task Seconds(float seconds)
    { float end = enemy.FightSeconds + seconds; await Until(() => enemy.FightSeconds >= end, seconds + 5); }
    static SpriteRenderer[] Forming()
    {
        var list = new List<SpriteRenderer>();
        foreach (var sr in Root.GetComponentsInChildren<SpriteRenderer>()) if (sr.name == "Forming aerial sword / no damage") list.Add(sr);
        return list.ToArray();
    }
    static void Capture(string name)
    {
        var camera = game.GameCamera;
        var texture = new RenderTexture(1600, 900, 24);
        var prior = camera.targetTexture; var active = RenderTexture.active;
        camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
        File.WriteAllBytes(Output + "/sw05-" + name + ".png", image.EncodeToPNG());
        camera.targetTexture = prior; RenderTexture.active = active;
        UnityEngine.Object.Destroy(image); texture.Release(); UnityEngine.Object.Destroy(texture);
    }
    static void Log(string message, string stack, LogType type)
    { if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            && !stack.Contains("UnityEditor.Search")) runtimeError = message; }
    static async Task CheckStandingRisk()
    {
        foreach (float x in new[] { KarmaStage.ArenaLeft + 0.25f, 13f, KarmaStage.ArenaRight - 0.25f })
        {
            Reset();
            var body = game.Player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(x, 2); game.Player.transform.position = body.position;
            await Seconds(0.08f);
            if (game.Player.TestInvincible) game.Player.ToggleTestInvincibility();
            Set(game.Player, "invulnerableUntil", 0f); Set(game.Player, "guardUntil", 0f);
            game.Player.enabled = false;
            body.linearVelocity = Vector2.zero; body.gravityScale = game.Config.gravityScale;
            float health = game.Player.Health;
            Force();
            await Until(() => game.Player.Health < health, 8);
            Check(game.Player.Health < health, "standing still is punished at x=" + x);
            Call(enemy, "InterruptPattern"); Set(enemy, "timer", float.MaxValue);
            game.Player.enabled = true;
        }
    }
    static async Task CheckDamageContact()
    {
        Reset();
        await Seconds(0.06f);
        if (game.Player.TestInvincible) game.Player.ToggleTestInvincibility();
        Set(game.Player, "invulnerableUntil", 0f); Set(game.Player, "guardUntil", 0f);
        game.Player.enabled = false;
        var body = game.Player.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        body.linearVelocity = Vector2.zero;
        game.Player.transform.position = body.position;
        Physics2D.SyncTransforms();
        float before = game.Player.Health;
        float damage = (float)typeof(KarmaEnemy).GetProperty("Damage", Private).GetValue(enemy) * 2.5f;
        var shot = new KarmaAerialSwordPlanner.Shot { origin = game.Player.GetComponent<Collider2D>().bounds.center,
            velocity = Vector2.down * 16, acceleration = Vector2.down * 300,
            size = new Vector2(3, 2), life = 0.12f };
        KarmaSW05RainBlade.Spawn(game, enemy, Root, shot, damage, null);
        await Until(() => game.Player.Health < before, 2);
        Check(Mathf.Abs((before - game.Player.Health) - damage) < 0.01f, "real rain contact applies the full ultimate damage once");
        await Until(() => Root.GetComponentsInChildren<KarmaSW05RainBlade>().Length == 0, 2);
        float after = game.Player.Health;
        Set(game.Player, "invulnerableUntil", 0f); await Seconds(0.2f);
        Check(game.Player.Health == after, "planted sword cannot damage the player again");
        game.Player.enabled = true;
    }
    static async Task CheckHealthGates()
    {
        Reset();
        object behavior = Get<object>(enemy, "behavior");
        var ultimate = Get<object>(behavior, "ultimate");
        Func<bool> pending = () => (bool)ultimate.GetType().GetProperty("ThresholdPending").GetValue(ultimate);
        Action<float> health = fraction => typeof(KarmaEnemy).GetProperty("Health").SetValue(enemy, enemy.MaxHealth * fraction);
        health(0.51f); Check(!pending(), "SW05 is locked above half health");
        health(0.5f); Check(pending(), "50% queues the first ultimate");
        Set(enemy, "nextUltimate", float.MaxValue);
        Call(enemy, "BeginAttack");
        Check(Get<EnemyAttack>(enemy, "selected") == EnemyAttack.SW05, "50% gate overrides ordinary cooldown and basic attacks");
        await Until(() => Forming().Length > 0, 8);
        Call(enemy, "InterruptPattern"); Set(enemy, "timer", float.MaxValue);
        Check(!pending(), "first gate stays consumed after interruption");
        health(1); health(0.49f); Check(!pending(), "healing cannot rearm 50% gate");
        health(0.25f); Check(pending(), "25% queues the second ultimate");
        Call(enemy, "BeginAttack");
        Check(Get<EnemyAttack>(enemy, "selected") == EnemyAttack.SW05, "25% gate overrides cooldown and repeated previous ultimate");
        await Until(() => Forming().Length > 0, 8);
        Call(enemy, "InterruptPattern"); Set(enemy, "timer", float.MaxValue);
        health(0.01f); Check(!pending(), "second gate is consumed exactly once");
        for (int i = 0; i < 8; i++)
        {
            var args = new object[] { 10f, true, true, default(EnemyAttack) };
            bool found = (bool)behavior.GetType().GetMethod("TrySelect").Invoke(behavior, args);
            Check(!found || (EnemyAttack)args[3] != EnemyAttack.SW05, "ordinary attack cycle cannot repeat the ultimate");
        }
        Reset(); behavior = Get<object>(enemy, "behavior"); ultimate = Get<object>(behavior, "ultimate");
        health(0.2f);
        for (int i = 0; i < 2; i++)
        {
            Check(pending(), "burst damage retains each crossed health gate");
            Call(enemy, "BeginAttack"); await Until(() => Forming().Length > 0, 8);
            Call(enemy, "InterruptPattern"); Set(enemy, "timer", float.MaxValue);
        }
        Check(!pending(), "crossing both gates never creates a third cast");
    }
    static async void Run()
    {
        Directory.CreateDirectory(Output);
        Application.logMessageReceived += Log;
        try
        {
            Application.runInBackground = true;
            game = UnityEngine.Object.FindAnyObjectByType<KarmaGame>();
            var config = UnityEngine.Object.Instantiate(game.Config); Set(game, "config", config);
            config.playerHealth = 100000; config.BossTuning.sword.aerialSwordSeed = 5005;
            Time.maximumDeltaTime = 0.05f;
            Reset(); launches.Clear();
            var dodge = game.gameObject.AddComponent<SW05DodgeDriver>();
            dodge.Game = game; dodge.Owner = enemy;
            Set(game.Player, "invulnerableUntil", 0f); Set(game.Player, "guardUntil", 0f);
            float safeHealth = game.Player.Health;
            game.Player.enabled = false;
            Force();
            int firstFrame = Time.frameCount;
            await Until(() => Forming().Length > 0, 8);
            Check(Time.frameCount > firstFrame, "player loop advances");
            Check(enemy.Position.y >= 7 && enemy.Position.y <= 8, "boss floats at mid height");
            var blades = Forming();
            Check(blades.Length >= config.BossTuning.sword.aerialSwordVolleyMin && blades.Length <= config.BossTuning.sword.aerialSwordVolleyMax, "dense volley respects randomized count");
            await Seconds(0.15f);
            Vector3 before = blades[0].transform.position;
            Capture("ascent");
            Check(Root.GetComponentsInChildren<KarmaSW05RainBlade>().Length == 0, "ascent is harmless");
            await Seconds(0.24f);
            Check(Vector3.Distance(before, blades[0].transform.position) > 0.5f, "blades travel from boss into sky");
            await Seconds(0.94f);
            Capture("apex");
            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            bool tilted = false;
            foreach (var blade in Forming())
            {
                float angle = Mathf.Abs(Mathf.DeltaAngle(blade.transform.eulerAngles.z, 0));
                Check(angle <= 62, "crown sword faces downward with a controlled fan angle");
                tilted |= angle > 3;
                low = Mathf.Min(low, blade.transform.position.y); high = Mathf.Max(high, blade.transform.position.y);
            }
            Check(tilted && low > enemy.Position.y + 10, "circular summon rises to a high release formation");
            await Until(() => Root.GetComponentsInChildren<KarmaSW05RainBlade>().Length > 0, 3);
            var attacks = Root.GetComponentsInChildren<KarmaSW05RainBlade>();
            Check(attacks.Length == blades.Length, "full volley launches");
            float nearest = 0, farthest = float.PositiveInfinity;
            bool diagonal = false;
            float leftTip = float.PositiveInfinity, rightTip = float.NegativeInfinity;
            foreach (var blade in attacks)
            {
                var shot = Get<KarmaAerialSwordPlanner.Shot>(blade, "shot");
                nearest = Mathf.Max(nearest, shot.size.x); farthest = Mathf.Min(farthest, shot.size.x);
                diagonal |= Mathf.Abs(shot.velocity.normalized.x) > 0.35f;
                Vector2 tip = shot.PositionAt(shot.life) + shot.velocity.normalized * shot.size.x / 2;
                leftTip = Mathf.Min(leftTip, tip.x); rightTip = Mathf.Max(rightTip, tip.x);
                float ceiling = Mathf.Min(KarmaSpace.Units(12), game.GameCamera.transform.position.y + game.GameCamera.orthographicSize);
                Check(shot.origin.y + shot.velocity.normalized.y * shot.size.x / 2 >= ceiling - 4.6f,
                    "every sword releases from the top of the screen");
                Check(Mathf.Abs(tip.y - 0.05f) < 0.002f, "accelerated diagonal tip hits the advertised ground plane");
                Check(shot.life < 0.16f, "rain strikes rapidly after its warning");
            }
            Check(nearest > 9 && nearest / farthest > 1.5f, "circular layout has substantial near/far perspective");
            Check(diagonal, "outer swords fall on strongly slanted trajectories");
            Check(leftTip <= KarmaStage.ArenaLeft + 0.3f && rightTip >= KarmaStage.ArenaRight - 0.3f,
                "rain includes the entire arena width and both edges");
            Check(attacks[0].Visual.sprite.name == "SW05JudgementSword", "released sword uses refined weapon art");
            var flightTints = new List<Color>();
            foreach (var blade in attacks) flightTints.Add(blade.Visual.color);
            var firstShot = Get<KarmaAerialSwordPlanner.Shot>(attacks[0], "shot");
            float heading = Mathf.Atan2(firstShot.velocity.y, firstShot.velocity.x) * Mathf.Rad2Deg + 90;
            Check(Mathf.Abs(Mathf.DeltaAngle(attacks[0].Visual.transform.eulerAngles.z, heading)) < 0.01f, "slanted artwork matches the real falling hitbox");
            Check(firstShot.acceleration.magnitude >= 1200, "rain accelerates toward ground");
            float ordinaryDamage = (float)typeof(KarmaEnemy).GetProperty("Damage", Private).GetValue(enemy);
            Check(Get<float>(attacks[0], "damage") >= ordinaryDamage * 2.5f - 0.0001f, "ultimate deals at least 2.5x ordinary damage");
            Check(attacks[0].GetComponentsInChildren<TrailRenderer>().Length > 0, "falling sword has tapered wake");
            await Seconds(0.035f); Capture("rain");
            await Until(() => Root.GetComponentsInChildren<KarmaSW05RainBlade>().Length == 0, 3);
            await Seconds(0.04f); Capture("impact");
            bool contactShadow = false;
            foreach (var sr in Root.GetComponentsInChildren<SpriteRenderer>())
            {
                contactShadow |= sr.name == "SW05 wood penetration / no damage";
                Check(!sr.name.Contains("crystal ember"), "wood contact produces no debris");
            }
            Check(contactShadow, "wood strike has a small contact shadow");
            int planted = 0;
            foreach (var sr in Root.GetComponentsInChildren<SpriteRenderer>()) if (sr.name == "SW05 wood-embedded sword / no damage") planted++;
            Check(planted > 0, "slanted swords remain planted as harmless VFX");
            foreach (var sr in Root.GetComponentsInChildren<SpriteRenderer>())
                if (sr.name == "SW05 wood-embedded sword / no damage")
                {
                    Check(sr.sprite.name == "SW05JudgementSword", "wood contact keeps the airborne weapon picture");
                    Check(flightTints.Exists(tint => Mathf.Abs(tint.r - sr.color.r) + Mathf.Abs(tint.g - sr.color.g)
                        + Mathf.Abs(tint.b - sr.color.b) < 0.001f), "embedded sword retains its airborne depth tint");
                }
            await Until(() => !Get<bool>(enemy, "running"), 25);
            dodge.enabled = false; game.Player.enabled = true;
            Check(game.Player.Health == safeHealth, "real dynamic player follows a physically available escape through all three slams without damage");
            Check(dodge.Groups == 3, "every slam provides a new physical escape route");
            Check(launches.Count == dodge.ExpectedSwords && launches.Count >= 48 && launches.Count <= 54, "three complete dense rain slams");
            bool changed = false;
            var previousLanes = dodge.Plans[0].shots;
            for (int g = 1; g < dodge.Plans.Count; g++)
            {
                var nextLanes = dodge.Plans[g].shots;
                if (nextLanes.Length != previousLanes.Length) changed = true;
                else for (int i = 0; i < nextLanes.Length; i++)
                {
                    Vector2 a = previousLanes[i].PositionAt(previousLanes[i].life) + previousLanes[i].velocity.normalized * previousLanes[i].size.x / 2;
                    Vector2 b = nextLanes[i].PositionAt(nextLanes[i].life) + nextLanes[i].velocity.normalized * nextLanes[i].size.x / 2;
                    changed |= Mathf.Abs(a.x - b.x) > 0.5f;
                }
            }
            Check(changed, "successive slams have different landing layouts");
            Check(Mathf.Abs(enemy.Position.y - KarmaSpace.GuardianGroundY) < 0.05f, "boss lands");
            Check(!Get<bool>(enemy, "fullShield"), "landing releases shield");
            Check(Root.childCount == 0, "completion cleans pattern objects");
            Force(); await Until(() => Forming().Length > 0, 8);
            Call(enemy, "InterruptPattern"); await Seconds(0.1f);
            Check(Forming().Length == 0 && Root.GetComponentsInChildren<KarmaSW05VFX>().Length == 0, "interrupt cleans sky blades and VFX");
            await CheckStandingRisk();
            await CheckDamageContact();
            await CheckHealthGates();
            Check(runtimeError == null, "no runtime errors: " + runtimeError);
            var material = Resources.Load<Material>("SW05/SW05Celestial");
            Check(material != null && !UnityEditor.ShaderUtil.ShaderHasError(material.shader), "celestial material compiles");
            var crimson = Resources.Load<Material>("SW05/SW05CrimsonBlade");
            Check(crimson != null && !UnityEditor.ShaderUtil.ShaderHasError(crimson.shader), "crimson steel shader compiles");
            var wood = Resources.Load<Material>("SW05/SW05EmbeddedWood");
            Check(wood != null && !UnityEditor.ShaderUtil.ShaderHasError(wood.shader), "wood floor clipping shader compiles");
            File.WriteAllText(Output + "/sw05-play-validation.txt", "PASS: " + assertions + " SW05 flight, rain, impact, completion and cancellation assertions\n");
            SessionState.SetBool(Key, false); EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllText(Output + "/sw05-play-validation.txt", "FAIL: " + error + "\n");
            Debug.LogException(error); SessionState.SetBool(Key, false); EditorApplication.Exit(1);
        }
    }
}
// Drives only ordinary horizontal velocity and the planner's optional jump.
// Terrain, gravity, real player collider and real attack sweeps remain active.
public sealed class SW05DodgeDriver : MonoBehaviour
{
    public KarmaGame Game;
    public KarmaEnemy Owner;
    public int Groups, ExpectedSwords;
    public readonly List<KarmaAerialSwordPlanner.Group> Plans = new List<KarmaAerialSwordPlanner.Group>();
    KarmaAerialSwordPlanner.Group plan;
    KarmaAerialSwordPlanner planner;
    bool jumped;
    void FixedUpdate()
    {
        if (Game == null || Owner == null || Game.Paused) return;
        if (planner == null) planner = UnityEngine.Object.FindAnyObjectByType<KarmaAerialSwordPlanner>();
        if (planner == null || planner.LatestGroup == null) return;
        if (plan != planner.LatestGroup) { plan = planner.LatestGroup; jumped = false; Groups++; ExpectedSwords += plan.shots.Length; Plans.Add(plan); }
        var body = Game.Player.GetComponent<Rigidbody2D>();
        float vertical = body.linearVelocity.y;
        if (!jumped && plan.escapeJumpAt >= 0 && Owner.FightSeconds - planner.LatestGroupAt >= plan.escapeJumpAt)
        { vertical = Game.Config.jumpSpeed; jumped = true; }
        body.gravityScale = Game.Config.gravityScale;
        body.linearVelocity = new Vector2(plan.escapeInput * Game.Config.moveSpeed, vertical);
    }
}
#endif
