using UnityEngine;

namespace KarmaPrototype
{
    // Spatial compensation for the full-arena view (previous camera half-height: 6.8).
    // Scale lengths, speeds and accelerations together; never scale time or damage.
    public static class KarmaSpace
    {
        public const float Scale = 2;
        public const float GuardianArtScale = 1.7f;
        public const float GuardianSizeBoost = GuardianArtScale / 1.4f;
        public static float Units(float value) { return value * Scale; }
        public static Vector2 Units(Vector2 value) { return value * Scale; }
        public static readonly Vector2 PlayerSize = Units(new Vector2(0.7f, 1.55f));
        public const float PlayerHalfWidth = 0.35f * Scale;
        public const float PlayerHalfHeight = 0.775f * Scale;
        public const float GuardianGroundY = 1.15f * Scale;
    }
}
