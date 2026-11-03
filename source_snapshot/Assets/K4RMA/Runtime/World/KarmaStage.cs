using UnityEngine;

namespace KarmaPrototype
{
    // Stage geometry and scenery only: no progression or combat rules.
    public static class KarmaStage
    {
        // Boss center limits leave room for its silhouette inside the arena walls.
        public const float ArenaLeft = -15, ArenaRight = 55;
        public const float BackgroundWidth = 70;
        public const float ArenaCenter = (ArenaLeft + ArenaRight) / 2;
        public const float ArenaWidth = ArenaRight - ArenaLeft;
        public const float BossLeftWall = ArenaLeft + 1.1f, BossRightWall = ArenaRight - 1.1f;
        public static readonly Vector2 Spawn = new Vector2(3, KarmaSpace.Units(1));
        public static readonly Vector2 Gate = new Vector2(38, 1.8f);
        public static void Build(KarmaGame game, Transform parent, int stage)
        {
            Color stone = new Color(0.16f + stage * 0.014f, 0.17f, 0.23f);
            bool suppliedBackground = KarmaArtwork.Background(parent, stage, game);
            BuildArtworkProps(parent, stage);
            if (!suppliedBackground)
            {
            KarmaVisuals.Box(parent, "수련장 배경", new Vector2(20, 4), new Vector2(70, 26), new Color(0.09f, 0.07f, 0.10f), -20);
            for (int i = 0; i < 12; i++)
            {
                float x = i * 4 - 2;
                KarmaVisuals.Box(parent, "Pillar", new Vector2(x, 4), new Vector2(0.6f, 9), stone * 0.8f, -15);
                KarmaVisuals.Box(parent, "Window", new Vector2(x + 1.8f, 5.3f), new Vector2(1, 2.4f), new Color(0.16f, 0.17f, 0.3f), -14);
                KarmaVisuals.Box(parent, "Window light", new Vector2(x + 1.8f, 5.3f), new Vector2(0.08f, 2.4f), new Color(0.45f, 0.33f, 0.6f), -13);
            }
            for (int i = 0; i < 8; i++)
            {
                float x = 2 + i * 5;
                KarmaVisuals.Box(parent, "障子", new Vector2(x,4.5f),new Vector2(3.8f,5),new Color(0.35f,0.29f,0.25f),-12);
                for (int j = 0; j < 4; j++) KarmaVisuals.Box(parent,"障子 木枠",new Vector2(x-1.5f+j,4.5f),new Vector2(.07f,5),stone,-11);
                for (int j = 0; j < 5; j++) KarmaVisuals.Box(parent,"障子 横枠",new Vector2(x,2.5f+j),new Vector2(3.8f,.07f),stone,-11);
                KarmaVisuals.Box(parent,"燈籠",new Vector2(x,6.8f),new Vector2(.6f,.85f),new Color(.86f,.53f,.28f),-9);
            }
            KarmaVisuals.Box(parent,"도장 처마",new Vector2(20,8),new Vector2(44,.5f),new Color(.3f,.12f,.12f),-8);
            }
            Solid(parent, "Ground", new Vector2(ArenaCenter, -0.55f), new Vector2(ArenaWidth + 4, 1.1f), stone).GetComponent<SpriteRenderer>().enabled = !suppliedBackground;
            Solid(parent, "Left wall", new Vector2(ArenaLeft - 1, KarmaSpace.Units(4)), new Vector2(1, KarmaSpace.Units(12)), stone).GetComponent<SpriteRenderer>().enabled = !suppliedBackground;
            Solid(parent, "Right wall", new Vector2(ArenaRight + 1, KarmaSpace.Units(4)), new Vector2(1, KarmaSpace.Units(12)), stone).GetComponent<SpriteRenderer>().enabled = !suppliedBackground;
        }
        static void BuildArtworkProps(Transform parent, int stage)
        {
            string[] names = stage == 0
                ? new[] { "Gate", "Steps", "Well", "Rope", "LeftTree", "RightTree", "Leaves", "Rocks", "Bush", "Wood" }
                : new[] { "Lantern", "TrainingPost", "WaveBanner", "RiseBanner", "WardBanner", "Rail" };
            for (int i = 0; i < names.Length; i++)
            {
                var sprite = KarmaArtwork.Sprite("Props", names[i]);
                if (sprite == null) continue;
                // Decoration stays behind actors and never adds terrain/collision.
                var prop = new GameObject("Scenery/" + names[i]).transform; prop.SetParent(parent, false);
                bool tall = names[i].Contains("Tree") || names[i] == "Gate" || names[i].Contains("Banner");
                float scale = (tall ? 6 : 2.2f) / sprite.bounds.size.y;
                float x = i % 2 == 0 ? ArenaLeft + 1 + i * 1.5f : ArenaRight - 1 - i * 1.5f;
                float y = names[i].Contains("Banner") || names[i] == "Lantern" ? 5 : 0;
                prop.localPosition = new Vector3(x, y, 0);
                prop.localScale = Vector3.one * scale;
                var renderer = prop.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite; renderer.sortingOrder = -8; KarmaVisuals.ApplyMaterial(renderer);
            }
        }
        public static void RefreshBoundaries(Transform parent)
        {
            // Existing Play Mode geometry must also follow updated arena limits after a script reload.
            var ground = parent.Find("Ground");
            if (ground != null)
            {
                ground.localPosition = new Vector3(ArenaCenter, -0.55f, 0);
                ground.localScale = new Vector3(ArenaWidth + 4, 1.1f, 1);
            }
            var left = parent.Find("Left wall");
            if (left != null) left.localPosition = new Vector3(ArenaLeft - 1, KarmaSpace.Units(4), 0);
            var right = parent.Find("Right wall");
            if (right != null) right.localPosition = new Vector3(ArenaRight + 1, KarmaSpace.Units(4), 0);
        }
        static BoxCollider2D Solid(Transform parent, string name, Vector2 p, Vector2 size, Color color)
        {
            var sr = KarmaVisuals.Box(parent, name, p, size, color);
            sr.gameObject.AddComponent<KarmaSurface>();
            return sr.gameObject.AddComponent<BoxCollider2D>();
        }
    }
}
