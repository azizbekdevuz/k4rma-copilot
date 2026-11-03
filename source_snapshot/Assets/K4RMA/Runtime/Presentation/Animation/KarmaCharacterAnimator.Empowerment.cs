using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaCharacterAnimator
    {
        static Sprite[] LoadRevivalFrames(string form)
        {
            var inherited = KarmaArtwork.InheritedPoses(form);
            if (inherited.Length == 16)
            {
                var names = new[] { "Death", "RevivePush", "ReviveKneel", "ReviveRise", "ReviveStand", "Idle" };
                var result = new Sprite[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    string pose = names[i];
                    result[i] = System.Array.Find(inherited, s => s.name == pose);
                    if (result[i] == null) return System.Array.Empty<Sprite>();
                }
                return result;
            }
            var sprites = Resources.LoadAll<Sprite>("K4RMAArtwork/Characters/Guardians/Revival/" + form);
            System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
            return sprites;
        }
        public void Empower(Essence essence, float seconds, Sprite previousAppearance = null, bool fromNeutral = false,
            string previousForm = null)
        {
            deathAt = -1;
            empowermentAt = Time.time;
            empowermentUntil = Time.time + seconds;
            empowermentEssence = essence;
            beforeInheritance = previousAppearance;
            beforeRevivalFrames = !string.IsNullOrEmpty(previousForm) ? LoadRevivalFrames(previousForm)
                : fromNeutral ? LoadRevivalFrames("Neutral") : revivalFrames;
            EmpowermentPose();
        }
        void GuardianRevivalPose(float t)
        {
            ShowPose("Idle");
            var after = guardianAppearance != null ? guardianAppearance : picture.sprite;
            bool hasFrames = revivalFrames != null && revivalFrames.Length == 6
                && beforeRevivalFrames != null && beforeRevivalFrames.Length == 6;
            // Every new pose is authored at a shared body scale with a ground pivot.
            // Never stretch a lying sprite to the standing character's height.
            if (t < 0.12f && beforeInheritance != null)
            {
                picture.sprite = beforeInheritance;
                picture.transform.localScale = pictureScale;
                CurrentPose = "ReviveFallen";
            }
            else if (hasFrames && t < 0.9f)
            {
                int frame = t < 0.24f ? 1 : t < 0.36f ? 2 : t < 0.62f ? 3 : t < 0.76f ? 4 : 5;
                picture.sprite = t < 0.48f ? beforeRevivalFrames[frame] : revivalFrames[frame];
                picture.transform.localScale = pictureScale;
                CurrentPose = "Revive" + frame.ToString("00");
            }
            else
            {
                picture.sprite = after;
                picture.transform.localScale = pictureScale * (guardianAppearance != null ? appearanceScale : 1);
                CurrentPose = "ReviveReady";
            }
        }
        bool EmpowermentPose()
        {
            if (picture == null || Time.time >= empowermentUntil) return false;
            float t = Mathf.InverseLerp(empowermentAt, empowermentUntil, Time.time);
            if (sheet == "Player")
            {
                string pose = t < 0.42f ? "Guard" : t < 0.76f
                    ? empowermentEssence == Essence.Guard ? "Parry" : empowermentEssence == Essence.RisingSlash ? "Rising" : "FollowThrough"
                    : "Idle";
                ShowPose(pose);
            }
            else
            {
                GuardianRevivalPose(t);
            }
            Color color = empowermentEssence == Essence.Guard
                ? new Color(1, 0.9f, 0.4f) : empowermentEssence == Essence.RisingSlash
                ? new Color(0.4f, 1, 1) : new Color(1, 0.65f, 0.35f);
            picture.color = Color.Lerp(pictureColor, color, Mathf.Sin(t * Mathf.PI) * 0.55f);
            float lift = sheet == "Player" && empowermentEssence == Essence.RisingSlash ? Mathf.Sin(t * Mathf.PI) * 0.35f : 0;
            if (sheet != "Player")
            {
                picture.transform.localRotation = Quaternion.identity;
                picture.color = Color.Lerp(new Color(0.55f, 0.55f, 0.6f), picture.color,
                    Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.38f)));
                float flash = Mathf.Clamp01(1 - Mathf.Abs(t - 0.5f) / 0.07f);
                picture.color = Color.Lerp(picture.color, Color.white, flash);
            }
            picture.transform.localPosition = pictureHome + Vector3.up * lift;
            transform.localRotation = Quaternion.identity;
            return true;
        }
    }
}
