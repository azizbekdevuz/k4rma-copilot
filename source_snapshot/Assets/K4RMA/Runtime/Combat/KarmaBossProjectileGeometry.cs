using UnityEngine;
namespace KarmaPrototype
{
    internal static class KarmaBossProjectileGeometry
    {
        internal static float SwordShotLife(Vector2 origin, Vector2 velocity, float life)
        {
            life = Mathf.Max(0, life);
            if (origin.x < KarmaStage.ArenaLeft || origin.x > KarmaStage.ArenaRight || origin.y < 0 || origin.y > KarmaSpace.Units(12)) return 0;
            if (velocity.x > 0) life = Mathf.Min(life, (KarmaStage.ArenaRight - origin.x) / velocity.x);
            else if (velocity.x < 0) life = Mathf.Min(life, (KarmaStage.ArenaLeft - origin.x) / velocity.x);
            if (velocity.y > 0) life = Mathf.Min(life, (KarmaSpace.Units(12) - origin.y) / velocity.y);
            else if (velocity.y < 0) life = Mathf.Min(life, -origin.y / velocity.y);
            return Mathf.Max(0, life);
        }
        internal static float AimedSlashBoundaryLife(Vector2 origin, Vector2 velocity, Vector2 size, float life)
        {
            if (velocity.x > 0) life = Mathf.Min(life, (KarmaStage.ArenaRight - size.x / 2 - origin.x) / velocity.x);
            else if (velocity.x < 0) life = Mathf.Min(life, (KarmaStage.ArenaLeft + size.x / 2 - origin.x) / velocity.x);
            return Mathf.Max(0, life);
        }
    }
}
