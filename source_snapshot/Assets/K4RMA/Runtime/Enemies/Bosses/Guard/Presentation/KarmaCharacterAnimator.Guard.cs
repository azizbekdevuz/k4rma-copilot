using System.Collections.Generic;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaCharacterAnimator
    {
        KarmaHeavyStrikeMotion heavyStrike;
        Sprite guardStagger;
        Vector3 staggerScale;
        public Vector2 HeavyHandPosition { get { return heavyStrike.HandPosition; } }
        public void ConfigureHeavyStrike(IReadOnlyList<Essence> order)
        {
            heavyStrike = new KarmaHeavyStrikeMotion(picture, pictureHome, pictureScale, order);
            Sprite reference = null;
            foreach (var sprite in KarmaArtwork.Settings.spriteLibrary.All("GuardStagger" + heavyStrike.FormKey))
            {
                if (sprite.name == "Stagger") guardStagger = sprite;
                else if (sprite.name == "Reference") reference = sprite;
            }
            // Calibrate with a standing figure drawn at the same anatomical scale.
            // Using crouched height here would inflate the boss during every parry.
            if (guardStagger == null || reference == null)
                throw new System.InvalidOperationException("경직 스프라이트가 없습니다: " + heavyStrike.FormKey);
            staggerScale = pictureScale * (picture.sprite.bounds.size.y / reference.bounds.size.y);
        }
        public void PrepareHeavyStrike() { heavyStrike.Prepare(); }
        public void SwingHeavyStrike() { heavyStrike.Swing(); }
        public void EndHeavyStrike() { if (heavyStrike != null) heavyStrike.Stop(); }
    }
}
