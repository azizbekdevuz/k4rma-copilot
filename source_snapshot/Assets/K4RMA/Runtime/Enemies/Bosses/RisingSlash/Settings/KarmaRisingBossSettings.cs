using System;
using UnityEngine;
namespace KarmaPrototype
{
    // Independent tuning for this boss. Shared combat rules read the balance scales through BossBehavior.
    [Serializable]
    public sealed class KarmaRisingBossSettings : KarmaBossBalance
    {
        [Header("상승베기형")]
        [Min(0.1f)] public float preferredDistance = 2.05f * KarmaSpace.Scale, meleeReach = 4.1f * KarmaSpace.Scale;
        [Min(0.1f)] public float approachSpeedMultiplier = 1.55f, attackDecisionMultiplier = 0.58f;
        [Min(0.1f)] public float meleeDamageMultiplier = 0.8f, landingDamageMultiplier = 1, waveDamageMultiplier = 1.15f, fragmentDamageMultiplier = 0.85f;
        [Min(0.1f)] public float fragmentLifetime = 0.92f, fragmentParryStunSeconds = 2;

        [Min(1)] public float riseHeight = 4 * KarmaSpace.Scale;
        [Min(0.5f)] public float groundBladeWidth = 5 * KarmaSpace.Scale, groundBladeHeight = 6 * KarmaSpace.Scale;
        [Min(0.4f)] public float crossingWarning = 0.65f, crossingReset = 0.65f;
    }
}
