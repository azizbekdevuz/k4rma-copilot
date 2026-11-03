using System.Collections.Generic;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaHUD
    {
        int storyPage;
        Vector2 storyScroll;
        void Update()
        {
            if (game == null || game.Phase != RunPhase.Story) return;
            int previousPage = storyPage;
            if (KarmaInput.Pressed(KarmaAction.NextPage)) storyPage = Mathf.Min(KarmaStory.Pages.Length - 1, storyPage + 1);
            if (KarmaInput.Pressed(KarmaAction.PreviousPage)) storyPage = Mathf.Max(0, storyPage - 1);
            if (previousPage != storyPage) storyScroll = Vector2.zero;
        }
        void TitleScreen()
        {
            Fill(new Rect(0, 0, 1280, 720), new Color(0.02f, 0.03f, 0.07f, 0.25f));
            Label(330, 160, 620, 100, "K4RMA", title);
            Label(330, 270, 620, 35, "네 수호자 · 세 번의 고유화", centered);
            SpriteArt(new Rect(460, 320, 360, 18), KarmaArtwork.Sprite("UI", "LongRule"));
            if (GUI.Button(new Rect(480, 365, 320, 55), "시작 [Enter]", menuButton)) game.StartRun();
            if (GUI.Button(new Rect(480, 430, 320, 55), "게임 스토리 [S]", menuButton)) game.ShowPage(RunPhase.Story);
            if (GUI.Button(new Rect(480, 495, 320, 55), "조작 안내 [H]", menuButton)) game.ShowPage(RunPhase.Controls);
        }
        void StoryScreen()
        {
            Fill(new Rect(0, 0, 1280, 720), new Color(0.015f, 0.025f, 0.07f, 0.68f));
            Label(280, 40, 720, 60, "스승과 제자", title);
            SpriteArt(new Rect(460, 115, 360, 18), KarmaArtwork.Sprite("UI", "LongRule"));
            Label(280, 140, 720, 30, (storyPage + 1) + " / " + KarmaStory.Pages.Length, centered);
            float contentHeight = Mathf.Max(410, text.CalcHeight(new GUIContent(KarmaStory.Pages[storyPage]), 620));
            storyScroll = GUI.BeginScrollView(new Rect(155, 180, 640, 410), storyScroll, new Rect(0, 0, 620, contentHeight));
            GUI.Label(new Rect(0, 0, 620, contentHeight), KarmaStory.Pages[storyPage], text);
            GUI.EndScrollView();
            var illustration = storyPage == 3 ? KarmaArtwork.Sprite("Player", "Rising")
                : storyPage >= 5 && storyPage != 7 ? KarmaArtwork.GuardianPreview(new[] { Essence.SwordWave })
                : KarmaArtwork.Sprite("Player", "Idle");
            SpriteArt(new Rect(845, 195, 315, 350), illustration);
            Label(845, 555, 315, 30, "수호자의 시험", centered);
            if (GUI.Button(new Rect(155, 625, 280, 50), "이전 [←]", menuButton)) { storyPage = Mathf.Max(0, storyPage - 1); storyScroll = Vector2.zero; }
            if (GUI.Button(new Rect(500, 625, 280, 50), "다음 [→]", menuButton)) { storyPage = Mathf.Min(KarmaStory.Pages.Length - 1, storyPage + 1); storyScroll = Vector2.zero; }
            if (GUI.Button(new Rect(845, 625, 280, 50), "타이틀 [Esc]", menuButton)) game.ReturnToTitle();
        }
        void ControlsScreen()
        {
            Fill(new Rect(0, 0, 1280, 720), new Color(0.015f, 0.025f, 0.07f, 0.68f));
            Label(280, 40, 720, 60, "조작 안내", title);
            SpriteArt(new Rect(460, 115, 360, 18), KarmaArtwork.Sprite("UI", "LongRule"));
            Label(115, 175, 750, 420,
                "A/D · ←/→ : 이동    Space : 점프    J 유지 : 검 기본 공격\n\nQ : 검기 발사    왼쪽 Shift : 상승베기    E : 원형 방어\n고유화한 원형 키는 사용할 수 없습니다.\n\n관통베기 : 같은 방향키 / WASD 두 번 (0.25초 이내) · 해당 방향으로 관통\n도약베기 : 공중 방향키 / WASD + J 새로 누르기 · 베고 추가 점프 (착지 전 1회)\n↓ / S + J : 아래로 베고 점프 · 방향 입력이 없으면 바라보는 방향\n반격 : K로 짧은 받아내기 동작 · 공격 도착 순간에만 성공\n\n고유화 : 1/2/3 선택 → Enter 확정 · Esc 취소/재선택\nEsc : 일시정지    일시정지/결과에서 R : 처음부터    T : 타이틀\n\n사망하면 네 수호자의 시험을 Stage 1부터 다시 시작합니다.", text);
            Label(115, 595, 750, 28, "↓ / S 유지 : 앉기    P : 테스트 무적 켜기/끄기", small);
            SpriteArt(new Rect(915, 175, 250, 105), SkillIcon(Essence.SwordWave, false));
            Label(915, 280, 250, 28, "검기 · 관통베기", centered);
            SpriteArt(new Rect(915, 325, 250, 105), SkillIcon(Essence.RisingSlash, false));
            Label(915, 430, 250, 28, "상승베기 · 도약베기", centered);
            SpriteArt(new Rect(915, 475, 250, 105), SkillIcon(Essence.Guard, false));
            Label(915, 580, 250, 28, "방어 · 반격", centered);
            if (GUI.Button(new Rect(440, 635, 400, 50), "타이틀 [Esc]", menuButton)) game.ReturnToTitle();
        }
        void ChoiceScreen()
        {
            Label(280, 265, 720, 45, "시련 완료 · 고유화", title);
            int count = 0;
            for (int i = 0; i < 3; i++) if (game.Player.Essences.HasActive((Essence)i)) count++;
            int slot = 0;
            for (int i = 0; i < 3; i++)
            {
                var e = (Essence)i;
                if (!game.Player.Essences.HasActive(e)) continue;
                float centerX = 640 + (slot++ - (count - 1) * 0.5f) * 180;
                bool selected = game.PendingChoice == e;
                if (GUI.Button(new Rect(centerX - 80, 335, 160, 145), "", GUIStyle.none)) game.Choose(e);
                SpriteArt(new Rect(centerX - 45, 340, 90, 80), SkillIcon(e, false));
                Label(centerX - 80, 430, 160, 35, (i + 1) + " · " + names[i], centered);
                if (selected) SpriteArt(new Rect(centerX - 10, 480, 20, 25), KarmaArtwork.Settings.selectionCrescent);
                SpriteArt(new Rect(centerX - 12, 312, 24, 24), KarmaArtwork.Sprite("UI", selected ? "FilledSlot" : "EmptySlot"));
            }
            if (game.PendingChoice.HasValue)
            {
                var e = game.PendingChoice.Value;
                Label(280, 520, 720, 35, names[(int)e] + " → " + KarmaPatternCatalog.UniqueName(e), centered);
            }
            else Label(280, 520, 720, 35, "다음 시련에 남길 원형을 선택하세요.", centered);
            GUI.enabled = game.PendingChoice.HasValue;
            if (GUI.Button(new Rect(440, 570, 400, 45), "고유화 [Enter]", menuButton)) game.ConfirmChoice();
            GUI.enabled = true;
            if (GUI.Button(new Rect(490, 625, 300, 40), "선택 취소 [Esc]", menuButton)) game.CancelChoice();
        }
        void PauseScreen()
        {
            Label(280, 245, 720, 55, "일시정지", title);
            SpriteArt(new Rect(460, 315, 360, 18), KarmaArtwork.Sprite("UI", "LongRule"));
            if (GUI.Button(new Rect(440, 350, 400, 55), "계속하기 [Esc]", menuButton)) game.TogglePause();
            if (GUI.Button(new Rect(440, 425, 400, 55), "처음부터 [R]", menuButton)) game.StartRun();
            if (GUI.Button(new Rect(440, 500, 400, 55), "타이틀 [T]", menuButton)) game.ReturnToTitle();
        }
        void StageTransitionScreen()
        {
            // A low caption keeps the actual actors and their transformation visible.
            Fill(new Rect(280, 578, 720, 112), new Color(0.015f, 0.025f, 0.07f, 0.72f));
            SpriteArt(new Rect(460, 578, 360, 12), KarmaArtwork.Sprite("UI", "LongRule"));
            string caption = game.GuardianInheriting
                ? "원형 계승 · " + (game.Enemies.Count > 0 ? game.Enemies[0].DisplayName : "다음 수호자")
                : "고유화 · " + names[(int)game.LastPersonalizedEssence] + " → " + KarmaPatternCatalog.UniqueName(game.LastPersonalizedEssence);
            Label(300, 594, 680, 35, caption, centered);
            Label(300, 638, 680, 28, game.GuardianInheriting
                ? "내려놓은 원형의 힘이 다음 수호자에게 이어집니다."
                : "배운 원형이 제자의 동작으로 깨어납니다.", centered);
        }
        void EndScreen(bool victory)
        {
            if (victory)
            {
                Label(280, 180, 720, 60, "시련의 끝", title);
                SpriteArt(new Rect(460, 255, 360, 18), KarmaArtwork.Sprite("UI", "LongRule"));
                Label(180, 295, 920, 32, "시간: " + Clock(game.Elapsed) + " · 누적 피해: " + game.DamageTaken.ToString("0"), centered);
                Label(180, 335, 920, 55, "고유화 순서: " + KarmaPatternCatalog.FusionSummary(game.Player.Essences.PersonalizationOrder), centered);
                Label(180, 395, 920, 80, "스승: 이제 그 기술들은 누구의 것이냐?\n제자: 배웠습니다. 하지만 이제는 제 방식으로 쓸 수 있습니다.", centered);
                if (GUI.Button(new Rect(420, 510, 440, 55), "처음부터 다시 도전 [R]", menuButton)) game.StartRun();
                if (GUI.Button(new Rect(420, 585, 440, 55), "타이틀 [T]", menuButton)) game.ReturnToTitle();
                return;
            }
            Label(280, 180, 720, 60, "시련 실패", title);
            SpriteArt(new Rect(460, 255, 360, 18), KarmaArtwork.Sprite("UI", "LongRule"));
            Label(180, 295, 920, 32, "시간: " + Clock(game.Elapsed) + " · 누적 피해: " + game.DamageTaken.ToString("0"), centered);
            Label(180, 335, 920, 55, "고유화 순서: " + KarmaPatternCatalog.FusionSummary(game.Player.Essences.PersonalizationOrder), centered);
            Label(180, 405, 920, 55, "원형과 고유화 순서를 초기화하고\n첫 번째 시련부터 다시 도전합니다.", centered);
            if (GUI.Button(new Rect(420, 510, 440, 55), "처음부터 다시 도전 [R]", menuButton)) game.StartRun();
            if (GUI.Button(new Rect(420, 585, 440, 55), "타이틀 [T]", menuButton)) game.ReturnToTitle();
        }
    }
}
