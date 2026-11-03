using System.Collections;
using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        // Pattern execution and pattern-local state. Shared damage and cancellation stay in the host.
        sealed class GD01Pattern : GuardBossPattern
        {
            public GD01Pattern(KarmaEnemy owner) : base(owner) { }
            public void Tick()
            {
                if (owner.PrimaryType != Essence.Guard || owner.FightSeconds < owner.shieldUntil || owner.trial.Active || owner.Stunned || owner.state == ActionState.Recover || owner.frontalGuard) return;
                if (owner.shieldWarningUntil > 0)
                {
                    if (owner.FightSeconds >= owner.shieldWarningUntil) RefreshAutoShield();
                    return;
                }
                // Begin between attacks so an in-flight recovery cannot erase the new shield.
                if (!owner.running && owner.FightSeconds >= owner.nextShield && owner.FightSeconds >= owner.shieldUntil)
                { owner.shieldWarningUntil = owner.FightSeconds + Mathf.Max(0.3f, Settings.autoShieldWarning); }
                if (owner.shieldWarningUntil > 0)
                KarmaVisuals.Flash(owner.effectsRoot, owner.Position, KarmaSpace.Units(new Vector2(1.8f, 2.6f)), new Color(1, 0.85f, 0.2f, 0.3f), 0.25f);
            }
            public void RefreshAutoShield()
            {
                owner.shieldUntil = float.PositiveInfinity;
                owner.shieldWarningUntil = 0;
            }
            public override IEnumerator Execute() { yield break; }

        }
    }
}
