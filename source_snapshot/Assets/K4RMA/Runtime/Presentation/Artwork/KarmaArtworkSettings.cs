using UnityEngine;

namespace KarmaPrototype
{
    [CreateAssetMenu(menuName = "K4RMA/이미지 설정", fileName = "KarmaArtworkSettings")]
    public sealed class KarmaArtworkSettings : ScriptableObject
    {
        [Header("개별 포즈 · VFX · UI · 소품 스프라이트")]
        public KarmaSpriteLibrary spriteLibrary;
        [Header("플레이어 동작 — Sprite를 연결하세요")]
        [InspectorName("대기")] public Sprite playerIdle;
        [InspectorName("이동 1 / 공중")] public Sprite playerMove1;
        [InspectorName("이동 2")] public Sprite playerMove2;
        [InspectorName("검 공격 1")] public Sprite playerSlash1;
        [InspectorName("검 공격 2")] public Sprite playerSlash2;
        [InspectorName("상승 공격")] public Sprite playerRising;
        [InspectorName("하강 공격")] public Sprite playerFalling;
        [InspectorName("피격")] public Sprite playerHurt;
        [InspectorName("사망")] public Sprite playerDeath;

        [Header("수호자 외형 — Sprite를 연결하세요")]
        [InspectorName("기본 수호자")] public Sprite neutralGuardian;
        [InspectorName("검 수호자")] public Sprite swordGuardian;
        [InspectorName("상승 수호자")] public Sprite risingGuardian;
        [InspectorName("결계 수호자")] public Sprite guardGuardian;

        [Header("스테이지 — Sprite를 연결하세요")]
        [InspectorName("도장 배경")] public Sprite dojoBackground;

        [InspectorName("실외 도장 배경")] public Sprite mountainBackground;
        [Header("좌우 스크롤용 파노라마")]
        [InspectorName("실외 파노라마")] public Sprite mountainPanorama;
        [InspectorName("도장 파노라마")] public Sprite dojoPanorama;

        [Header("Production UI / VFX")]
        public Texture2D menuBackground;
        public Sprite[] skillIcons;
        public Sprite selectionCrescent;
        public Sprite hitEffect;

        public Sprite[] PlayerFrames()
        {
            return new[] { playerIdle, playerMove1, playerMove2, playerSlash1, playerSlash2,
                playerRising, playerFalling, playerHurt, playerDeath };
        }

        public Sprite Guardian(Essence? type)
        {
            if (!type.HasValue && neutralGuardian != null) return neutralGuardian;
            return type == Essence.SwordWave ? swordGuardian : type == Essence.RisingSlash ? risingGuardian : guardGuardian;
        }

        public Texture2D Texture(string name) { return name == "MenuBackground" ? menuBackground : null; }
    }
}
