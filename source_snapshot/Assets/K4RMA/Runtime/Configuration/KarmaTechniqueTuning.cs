using System;
using UnityEngine;
namespace KarmaPrototype
{
    [Serializable]
    public sealed class KarmaTechniqueTuning
    {
        [Header("검 타격 연출")]
        [Min(0.5f)] public float longSlashReach = 3.4f * KarmaSpace.Scale;
        [Min(0.5f)] public float longSlashHeight = 2.6f * KarmaSpace.Scale;
        [Min(0.01f)] public float slashLife = 0.18f;
        [Min(0)] public float hitStopSeconds = 0.035f;
        [Min(0)] public float hitShakeSeconds = 0.09f;
        [Min(0)] public float hitShakeStrength = 0.075f * KarmaSpace.Scale;
        [Header("고유화 기술")]
        [Min(0.05f)] public float doubleTapWindow = 0.25f;
        [Min(0.1f)] public float pierceDistance = 4 * KarmaSpace.Scale;
        [Tooltip("관통베기로 실제 이동한 순간부터 적용하는 짧은 피격 무적 시간")]
        [Range(0, 0.2f)] public float pierceInvulnerabilitySeconds = 0.1f;
        [Min(0.1f)] public float airSlashDistance = 1.2f * KarmaSpace.Scale;
        [Min(0.02f)] public float airSlashDuration = 0.12f;
        [Min(0.02f)] public float airSlashRecovery = 0.16f;
        [Min(0.02f)] public float airSlashInputBuffer = 0.16f;
        [Min(0.1f)] public float airSlashJumpScale = 0.9f;
        [Min(0)] public float airSlashHorizontalBoost = 1.5f;
        [Min(0.05f)] public float counterWindow = 0.18f;
        [Min(0.2f)] public float counterCooldown = 1.1f;
        [Min(1)] public float counterDamage = 85;
        [Min(1)] public float risingDamage = 60;
        [Min(1)] public float risingSpeed = 15 * KarmaSpace.Scale;
    }
}
