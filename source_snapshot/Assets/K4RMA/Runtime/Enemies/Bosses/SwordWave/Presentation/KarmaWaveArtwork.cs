using UnityEngine;
namespace KarmaPrototype
{
    // Alpha >= 128 source-pixel bounds map to the unchanged damage rectangle.
    // Transparent corners remain part of that rectangle; no released outline.
    public static class KarmaWaveArtwork
    {
        public static string Pose(EnemyAttack attack, bool stationary = false)
        {
            if (attack == EnemyAttack.SW02) return "SW02_AimedSlash";
            if (attack == EnemyAttack.SW03 && stationary) return "SW03_RisingBlade";
            if (attack == EnemyAttack.SW04) return "SW04_CounterWave";
            if (attack == EnemyAttack.SW05) return "SW05_AerialSword";
            return "SW01_StaggeredWave";
        }
        public static Rect CorePixels(string pose)
        {
            switch (pose)
            {
                case "SW01_StaggeredWave": return new Rect(43, 35, 1373, 279);
                case "SW02_AimedSlash": return new Rect(40, 28, 1296, 380);
                case "SW03_RisingBlade": return new Rect(39, 41, 233, 1325);
                case "SW03_GroundImpact": return new Rect(39, 40, 359, 344);
                case "SW04_CounterWave": return new Rect(41, 32, 1325, 233);
                case "SW05_AerialSword": return new Rect(44, 51, 1366, 282);
                default: throw new System.ArgumentOutOfRangeException(nameof(pose));
            }
        }
        public static void Apply(SpriteRenderer renderer, string pose, Vector2 size, Vector2 velocity)
        {
            renderer.sprite = KarmaArtwork.Sprite("SwordBossVFX", pose);
            if (renderer.sprite == null) throw new System.InvalidOperationException("Missing SwordBossVFX/" + pose);
            // Preserve the authored orange PNG and its alpha/geometry. Only SW05
            // maps its warm source colors to a blue-white blade at render time.
            KarmaVisuals.ApplyMaterial(renderer);
            if (pose == "SW05_AerialSword")
            {
                var material = Resources.Load<Material>("KarmaAerialSword");
                if (material == null) throw new System.InvalidOperationException("Missing KarmaAerialSword material");
                renderer.sharedMaterial = material;
            }
            Rect core = CorePixels(pose);
            float ppu = renderer.sprite.pixelsPerUnit;
            // SW02 is authored with its bright cutting edge facing -X and its
            // trailing glow facing +X. Mirror the picture about its core pivot;
            // rotation and the damage axis still follow the world-space velocity.
            bool horizontalSlash = pose == "SW02_AimedSlash" && Mathf.Abs(velocity.y) < 0.000001f;
            renderer.flipX = pose == "SW02_AimedSlash" && (!horizontalSlash || velocity.x >= 0);
            renderer.flipY = false;
            renderer.transform.localScale = new Vector3(size.x * ppu / core.width, size.y * ppu / core.height, 1);
            renderer.transform.localRotation = Quaternion.Euler(0, 0, !horizontalSlash && velocity.sqrMagnitude > 0.000001f
                ? Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg : 0);
        }
    }
}
