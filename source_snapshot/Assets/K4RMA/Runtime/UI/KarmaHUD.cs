using System;
using System.Collections.Generic;
using UnityEngine;

namespace KarmaPrototype
{
    // IMGUI keeps setup dependency-free. Replace this module with Canvas/TMP later.
    public sealed partial class KarmaHUD : MonoBehaviour
    {
        KarmaGame game;
        GUIStyle title, heading, text, small, button, menuButton, centered;
        Font runtimeFont;
        readonly string[] names = { "검기", "상승베기", "방어" };
        public void Initialize(KarmaGame director) { game = director; }
        Font ResolveFont()
        {
            if (game.Config.koreanFont != null) return game.Config.koreanFont;

            var installed = new HashSet<string>(Font.GetOSInstalledFontNames(), StringComparer.OrdinalIgnoreCase);
            bool mac = Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer;
            var candidates = new List<string>(mac
                ? new[] { "Apple SD Gothic Neo", "Malgun Gothic", "맑은 고딕" }
                : new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo" });
            candidates.AddRange(new[] { "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic", "Arial Unicode MS" });
            foreach (string candidate in candidates)
            {
                if (!installed.Contains(candidate)) continue;
                // Do not pass unavailable OS faces to Unity's font fallback chain.
                var font = Font.CreateDynamicFontFromOSFont(candidate, 18);
                if (font != null && font.HasCharacter('가'))
                {
                    runtimeFont = font;
                    return font;
                }
                if (font != null) Destroy(font);
            }
            Debug.LogWarning("K4RMA: 사용 가능한 한글 UI 폰트를 찾지 못했습니다. KarmaConfig의 한글 UI 폰트를 연결하세요.", this);
            return null;
        }
        void Styles()
        {
            if (title != null) return;
            Font font = ResolveFont();
            title = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            heading = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, wordWrap = true };
            text = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 18, wordWrap = true, padding = new RectOffset(15, 15, 10, 10) };
            menuButton = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            menuButton.normal.textColor = new Color(0.92f, 0.9f, 0.82f);
            menuButton.hover.textColor = Color.white;
            centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
            foreach (var s in new[] { title, heading, text, small }) s.normal.textColor = new Color(0.9f, 0.91f, 0.94f);
            foreach (var s in new[] { title, heading, text, small, button, menuButton, centered }) if (font != null) s.font = font;
        }
        void OnGUI()
        {
            if (game == null) return;
            Styles();
            Matrix4x4 saved = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));
            if (game.Phase == RunPhase.Title || game.Phase == RunPhase.Story || game.Phase == RunPhase.Controls)
                Art(new Rect(0, 0, 1280, 720), "MenuBackground");
            if (game.Phase == RunPhase.Title) TitleScreen();
            else if (game.Phase == RunPhase.Story) StoryScreen();
            else if (game.Phase == RunPhase.Controls) ControlsScreen();
            else
            {
                TopBar();
                if (game.Paused || game.Phase == RunPhase.Choosing
                    || game.Phase == RunPhase.Defeat || game.Phase == RunPhase.Victory)
                    Fill(new Rect(0, 0, 1280, 720), new Color(0.015f, 0.025f, 0.07f, 0.78f));
                if (game.Paused) PauseScreen();
                else switch (game.Phase)
                {
                    case RunPhase.Choosing: ChoiceScreen(); break;
                    case RunPhase.Transition: StageTransitionScreen(); break;
                    case RunPhase.Defeat: EndScreen(false); break;
                    case RunPhase.Victory: EndScreen(true); break;
                    case RunPhase.Gate:
                        Panel(new Rect(300, 525, 680, 110));
                        Label(325, 539, 640, 35, "시험 통과", heading);
                        Label(325, 578, 640, 44, "오른쪽 수련문으로 이동한 뒤 F를 눌러 고유화할 기술을 고르세요.", text);
                        break;
                }
            }
            GUI.matrix = saved;
        }

        void OnDestroy()
        {
            if (runtimeFont != null) Destroy(runtimeFont);
        }

        static void Art(Rect bounds, string name, Rect? source = null)
        {
            var texture = KarmaArtwork.Texture(name);
            if (texture == null)
            {
                Fill(bounds, new Color(0.22f, 0.035f, 0.035f));
                GUI.Label(bounds, "이미지 파일 누락: " + name + "\nAssets/K4RMA 전체를 적용하세요.");
                return;
            }
            Rect crop = source ?? new Rect(0, 0, texture.width, texture.height);
            float ratio = crop.width / crop.height;
            float width = Mathf.Min(bounds.width, bounds.height * ratio);
            float height = width / ratio;
            var destination = new Rect(bounds.x + (bounds.width - width) * 0.5f,
                bounds.y + (bounds.height - height) * 0.5f, width, height);
            Color old = GUI.color; GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(destination, texture, KarmaArtwork.UV(texture, crop), true);
            GUI.color = old;
        }
        static void SpriteArt(Rect bounds, Sprite sprite, Color? tint = null)
        {
            if (sprite == null) return;
            float ratio = sprite.rect.width / sprite.rect.height;
            float width = Mathf.Min(bounds.width, bounds.height * ratio), height = width / ratio;
            var rect = new Rect(bounds.center.x - width / 2, bounds.center.y - height / 2, width, height);
            Color old = GUI.color; GUI.color = tint ?? Color.white;
            var source = sprite.rect;
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, new Rect(source.x / sprite.texture.width,
                source.y / sprite.texture.height, source.width / sprite.texture.width, source.height / sprite.texture.height), true);
            GUI.color = old;
        }
        static Sprite SkillIcon(Essence essence, bool unique)
        {
            var icons = KarmaArtwork.Settings.skillIcons;
            int index = (int)essence + (unique ? 3 : 0);
            return icons != null && index < icons.Length ? icons[index] : null;
        }
        void Label(float x, float y, float w, float h, string value, GUIStyle style) { GUI.Label(new Rect(x, y, w, h), value, style); }
        static string Clock(float seconds) { return ((int)seconds / 60) + ":" + ((int)seconds % 60).ToString("00"); }
        static void Panel(Rect rect) { Fill(rect, new Color(0.045f, 0.052f, 0.09f, 0.95f)); }
        static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
    }
}
