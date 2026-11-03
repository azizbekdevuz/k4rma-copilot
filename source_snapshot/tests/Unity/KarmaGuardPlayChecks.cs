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

// Exercises actual Update/coroutine contacts in a disposable Unity project.
[InitializeOnLoad]
public static class KarmaGuardPlayChecks
{
    const string BatchKey = "K4RMA.GuardPlayChecks.Batch";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static KarmaGame game;
    static KarmaEnemy enemy;
    static int assertions;
    static string runtimeError;
    static bool started;
    static KarmaGuardPlayChecks() { EditorApplication.update += Pump; }
    static void Pump()
    {
        if (!SessionState.GetBool(BatchKey, false)) return;
        EditorApplication.QueuePlayerLoopUpdate();
        // Unity Search may enable Error Pause during its initial index build.
        // Clear only pre-test startup pauses; runtime errors are captured below.
        if (!started && EditorApplication.isPlaying) EditorApplication.isPaused = false;
        if (!started && EditorApplication.isPlaying && Time.frameCount > 1)
        { started = true; RunBatchChecks(); }

    }
    public static void RunBatch()
    {
        Debug.Log("Starting guard Play Mode validation");
        SessionState.SetBool(BatchKey, true);
        EditorApplication.update -= EditorApplication.QueuePlayerLoopUpdate;
        EditorApplication.update += EditorApplication.QueuePlayerLoopUpdate;
        var gameView = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        gameView.Show();
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/K4RMA/Scenes/KarmaPrototype.unity");
        EditorApplication.EnterPlaymode();
    }
    static async void RunBatchChecks()
    {
        try { await Run(); SessionState.SetBool(BatchKey, false); EditorApplication.Exit(0); }
        catch (Exception error)
        {
            Directory.CreateDirectory("output/validation");
            File.WriteAllText("output/validation/guard-play-validation.txt", "FAIL: " + error + "\n");
            Debug.LogException(error);
            SessionState.SetBool(BatchKey, false); EditorApplication.Exit(1);
        }
    }
    static object Pattern(string key)
    {
        var behavior = typeof(KarmaEnemy).GetField("behavior", Private).GetValue(enemy);
        if (key == "GD01") return behavior.GetType().GetField("shield", Private).GetValue(behavior);
        var patterns = (System.Collections.IDictionary)behavior.GetType().BaseType.GetField("patterns", Private).GetValue(behavior);
        return patterns[Enum.Parse(typeof(EnemyAttack), key)];
    }
    static FieldInfo Field(object target, string name) { return target.GetType().GetField(name, Private); }
    static T Get<T>(object target, string name)
    {
        if (ReferenceEquals(target, enemy) && name == "heavyParryQueued") target = Pattern("GD02");
        return (T)Field(target, name).GetValue(target);
    }
    static void Set(object target, string name, object value) { Field(target, name).SetValue(target, value); }
    static object Call(object target, string name, params object[] args)
    {
        if (ReferenceEquals(target, enemy))
        {
            if (name == "BreakAutoShield") { Set(enemy, "shieldUntil", 0f); Set(enemy, "shieldWarningUntil", 0f); Set(enemy, "nextShield", enemy.FightSeconds + game.Config.BossTuning.guard.autoShieldPeriod); return null; }
            if (name == "RefreshAutoShield") { Set(enemy, "shieldUntil", float.PositiveInfinity); Set(enemy, "shieldWarningUntil", 0f); return null; }
            if (name == "ShieldFragment") target = Pattern("GD03");
            if (name.StartsWith("Trial")) target = Pattern("GD05");
        }
        return target.GetType().GetMethod(name, Private | BindingFlags.Public).Invoke(target, args);
    }
    static void Check(bool ok, string label)
    { assertions++; if (!ok) throw new Exception(label); }

    public static Task<string> Run() => RunChecks(false);
    public static Task<string> RunGD03Only() => RunChecks(true);
    static async Task<string> RunChecks(bool shardsOnly)
    {
        Check(Application.isPlaying, "Play Mode required");
        game = UnityEngine.Object.FindAnyObjectByType<KarmaGame>();
        Check(game != null, "K4RMA game exists");
        var original = game.Config;
        var config = UnityEngine.Object.Instantiate(original);
        Set(game, "config", config);
        config.playerHealth = 100000;
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        assertions = 0;
        runtimeError = null;
        Application.logMessageReceived += CaptureError;
        try
        {
            EditorApplication.isPaused = false;
            int frame = Time.frameCount;
            double frameDeadline = EditorApplication.timeSinceStartup + 10;
            while (Time.frameCount <= frame && EditorApplication.timeSinceStartup < frameDeadline)
            { EditorApplication.QueuePlayerLoopUpdate(); await Task.Delay(50); }
            Check(Time.frameCount > frame, "guard checks run on an advancing player loop");
            if (shardsOnly)
            {
                await CheckThrowToSplit();
                await CheckSequentialFragments();
                Check(runtimeError == null, "GD03 connected throw, split, chase and return emit no game runtime errors: " + runtimeError);
                string shardResult = "PASS: " + assertions + " GD03 connected throw / sequential fragment regression assertions";
                Directory.CreateDirectory("output/validation");
                File.WriteAllText("output/validation/gd03-connected-regression-validation.txt", shardResult + "\n");
                return shardResult;
            }
            await CheckPersistentShield();
            Reset();
            Call(enemy, "BreakAutoShield");
            Set(enemy, "running", true);
            Set(enemy, "nextShield", enemy.FightSeconds);
            await Seconds(0.25f);
            Check(Get<float>(enemy, "shieldWarningUntil") == 0, "GD01 defers generation during another attack");
            Set(enemy, "running", false);
            await Until(() => Get<float>(enemy, "shieldWarningUntil") > 0, 2);
            Set(enemy, "timer", 0f);
            await Seconds(0.2f);
            Check(!Get<bool>(enemy, "running"), "GD01 generation warning finishes before another attack starts");
            await Until(() => enemy.AutoShieldActive, 2);
            Check(enemy.Armored, "GD01 reforms after its complete generation warning");

            Reset();
            Place(new Vector2(5, KarmaSpace.GuardianGroundY));
            Set(game.Player, "invulnerableUntil", 0f);
            float health = game.Player.Health;
            Force(EnemyAttack.GD02);
            var warning = Get<Transform>(enemy, "effectsRoot").GetComponentInChildren<KarmaAttackWarning>();
            bool initialized = warning != null;
            if (warning != null)
                foreach (var line in warning.GetComponentsInChildren<LineRenderer>())
                    initialized &= line.widthMultiplier < KarmaSpace.Units(0.1f) && line.startColor.b < 0.5f;
            Check(initialized, "warning lines have their intended color and thickness on the first frame");
            await Until(() => Attacks().Length > 0, 3);
            Place(enemy.Position + Vector2.left * KarmaSpace.Units(2));
            await Until(() => game.Player.Health < health, 1);
            game.Player.Heal(config.playerHealth);
            await Until(() => State() == "Recover", 2);
            Check(enemy.AutoShieldActive, "GD02 hit and subsequent healing keep GD01 active through recovery");

            Reset();
            Set(enemy, "selected", EnemyAttack.GD02);
            game.Player.BeginCounter();
            Set(game.Player, "invulnerableUntil", 0f);
            int parries = game.Player.ParryCount;
            enemy.TryAttackHit(game.Player.Position, game.Player.Position, Vector2.one, 10, new KarmaAttackContact(breaksShieldOnParry: true), 0);
            await Until(() => enemy.Stunned, 1);
            Check(game.Player.ParryCount == parries + 1 && !enemy.Armored, "GD02 real parry interrupts and breaks shield");
            Check(!enemy.AutoShieldActive && Get<float>(enemy, "nextShield") > enemy.FightSeconds + 7.8f,
                "GD02 parry starts the regeneration delay with GD01 absent");

            await CheckThrowToSplit();
            await CheckSequentialFragments();

            Reset();
            Set(enemy, "selected", EnemyAttack.GD02);
            Set(game.Player, "invulnerableUntil", 0f); game.Player.BeginCounter();
            enemy.TryAttackHit(game.Player.Position, game.Player.Position, Vector2.one, 10, new KarmaAttackContact(breaksShieldOnParry: true), 0);
            await Until(() => enemy.Stunned, 1);
            Place(new Vector2(15, KarmaSpace.Units(1)));
            Force(EnemyAttack.GD04);
            await Until(() => Walls().Count == config.BossTuning.guard.barrierSteps, 5);
            var walls = Walls();
            var wall = walls[0];
            var bounds = wall.GetComponent<BoxCollider2D>().bounds;
            CheckStairGeometry(walls);
            bool clearOfSteps = true;
            foreach (var step in walls)
            {
                var b = step.GetComponent<BoxCollider2D>().bounds;
                clearOfSteps &= Mathf.Abs(game.Player.Position.x - b.center.x) >= b.extents.x + KarmaSpace.PlayerHalfWidth - 0.02f
                    || game.Player.Position.y - KarmaSpace.PlayerHalfHeight >= b.max.y - 0.02f;
            }
            Check(clearOfSteps, "GD04 rising steps lift the player without trapping them inside an adjacent step");
            Check(Attacks().Length == 0, "GD04 standing wall blocks movement without ongoing damage");
            await Task.Delay(25);

            float before = enemy.Health;
            enemy.TakeDamage(100, enemy.Position + Vector2.left);
            Check(Mathf.Abs(before - enemy.Health - 100 * config.BossTuning.guard.shieldDamageMultiplier) < 0.01f, "GD04 reduces front attacks");
            before = enemy.Health;
            enemy.TakeDamage(100, enemy.Position + Vector2.right);
            Check(Mathf.Abs(before - enemy.Health - 100) < 0.01f, "GD04 permits full damage from behind");
            bounds = walls[walls.Count - 1].GetComponent<BoxCollider2D>().bounds;
            Place(new Vector2(bounds.center.x, bounds.max.y + KarmaSpace.PlayerHalfHeight + 0.2f));
            float aboveX = game.Player.Position.x;
            game.Player.ResolveBarrier(bounds.center.x, bounds.size.x, bounds.max.y);
            Check(Mathf.Abs(game.Player.Position.x - aboveX) < 0.001f, "GD04 does not shove a player already above a step");
            Place(new Vector2(bounds.min.x - KarmaSpace.PlayerHalfWidth - KarmaSpace.Units(0.2f), KarmaSpace.Units(1)));
            Physics2D.SyncTransforms();
            var stoppedAt = (Vector2)Call(game.Player, "MoveSlash", Vector2.right, KarmaSpace.Units(5));
            Check(stoppedAt.x <= bounds.min.x - KarmaSpace.PlayerHalfWidth + 0.05f, "GD04 blocks a piercing slash");
            Call(enemy, "InterruptPattern");
            Check(Walls().Count == 0, "GD04 interruption removes every step immediately");

            Reset();
            enemy.transform.position = new Vector3(KarmaStage.ArenaLeft + 2, KarmaSpace.GuardianGroundY, 0);
            typeof(KarmaEnemy).GetProperty("Health").SetValue(enemy, enemy.MaxHealth * 0.4f);
            Force(EnemyAttack.GD04);
            await Until(() => Walls().Count == config.BossTuning.guard.barrierSteps, 5);
            CheckStairGeometry(Walls());
            Check(enemy.Facing == 1, "GD04 near the arena edge chooses one direction with room for every step");

            await CheckBarrierCounterChain();

            config.BossTuning.guard.trialAttackCount = 5;
            config.BossTuning.guard.trialAllowedHits = 0;
            Reset();
            typeof(KarmaEnemy).GetProperty("Health").SetValue(enemy, enemy.MaxHealth * 0.45f);
            Force(EnemyAttack.GD05);
            float immuneHealth = enemy.Health;
            enemy.TakeDamage(enemy.MaxHealth * 10);
            Check(enemy.Invulnerable && enemy.Health == immuneHealth, "GD05 is immune from the room-formation telegraph");
            await Until(() => enemy.TrialActive, 3);
            var dome = UnityEngine.Object.FindAnyObjectByType<KarmaGuardDome>();
            Check(dome != null && dome.GetComponent<EdgeCollider2D>().pointCount == 65, "GD05 creates a semicircular room with a curved roof");
            Check(Mathf.Approximately(dome.Radius, 20) && Mathf.Approximately(Get<float>(game.Player, "arenaLeft"), 1.2f)
                && Mathf.Approximately(Get<float>(game.Player, "arenaRight"), 38.8f), "GD05 enlarged dome and confinement agree");
            var roofView = game.GameCamera.WorldToViewportPoint(new Vector3(20, dome.Radius, 0));
            var floorView = game.GameCamera.WorldToViewportPoint(new Vector3(20, 0, 0));
            Check(roofView.y < 0.8f && floorView.y > 0.04f, "GD05 camera fits the enlarged dome below the HUD");
            Check(dome.Overlaps(new Vector2(20, dome.Radius * 0.5f), Vector2.zero)
                && !dome.Overlaps(new Vector2(20 + dome.Radius + 1, 0), Vector2.zero)
                && !dome.Overlaps(new Vector2(20, -1), Vector2.zero), "GD05 room blast follows the dome footprint");
            Check(Get<bool>(game.Player, "confined"), "GD05 confines the player during the trial");
            float clock = enemy.FightSeconds;
            float domeClock = dome.PhaseSeconds;
            game.TogglePause();
            await Task.Delay(150);
            Check(enemy.FightSeconds == clock && enemy.TrialActive && dome.PhaseSeconds == domeClock, "GD05 pause freezes combat, trial and dome clocks");
            game.TogglePause();
            var cues = new[] { "낮은 파동", "봉인 낙하", "양쪽 협공", "바닥 봉인", "높낮이 연속" };
            var seen = new HashSet<string>();
            var peaks = new Dictionary<string, int>();
            bool trialGeometry = true, sharedContact = true, safeGaps = true, scaledAttacks = true;
            await Until(() =>
            {
                foreach (string cue in cues)
                    if (enemy.TrialActive && cue == cues[Get<KarmaTrialState>(enemy, "trial").Completed % cues.Length])
                    {
                        seen.Add(cue);
                        var shots = Attacks();
                        if (!peaks.ContainsKey(cue) || shots.Length > peaks[cue]) peaks[cue] = shots.Length;
                        if (shots.Length > 0)
                        {
                            var contact = Get<KarmaAttackContact>(shots[0], "contact");
                            var xs = new List<float>();
                            foreach (var shot in shots)
                            {
                                var size = Get<Vector2>(shot, "size");
                                var p = (Vector2)shot.transform.position;
                                var farCorner = new Vector2(Mathf.Abs(p.x - 20) + size.x / 2, p.y + size.y / 2);
                                trialGeometry &= p.y - size.y / 2 >= -0.02f && farCorner.magnitude <= dome.Radius + 0.05f;
                                sharedContact &= ReferenceEquals(contact, Get<KarmaAttackContact>(shot, "contact"));
                                Vector2 baseline = cue == "봉인 낙하" ? KarmaSpace.Units(new Vector2(0.65f, 0.85f))
                                    : cue == "바닥 봉인" ? KarmaSpace.Units(new Vector2(0.9f, 3.8f))
                                    : new Vector2(config.BossTuning.guard.trialWaveWidth, config.BossTuning.guard.trialWaveHeight);
                                scaledAttacks &= Vector2.Distance(size, baseline * 1.3f) < 0.01f;
                                xs.Add(p.x);
                            }
                            if ((cue == "봉인 낙하" || cue == "바닥 봉인") && shots.Length == 5)
                            {
                                xs.Sort(); float width = Get<Vector2>(shots[0], "size").x;
                                for (int i = 1; i < xs.Count; i++)
                                    safeGaps &= xs[i] - xs[i - 1] - width > KarmaSpace.PlayerHalfWidth * 2 + 0.4f;
                            }
                        }
                    }
                if (enemy.Invulnerable)
                {
                    float beforeImmune = enemy.Health;
                    enemy.TakeDamage(100);
                    if (enemy.Health != beforeImmune) throw new Exception("GD05 lost immunity before recovery");
                }
                return State() == "Recover";
            }, 22);
            Check(seen.Count == 5, "GD05 runs five different readable attack beats");
            foreach (var expected in new[] { new KeyValuePair<string, int>("낮은 파동", 2),
                new KeyValuePair<string, int>("봉인 낙하", 5), new KeyValuePair<string, int>("양쪽 협공", 4),
                new KeyValuePair<string, int>("바닥 봉인", 5), new KeyValuePair<string, int>("높낮이 연속", 4) })
                Check(peaks[expected.Key] == expected.Value, "GD05 larger simultaneous count: " + expected.Key);
            Check(trialGeometry && scaledAttacks, "GD05 enlarged attacks and complete paths stay inside the semicircle");
            Check(sharedContact && safeGaps, "GD05 expanded volleys share one contact and leave usable rain/pillar corridors");
            clock = enemy.FightSeconds;
            Check(!enemy.TrialActive && !Get<bool>(game.Player, "confined")
                && UnityEngine.Object.FindAnyObjectByType<KarmaGuardDome>() == null, "GD05 success dissolves the room and releases arena restrictions");
            Check(!enemy.AutoShieldActive, "GD05 success removes the room and breaks GD01");
            immuneHealth = enemy.Health; enemy.TakeDamage(100);
            Check(!enemy.Invulnerable && enemy.Health < immuneHealth, "GD05 success unlocks real damage during the punish window");
            await Until(() => !Get<bool>(enemy, "running"), 7);
            Check(enemy.FightSeconds - clock >= config.BossTuning.guard.trialStunSeconds - 0.08f,
                "GD05 success retains the full configured punish window even while enraged");

            Reset();
            Force(EnemyAttack.GD05);
            await Until(() => enemy.TrialActive, 3);
            Set(game.Player, "invulnerableUntil", 0f);
            enemy.TryAttackHit(game.Player.Position, game.Player.Position, Vector2.one, 10, new KarmaAttackContact(), 0);
            Set(game.Player, "invulnerableUntil", Time.time + 100);
            var trial = Get<KarmaTrialState>(enemy, "trial");
            Check(trial.Hits == 1 && !trial.Finished && enemy.TrialActive, "GD05 excess hits do not end the trial early");
            await Until(() => UnityEngine.Object.FindAnyObjectByType<KarmaGuardDome>()?.Charging == true, 22);
            dome = UnityEngine.Object.FindAnyObjectByType<KarmaGuardDome>();
            Check(!enemy.TrialActive && Get<bool>(game.Player, "confined") && dome != null && dome.Charging,
                "GD05 failure keeps the room intact while charging its explosion");
            immuneHealth = enemy.Health; enemy.TakeDamage(enemy.MaxHealth * 10);
            Check(enemy.Invulnerable && enemy.Health == immuneHealth, "GD05 failed-room overload remains immune");
            clock = dome.PhaseSeconds;
            game.TogglePause(); await Task.Delay(150);
            Check(dome.PhaseSeconds == clock && dome.Charging, "GD05 overload animation pauses with gameplay");
            game.TogglePause();
            health = game.Player.Health; Set(game.Player, "invulnerableUntil", 0f);
            await Until(() => dome.Exploding, 2);
            Check(dome.gameObject.activeInHierarchy && Attacks().Length == 0,
                "GD05 shell itself explodes without spawning a generic giant attack");
            await Seconds(0.25f);
            Check(Mathf.Abs(health - game.Player.Health - config.enemyDamage * config.BossTuning.damageMultiplier * config.BossTuning.guard.trialExplosionMultiplier) < 0.01f,
                "GD05 room explosion deals exactly one configured hit");
            await Until(() => !Get<bool>(enemy, "running"), 5);
            Check(!Get<bool>(game.Player, "confined") && Attacks().Length == 0, "GD05 failure cleans up attacks and movement restrictions");
            Check(enemy.AutoShieldActive, "GD05 room detonation does not break GD01");
            immuneHealth = enemy.Health; enemy.TakeDamage(100);
            Check(!enemy.Invulnerable && enemy.Health < immuneHealth, "GD05 failure releases immunity after the room explosion");

            config.BossTuning.guard.trialAttackCount = 3;
            Reset(); Force(EnemyAttack.GD05);
            await Until(() => enemy.TrialActive, 3);
            Set(game.Player, "invulnerableUntil", 0f);
            enemy.TryAttackHit(game.Player.Position, game.Player.Position, Vector2.one, 10, new KarmaAttackContact(), 0);
            Set(game.Player, "invulnerableUntil", Time.time + 100);
            await Until(() => UnityEngine.Object.FindAnyObjectByType<KarmaGuardDome>()?.Charging == true, 15);
            dome = UnityEngine.Object.FindAnyObjectByType<KarmaGuardDome>();
            await Until(() => dome.PhaseSeconds >= 0.9f, 2);
            health = game.Player.Health; parries = game.Player.ParryCount;
            Set(game.Player, "invulnerableUntil", 0f); game.Player.BeginCounter();
            await Until(() => dome.Exploding, 2); await Seconds(0.25f);
            Check(game.Player.ParryCount == parries + 1 && game.Player.Health == health,
                "GD05 room detonation retains a timed counter opportunity");
            await Until(() => !Get<bool>(enemy, "running"), 5);

            Reset();
            Force(EnemyAttack.GD05);
            await Until(() => enemy.TrialActive, 3);
            game.PlayerDied();
            Check(!enemy.TrialActive && !Get<bool>(game.Player, "confined") && Attacks().Length == 0
                && UnityEngine.Object.FindAnyObjectByType<KarmaGuardDome>() == null && !enemy.Invulnerable,
                "GD05 death cancels the trial and releases the player synchronously");

            Check(runtimeError == null, "guard paths emit no Unity runtime errors: " + runtimeError);

            string result = "PASS: " + assertions + " guard Play Mode assertions (GD01-GD05)";
            Directory.CreateDirectory("output/validation");
            File.WriteAllText("output/validation/guard-play-validation.txt", result + "\n");
            Debug.Log(result);
            return result;
        }
        finally
        {
            Application.logMessageReceived -= CaptureError;
                if (game.Paused) game.TogglePause();
            game.ReturnToTitle();
            Set(game, "config", original);
            UnityEngine.Object.Destroy(config);
            Application.runInBackground = background;
        }
    }

    static void CaptureError(string message, string stack, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            && (stack.Contains("Karma") || message.Contains("override geometry"))) runtimeError = message;
    }

    static SpriteRenderer[] Fragments()
    {
        var found = new List<SpriteRenderer>();
        foreach (var sr in Get<Transform>(enemy, "effectsRoot").GetComponentsInChildren<SpriteRenderer>())
            if (sr.name.StartsWith("GD03 방패 ")) found.Add(sr);
        return found.ToArray();
    }

    static SpriteRenderer WholeShield()
    {
        var root = Get<Transform>(enemy, "effectsRoot").Find("GD03 큰 방패 투척");
        return root != null && root.gameObject.activeInHierarchy ? root.GetComponent<SpriteRenderer>() : null;
    }

    static async Task CheckThrowToSplit()
    {
        Reset();
        enemy.transform.position = new Vector3(32, KarmaSpace.GuardianGroundY, 0);
        Place(new Vector2(12, KarmaSpace.PlayerHalfHeight));
        Set(game.Player, "invulnerableUntil", 0f);
        await Task.Delay(35);
        float health = game.Player.Health;
        Force(EnemyAttack.GD03);
        await Until(() => WholeShield() != null, 2);
        var shield = WholeShield();
        Check(shield.sprite == KarmaArtwork.Sprite("GuardBossVFX", "WholeHot"), "GD03 launches the selected whole heated shield");
        Check(Fragments().Length == 0, "GD03 has no detached shards while the whole shield is flying");
        Vector3 from = shield.transform.position;
        await Seconds(0.1f);
        Check(shield != null && shield.transform.position.x < from.x - 1, "GD03 whole shield visibly travels toward the locked player position");
        Vector3 atPause = shield.transform.position, rotation = shield.transform.eulerAngles;
        float clock = enemy.FightSeconds;
        game.TogglePause(); await Task.Delay(150);
        Check(enemy.FightSeconds == clock && shield.transform.position == atPause && shield.transform.eulerAngles == rotation,
            "GD03 pause freezes both travel and shield spin");
        game.TogglePause();
        await Until(() => Fragments().Length == game.Config.BossTuning.guard.shieldFragments, 2);
        Vector2 impact = shield.transform.position;
        Check(Mathf.Abs(impact.x - 12) < 0.02f && shield.color.a > 0,
            "GD03 first shard frame overlaps the actual arriving whole shield without an empty gap");
        Check(Mathf.Abs(health - game.Player.Health - game.Config.enemyDamage * game.Config.BossTuning.damageMultiplier * 0.8f) < 0.02f
            && !game.Player.Stunned, "GD03 initial whole shield deals exactly one damage-only contact");
        var born = Fragments();
        var positions = new Vector3[born.Length];
        var ids = new HashSet<SpriteRenderer>();
        bool bornTogether = true;
        for (int i = 0; i < born.Length; i++)
        {
            ids.Add(born[i]); positions[i] = born[i].transform.position;
            bornTogether &= Vector2.Distance(positions[i], impact) < KarmaSpace.Units(0.75f)
                && born[i].sprite == KarmaArtwork.Sprite("GuardBossVFX", "ShardHot");
        }
        Check(bornTogether, "GD03 hot shards emerge together from the arriving shield rather than their distant rest positions");
        health = game.Player.Health; Set(game.Player, "invulnerableUntil", 0f);
        await Seconds(0.06f);
        bool movedOut = true;
        for (int i = 0; i < born.Length; i++)
            movedOut &= Vector2.Distance(born[i].transform.position, impact) > Vector2.Distance(positions[i], impact) + 0.1f;
        Check(movedOut, "GD03 shattering visibly moves every shard outward");
        positions = Array.ConvertAll(born, p => p.transform.position);
        game.TogglePause(); await Task.Delay(150);
        bool frozen = true;
        for (int i = 0; i < born.Length; i++) frozen &= born[i].transform.position == positions[i];
        Check(frozen, "GD03 shatter spread freezes during pause");
        game.TogglePause();
        await Until(() => WholeShield() == null, 1);
        Check(game.Player.Health == health && !game.Player.Stunned,
            "GD03 shatter presentation adds no hidden damage or stun");
        bool sameObjects = true, spreadFinished = true;
        foreach (var piece in Fragments())
        {
            sameObjects &= ids.Contains(piece);
            spreadFinished &= Vector2.Distance(piece.transform.position, impact) > KarmaSpace.Units(1.4f);
        }
        Check(sameObjects && spreadFinished, "GD03 keeps the same shards from shattering into their spread formation");
        var readyPositions = Array.ConvertAll(born, p => p.transform.position);
        await Seconds(0.12f);
        bool stillAiming = true;
        for (int i = 0; i < born.Length; i++) stillAiming &= born[i].transform.position == readyPositions[i];
        Check(stillAiming && game.Player.Health == health, "GD03 preserves the readable aim warning between spreading and tracking");
        await Until(() => Array.Exists(Fragments(), p => p.name.EndsWith("추적")), 2);
        Check(Array.TrueForAll(Fragments(), p => ids.Contains(p)),
            "GD03 tracking starts on the original shattered objects, with no replacement pop-in");
        Call(enemy, "InterruptPattern");
        Check(WholeShield() == null && Fragments().Length == 0 && enemy.AutoShieldActive,
            "GD03 interruption clears the connected sequence while preserving GD01");

        foreach (string phase in new[] { "flight", "split", "aim" })
        {
            Reset(); Force(EnemyAttack.GD03);
            await Until(() => WholeShield() != null, 2);
            if (phase != "flight") await Until(() => Fragments().Length > 0, 2);
            if (phase == "aim") await Until(() => WholeShield() == null, 1);
            Call(enemy, "InterruptPattern");
            await Seconds(0.4f);
            Check(WholeShield() == null && Fragments().Length == 0
                && Get<Transform>(enemy, "effectsRoot").GetComponentsInChildren<TrailRenderer>().Length == 0,
                "GD03 cancellation leaves no late shield, shards or trails in " + phase);
        }

        Reset();
        Place(new Vector2(28, KarmaSpace.PlayerHalfHeight)); Set(game.Player, "invulnerableUntil", 0f);
        health = game.Player.Health;
        Force(EnemyAttack.GD03, 1);
        await Until(() => Fragments().Length > 0, 3);
        Check(game.Player.Health < health && !game.Player.Stunned, "GD03 right-facing whole throw also hits without stun");

        foreach (bool duringSplit in new[] { false, true })
        {
            Reset(); Force(EnemyAttack.GD03);
            await Until(() => duringSplit ? Fragments().Length > 0 : WholeShield() != null, 3);
            game.PlayerDied(); await Task.Delay(80);
            Check(WholeShield() == null && Fragments().Length == 0 && !Get<bool>(enemy, "running"),
                "GD03 player death stops the connected sequence during " + (duringSplit ? "split" : "flight"));
        }
    }

    static async Task CheckSequentialFragments()
    {
        Reset(); Force(EnemyAttack.GD03);
        var launched = new SortedDictionary<int, float>();
        var stopped = new Dictionary<int, Vector2>();
        var prior = new Dictionary<int, Vector2>();
        var returns = new HashSet<int>();
        int homing = 0, aligned = 0, fixedReturn = 0;
        bool paused = false;
        bool heatStates = true, trailStates = true, compact = true;
        var hot = KarmaArtwork.Sprite("GuardBossVFX", "ShardHot");
        var cooled = KarmaArtwork.Sprite("GuardBossVFX", "ShardCooled");
        Check(hot != null && cooled != null && hot != cooled, "GD03 selected D+B hot and cooled sprites are linked");
        Check(hot.texture == cooled.texture && hot.texture.name == "ShardDB", "GD03 heat states use the new production sheet");
        Vector2 lastTarget = game.Player.Position;
        double deadline = EditorApplication.timeSinceStartup + 9;
        while (State() != "Recover")
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("GD03 sequence timeout");
            foreach (var piece in Fragments())
            {
                int id = int.Parse(piece.name.Split(' ')[2]);
                Vector2 p = piece.transform.position;
                var trail = piece.GetComponent<TrailRenderer>();
                compact &= Mathf.Max(piece.sprite.bounds.size.x * Mathf.Abs(piece.transform.localScale.x),
                    piece.sprite.bounds.size.y * Mathf.Abs(piece.transform.localScale.y)) <= KarmaSpace.Units(0.65f) + 0.01f;
                if (piece.name.EndsWith("추적"))
                {
                    heatStates &= piece.sprite == hot && piece.sharedMaterial == KarmaVisuals.DefaultMaterial;
                    trailStates &= trail != null && trail.emitting && trail.startColor.r > 0.9f && trail.startColor.g < 0.3f;
                    if (!launched.ContainsKey(id)) launched[id] = enemy.FightSeconds;
                    else if (prior.TryGetValue(id, out var before) && (p - before).sqrMagnitude > 0.00001f)
                    { homing++; if (Vector2.Dot(p - before, lastTarget - before) >= -0.02f) aligned++; }
                    prior[id] = p;
                }
                if (piece.name.EndsWith("정지") && !stopped.ContainsKey(id)) stopped[id] = p;
                if (piece.name.EndsWith("복귀") && stopped.TryGetValue(id, out var start))
                {
                    heatStates &= piece.sprite == cooled && piece.sharedMaterial == KarmaVisuals.DefaultMaterial;
                    trailStates &= trail != null && trail.emitting && trail.startColor.r < 0.8f && trail.time <= 0.056f;
                    returns.Add(id);
                    Vector2 line = enemy.Position - start, offset = p - start;
                    if (line.magnitude > 0.01f)
                    {
                        if (Mathf.Abs(line.x * offset.y - line.y * offset.x) / line.magnitude > 0.04f)
                            throw new Exception("GD03 return followed player instead of its fixed line");
                        fixedReturn++;
                    }
                }
            }
            if (launched.Count >= 2 && !paused)
            {
                var pieces = Fragments(); var positions = new Vector3[pieces.Length];
                for (int i = 0; i < pieces.Length; i++) positions[i] = pieces[i].transform.position;
                float clock = enemy.FightSeconds;
                game.TogglePause(); await Task.Delay(150);
                bool frozen = enemy.FightSeconds == clock;
                for (int i = 0; i < pieces.Length; i++) frozen &= Vector3.Distance(positions[i], pieces[i].transform.position) < 0.001f;
                Check(frozen, "GD03 pause freezes every independently moving shield");
                game.TogglePause(); paused = true;
            }
            lastTarget = new Vector2(13 + Mathf.Sin(enemy.FightSeconds * 5) * 5, 3 + Mathf.Sin(enemy.FightSeconds * 3));
            Place(lastTarget);
            await Task.Delay(5);
        }
        Check(launched.Count == 4 && returns.Count == 4, "GD03 launches and returns all four shields");
        float previous = -100;
        foreach (var time in launched.Values)
        {
            if (previous > -1) Check(time - previous >= game.Config.BossTuning.guard.fragmentLaunchGap - 0.06f,
                "GD03 shields launch with a real stagger");
            previous = time;
        }
        Check(homing > 20 && aligned >= homing * 0.9f, "GD03 outbound shields follow the moving player");
        Check(fixedReturn > 20, "GD03 return paths stay fixed despite subsequent player movement");
        Check(heatStates && trailStates, "GD03 hot tracking and cooled return use distinct sprites and red/gray trails");
        Check(compact, "GD03 selected art retains the existing compact projectile footprint");
        Check(Fragments().Length == 0 && enemy.AutoShieldActive, "GD03 cleans every shield while preserving GD01");

        // Isolate one shield to prove its outward hit cannot consume its return hit.
        Reset(); Call(enemy, "InterruptPattern");
        Set(enemy, "selected", EnemyAttack.GD03); Set(enemy, "running", true);
        Set(enemy, "state", Enum.Parse(Get<object>(enemy, "state").GetType(), "Attack"));
        Set(enemy, "patternStarted", enemy.FightSeconds);
        Place(new Vector2(8, 2)); Set(game.Player, "invulnerableUntil", 0f);
        var single = KarmaVisuals.Box(Get<Transform>(enemy, "effectsRoot"), "single fragment test", new Vector2(8, 6), Vector2.one, Color.white, 26);
        KarmaCombatVFX.ApplySprite(single, "GuardBossVFX", "ShardHot", KarmaSpace.Units(Vector2.one * 0.65f), true);
        float health = game.Player.Health;
        enemy.StartCoroutine((IEnumerator)Call(enemy, "ShieldFragment", single, 1));
        await Until(() => game.Player.Health < health, 3);
        Check(game.Player.Stunned, "GD03 outgoing shield has a real damage and stun contact");
        Check(single.sprite == hot, "GD03 damage/stun contact occurs on the heated tracking sprite");
        await Until(() => single.name.EndsWith("정지"), 2);
        Check(single.sprite == cooled && !single.GetComponent<TrailRenderer>().emitting,
            "GD03 cools and clears its red trail at the stop before returning");
        Vector2 returnStart = single.transform.position;
        game.Player.ClearStatus(); Set(game.Player, "invulnerableUntil", 0f);
        Place(Vector2.Lerp(returnStart, enemy.Position, 0.55f));
        health = game.Player.Health;
        await Until(() => game.Player.Health < health, 2);
        Check(single.name.EndsWith("복귀") && !game.Player.Stunned, "GD03 returning shield deals damage without player stun");
        Check(Mathf.Abs(health - game.Player.Health - game.Config.enemyDamage * game.Config.BossTuning.damageMultiplier * 0.8f) < 0.02f,
            "GD03 damage-only return preserves its configured damage");
        health = game.Player.Health; Set(game.Player, "invulnerableUntil", 0f);
        await Until(() => single == null, 2);
        Check(game.Player.Health == health, "each GD03 return leg can hit only once");
    }

    static List<Transform> Walls()
    {
        var found = new List<Transform>();
        foreach (Transform child in Get<Transform>(enemy, "effectsRoot"))
            if (child.gameObject.activeInHierarchy && child.name.StartsWith("완성 방벽")) found.Add(child);
        return found;
    }

    static List<Transform> RisingSteps()
    {
        var found = new List<Transform>();
        foreach (Transform child in Get<Transform>(enemy, "effectsRoot"))
            if (child.gameObject.activeInHierarchy && child.name.StartsWith("GD04 상승 지면")) found.Add(child);
        return found;
    }

    static async Task StartRearChain(Essence[] order, int facing, bool late = false)
    {
        Reset(order);
        Place(enemy.Position + new Vector2(facing * KarmaSpace.Units(2), -0.3f));
        Force(EnemyAttack.GD04, facing);
        await Until(() => late ? Walls().Count == game.Config.BossTuning.guard.barrierSteps : RisingSteps().Count > 0, 4);
        Check(Get<EnemyAttack>(enemy, "selected") == EnemyAttack.GD04, "GD04 does not chain while the player remains in front");
        float turnedAt = enemy.FightSeconds;
        Place(enemy.Position + new Vector2(-facing * KarmaSpace.Units(2), -0.3f));
        Set(game.Player, "invulnerableUntil", 0f);
        await Until(() => Get<EnemyAttack>(enemy, "selected") == EnemyAttack.GD02, 1);
        Check(enemy.Facing == -facing && State() == "Telegraph" && enemy.FightSeconds - turnedAt < 0.12f,
            "rear dodge immediately turns GD04 into one GD02 telegraph without recovery");
        Check(enemy.AutoShieldActive && Get<EnemyAttack>(enemy, "previous") == EnemyAttack.GD02,
            "rear chain keeps GD01 and records GD02 for the following selection");
        await Until(() => enemy.HeavyParryWindowOpen, 2);
        Check(RisingSteps().Count > 0 && enemy.AutoShieldActive, "GD04 terrain remains present throughout GD02's cue");
        bool onOriginalSide = true;
        foreach (var step in RisingSteps()) onOriginalSide &= (step.position.x - enemy.Position.x) * facing > 0;
        Check(onOriginalSide, "turning into GD02 never mirrors the existing terrain to the player's side");
        foreach (var attack in Attacks())
            Check(!Get<KarmaAttackContact>(attack, "contact").BreaksShieldOnParry,
                "still-rising GD04 contacts cannot consume the shield-breaking parry");
    }

    static async Task CheckBarrierCounterChain()
    {
        var orders = new[] {
            new[] { Essence.Guard, Essence.RisingSlash },
            new[] { Essence.Guard, Essence.SwordWave, Essence.RisingSlash },
            new[] { Essence.Guard, Essence.RisingSlash, Essence.SwordWave }
        };
        foreach (var order in orders)
        foreach (int facing in new[] { -1, 1 })
        {
            await StartRearChain(order, facing);
            float clock = enemy.FightSeconds;
            Vector2 cue = Get<Transform>(enemy, "effectsRoot").Find("GD02 parry cue").position;
            game.TogglePause(); await Task.Delay(150);
            Check(enemy.FightSeconds == clock
                && Vector2.Distance(Get<Transform>(enemy, "effectsRoot").Find("GD02 parry cue").position, cue) < 0.001f,
                "pause freezes the chained parry cue and terrain sequence");
            game.TogglePause();
            float health = game.Player.Health;
            int parries = game.Player.ParryCount;
            game.Player.Essences.TryCounter();
            await Until(() => enemy.Stunned, 2);
            Check(game.Player.ParryCount == parries + 1 && game.Player.Health == health && !enemy.AutoShieldActive,
                "rear chain accepts K, prevents damage and breaks GD01 in each inherited form and direction");
            Check(Walls().Count == 0 && RisingSteps().Count == 0 && Attacks().Length == 0 && !enemy.HeavyParryWindowOpen,
                "chained parry stops every unfinished rise and removes all terrain and attacks");
            await Until(() => !enemy.Stunned, 3);
            Check(!Get<bool>(enemy, "running") && !enemy.AutoShieldActive,
                "chained parry recovers normally without rearming GD01 or leaving the sequence stuck");
        }

        // The completed terrain also remains available while a late rear dodge starts GD02.
        await StartRearChain(orders[1], 1, late: true);
        await Task.Delay(25);

        game.Player.Essences.TryCounter();
        await Until(() => enemy.Stunned, 2);
        Check(!enemy.AutoShieldActive, "late rear dodge during terrain hold still grants a shield-breaking parry");

        // A failed parry must finish every step before recovery removes their shared parent objects.
        await StartRearChain(orders[0], -1);
        float before = game.Player.Health;
        await Until(() => game.Player.Stunned, 2);
        Check(Mathf.Abs(before - game.Player.Health - game.Config.enemyDamage * game.Config.BossTuning.damageMultiplier * 1.5f) < 0.02f
            && enemy.AutoShieldActive, "missing the chained parry deals one heavy hit and preserves GD01");
        int mostSteps = 0;
        await Until(() =>
        {
            mostSteps = Mathf.Max(mostSteps, RisingSteps().Count);
            return !Get<bool>(enemy, "running");
        }, 5);
        Set(enemy, "timer", float.MaxValue);
        Check(mostSteps == game.Config.BossTuning.guard.barrierSteps && Walls().Count == 0 && Attacks().Length == 0,
            "missed rear counter builds all six steps and clears them without a coroutine error");
        await Seconds(0.7f);
        Check(!Get<bool>(enemy, "running") && !enemy.HeavyParryWindowOpen && enemy.AutoShieldActive,
            "one GD04 cannot repeatedly chain GD02 while the player stays behind");
        Call(enemy, "BeginAttack");
        Check(Get<bool>(enemy, "running") && Get<EnemyAttack>(enemy, "selected") == EnemyAttack.GroundStrike,
            "normal attack selection resumes after a missed chain");

        await StartRearChain(orders[1], 1);
        typeof(KarmaEnemy).GetProperty("Health").SetValue(enemy, enemy.MaxHealth * 0.4f);
        Place(new Vector2(enemy.Position.x - KarmaSpace.Units(2), KarmaSpace.Units(9)));
        KarmaBossAttack followUp = null;
        await Until(() =>
        {
            foreach (var attack in Attacks())
                if (Get<Vector2>(attack, "velocity").sqrMagnitude > 0.01f) { followUp = attack; return true; }
            return false;
        }, 2);
        Check(Get<Vector2>(followUp, "velocity").x < 0 && RisingSteps().Count > 0,
            "enraged inherited-wave rear chain fires away from the original terrain while keeping its steps");
        Place((Vector2)followUp.transform.position + Vector2.left * KarmaSpace.Units(0.2f));
        Set(game.Player, "invulnerableUntil", 0f);
        int counters = game.Player.ParryCount;
        game.Player.Essences.TryCounter();
        await Until(() => game.Player.ParryCount > counters, 1);
        Check(enemy.AutoShieldActive && !enemy.Stunned && !Get<bool>(enemy, "pendingParry"),
            "countering a chained shockwave still cannot break GD01 or stun the boss");
        await Until(() => !Get<bool>(enemy, "running"), 4);
        Check(Walls().Count == 0 && RisingSteps().Count == 0,
            "chained shockwave returns to normal recovery and clears the GD04 terrain");

        foreach (string scenario in new[] { "no shield", "front", "far rear", "already rear" })
        {
            Reset(orders[0]);
            if (scenario == "no shield") Call(enemy, "BreakAutoShield");
            Place(enemy.Position + new Vector2((scenario == "already rear" ? -1 : 1) * KarmaSpace.Units(2), -0.3f));
            Force(EnemyAttack.GD04, 1);
            await Until(() => RisingSteps().Count > 0, 3);
            if (scenario != "front" && scenario != "already rear")
                Place(enemy.Position + new Vector2(-KarmaSpace.Units(scenario == "far rear" ? 7 : 2), -0.3f));
            await Until(() => !Get<bool>(enemy, "running"), 5);
            Check(Get<EnemyAttack>(enemy, "selected") == EnemyAttack.GD04 && Walls().Count == 0,
                "GD04 does not force a rear chain for " + scenario);
        }

        // A boundary-induced direction change must use the actual terrain side.
        Reset(orders[0]);
        enemy.transform.position = new Vector3(KarmaStage.ArenaLeft + 6, KarmaSpace.GuardianGroundY, 0);
        Place(enemy.Position + new Vector2(KarmaSpace.Units(2), -0.3f));
        Force(EnemyAttack.GD04, -1);
        await Until(() => RisingSteps().Count > 0, 3);
        Check(enemy.Facing == 1, "edge case fixes terrain direction before evaluating the rear dodge");
        Place(enemy.Position - new Vector2(KarmaSpace.Units(2), 0.3f));
        Set(game.Player, "invulnerableUntil", 0f);
        await Until(() => enemy.HeavyParryWindowOpen, 2);
        Check(enemy.Facing == -1 && RisingSteps().Count > 0, "edge chain aims away from the actual rising ground");
        game.Player.Essences.TryCounter();
        await Until(() => enemy.Stunned, 2);

        await StartRearChain(orders[0], 1);
        game.Player.Essences.TryCounter();
        Call(enemy, "InterruptPattern");
        await Seconds(1);
        Check(Walls().Count == 0 && RisingSteps().Count == 0 && Attacks().Length == 0
            && !enemy.HeavyParryWindowOpen && !Get<bool>(enemy, "heavyParryQueued") && enemy.AutoShieldActive,
            "interrupt cancels the rear chain, pending K and delayed terrain creation while preserving GD01");

        await StartRearChain(orders[0], -1);
        game.PlayerDied();
        await Task.Delay(80);
        Check(!Get<bool>(enemy, "running") && Walls().Count == 0 && RisingSteps().Count == 0 && !enemy.AutoShieldActive,
            "player death during a rear chain releases the entire attack sequence");

        await StartRearChain(orders[2], 1);
        var effects = Get<Transform>(enemy, "effectsRoot");
        enemy.TakeDamage(enemy.MaxHealth * 4);
        await Task.Delay(80);
        Check(effects == null && game.Enemies.Count == 0,
            "boss death during the rear chain clears unfinished terrain and allows room progression");
    }

    static void CheckStairGeometry(List<Transform> walls)
    {
        Physics2D.SyncTransforms();
        float lastHeight = 0, lastX = 0, min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < walls.Count; i++)
        {
            var b = walls[i].GetComponent<BoxCollider2D>().bounds;
            Check(Mathf.Abs(b.min.y) < 0.02f && b.size.y > lastHeight && (i == 0 || (b.center.x - lastX) * enemy.Facing > 0),
                "GD04 ground-rooted steps rise in height along one direction");
            Check(b.min.x >= KarmaStage.ArenaLeft && b.max.x <= KarmaStage.ArenaRight, "GD04 step stays inside arena");
            lastHeight = b.size.y; lastX = b.center.x; min = Mathf.Min(min, b.min.x); max = Mathf.Max(max, b.max.x);
        }
        Check(walls.Count == 6 && max - min >= KarmaSpace.Units(8), "GD04 fills a continuous corridor with six substantial steps");
    }

    static async Task CheckPersistentShield()
    {
        var orders = new[] {
            new[] { Essence.Guard }, new[] { Essence.Guard, Essence.SwordWave }, new[] { Essence.Guard, Essence.RisingSlash },
            new[] { Essence.Guard, Essence.SwordWave, Essence.RisingSlash }, new[] { Essence.Guard, Essence.RisingSlash, Essence.SwordWave }
        };
        foreach (var order in orders)
        {
            Reset(order);
            Check(enemy.AutoShieldActive && Get<SpriteRenderer>(enemy, "shieldHalo").enabled,
                "GD01 starts active and visible in " + KarmaPatternCatalog.FusionSummary(order));
            Place(new Vector2(5, KarmaSpace.Units(1)));
            Force(EnemyAttack.GD02);
            await Until(() => !Get<bool>(enemy, "running"), 3);
            Check(enemy.AutoShieldActive, "dodging GD02 preserves GD01 through recovery and pattern completion");
        }
        foreach (var primary in new[] { Essence.SwordWave, Essence.RisingSlash })
        {
            Reset(new[] { primary });
            Check(!enemy.AutoShieldActive, "GD01 is not granted to " + primary + " primary bosses");
        }
        Reset();
        Call(enemy, "InterruptPattern");
        Check(enemy.AutoShieldActive, "generic interruption cannot break GD01");
        Set(enemy, "running", true);
        await Seconds(6);
        Check(enemy.AutoShieldActive, "GD01 has no five-second expiration or expiration countdown");
        float health = enemy.Health;
        enemy.TakeDamage(100, enemy.Position + Vector2.right);
        Check(Mathf.Abs(health - enemy.Health - 100 * game.Config.BossTuning.guard.shieldDamageMultiplier) < 0.01f,
            "persistent GD01 still applies damage reduction");
        Force(EnemyAttack.GD04);
        Check(enemy.AutoShieldActive, "GD04 entry cannot break GD01");
        await Until(() => !Get<bool>(enemy, "running"), 6);
        Check(enemy.AutoShieldActive, "GD04 recovery cannot break GD01");
        Reset();
        Set(enemy, "nextUltimate", 0f);
        var selected = new HashSet<EnemyAttack>();
        for (int i = 0; i < 10; i++)
        {
            Call(enemy, "BeginAttack"); selected.Add(Get<EnemyAttack>(enemy, "selected"));
            Call(enemy, "InterruptPattern");
        }
        Check(selected.Contains(EnemyAttack.GD02) && selected.Contains(EnemyAttack.GD03)
            && selected.Contains(EnemyAttack.GD04) && selected.Contains(EnemyAttack.GD05),
            "persistent GD01 does not force GD02 forever or starve inherited attacks");
        game.PlayerDied();
        Check(!enemy.AutoShieldActive && !Get<SpriteRenderer>(enemy, "shieldHalo").enabled,
            "ending combat cleans up GD01 state and visual");
    }

    static void Reset(Essence[] order = null)
    {
        game.StartRun();
        order = order ?? new[] { Essence.Guard, Essence.SwordWave, Essence.RisingSlash };
        foreach (var essence in order) { game.Player.Essences.Preview(essence); game.Player.Essences.Confirm(out _); }
        typeof(KarmaGame).GetProperty("StageIndex").SetValue(game, order.Length);
        Call(game, "LoadRoom", true);
        enemy = game.Enemies[0];
        enemy.transform.position = new Vector3(20, KarmaSpace.GuardianGroundY, 0);
        game.Player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        Place(new Vector2(13, KarmaSpace.Units(1)));
        Set(game.Player, "invulnerableUntil", Time.time + 100);
        Set(enemy, "engaged", true); Set(enemy, "timer", float.MaxValue); Set(enemy, "nextShield", float.MaxValue);
    }
    static void Place(Vector2 p) { game.Player.transform.position = p; game.Player.GetComponent<Rigidbody2D>().position = p; }
    static void Force(EnemyAttack pattern, int facing = -1)
    {
        Call(enemy, "InterruptPattern");
        Set(enemy, "selected", pattern); Set(enemy, "direction", (float)facing); Set(enemy, "running", true);
        Set(enemy, "patternStarted", enemy.FightSeconds);
        if (pattern == EnemyAttack.GD02) Call(enemy, "RefreshAutoShield");
        enemy.StartCoroutine((IEnumerator)Call(enemy, "RunPattern"));
    }
    static string State() { return Get<object>(enemy, "state").ToString(); }
    static KarmaBossAttack[] Attacks() { return Get<Transform>(enemy, "effectsRoot").GetComponentsInChildren<KarmaBossAttack>(); }
    static Transform Wall()
    {
        foreach (Transform child in Get<Transform>(enemy, "effectsRoot"))
            if (child.gameObject.activeInHierarchy && child.name.StartsWith("완성 방벽")) return child;
        return null;
    }
    static Task Seconds(float seconds)
    { float target = enemy.FightSeconds + seconds; return Until(() => enemy.FightSeconds >= target, seconds + 3); }
    static async Task Until(Func<bool> condition, float timeout)
    {
        double until = UnityEditor.EditorApplication.timeSinceStartup + timeout;
        while (!condition())
        {
            if (UnityEditor.EditorApplication.timeSinceStartup > until) throw new Exception("Guard check timed out: pattern=" + Get<EnemyAttack>(enemy, "selected") + ", state=" + State());
            await Task.Delay(10);
        }
    }
}
#endif
