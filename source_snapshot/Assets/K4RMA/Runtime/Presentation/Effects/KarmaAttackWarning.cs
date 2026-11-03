using System.Collections.Generic;
using UnityEngine;
namespace KarmaPrototype
{
    // Convex hull of the two endpoint boxes: exact footprint of a swept AABB.
    // Unlike a diagonal bounding rectangle, this preserves the real safe corners.
    public sealed class KarmaAttackWarning : MonoBehaviour
    {
        Sprite shape;
        SpriteRenderer visual;
        LineRenderer edge, arrow;
        Color baseColor;
        bool restrainedVisual, aerialVisual, aerialGround;
        public void SetPath(SpriteRenderer renderer, Vector2 from, Vector2 to, Vector2 size, bool restrained = false, bool oriented = false, bool aerial = false)
        {
            if (visual == null) { visual = renderer; baseColor = renderer.color; }
            // Tracking calls reuse this component without repeating its visual options.
            restrainedVisual |= restrained;
            aerialVisual |= aerial;
            Vector2 center = (from + to) / 2;
            aerialGround = aerialVisual && (to - from).sqrMagnitude < 0.0001f
                && center.y < 0.6f && size.x > size.y * 2;
            Vector2 axis = oriented && (to - from).sqrMagnitude > 0.000001f ? (to - from).normalized : Vector2.right;
            Vector2 normal = new Vector2(-axis.y, axis.x);
            Vector2 bounds = new Vector2(Mathf.Abs(to.x - from.x) + Mathf.Abs(axis.x) * size.x + Mathf.Abs(normal.x) * size.y,
                Mathf.Abs(to.y - from.y) + Mathf.Abs(axis.y) * size.x + Mathf.Abs(normal.y) * size.y);
            var points = new List<Vector2>(8);
            foreach (var p in new[] { from, to })
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        points.Add(p + axis * (size.x * x / 2) + normal * (size.y * y / 2));
            points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var hull = new List<Vector2>();
            foreach (var p in points)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], p - hull[hull.Count - 1]) <= 0) hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            int lower = hull.Count;
            for (int i = points.Count - 2; i >= 0; i--)
            {
                var p = points[i];
                while (hull.Count > lower && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], p - hull[hull.Count - 1]) <= 0) hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            hull.RemoveAt(hull.Count - 1);
            var vertices = new Vector2[hull.Count];
            // OverrideGeometry takes Sprite Rect coordinates, not pivot-relative
            // positions. Unity applies the centered pivot itself. Clamp rounding
            // error so every point stays inside this 1x1 pixel rectangle.
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = new Vector2(
                    Mathf.Clamp01((hull[i].x - center.x) / bounds.x + 0.5f),
                    Mathf.Clamp01((hull[i].y - center.y) / bounds.y + 0.5f));
            var triangles = new ushort[(hull.Count - 2) * 3];
            for (int i = 0; i < hull.Count - 2; i++)
            { triangles[i * 3] = 0; triangles[i * 3 + 1] = (ushort)(i + 1); triangles[i * 3 + 2] = (ushort)(i + 2); }
            if (shape == null)
            {
                shape = Sprite.Create(renderer.sprite.texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
                renderer.sprite = shape;
            }
            shape.OverrideGeometry(vertices, triangles);
            transform.localPosition = center; transform.localScale = new Vector3(bounds.x, bounds.y, 1);
            if (edge == null)
            {
                var go = new GameObject("Locked danger outline"); go.transform.SetParent(transform, false);
                edge = go.AddComponent<LineRenderer>(); edge.useWorldSpace = false; edge.loop = true;
                edge.sharedMaterial = !restrainedVisual && KarmaCombatVFX.GlowMaterial != null ? KarmaCombatVFX.GlowMaterial : renderer.sharedMaterial;
                edge.sortingOrder = 16; edge.numCornerVertices = 2;
            }
            edge.useWorldSpace = restrainedVisual;
            edge.positionCount = hull.Count;
            for (int i = 0; i < hull.Count; i++)
            {
                var point = new Vector3((hull[i].x - center.x) / bounds.x, (hull[i].y - center.y) / bounds.y, 0);
                edge.SetPosition(i, restrainedVisual ? transform.TransformPoint(point) : point);
            }
            if (arrow == null)
            {
                var go = new GameObject("Attack direction"); go.transform.SetParent(transform, false);
                arrow = go.AddComponent<LineRenderer>(); arrow.useWorldSpace = false;
                arrow.sharedMaterial = edge.sharedMaterial; arrow.sortingOrder = 17; arrow.numCornerVertices = 2;
            }
            arrow.useWorldSpace = restrainedVisual;
            arrow.enabled = (to - from).sqrMagnitude > 0.01f;
            if (arrow.enabled)
            {
                axis = (to - from).normalized; normal = new Vector2(-axis.y, axis.x);
                float length = Mathf.Min(KarmaSpace.Units(0.5f), Vector2.Distance(from, to) * 0.2f, Mathf.Min(size.x, size.y) * 0.45f);
                // In a dense volley, keep each direction cue close to its own
                // forming sword instead of placing every arrow across the arena.
                Vector2 tip = aerialVisual
                    ? from + axis * Mathf.Min(Vector2.Distance(from, to) * 0.25f, KarmaSpace.Units(1.2f))
                    : Vector2.Lerp(from, to, 0.7f);
                var head = new[] { tip - axis * length + normal * length, tip, tip - axis * length - normal * length };
                arrow.positionCount = 3;
                for (int i = 0; i < 3; i++)
                {
                    var point = new Vector3((head[i].x - center.x) / bounds.x, (head[i].y - center.y) / bounds.y, 0);
                    arrow.SetPosition(i, restrainedVisual ? transform.TransformPoint(point) : point);
                }
            }
            // Set the first rendered frame too; LineRenderer defaults are opaque white
            // and one unit wide until its first Update.
            ApplyStyle();
        }
        public void Emphasize() { baseColor.a = Mathf.Max(baseColor.a, 0.58f); }
        void Update()
        { ApplyStyle(); }
        void ApplyStyle()
        {
            if (visual == null || edge == null) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 15);
            var fill = baseColor;
            fill.a = baseColor.a * (aerialGround ? 0.75f + pulse * 0.25f : aerialVisual ? 0.10f + pulse * 0.04f : restrainedVisual ? 0.24f + pulse * 0.08f : 0.6f + pulse * 0.35f);
            visual.color = fill;
            var color = new Color(baseColor.r, baseColor.g, baseColor.b,
                aerialGround ? 0.85f + pulse * 0.15f : aerialVisual ? 0.34f + pulse * 0.10f : restrainedVisual ? 0.72f + pulse * 0.16f : 0.7f + pulse * 0.3f);
            var directionColor = color;
            if (aerialVisual) directionColor.a = 0.80f + pulse * 0.10f;
            arrow.startColor = arrow.endColor = directionColor;
            arrow.widthMultiplier = KarmaSpace.Units(aerialVisual ? 0.03f : restrainedVisual ? 0.04f : 0.055f);
            edge.startColor = edge.endColor = color;
            edge.widthMultiplier = KarmaSpace.Units(aerialGround ? 0.04f + pulse * 0.012f : aerialVisual ? 0.018f + pulse * 0.004f : restrainedVisual ? 0.025f + pulse * 0.01f : 0.035f + pulse * 0.015f);
        }
        static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
        void OnDestroy() { if (shape != null) Destroy(shape); }
    }
}
