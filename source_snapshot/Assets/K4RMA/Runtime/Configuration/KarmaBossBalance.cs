using System;
using UnityEngine;
namespace KarmaPrototype
{
    [Serializable]
    public class KarmaBossBalance
    {
        [Range(0, 1)] public float shieldDamageMultiplier = 0.3f;
        [Min(0.05f)] public float damageScale = 1;
        [Min(0.05f)] public float movementScale = 1;
        [Min(0.05f)] public float decisionScale = 1;
        [Min(0.05f)] public float recoveryScale = 1;
    }
}
