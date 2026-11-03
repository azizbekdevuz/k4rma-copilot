using System.Collections.Generic;
using UnityEngine;

namespace KarmaPrototype
{
    // Composition root: create this one component in an empty scene.
    public sealed partial class KarmaGame : MonoBehaviour
    {
        [SerializeField] KarmaConfig config;
        public KarmaConfig Config { get { return config; } }
        public RunPhase Phase { get; private set; } = RunPhase.Title;
        public KarmaPlayer Player { get; private set; }
        [HideInInspector] public List<KarmaEnemy> Enemies = new List<KarmaEnemy>();
        [HideInInspector] public List<KarmaProjectile> Projectiles = new List<KarmaProjectile>();
        public Transform World { get; private set; }
        public int StageIndex { get; private set; }
        public bool Paused { get; private set; }
        public bool IsCombat { get { return Phase == RunPhase.Combat && !Paused; } }
        public bool CanMove { get { return !Paused && (Phase == RunPhase.Combat || Phase == RunPhase.Gate); } }
        public float Elapsed { get; private set; }
        public float DamageTaken { get; set; }
        public int Retries { get; private set; }
        public readonly float[] BossSplits = new float[4];
        public Essence? PendingChoice { get { return Player == null ? (Essence?)null : Player.Essences.Pending; } }
        public Essence LastPersonalizedEssence { get; private set; }
        public Camera GameCamera { get; private set; }
        Transform runRoot;
        float transitionLeft;
        bool ownsConfig;
        void OnEnable()
        {
            if (World == null) return;
            KarmaStage.RefreshBoundaries(World);

        }
        void Awake()
        {
            if (config == null) { config = ScriptableObject.CreateInstance<KarmaConfig>(); ownsConfig = true; }
            KarmaVisuals.UseMaterial(config.spriteMaterial);
            GameCamera = Camera.main;
            if (GameCamera == null)
            {
                var cameraObject = new GameObject("K4RMA Camera");
                cameraObject.transform.SetParent(transform);
                GameCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            GameCamera.orthographic = true; GameCamera.orthographicSize = 6.8f;
            GameCamera.clearFlags = CameraClearFlags.SolidColor;
            GameCamera.backgroundColor = new Color(0.065f, 0.075f, 0.12f);
            GameCamera.transform.rotation = Quaternion.identity;
            GameCamera.transform.position = new Vector3(12, 4.4f, -10);
            gameObject.AddComponent<KarmaHUD>().Initialize(this);
            Time.timeScale = 1;
        }
        void Update()
        {
            UpdateHitFeedback();
            if (Phase == RunPhase.Title)
            {
                if (KarmaInput.Pressed(KarmaAction.Confirm)) StartRun();
                else if (KarmaInput.Pressed(KarmaAction.Story)) ShowPage(RunPhase.Story);
                else if (KarmaInput.Pressed(KarmaAction.Controls)) ShowPage(RunPhase.Controls);
                return;
            }
            if (Phase == RunPhase.Story || Phase == RunPhase.Controls)
            {
                if (KarmaInput.Pressed(KarmaAction.Pause)) ReturnToTitle();
                return;
            }
            if (KarmaInput.Pressed(KarmaAction.Pause))
            {
                if (Phase == RunPhase.Choosing && !Paused) CancelChoice(); else TogglePause();
            }
            if (KarmaInput.Pressed(KarmaAction.Title) && (Paused || Phase == RunPhase.Victory || Phase == RunPhase.Defeat)) { ReturnToTitle(); return; }
            if (KarmaInput.Pressed(KarmaAction.Restart) && (Paused || Phase == RunPhase.Victory || Phase == RunPhase.Defeat)) { StartRun(); return; }
            if (Paused) return;
            if (Phase == RunPhase.PlayerFallen)
            {
                UpdatePlayerFall();
                return;
            }
            if (Phase == RunPhase.Combat || Phase == RunPhase.Gate || Phase == RunPhase.Choosing || Phase == RunPhase.Transition || Phase == RunPhase.GuardianFallen)
                Elapsed += Time.deltaTime;
            if (Phase == RunPhase.GuardianFallen)
            {
                UpdateGuardianFall();
                return;
            }
            if (Phase == RunPhase.Gate && Player.Position.x > 35.5f && KarmaInput.Pressed(KarmaAction.Interact))
                Phase = RunPhase.Choosing;
            if (Phase == RunPhase.Choosing)
            {
                if (KarmaInput.Pressed(KarmaAction.ChooseSwordWave)) Choose(Essence.SwordWave);
                else if (KarmaInput.Pressed(KarmaAction.ChooseRisingSlash)) Choose(Essence.RisingSlash);
                else if (KarmaInput.Pressed(KarmaAction.ChooseGuard)) Choose(Essence.Guard);
                else if (KarmaInput.Pressed(KarmaAction.Confirm)) ConfirmChoice();
            }
            if (Phase == RunPhase.Transition)
                UpdatePersonalization();
            if (Phase == RunPhase.Defeat && KarmaInput.Pressed(KarmaAction.Restart)) RetryRoom();
        }

        void OnDisable() { Time.timeScale = 1; }
        void OnDestroy() { if (ownsConfig && config != null) Destroy(config); }
    }
}
