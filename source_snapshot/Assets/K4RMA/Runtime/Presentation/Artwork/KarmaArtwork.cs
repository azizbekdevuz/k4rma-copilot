using System;
using System.Collections.Generic;
using UnityEngine;

namespace KarmaPrototype
{
    // Native Unity assets are linked in the shared Inspector settings asset.
    public static class KarmaArtwork
    {
        public const string SettingsResourcePath = "KarmaArtworkSettings";
        // Enlarge the foot-pivoted artwork for the full-arena camera view.
        const float CharacterPictureScale = KarmaSpace.Scale;
        static KarmaArtworkSettings settings;
        static readonly HashSet<string> failures = new HashSet<string>();
        public static KarmaArtworkSettings Settings
        {
            get
            {
                if (settings == null) settings = Resources.Load<KarmaArtworkSettings>(SettingsResourcePath);
                if (settings == null) throw new InvalidOperationException(
                    "이미지 설정이 없습니다: Assets/K4RMA/Resources/KarmaArtworkSettings.asset");
                return settings;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            // Imported textures and sprites belong to Unity, not this runtime cache.
            settings = null;
            failures.Clear();
        }
        public static Texture2D Texture(string name)
        {
            var texture = Settings.Texture(name);
            if (texture == null && failures.Add(name))
                Debug.LogError("이미지 슬롯이 비어 있습니다: " + name + " / KarmaArtworkSettings를 확인하세요.");
            return texture;
        }
        public static Rect UV(Texture2D texture, Rect topLeft)
        {
            return new Rect(topLeft.x / texture.width, 1 - (topLeft.y + topLeft.height) / texture.height,
                topLeft.width / texture.width, topLeft.height / texture.height);
        }
        public static Sprite Sprite(string sheet, string pose)
        {
            return Settings.spriteLibrary != null ? Settings.spriteLibrary.Get(sheet, pose) : null;
        }
        public static Sprite[] InheritedPoses(string form)
        {
            return Resources.LoadAll<Sprite>("K4RMAArtwork/Characters/Guardians/Inherited/" + form);
        }
        public static Sprite[] PlayerFrames()
        {
            var frames = Settings.PlayerFrames();
            for (int i = 0; i < frames.Length; i++)
                if (frames[i] == null) throw new InvalidOperationException(
                    "플레이어 동작 슬롯이 비어 있습니다: " + i + " / KarmaArtworkSettings를 확인하세요.");
            return frames;
        }
        public static Transform Player(Transform parent)
        {
            var frames = PlayerFrames();
            var root = new GameObject("Artwork").transform; root.SetParent(parent, false);
            var picture = new GameObject("Player sprite"); picture.transform.SetParent(root, false);
            picture.transform.localPosition = new Vector3(0, -KarmaSpace.PlayerHalfHeight, 0);
            picture.transform.localScale = Vector3.one * CharacterPictureScale;
            var renderer = picture.AddComponent<SpriteRenderer>(); renderer.sortingOrder = 5; KarmaVisuals.ApplyMaterial(renderer);
            var animator = root.gameObject.AddComponent<KarmaCharacterAnimator>();
            animator.ConfigureSprites(renderer, frames);
            animator.ConfigureSheet("Player");
            return root;
        }
        public static Transform Guardian(Transform parent, IReadOnlyList<Essence> order, bool final)
        {
            var sprite = Settings.Guardian(KarmaPatternCatalog.Primary(order));
            if (sprite == null) throw new InvalidOperationException("수호자 Sprite 슬롯이 비어 있습니다. KarmaArtworkSettings를 확인하세요.");
            var root = new GameObject("Artwork").transform; root.SetParent(parent, false);
            var picture = new GameObject("Guardian sprite"); picture.transform.SetParent(root, false);
            picture.transform.localPosition = new Vector3(0, -KarmaSpace.GuardianGroundY / KarmaSpace.GuardianArtScale, 0);
            picture.transform.localScale = Vector3.one * CharacterPictureScale;
            var renderer = picture.AddComponent<SpriteRenderer>(); renderer.sortingOrder = 5; KarmaVisuals.ApplyMaterial(renderer);
            var animator = root.gameObject.AddComponent<KarmaCharacterAnimator>();
            animator.ConfigureSprites(renderer, new[] { sprite });
            var primary = KarmaPatternCatalog.Primary(order);
            animator.ConfigureSheet(!primary.HasValue ? "Neutral" : primary == Essence.SwordWave ? "Wave" : primary == Essence.RisingSlash ? "Rise" : "Ward");
            // Keep the prototype's original Wave sheet and inherited pose atlases.
            if (primary == Essence.SwordWave) root.gameObject.AddComponent<KarmaWaveMotion>();
            if (final) renderer.color = new Color(1, 0.88f, 0.73f);
            if (order.Count >= 2) animator.SetGuardianAppearance(GuardianPreview(order), GuardianFormName(order));
            return root;
        }
        public static readonly Color BossSwordColor = new Color(0.42f, 0.68f, 1);
        public static readonly Color BarrierColor = new Color(1, 0.83f, 0.48f);
        public static string GuardianFormName(IReadOnlyList<Essence> order)
        {
            var primary = KarmaPatternCatalog.Primary(order);
            if (!primary.HasValue) return "Neutral";
            string name = primary == Essence.SwordWave ? "Wave" : primary == Essence.RisingSlash ? "Rise" : "Ward";
            if (order.Count == 3) name += "Final";
            else if (order.Count == 2)
                name += order[1] == Essence.SwordWave ? "Wave" : order[1] == Essence.RisingSlash ? "Rise" : "Ward";
            return name;
        }
        public static Sprite GuardianPreview(IReadOnlyList<Essence> order)
        {
            var primary = KarmaPatternCatalog.Primary(order);
            string name = GuardianFormName(order);
            return Sprite("Inheritance", name) ?? Settings.Guardian(primary);
        }
        public static bool Background(Transform parent, int stage = 1, KarmaGame game = null)
        {
            bool mountain = stage == 0 && Settings.mountainBackground != null;
            var original = mountain ? Settings.mountainBackground : Settings.dojoBackground;
            var panorama = mountain ? Settings.mountainPanorama : Settings.dojoPanorama;
            var sprite = panorama != null ? panorama : original;
            if (sprite == null) throw new InvalidOperationException("도장 배경 슬롯이 비어 있습니다.");
            // A single authored panorama covers the scroll range without mirrored repeats.
            float width = panorama != null ? KarmaStage.BackgroundWidth : 50;
            float height = original != null ? 50 * original.rect.height / original.rect.width
                : width * sprite.rect.height / sprite.rect.width;
            float floorFromTop = mountain ? (panorama != null ? 0.724f : 0.74f) : 0.8f;
            var go = new GameObject(stage == 0 ? "Mountain dojo artwork" : "Trial hall artwork");
            go.transform.SetParent(parent, false);
            // Account for a replacement sprite's actual pivot, too.
            go.transform.localPosition = new Vector3(20 + (sprite.pivot.x / sprite.rect.width - 0.5f) * width,
                (sprite.pivot.y / sprite.rect.height - (1 - floorFromTop)) * height, 0);
            go.transform.localScale = new Vector3(width / sprite.bounds.size.x, height / sprite.bounds.size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sortingOrder = -20; KarmaVisuals.ApplyMaterial(renderer);
            if (game != null) game.FrameStageBackground(renderer.bounds);
            return true;
        }
    }
}
