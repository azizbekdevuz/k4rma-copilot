using UnityEngine;

namespace KarmaPrototype
{
    [CreateAssetMenu(menuName = "K4RMA/게임 설정", fileName = "KarmaConfig")]
    public sealed class KarmaConfig : ScriptableObject
    {
        [Header("보스전 난이도 — 실제 보스 체력은 이 프로필 사용")]
        [InspectorName("보스 패턴·난이도 설정")] public KarmaBossTuning bossTuning = new KarmaBossTuning();
        public KarmaBossTuning BossTuning
        {
            get { if (bossTuning == null) bossTuning = new KarmaBossTuning(); return bossTuning; }
        }
        [Header("플레이어")]
        [Min(1)] [InspectorName("플레이어 최대 체력")] public float playerHealth = 160;
        // Match the full-arena camera: twice the travel, with the same jump airtime.
        [Min(1)] [InspectorName("이동 속도")] public float moveSpeed = 7 * KarmaSpace.Scale;
        [Min(1)] [InspectorName("점프 속도")] public float jumpSpeed = 13 * KarmaSpace.Scale;
        [Min(0.1f)] [InspectorName("중력 배율")] public float gravityScale = 3 * KarmaSpace.Scale;
        [Min(0)] [InspectorName("피격 후 무적 시간")] public float hitInvulnerability = 0.7f;
        [Header("검 공격")]
        [Min(1)] [InspectorName("검 공격력")] public float swordDamage = 60;
        [Min(0.1f)] [InspectorName("검 공격 간격")] public float swordCooldown = 0.32f;
        [Header("액티브 기술")]
        [Min(0.1f)] [InspectorName("검기 쿨타임")] public float swordWaveCooldown = 2.2f;
        [Min(1)] [InspectorName("검기 피해")] public float swordWaveDamage = 72;
        [Min(1)] [InspectorName("검기 속도")] public float swordWaveSpeed = 14 * KarmaSpace.Scale;
        [Min(0.1f)] [InspectorName("상승베기 쿨타임")] public float risingSlashCooldown = 1.6f;
        [Min(0.1f)] [InspectorName("보호막 쿨타임")] public float guardCooldown = 5;
        [Min(0.1f)] [InspectorName("보호막 지속 시간")] public float guardDuration = 1.5f;
        [Header("원형·고유화 기술 조정")]
        public KarmaTechniqueTuning techniques = new KarmaTechniqueTuning();
        public KarmaTechniqueTuning Techniques { get { if (techniques == null) techniques = new KarmaTechniqueTuning(); return techniques; } }
        [Header("적 기본 능력 — 난이도 프로필의 배율 적용")]
        [Min(1)] [InspectorName("적 공격력")] public float enemyDamage = 14;
        [Min(0.1f)] [InspectorName("적 이동 속도")] public float enemyMoveSpeed = 2.4f * KarmaSpace.Scale;
        [Min(0.2f)] [InspectorName("적 공격 선택 간격")] public float enemyDecisionDelay = 1.2f;
        [Min(0.2f)] [InspectorName("기본 공격 예고 시간")] public float enemyTelegraphTime = 0.75f;
        [Min(0)] [InspectorName("고유화 후 체력 회복")] public float healAfterPersonalization = 45;
        [Header("화면 표시")]
        [InspectorName("한글 UI 폰트 (비우면 자동)")] public Font koreanFont;
        [InspectorName("2D 표시 재질 (빌드에 포함)")] public Material spriteMaterial;
        [InspectorName("검기 색상")] public Color swordWaveColor = new Color(1, 0.38f, 0.18f);
        [InspectorName("상승베기 색상")] public Color risingSlashColor = new Color(0.25f, 0.9f, 0.9f);
        [InspectorName("보호막 색상")] public Color guardColor = new Color(0.67f, 0.48f, 1);
        public Color ColorOf(Essence essence)
        {
            return essence == Essence.SwordWave ? swordWaveColor : essence == Essence.RisingSlash ? risingSlashColor : guardColor;
        }
    }
}
