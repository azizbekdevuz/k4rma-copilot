using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KarmaPrototype
{
    /// <summary>무의식화 여부와 선택 순서. 시간·입력·씬·이펙트에 의존하지 않음.</summary>
    public sealed class KarmaProgressionState
    {
        readonly bool[] remnants = new bool[3];
        readonly List<Essence> order = new List<Essence>();
        readonly ReadOnlyCollection<Essence> view;
        public KarmaProgressionState() { view = order.AsReadOnly(); }
        public IReadOnlyList<Essence> Order { get { return view; } }
        public int Count { get { return order.Count; } }
        public Essence? Pending { get; private set; }
        public bool Preview(Essence essence)
        {
            if (!HasActive(essence)) return false;
            Pending = essence; return true;
        }
        public void Cancel() { Pending = null; }
        public bool Confirm(out Essence essence)
        {
            essence = Pending ?? Essence.SwordWave;
            if (!Pending.HasValue || !TryInternalize(essence)) return false;
            Pending = null; return true;
        }
        public bool HasRemnant(Essence essence) { return remnants[Index(essence)]; }
        public bool HasActive(Essence essence) { return !HasRemnant(essence); }
        public bool TryInternalize(Essence essence)
        {
            int index = Index(essence);
            if (remnants[index]) return false;
            remnants[index] = true;
            order.Add(essence);
            return true;
        }
        static int Index(Essence essence)
        {
            int index = (int)essence;
            if (index < 0 || index >= 3) throw new ArgumentOutOfRangeException(nameof(essence));
            return index;
        }
    }
}
