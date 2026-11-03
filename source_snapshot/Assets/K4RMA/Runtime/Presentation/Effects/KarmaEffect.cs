using UnityEngine;

namespace KarmaPrototype
{
    public sealed class KarmaEffect : MonoBehaviour
    {
        float left, duration, initialAlpha;
        SpriteRenderer sprite;
        public void Initialize(float seconds)
        {
            duration = left = seconds;
            sprite = GetComponent<SpriteRenderer>();
            initialAlpha = sprite.color.a;
        }
        void Update()
        {
            left -= Time.deltaTime;
            if (left <= 0) { Destroy(gameObject); return; }
            var color = sprite.color;
            color.a = initialAlpha * Mathf.Clamp01(left / duration);
            sprite.color = color;
        }
    }
}
