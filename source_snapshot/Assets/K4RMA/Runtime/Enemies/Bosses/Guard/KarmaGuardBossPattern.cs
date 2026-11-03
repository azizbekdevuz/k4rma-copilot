using UnityEngine;
namespace KarmaPrototype
{
    public sealed partial class KarmaEnemy
    {
        abstract class GuardBossPattern : BossPattern
        {
            protected KarmaGuardBossSettings Settings { get { return owner.Tuning.guard; } }
            protected GuardBossPattern(KarmaEnemy owner) : base(owner) { }
            protected void GuardDecoration(string sheet, string pose, Vector2 center, Vector2 size, Color color, int order = 7)
            {
                if (KarmaArtwork.Sprite(sheet, pose) == null) return;
                var sr = KarmaVisuals.Box(owner.effectsRoot, "청동 결계 장식 · 피해 없음", center, size, color, order);
                KarmaCombatVFX.ApplySprite(sr, sheet, pose, size);
            }
            protected void BreakShield()
            {
                owner.shieldUntil = owner.shieldWarningUntil = 0;
                owner.nextShield = owner.FightSeconds + Mathf.Max(2, Settings.autoShieldPeriod);
            }
            protected Vector2 WaveSize { get { return new Vector2(Settings.trialWaveWidth, Settings.trialWaveHeight); } }
        }
    }
}
