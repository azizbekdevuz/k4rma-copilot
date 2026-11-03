using System;
using System.Collections.Generic;
using UnityEngine;

namespace KarmaPrototype
{
    // Every cutout stays an ordinary, editable Unity Sprite subasset.
    [CreateAssetMenu(menuName = "K4RMA/스프라이트 라이브러리")]
    public sealed class KarmaSpriteLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Sheet
        {
            public string id, source;
            public Sprite[] sprites;
            public string[] poses; // Optional stable logical names for grouped single-image effects.
        }
        public Sheet[] sheets = Array.Empty<Sheet>();
        Dictionary<string, Sprite> lookup;
        public Sprite Get(string sheet, string pose)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                foreach (var entry in sheets)
                    if (entry != null && entry.sprites != null)
                        for (int i = 0; i < entry.sprites.Length; i++)
                        {
                            var sprite = entry.sprites[i];
                            if (sprite == null) continue;
                            string logicalPose = entry.poses != null && i < entry.poses.Length && !string.IsNullOrEmpty(entry.poses[i])
                                ? entry.poses[i] : sprite.name;
                            lookup[entry.id + "/" + logicalPose] = sprite;
                        }
            }
            lookup.TryGetValue(sheet + "/" + pose, out var result);
            return result;
        }
        public Sprite[] All(string sheet)
        {
            foreach (var entry in sheets) if (entry.id == sheet) return entry.sprites;
            return Array.Empty<Sprite>();
        }
        void OnEnable() { lookup = null; }
        void OnValidate() { lookup = null; }
    }
}
