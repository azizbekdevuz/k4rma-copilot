using UnityEngine;

namespace KarmaPrototype
{
    // The room shell owns presentation and its roof, never damage or trial scoring.
    public sealed class KarmaGuardDome : MonoBehaviour
    {
        const int Sections = 18;
        readonly Color gold = new Color(1, 0.78f, 0.08f);
        KarmaGame game;
        Sprite fillShape;
        SpriteRenderer fill;
        SpriteRenderer[] debris;
        LineRenderer inner, floor;
        LineRenderer[] shell, cracks;
        Vector2[] normals;
        EdgeCollider2D roof;
        float age, phaseAge, chargeDuration;
        int phase;
        public float Radius { get; private set; }
        public bool Exploding { get { return phase == 2; } }
        public float PhaseSeconds { get { return phaseAge; } }
        public bool Charging { get { return phase == 1; } }

        public static KarmaGuardDome Create(KarmaGame game, Transform parent, Vector2 center, float radius)
        {
            var root = new GameObject("GD05 노란 반원 보호막");
            root.transform.SetParent(parent, false); root.transform.localPosition = center;
            var dome = root.AddComponent<KarmaGuardDome>();
            dome.game = game; dome.Radius = radius; dome.Build();
            return dome;
        }
        Vector2 Arc(float angle, float radius) { return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius; }
        LineRenderer Line(string name, Transform parent, Vector2[] points, float width, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.positionCount = points.Length;
            line.widthMultiplier = width; line.numCornerVertices = 2; line.numCapVertices = 2;
            line.sharedMaterial = KarmaCombatVFX.GlowMaterial != null ? KarmaCombatVFX.GlowMaterial : KarmaVisuals.DefaultMaterial;
            line.sortingOrder = order;
            for (int i = 0; i < points.Length; i++) line.SetPosition(i, points[i]);
            Tint(line, 0.8f); return line;
        }
        void Tint(LineRenderer line, float alpha)
        { line.startColor = line.endColor = new Color(gold.r, gold.g, gold.b, alpha); }
        Vector2[] ArcPoints(float radius, float first, float last, int samples)
        {
            var points = new Vector2[samples];
            for (int i = 0; i < samples; i++) points[i] = Arc(Mathf.Lerp(first, last, i / (float)(samples - 1)), radius);
            return points;
        }
        void Build()
        {
            fill = KarmaVisuals.Box(transform, "보호막 반투명 내부", Vector2.up * Radius / 2,
                new Vector2(Radius * 2, Radius), Color.white, 1);
            fillShape = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
            fillShape.name = "GD05 semicircle fill";
            var vertices = new Vector2[66]; var triangles = new ushort[64 * 3];
            vertices[0] = new Vector2(0.5f, 0);
            for (int i = 0; i <= 64; i++)
            {
                float a = Mathf.PI * i / 64;
                vertices[i + 1] = new Vector2(Mathf.Clamp01(0.5f + Mathf.Cos(a) * 0.5f), Mathf.Clamp01(Mathf.Sin(a)));
                if (i < 64) { triangles[i * 3] = 0; triangles[i * 3 + 1] = (ushort)(i + 1); triangles[i * 3 + 2] = (ushort)(i + 2); }
            }
            fillShape.OverrideGeometry(vertices, triangles); fill.sprite = fillShape;
            shell = new LineRenderer[Sections]; normals = new Vector2[Sections]; debris = new SpriteRenderer[Sections];
            for (int i = 0; i < Sections; i++)
            {
                float a = Mathf.PI * i / Sections, b = Mathf.PI * (i + 1) / Sections;
                shell[i] = Line("황금 보호막 조각", transform, ArcPoints(Radius, a + 0.006f, b - 0.006f, 5), KarmaSpace.Units(0.055f), 12);
                normals[i] = Arc((a + b) / 2, 1);
            }
            var roofPoints = ArcPoints(Radius, 0, Mathf.PI, 65);
            roof = gameObject.AddComponent<EdgeCollider2D>(); roof.points = roofPoints;
            gameObject.AddComponent<KarmaSurface>();
            inner = Line("안쪽 금빛 테두리", transform, ArcPoints(Radius * 0.985f, 0, Mathf.PI, 65), KarmaSpace.Units(0.025f), 11);
            floor = Line("결계 바닥선", transform, new[] { Vector2.left * Radius, Vector2.right * Radius }, KarmaSpace.Units(0.04f), 4);
            cracks = new LineRenderer[9];
            for (int i = 0; i < cracks.Length; i++)
            {
                float a = Mathf.PI * (i + 0.5f) / cracks.Length;
                var points = new Vector2[5];
                for (int j = 0; j < points.Length; j++)
                    points[j] = Arc(a + (j % 2 == 0 ? 0 : 0.045f), Radius * (1 - j * 0.065f));
                cracks[i] = Line("과부하 균열", transform, points, KarmaSpace.Units(0.035f), 13);
                cracks[i].enabled = false;
            }
            Paint();
        }
        public bool Overlaps(Vector2 center, Vector2 halfSize)
        {
            Vector2 delta = center - (Vector2)transform.position;
            if (delta.y + halfSize.y < 0) return false;
            var nearest = new Vector2(Mathf.Max(0, Mathf.Abs(delta.x) - halfSize.x), Mathf.Max(0, delta.y - halfSize.y));
            return nearest.sqrMagnitude <= Radius * Radius;
        }
        public void Charge(float seconds) { phase = 1; phaseAge = 0; chargeDuration = Mathf.Max(0.1f, seconds); }
        public void Explode()
        {
            phase = 2; phaseAge = 0; roof.enabled = false;
            for (int i = 0; i < Sections; i++)
            {
                if (KarmaArtwork.Sprite("GuardBossVFX", "Shard") == null) continue;
                var shard = KarmaVisuals.Box(shell[i].transform, "터진 보호막 파편", normals[i] * Radius,
                    Vector2.one, Color.white, 14);
                debris[i] = shard;
                KarmaCombatVFX.ApplySprite(shard, "GuardBossVFX", "Shard", Vector2.one * KarmaSpace.Units(0.4f + i % 3 * 0.15f), true);
                shard.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(normals[i].y, normals[i].x) * Mathf.Rad2Deg);
            }
            Paint();
        }
        public void Dissolve() { phase = 3; phaseAge = 0; roof.enabled = false; }
        void Update()
        {
            if (game == null || game.Paused) return;
            age += Time.deltaTime; phaseAge += Time.deltaTime; Paint();
        }
        void Paint()
        {
            float charge = phase == 1 ? Mathf.Clamp01(phaseAge / chargeDuration) : 0;
            float fade = phase == 2 ? 1 - Mathf.Clamp01(phaseAge / 0.75f)
                : phase == 3 ? 1 - Mathf.Clamp01(phaseAge / 0.4f) : 1;
            float pulse = 0.5f + 0.5f * Mathf.Sin(age * (3 + charge * 30));
            float fillAlpha = phase == 2 ? 0.55f * fade * fade : (0.035f + pulse * 0.015f + charge * 0.16f) * fade;
            fill.color = new Color(1, 0.8f + charge * 0.15f, 0.12f, fillAlpha);
            Tint(inner, (0.4f + charge * pulse) * fade); Tint(floor, (0.5f + charge) * fade);
            for (int i = 0; i < Sections; i++)
            {
                Tint(shell[i], (0.75f + pulse * 0.2f + charge * 0.8f) * fade);
                shell[i].transform.localPosition = phase == 2
                    ? normals[i] * phaseAge * KarmaSpace.Units(5 + i % 3) + Vector2.down * phaseAge * phaseAge * KarmaSpace.Units(2)
                    : Vector2.zero;
                if (debris[i] != null) debris[i].color = new Color(1, 1, 1, fade);
            }
            inner.transform.localScale = Vector3.one * (phase == 2 ? 1 + phaseAge * 0.3f : 1);
            for (int i = 0; i < cracks.Length; i++)
            {
                cracks[i].enabled = phase == 1 && charge > i * 0.055f || phase == 2;
                Tint(cracks[i], (phase == 2 ? fade : charge) * 0.9f);
            }
        }
        void OnDestroy() { if (fillShape != null) Destroy(fillShape); }
    }
}
