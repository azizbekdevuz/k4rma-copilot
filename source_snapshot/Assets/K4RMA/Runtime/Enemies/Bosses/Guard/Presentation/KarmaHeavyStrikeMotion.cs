using System.Collections.Generic;
using UnityEngine;

namespace KarmaPrototype
{
    // Each inherited body has its own overhead poses, with one shared GD02 timing.
    sealed class KarmaHeavyStrikeMotion
    {
        readonly SpriteRenderer picture;
        readonly Vector3 home, scale;
        readonly Sprite[] poses = new Sprite[3];
        readonly Vector2 hand;
        bool active, swinging;
        float swingAt;
        public string FormKey { get; private set; }
        public string CurrentPose { get; private set; }

        public KarmaHeavyStrikeMotion(SpriteRenderer renderer, Vector3 position, Vector3 originalScale,
            IReadOnlyList<Essence> order)
        {
            picture = renderer; home = position;
            string key = order.Count >= 3 ? "WardFinal" : order.Count == 2
                ? order[1] == Essence.SwordWave ? "WardWave" : "WardRise" : "Ward";
            FormKey = key;
            string[] names = { "Windup", "Downstroke", "FollowThrough" };
            foreach (var sprite in KarmaArtwork.Settings.spriteLibrary.All("GuardHeavy" + key))
                for (int i = 0; i < names.Length; i++) if (sprite.name == names[i]) poses[i] = sprite;
            float bodyFraction = key == "Ward" ? 0.65f : key == "WardRise" ? 0.54f : 0.56f;
            scale = originalScale * (picture.sprite.bounds.size.y / (poses[0].bounds.size.y * bodyFraction));
            hand = key == "Ward" ? new Vector2(0.28f, 0.80f)
                : key == "WardWave" ? new Vector2(0.33f, 0.755f)
                : key == "WardRise" ? new Vector2(0.32f, 0.80f) : new Vector2(0.60f, 0.80f);
        }
        public Vector2 HandPosition
        {
            get
            {
                var sprite = poses[0];
                Vector2 local = (Vector2.Scale(hand, sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
                return picture.transform.TransformPoint(local);
            }
        }
        public void Prepare() { active = true; swinging = false; Apply(0); }
        public void Swing() { swinging = true; swingAt = Time.time; Apply(1); }
        public void Stop() { active = swinging = false; }
        public bool Show(EnemyAttack attack)
        {
            if (!active || attack != EnemyAttack.GD02) return false;
            int frame = !swinging ? 0 : Time.time - swingAt < 0.07f ? 1 : 2;
            Apply(frame);
            return true;
        }
        void Apply(int index)
        {
            picture.sprite = poses[index];
            picture.transform.localPosition = home;
            picture.transform.localScale = scale;
            picture.transform.localRotation = Quaternion.identity;
            CurrentPose = "GD02/" + poses[index].name;
        }
    }
}
