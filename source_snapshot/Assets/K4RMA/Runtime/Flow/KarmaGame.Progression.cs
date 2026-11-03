using System.Collections.Generic;
using UnityEngine;

namespace KarmaPrototype
{
    public sealed partial class KarmaGame
    {
        const float GuardianFallDuration = 1.8f, PersonalizationDuration = 2.4f, InheritanceDuration = 3.2f;
        const float PlayerFallDuration = 1.8f;
        bool playerFallSettled;
        bool guardianInheriting;
        Transform fallenGuardian;
        Vector3 fallenGuardianPosition;
        Vector3 fallenGuardianScale;
        bool ceremonySpacingStarted, ceremonySpacingFinished;
        public bool GuardianInheriting => Phase == RunPhase.Transition && guardianInheriting;
        public void StartRun()
        {
            guardianInheriting = false;
            fallenGuardian = null;
            ResetHitFeedback();
            Paused = false; Time.timeScale = 1;
            if (runRoot != null) { runRoot.gameObject.SetActive(false); Destroy(runRoot.gameObject); }
            Enemies.Clear(); Projectiles.Clear();
            runRoot = new GameObject("Generated K4RMA Run").transform;
            runRoot.SetParent(transform, false);
            var p = new GameObject("Player"); p.transform.SetParent(runRoot, false);
            Player = p.AddComponent<KarmaPlayer>(); Player.Initialize(this);
            Player.Essences.Cancel(); LastPersonalizedEssence = Essence.SwordWave; StageIndex = 0; Elapsed = 0; DamageTaken = 0; Retries = 0;
            for (int i = 0; i < BossSplits.Length; i++) BossSplits[i] = 0;
            LoadRoom(true);
        }
        void LoadRoom(bool fullHealth)
        {
            ResetHitFeedback();
            if (World != null) { World.gameObject.SetActive(false); Destroy(World.gameObject); }
            Enemies.Clear(); Projectiles.Clear();
            World = new GameObject("Stage " + (StageIndex + 1)).transform;
            World.SetParent(runRoot, false);
            KarmaStage.Build(this, World, StageIndex);
            Player.ResetForRoom(KarmaStage.Spawn, fullHealth);
            var enemyObject = new GameObject(StageIndex == 3 ? "마지막 수호자" : "수호자");
            enemyObject.transform.SetParent(World, false);
            enemyObject.transform.position = new Vector3(27, KarmaSpace.GuardianGroundY, 0);
            var enemy = enemyObject.AddComponent<KarmaEnemy>(); enemy.Initialize(this, StageIndex);
            Enemies.Add(enemy);
            Phase = RunPhase.Combat;
        }
        public void EnemyDefeated(KarmaEnemy enemy, Transform fallenArt = null)
        {
            BossSplits[StageIndex] = enemy.FightSeconds;
            Enemies.Remove(enemy);
            if (Enemies.Count != 0) return;
            fallenGuardian = fallenArt;
            fallenGuardianPosition = enemy.transform.position;
            fallenGuardianScale = fallenArt != null ? fallenArt.localScale : Vector3.one * KarmaSpace.GuardianArtScale;
            guardianInheriting = false;
            Phase = RunPhase.GuardianFallen;
            ceremonySpacingStarted = ceremonySpacingFinished = false;
            transitionLeft = GuardianFallDuration;
            Player.Essences.Cancel();
            Player.ClearStatus();
            ClearProjectiles();
        }
        void UpdateGuardianFall()
        {
            transitionLeft -= Time.deltaTime;
            if (!Player.ReadyForPersonalization) return;
            if (!ceremonySpacingStarted)
            {
                ceremonySpacingStarted = true;
                var picture = fallenGuardian != null ? fallenGuardian.GetComponentInChildren<SpriteRenderer>() : null;
                var bounds = picture != null ? picture.bounds : new Bounds(fallenGuardianPosition, Vector3.one * 4);
                Player.BeginCeremonySpacing(bounds, fallenGuardianPosition.x);
            }
            if (Player.SeparatingForCeremony) return;
            if (!ceremonySpacingFinished)
            {
                ceremonySpacingFinished = true;
                transitionLeft = Mathf.Max(transitionLeft, 0.3f);
            }
            if (transitionLeft <= 0)
                Phase = StageIndex == 3 ? RunPhase.Victory : RunPhase.Choosing;
        }
        public void Choose(Essence essence)
        {
            if (Phase != RunPhase.Choosing || Paused || !Player.Essences.HasActive(essence)) return;
            Player.Essences.Preview(essence);
        }
        public void CancelChoice() { if (Phase == RunPhase.Choosing) Player.Essences.Cancel(); }
        public void ConfirmChoice()
        {
            if (Phase != RunPhase.Choosing || Paused || !PendingChoice.HasValue) return;
            Essence essence;
            if (!Player.Essences.Confirm(out essence)) return;
            Player.Essences.Cancel();
            LastPersonalizedEssence = essence;
            Player.Heal(config.healAfterPersonalization);
            guardianInheriting = false;
            Phase = RunPhase.Transition; transitionLeft = PersonalizationDuration;
            KarmaPersonalizationVFX.Play(this, Player.transform, essence, PersonalizationDuration, false);
        }
        void UpdatePersonalization()
        {
            transitionLeft -= Time.deltaTime;
            if (transitionLeft > 0) return;
            if (!guardianInheriting)
            {
                Vector2 playerPosition = Player.Position;
                Vector3 cameraPosition = GameCamera.transform.position;
                float cameraSize = GameCamera.orthographicSize;
                var fallenPicture = fallenGuardian != null ? fallenGuardian.GetComponentInChildren<SpriteRenderer>() : null;
                Sprite fallenAppearance = fallenPicture != null ? fallenPicture.sprite : null;
                StageIndex++;
                LoadRoom(false);
                // Preserve the defeated guardian's place so inheritance reads as a revival.
                Player.ResetForRoom(playerPosition, false);
                Player.FaceGuardian(fallenGuardianPosition.x);
                Enemies[0].PrepareRevival(fallenGuardianPosition, fallenGuardianScale);
                fallenGuardian = null;
                GameCamera.transform.position = cameraPosition;
                GameCamera.orthographicSize = cameraSize;
                // The new guardian exists, but its AI stays idle until the inheritance finishes.
                Phase = RunPhase.Transition;
                guardianInheriting = true;
                transitionLeft = InheritanceDuration;
                var previous = new List<Essence>(Player.Essences.PersonalizationOrder);
                previous.RemoveAt(previous.Count - 1);
                KarmaPersonalizationVFX.Play(this, Enemies[0].transform, LastPersonalizedEssence,
                    InheritanceDuration, true, fallenAppearance != null ? fallenAppearance : KarmaArtwork.GuardianPreview(previous),
                    KarmaArtwork.GuardianFormName(previous));
            }
            else
            {
                guardianInheriting = false;
                Phase = RunPhase.Combat;
            }
        }
        public void PlayerDied()
        {
            if (Player == null || Phase == RunPhase.PlayerFallen || Phase == RunPhase.Defeat || Phase == RunPhase.Victory) return;
            ResetHitFeedback();
            Phase = RunPhase.PlayerFallen;
            transitionLeft = PlayerFallDuration;
            playerFallSettled = false;
            Player.Essences.Cancel();
            foreach (var enemy in Enemies.ToArray()) if (enemy != null) enemy.CancelCombat();
            ClearProjectiles();
            Player.BeginDefeat();
        }
        void UpdatePlayerFall()
        {
            transitionLeft -= Time.deltaTime;
            if (!Player.ReadyForDefeat) return;
            if (!playerFallSettled)
            {
                playerFallSettled = true;
                // An airborne death must remain visible after landing, too.
                transitionLeft = Mathf.Max(transitionLeft, 0.8f);
            }
            if (transitionLeft <= 0) Phase = RunPhase.Defeat;
        }
        void ClearProjectiles()
        {
            foreach (var p in Projectiles.ToArray()) if (p != null) Destroy(p.gameObject);
            Projectiles.Clear();
        }
        public void RetryRoom()
        {
            if (Phase != RunPhase.Defeat) return;
            StartRun();
        }
        public void ShowPage(RunPhase page)
        {
            if (Phase == RunPhase.Title && (page == RunPhase.Story || page == RunPhase.Controls)) Phase = page;
        }
        public void ReturnToTitle()
        {
            ResetHitFeedback();
            Paused = false; Time.timeScale = 1; ClearProjectiles();
            if (runRoot != null) { runRoot.gameObject.SetActive(false); Destroy(runRoot.gameObject); }
            runRoot = null; World = null; Player = null; Enemies.Clear();
            guardianInheriting = false; fallenGuardian = null;
            Phase = RunPhase.Title;
        }
        public void TogglePause()
        {
            if (Phase != RunPhase.Combat && Phase != RunPhase.Gate && Phase != RunPhase.Choosing) return;
            Paused = !Paused; Time.timeScale = Paused ? 0 : 1;
        }
    }
}
