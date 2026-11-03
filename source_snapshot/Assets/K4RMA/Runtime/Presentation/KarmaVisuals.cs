using UnityEngine;

namespace KarmaPrototype
{
    // Shared sprite materials and simple geometry used by stage and combat effects.
    public static class KarmaVisuals
    {
        static Sprite square;
        static Material material;
        public static Material DefaultMaterial => Resources.Load<Material>("KarmaSpriteUnlit");
        public static void UseMaterial(Material value) { material = value != null ? value : DefaultMaterial; }
        public static void ApplyMaterial(SpriteRenderer renderer)
        {
            if (material == null) material = DefaultMaterial;
            if (material != null) renderer.sharedMaterial = material;
        }
        public static SpriteRenderer Box(Transform parent, string name, Vector2 position,
            Vector2 size, Color color, int order = 0)
        {
            if (square == null)
            {
                var texture = new Texture2D(1, 1);
                texture.name = "K4RMA_RuntimePixel";
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                square = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = order;
            ApplyMaterial(renderer);
            return renderer;
        }
        public static void Flash(Transform parent, Vector2 position, Vector2 size, Color color, float life = 0.18f)
        {
            var sr = Box(parent, "Effect", position, size, color, 10);
            sr.gameObject.AddComponent<KarmaEffect>().Initialize(life);
        }
    }
}
