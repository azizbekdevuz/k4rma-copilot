using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaCharacterAnimator
    {
        public void PrepareWaveAttack(float warningSeconds, bool aimed)
        {
            PlayPose(aimed ? "Guard" : "Windup", warningSeconds);
            // This is a visual lead-in inside the existing warning, not an attack delay.
            waveSwingAt = poseUntil - Mathf.Min(0.12f, warningSeconds * 0.3f);
        }
        public void PlayWavePhase(string name, float warningSeconds = 0)
        {
            if (sheet != "Wave") return;
            wavePhase = name;
            wavePhaseSwingAt = warningSeconds > 0
                ? Time.time + warningSeconds - Mathf.Min(0.12f, warningSeconds * 0.3f)
                : float.PositiveInfinity;
        }
        public void ReleaseWavePhase()
        {
            if (sheet != "Wave") return;
            waveReleasedUntil = Time.time + 0.18f;
            WaveMotionRevision++;
        }
        public void ShowGuardianSpacing()
        {
            if (sheet != "Wave" || picture == null || deathAt >= 0 || Time.time < hurtUntil
                || !poses.ContainsKey("Move")) return;
            Sprite damageSprite = GuardianDamageSprite;
            Vector3 damageScale = GuardianDamageScale;
            ShowPose("Move");
            GuardianDamageSprite = damageSprite;
            GuardianDamageScale = damageScale;
            GuardianPhaseOverridesPose = true;
        }
        public void SetGuardianAppearance(Sprite sprite, string form = null)
        {
            if (sprite == null || picture == null) return;
            guardianAppearance = sprite;
            appearanceScale = picture.sprite.bounds.size.y / Mathf.Max(0.01f, sprite.bounds.size.y);
            if (string.IsNullOrEmpty(form)) return;
            var inherited = KarmaArtwork.InheritedPoses(form);
            if (inherited.Length != 16) return;
            foreach (var pose in inherited) poses[pose.name] = pose;
            guardianAppearance = poses["Idle"];
            appearanceScale = 1;
            revivalFrames = LoadRevivalFrames(form);
        }
        public void GuardianPose(string action, EnemyAttack attack, bool airborne, bool armored)
        {
            // Poses follow the actual combat state, including long attacks and recovery.
            if (picture == null || deathAt >= 0) return;
            if (EmpowermentPose()) return;
            picture.color = Time.time < hurtUntil ? new Color(1, 0.45f, 0.45f) : pictureColor;
            transform.localRotation = Quaternion.identity;
            if (action == "Stunned" && guardStagger != null)
            {
                picture.sprite = guardStagger;
                picture.transform.localPosition = pictureHome;
                picture.transform.localScale = staggerScale;
                picture.transform.localRotation = Quaternion.identity;
                picture.color = new Color(pictureColor.r * 0.82f, pictureColor.g * 0.82f, pictureColor.b * 0.82f, pictureColor.a);
                CurrentPose = "Stagger/" + heavyStrike.FormKey;
                guardianPreviousPosition = transform.position;
                return;
            }
            if (heavyStrike != null && heavyStrike.Show(attack))
            { CurrentPose = heavyStrike.CurrentPose; return; }
            bool basicWave = sheet == "Wave" && (attack == EnemyAttack.SWBasic || attack == EnemyAttack.SW01 || attack == EnemyAttack.SW02);
            bool waveAction = basicWave && (action == "Telegraph" || action == "Attack");
            bool phaseAction = sheet == "Wave" && (attack == EnemyAttack.SW03 || attack == EnemyAttack.SW04 || attack == EnemyAttack.SW05)
                && (action == "Telegraph" || action == "Attack");
            if (!phaseAction) { wavePhase = null; wavePhaseSwingAt = float.PositiveInfinity; waveReleasedUntil = 0; }
            if (sheet == "Wave" && !waveAction)
            {
                poseUntil = 0; waveSwingAt = float.PositiveInfinity;
            }
            string pose;
            if (Time.time < hurtUntil) pose = "Hurt";
            else if (action == "Stunned") pose = sheet == "Neutral" ? "Kneel" : sheet == "Ward" ? "Crouch" : "Hurt";
            else if (action == "Recover") pose = sheet == "Ward" ? "Recover" : sheet == "Rise" ? "Landing" : sheet == "Wave" ? "FollowThrough" : "Recovery";
            else if (waveAction)
            {
                // A second warning can begin in the launch frame. Finish showing the
                // preceding release before starting its next windup, without extra waits.
                // Hold the prepared swing until Attack(), even if Wait resumes next frame.
                pose = Time.time < actionUntil ? "Wave"
                    : (Time.time < poseUntil || Time.time >= waveSwingAt) ? (Time.time >= waveSwingAt ? "Slash" : explicitPose)
                    : action == "Telegraph" ? "Windup" : "Guard";
            }
            else if (action == "Telegraph") pose = armored && poses.ContainsKey("Guard") ? "Guard" : "Windup";
            else if (action == "Attack")
            {
                if (sheet == "Rise") pose = airborne ? "AirSlash" : attack == EnemyAttack.UP01 || attack == EnemyAttack.UP03 ? "Rising" : "Slash";
                else if (sheet == "Ward") pose = attack == EnemyAttack.GD02 ? "ShieldBash" : attack == EnemyAttack.GD03 ? "Thrust" : "Windup";
                else if (sheet == "Wave") pose = attack == EnemyAttack.SW01 || attack == EnemyAttack.SW02 ? "Wave" : airborne ? "Rising" : "Slash";
                else pose = "Slash";
            }
            else pose = armored && poses.ContainsKey("Guard") ? "Guard"
                : (transform.position - guardianPreviousPosition).sqrMagnitude > 0.00001f
                    ? sheet == "Rise" ? "Dash" : "Move" : "Idle";
            if (!poses.ContainsKey(pose)) pose = "Idle";
            // Compute the same pose-dependent damage silhouette as before this
            // integration, before selecting the additional presentation phases.
            Sprite damageSprite = poses.TryGetValue(pose, out var original) ? original : picture.sprite;
            bool damageComposed = guardianAppearance != null && (pose == "Idle" || pose == "Guard");
            if (damageComposed) damageSprite = guardianAppearance;
            Vector3 damageScale = pictureScale * (damageComposed ? appearanceScale : 1);
            bool phaseOverridesPose = false;
            if (phaseAction && Time.time >= hurtUntil && wavePhase != null)
            {
                string phasePose = Time.time < waveReleasedUntil ? "Wave"
                    : Time.time >= wavePhaseSwingAt ? "Slash" : wavePhase;
                if (poses.ContainsKey(phasePose)) { phaseOverridesPose = phasePose != pose; pose = phasePose; }
            }
            // A live sword guard can accompany ordinary spacing. Show that
            // measured movement while retaining the Guard damage pose above.
            // Locked warnings, attacks and recovery keep their existing poses.
            if (sheet == "Wave" && action == "Approach" && Time.time >= hurtUntil
                && (transform.position - guardianPreviousPosition).sqrMagnitude > 0.00001f
                && poses.ContainsKey("Move"))
            {
                phaseOverridesPose |= pose != "Move";
                pose = "Move";
            }
            ShowPose(pose);
            // Inherited idle artwork remains intact. During a Wave attack its
            // single Guard key pose must stay on the same sheet/scale as the
            // following Windup/Slash, avoiding a body swap between each shot.
            bool attackGuard = sheet == "Wave" && pose == "Guard" && (waveAction || phaseAction);
            bool composed = guardianAppearance != null && (pose == "Idle" || pose == "Guard") && !attackGuard;
            if (composed) picture.sprite = guardianAppearance;
            picture.transform.localScale = pictureScale * (composed ? appearanceScale : 1);
            if (sheet == "Wave")
            {
                GuardianDamageSprite = damageSprite; GuardianDamageScale = damageScale;
                GuardianPhaseOverridesPose = phaseOverridesPose || picture.sprite != damageSprite
                    || picture.transform.localScale != damageScale;
            }
            // Keep the selected single-pose cutout through every phase while
            // retaining logical poses, transform motion and the old hit silhouette.
            guardianPreviousPosition = transform.position;
            picture.transform.localPosition = pictureHome;
            picture.color = Time.time < hurtUntil ? new Color(1, 0.45f, 0.45f) : pictureColor;
            transform.localRotation = Quaternion.identity;
        }
    }
}
