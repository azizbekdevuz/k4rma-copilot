using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaHUD
    {
        static readonly Color HudIvory = new Color(0.97f, 0.93f, 0.81f);

        void TopBar()
        {
            if (game.Player == null) return;
            var healthDiamond = KarmaArtwork.Sprite("UI", "Health");
            var healthOutline = KarmaArtwork.Sprite("UI", "HealthOutline");
            float health = Mathf.Clamp01(game.Player.Health / Mathf.Max(1, game.Config.playerHealth)) * 5;
            for (int i = 0; i < 5; i++)
            {
                var r = new Rect(47 + i * 23, 43, 20, 20);
                SpriteArt(r, healthOutline, new Color(HudIvory.r, HudIvory.g, HudIvory.b, 0.45f));
                float fraction = Mathf.Clamp01(health - i);
                if (fraction <= 0) continue;
                // Clip the last diamond so small amounts of damage remain visible.
                GUI.BeginGroup(new Rect(r.x, r.y, r.width * fraction, r.height));
                SpriteArt(new Rect(0, 0, r.width, r.height), healthDiamond, Color.white);
                GUI.EndGroup();
            }

            // Only confirmed personalizations appear, packed in acquisition order.
            var owned = game.Player.Essences.PersonalizationOrder;
            for (int i = 0; i < owned.Count; i++)
            {
                var essence = owned[i];
                var r = new Rect(43 + i * 45, 76, 32, 32);
                bool cooling = game.Player.Essences.Cooldown(essence) > 0;
                Color color = HudIvory;
                color.a = cooling ? 0.35f : 1;
                if (game.Player.Essences.JustTriggered(essence)) color = Color.white;
                SpriteArt(r, SkillIcon(essence, true), color);

            }

            if (game.Enemies.Count == 0 || game.Enemies[0] == null) return;
            var enemy = game.Enemies[0];
            if (!enemy.Alive) return;
            float ratio = Mathf.Clamp01(enemy.Health / Mathf.Max(1, enemy.MaxHealth));
            // Keep the boss readable above the action, clear of the player's corner HUD.
            const float left = 330, width = 620, y = 56;
            Fill(new Rect(left - 16, 16, width + 32, 62), new Color(0.015f, 0.025f, 0.045f, 0.65f));
            Label(left, 20, width, 27, enemy.DisplayName, centered);
            Fill(new Rect(left, y - 2, width, 12), new Color(0.015f, 0.025f, 0.045f, 0.95f));
            Color track = new Color(HudIvory.r, HudIvory.g, HudIvory.b, 0.65f);
            Fill(new Rect(left, y - 2, width, 0.8f), track);
            Fill(new Rect(left, y + 9.2f, width, 0.8f), track);
            Fill(new Rect(left, y, width * ratio, 8), HudIvory);
            Fill(new Rect(left - 3, y - 5, 1.2f, 20), HudIvory);
            Fill(new Rect(left + width + 2, y - 5, 1.2f, 20), HudIvory);
        }

    }
}
