using System.Collections.Generic;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy : MonoBehaviour
    {
        BossBehavior behavior;
        enum ActionState { Approach, Telegraph, Attack, Recover, Stunned }
        KarmaGame game;
        Transform art, effectsRoot;
        KarmaCharacterAnimator animator;
        SpriteRenderer shieldHalo;
        List<EnemyAttack> attacks;
        ActionState state;
        internal int CurrentPattern { get { return (int)selected; } }
        EnemyAttack selected, previous = (EnemyAttack)(-1);
        float timer, direction, patternStarted, nextUltimate, stunnedUntil;
        float shieldUntil, nextShield, shieldWarningUntil;
        bool engaged, running, stopped, fullShield, frontalGuard, swordGuard, pendingParry;
        bool announcedEnrage;
        readonly KarmaTrialState trial = new KarmaTrialState();
        KarmaBossTuning Tuning { get { return game.Config.BossTuning; } }
        float Damage { get { return game.Config.enemyDamage * Tuning.damageMultiplier * behavior.Balance.damageScale; } }
        public Essence? PrimaryType { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool Alive { get { return Health > 0; } }
        public bool Enraged { get { return Alive && Health <= MaxHealth * Tuning.enrageThreshold; } }
        float Tempo { get { return Enraged ? Mathf.Clamp(Tuning.enrageTempo, 0.65f, 1) : 1; } }
        float Warning(float seconds) { return Mathf.Max(0.28f, seconds * Mathf.Clamp(Tuning.warningMultiplier, 0.65f, 1.5f) * Tempo); }
        public int Facing { get { return direction < 0 ? -1 : 1; } }
        public Vector2 Position { get { return transform.position; } }
        public string DisplayName { get; private set; }
        public float FightSeconds { get; private set; }
        public bool Stunned { get { return state == ActionState.Stunned; } }
        public bool Invulnerable { get { return fullShield; } }
        public bool Armored { get { return fullShield || swordGuard || frontalGuard || FightSeconds < shieldUntil; } }
        public bool AutoShieldActive { get { return FightSeconds < shieldUntil; } }
        internal static float TrialRadius { get { return GD05Pattern.TrialRadius; } }
        internal static float TrialCenter { get { return GD05Pattern.TrialCenter; } }
        public bool TrialActive { get { return trial.Active; } }
        public bool HeavyParryWindowOpen { get { return behavior is GuardBossBehavior guard && guard.Heavy.ParryWindowOpen; } }
        public bool TryQueueHeavyParry() { return behavior is GuardBossBehavior guard && guard.Heavy.TryQueueParry(); }
        public void Initialize(KarmaGame director, int stage)
        {
            game = director;
            MaxHealth = Tuning.HealthFor(stage); Health = MaxHealth;
            PrimaryType = KarmaPatternCatalog.Primary(game.Player.Essences.PersonalizationOrder);
            DisplayName = (stage == 3 ? "마지막 수호자" : "수호자 " + (stage + 1)) + " · " + KarmaPatternCatalog.TypeName(game.Player.Essences.PersonalizationOrder);
            art = KarmaArtwork.Guardian(transform, game.Player.Essences.PersonalizationOrder, stage == 3);
            art.localScale = Vector3.one * KarmaSpace.GuardianArtScale; animator = art.GetComponent<KarmaCharacterAnimator>();
            behavior = CreateBehavior();
            attacks = KarmaPatternCatalog.BuildCycle(game.Player.Essences.PersonalizationOrder);
            effectsRoot = new GameObject("Guardian pattern objects").transform; effectsRoot.SetParent(game.World, false);
            shieldHalo = KarmaRemnantVFX.CreateGuardHalo(transform, KarmaArtwork.BarrierColor);
            shieldHalo.transform.localScale = new Vector3(KarmaSpace.Units(2.2f) * KarmaSpace.GuardianSizeBoost, KarmaSpace.Units(3.1f) * KarmaSpace.GuardianSizeBoost, 1); shieldHalo.enabled = false;
            if (PrimaryType == Essence.Guard && KarmaCombatVFX.ApplySprite(shieldHalo, "GuardBossVFX", "Shield",
                KarmaSpace.Units(new Vector2(4.4f, 3.8f)) * KarmaSpace.GuardianSizeBoost))
                shieldHalo.transform.localPosition = Vector2.up * KarmaSpace.Units(1.2f);
            timer = 1.4f; nextShield = Tuning.guard.autoShieldPeriod; nextUltimate = Tuning.ultimateCooldown;
            state = ActionState.Approach;
            if (behavior is GuardBossBehavior guard) guard.Initialize();
        }
        public void PrepareRevival(Vector3 position, Vector3 artworkScale)
        {
            transform.position = position;
            art.localScale = artworkScale;
            engaged = true;
        }
        bool Active { get { return !stopped && Alive && game != null && game.Phase == RunPhase.Combat; } }
    }
}
