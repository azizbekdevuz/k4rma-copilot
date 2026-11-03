using System;
using UnityEngine;
namespace KarmaPrototype
{
    // Independent tuning for this boss. Shared combat rules read the balance scales through BossBehavior.
    [Serializable]
    public sealed class KarmaSwordBossSettings : KarmaBossBalance
    {
        [Header("검기형 · 초기 밸런스")]
        // Only the idle between completed regular patterns; warnings/recovery stay unchanged.
        [Range(0.25f, 1.5f)] public float swordDecisionMultiplier = 0.75f;
        [Min(0.05f)] public float swordSelectionRetry = 0.1f;
        [Min(0.5f)] public float swordBasicCooldown = 2.4f;
        [Tooltip("World-unit spacing for SW01/SW02. Basic waves remain limited to ten units when the player closes in.")]
        [Range(9, 14)] public float swordPreferredDistance = 11.75f;
        [Tooltip("Completed sword skills between priority basic attacks. Cooldown and actual reach still apply.")]
        [Range(1, 6)] public int swordSkillsBetweenBasics = 1;
        [Min(4.5f)] public float swordRetreatCooldown = 5;
        [Header("검기형 · 거리 유지 (월드 단위)")]
        [Range(5.5f, 6.5f)] public float swordRetreatStartDistance = 6.25f;
        [Range(9, 14)] public float swordRetreatStopDistance = 11.75f;
        [Range(2, 18)] public float swordRetreatDistance = 16;
        [Range(0.35f, 0.75f)] public float swordRetreatDuration = 0.6f;
        [Min(1)] public float swordRetreatSpeed = 32;
        [Range(0.25f, 1.25f)] public float swordMeleeSpacingMargin = 0.65f;
        [Tooltip("이 거리에서 접근을 시작하고 선호 교전 거리까지 이동한 뒤 공격합니다.")]
        [Range(10, 12)] public float swordApproachStartDistance = 10.5f;
        [Header("검기형 · 공격")]
        [Min(0.1f)] public float basicDamageMultiplier = 0.5f, aimedDamageMultiplier = 1.15f;
        [Min(1)] public float swordSpeed = 16 * KarmaSpace.Scale;
        [Min(0.2f)] public float swordLifetime = 1.65f;
        [Min(0.1f)] public float swordWidth = 1.2f * KarmaSpace.Scale, swordHeight = 0.5f * KarmaSpace.Scale;
        [Min(0.3f)] public float counterWarning = 0.45f;
        [Min(0.6f)] public float swordGuardDuration = 3;
        [Min(0.5f)] public float swordGuardResponseCooldown = 0.8f;
        [Header("SW05 · 체력 50% / 25% · 검 소나기 3연격")]
        [Tooltip("일반 공격 대비 검 소나기 명중 피해 배율")]
        [Min(1)] public float aerialSwordDamageMultiplier = 2.5f;
        [Tooltip("회차별 무작위 검 수. 매회 이동 가능한 회피 경로를 확보합니다.")]
        [Range(1, 18)] public int aerialSwordVolleyMin = 16, aerialSwordVolleyMax = 18;
        [Tooltip("0: 매 공격마다 무작위, 그 외: 동일 물리 상태에서 재현 가능한 시드")]
        public int aerialSwordSeed;
    }
}
