using System;
using UnityEngine;
namespace KarmaPrototype
{
    [Serializable]
    public sealed class KarmaBossTuning
    {
        [Header("보스 공통")]
        [Min(1)] public float stageOneHealth = 1050, stageTwoHealth = 1550, stageThreeHealth = 2050, finalHealth = 2800;
        [Min(0.05f)] public float healthMultiplier = 1;
        [Min(0.1f)] public float damageMultiplier = 1.15f, moveMultiplier = 1.7f, decisionMultiplier = 0.42f;
        [Range(0.4f, 2)] public float recoveryMultiplier = 0.72f;
        [Header("체력 절반 이하 · 강화 페이즈")]
        [Range(0.1f, 0.8f)] public float enrageThreshold = 0.5f;
        [Range(0.65f, 1)] public float enrageTempo = 0.82f;
        [Range(0.65f, 1.5f)] public float warningMultiplier = 0.9f;
        [Min(10)] public float ultimateCooldown = 20;
        [Min(5)] public float patternTimeout = 30;
        [Header("보스별 독립 설정")]
        public KarmaSwordBossSettings sword = new KarmaSwordBossSettings();
        public KarmaRisingBossSettings rising = new KarmaRisingBossSettings();
        public KarmaGuardBossSettings guard = new KarmaGuardBossSettings();
        public float HealthFor(int stage)
        {
            float hp = stage == 0 ? stageOneHealth : stage == 1 ? stageTwoHealth : stage == 2 ? stageThreeHealth : finalHealth;
            return Mathf.Max(1, hp * Mathf.Max(0.05f, healthMultiplier));
        }
    }
}
