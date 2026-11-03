using UnityEngine;
using System.Collections.Generic;
namespace KarmaPrototype
{
    public sealed partial class KarmaCharacterAnimator : MonoBehaviour
    {
        SpriteRenderer picture;
        Sprite[] frames;
        Sprite[] runFrames;
        float runAt;
        Vector2 slashDirection = Vector2.right;
        Vector3 pictureHome;
        Color pictureColor;
        float actionUntil, hurtUntil, deathAt = -1;
        readonly Dictionary<string, Sprite> poses = new Dictionary<string, Sprite>();
        string sheet, explicitPose;
        float poseUntil, landedUntil;
        float waveSwingAt = float.PositiveInfinity;
        string wavePhase;
        float wavePhaseSwingAt = float.PositiveInfinity, waveReleasedUntil;
        bool wasAirborne;
        Vector3 guardianPreviousPosition;
        Sprite guardianAppearance;
        internal Sprite GuardianDamageSprite { get; private set; }
        internal Vector3 GuardianDamageScale { get; private set; }
        internal bool GuardianPhaseOverridesPose { get; private set; }
        internal int WaveMotionRevision { get; private set; }
        Vector3 pictureScale;
        float appearanceScale = 1;
        float empowermentAt, empowermentUntil;
        Essence empowermentEssence;
        Sprite beforeInheritance;
        Sprite[] revivalFrames, beforeRevivalFrames;
        public string CurrentPose { get; private set; }
        public void ConfigureSheet(string name)
        {
            sheet = name; poses.Clear();
            revivalFrames = name == "Player" ? null : LoadRevivalFrames(name);
            runFrames = null;
            guardianPreviousPosition = transform.position;
            var library = KarmaArtwork.Settings.spriteLibrary;
            if (library == null) return;
            foreach (var sprite in library.All(name)) if (sprite != null) poses[sprite.name] = sprite;
            if (name == "Player" && poses.TryGetValue("Run", out var run))
            {
                // The second movement key pose is also used for jump takeoff in
                // the original sheet. Prefer a dedicated Run2 when supplied.
                if (!poses.TryGetValue("Run2", out var next)) next = KarmaArtwork.Settings.playerMove2;
                runFrames = next != null && next != run ? new[] { run, next } : new[] { run };
            }
        }
        public void PlayPose(string name, float seconds)
        {
            explicitPose = name; poseUntil = Time.time + seconds;
            waveSwingAt = float.PositiveInfinity;
            // GuardianPose resolves Wave reservations after hurt/stun/death priority.
            // Player and other sheets retain their immediate pose behavior.
            if (sheet != "Wave") ShowPose(name);
        }
        void ShowPose(string name)
        {
            if (picture == null || !poses.TryGetValue(name, out var sprite)) return;
            picture.sprite = sprite; CurrentPose = name;
            picture.transform.localScale = pictureScale;
        }
        public void ConfigureSprites(SpriteRenderer renderer, Sprite[] poses)
        {
            picture = renderer; frames = poses; picture.sprite = frames[0];
            pictureHome = picture.transform.localPosition; pictureColor = picture.color;
            pictureScale = picture.transform.localScale;
        }
        public void Attack() { Attack(Vector2.right); }
        public void Attack(Vector2 direction) { slashDirection = direction; actionUntil = Time.time + 0.18f; }
        public void Hurt() { hurtUntil = Time.time + 0.16f; }
        public void Die() { EndHeavyStrike(); deathAt = Time.time; }
        public void Pose(float movement, bool airborne, float verticalSpeed = 0, bool crouching = false)
        {
            if (picture == null) return;
            if (deathAt >= 0)
            {
                float sinceDeath = Time.time - deathAt;
                if (sheet == "Player" && poses.ContainsKey("Death"))
                {
                    ShowPose(sinceDeath < 0.16f ? "Hurt" : sinceDeath < 0.38f ? "Crouch" : "Death");
                    picture.transform.localPosition = pictureHome;
                    picture.transform.localScale = pictureScale;
                    picture.transform.localRotation = Quaternion.identity;
                    picture.color = pictureColor;
                    transform.localRotation = Quaternion.identity;
                }
                else if (poses.ContainsKey("Death")) ShowPose("Death");
                else if (picture != null && frames.Length >= 9) picture.sprite = frames[8];
                if (sheet != "Player" && picture != null)
                {
                    if (revivalFrames != null && revivalFrames.Length == 6) picture.sprite = revivalFrames[0];
                    float fall = Mathf.SmoothStep(0, 1, (Time.time - deathAt) / 0.65f);
                    picture.transform.localScale = pictureScale;
                    picture.transform.localPosition = pictureHome;
                    picture.transform.localRotation = Quaternion.identity;
                    picture.color = Color.Lerp(pictureColor, new Color(0.55f, 0.55f, 0.6f), fall);
                }
                return;
            }
            if (EmpowermentPose()) return;
            if (picture != null)
            {
                int frame = 0;
                if (frames.Length > 1)
                {
                    if (Time.time < hurtUntil && frames.Length >= 9) frame = 7;
                    else if (Time.time < actionUntil)
                        frame = slashDirection.y < -0.25f ? 6 : slashDirection.y > 0.25f ? 5 : actionUntil - Time.time > 0.09f ? 3 : 4;
                    else if (airborne) frame = 1;
                    else if (Mathf.Abs(movement) > 0.1f) frame = (int)(Time.time * 9) % 2 == 0 ? 1 : 2;
                }
                picture.sprite = frames[frame];
                if (sheet == "Player" && poses.Count > 0)
                {
                    if (wasAirborne && !airborne) landedUntil = Time.time + 0.1f;
                    wasAirborne = airborne;
                    string pose = Time.time < hurtUntil ? "Hurt"
                        : Time.time < poseUntil ? (explicitPose == "Pierce" && poseUntil - Time.time > 0.15f ? "Dash" : explicitPose)
                        : Time.time < actionUntil ? (slashDirection.y < -0.25f ? "AirSlash"
                            : slashDirection.y > 0.25f ? "Rising"
                            : actionUntil - Time.time > 0.13f ? "Windup"
                            : actionUntil - Time.time > 0.07f ? "Slash" : "FollowThrough")
                        : actionUntil > 0 && Time.time < actionUntil + 0.08f ? "Recovery"
                        : airborne ? (verticalSpeed > 2 ? "Jump" : verticalSpeed < -2 ? "Fall" : "Apex")
                        : crouching ? "Crouch"
                        : Time.time < landedUntil ? "Land"
                        : Mathf.Abs(movement) > 0.1f ? "Run" : "Idle";
                    if (pose == "Run" && runFrames != null)
                    {
                        if (CurrentPose != "Run") runAt = Time.time;
                        ShowPose("Run");
                        picture.sprite = runFrames[(int)((Time.time - runAt) * 9) % runFrames.Length];
                    }
                    else ShowPose(pose);
                }
                float bob = airborne || crouching ? 0 : Mathf.Abs(movement) > 0.1f ? Mathf.Sin(Time.time * 18) * 0.025f : Mathf.Sin(Time.time * 2) * 0.008f;
                picture.transform.localPosition = pictureHome + Vector3.up * bob;
                picture.color = Time.time < hurtUntil ? new Color(1, 0.45f, 0.45f) : pictureColor;
                transform.localRotation = Quaternion.Euler(0, 0, Time.time < hurtUntil ? -8 : 0);
            }
        }
        void Update() { if (deathAt >= 0) Pose(0,false); }
    }
}
