using System.Collections.Generic;
using UnityEngine;

namespace KarmaPrototype
{
    // Unity facade: persistent progression lives in Core/KarmaProgressionState.
    // The director asks this component to personalize an essence.
    public sealed class KarmaEssences : MonoBehaviour, ISerializationCallbackReceiver
    {
        KarmaProgressionState progression = new KarmaProgressionState();
        [SerializeField, HideInInspector] List<Essence> acquiredOrder = new List<Essence>();
        public void OnBeforeSerialize()
        {
            acquiredOrder.Clear();
            foreach (var essence in progression.Order) acquiredOrder.Add(essence);
        }
        public void OnAfterDeserialize()
        {
            progression = new KarmaProgressionState();
            if (acquiredOrder == null) acquiredOrder = new List<Essence>();
            foreach (var essence in acquiredOrder) progression.TryInternalize(essence);
        }
        readonly float[] readyAt = new float[3];
        KarmaPlayer player;
        KarmaGame game;
        readonly float[] lastProc = { -100, -100, -100 };
        public void NotifyUnique(Essence e)
        {
            lastProc[(int)e] = Time.time;
            KarmaRemnantVFX.Proc(game, player.Position, player.Facing, e);
        }
        public bool JustTriggered(Essence e) { return Time.time - lastProc[(int)e] < 0.7f; }
        public IReadOnlyList<Essence> PersonalizationOrder { get { return progression.Order; } }
        public int Count { get { return progression.Count; } }
        public Essence? Pending { get { return progression.Pending; } }
        public bool Preview(Essence e) { return progression.Preview(e); }
        public void Cancel() { progression.Cancel(); }
        public bool Confirm(out Essence e) { return progression.Confirm(out e); }
        public bool HasRemnant(Essence e) { return progression.HasRemnant(e); }
        public bool HasActive(Essence e) { return !HasRemnant(e); }
        public float Cooldown(Essence e) { return Mathf.Max(0, readyAt[(int)e] - Time.time); }
        public void Initialize(KarmaPlayer owner, KarmaGame director) { player = owner; game = director; }
        public void ResetTemporaryState()
        {
            for (int i = 0; i < readyAt.Length; i++) readyAt[i] = 0;
            for (int i = 0; i < lastProc.Length; i++) lastProc[i] = -100;
        }

        public void TryUse(Essence e)
        {
            if (!game.IsCombat || player.Stunned || !HasActive(e) || Cooldown(e) > 0) return;
            var c = game.Config;
            switch (e)
            {
                case Essence.SwordWave:
                    readyAt[0] = Time.time + c.swordWaveCooldown;
                    player.BeginSwordWave();
                    KarmaProjectile.Spawn(game, player.Position + Vector2.right * player.Facing * KarmaSpace.Units(0.7f),
                        Vector2.right * player.Facing * c.swordWaveSpeed, c.swordWaveDamage, c.swordWaveColor, KarmaSpace.Units(0.25f));
                    break;
                case Essence.RisingSlash:
                    readyAt[1] = Time.time + c.risingSlashCooldown;
                    player.BeginRisingSlash(); break;
                case Essence.Guard:
                    readyAt[2] = Time.time + c.guardCooldown;
                    player.BeginGuard(c.guardDuration); break;
            }
        }
        public void TryCounter()
        {
            if (!game.IsCombat || player.Stunned || !HasRemnant(Essence.Guard)) return;
            bool queued = false;
            foreach (var enemy in game.Enemies)
                if (enemy != null && enemy.TryQueueHeavyParry()) queued = true;
            // A visible GD02 tell accepts K throughout its window, even after an early normal counter.
            if (!queued && Cooldown(Essence.Guard) > 0) return;
            readyAt[2] = Time.time + game.Config.Techniques.counterCooldown;
            player.BeginCounter();
        }
    }
}
