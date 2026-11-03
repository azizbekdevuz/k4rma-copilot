using System.Collections.Generic;
namespace KarmaPrototype
{
    public static class KarmaPatternCatalog
    {
        public const float GuardHeavyReach = 4;
        public static string Name(Essence e) { return e == Essence.SwordWave ? "검기" : e == Essence.RisingSlash ? "상승베기" : "방어"; }
        public static string UniqueName(Essence e) { return e == Essence.SwordWave ? "관통베기" : e == Essence.RisingSlash ? "도약베기" : "반격"; }
        public static Essence? Primary(IReadOnlyList<Essence> order) { return order.Count == 0 ? (Essence?)null : order[0]; }
        public static string TypeName(IReadOnlyList<Essence> order) { return order.Count == 0 ? "기본형" : Name(order[0]) + "형"; }
        public static bool IsUltimate(EnemyAttack a)
        { return a == EnemyAttack.SW05 || a == EnemyAttack.UP05 || a == EnemyAttack.GD05; }
        public static List<EnemyAttack> BuildCycle(IReadOnlyList<Essence> returned)
        {
            var result = new List<EnemyAttack>();
            var owned = new HashSet<Essence>(returned);
            if (returned.Count == 0) { result.Add(EnemyAttack.GroundStrike); return result; }
            switch (returned[0])
            {
                case Essence.SwordWave:
                    result.Add(EnemyAttack.SW01); result.Add(EnemyAttack.SW02);
                    if (owned.Contains(Essence.RisingSlash)) result.Add(EnemyAttack.SW03);
                    if (owned.Contains(Essence.Guard)) result.Add(EnemyAttack.SW04);
                    if (owned.Count == 3) result.Add(EnemyAttack.SW05);
                    break;
                case Essence.RisingSlash:
                    result.Add(EnemyAttack.GroundStrike);
                    result.Add(EnemyAttack.UP01); result.Add(EnemyAttack.UP02);
                    if (owned.Contains(Essence.SwordWave)) result.Add(EnemyAttack.UP03);
                    if (owned.Contains(Essence.Guard)) result.Add(EnemyAttack.UP04);
                    if (owned.Count == 3) result.Add(EnemyAttack.UP05);
                    break;
                case Essence.Guard:
                    // GD01 is a separate periodic defense, never an attack in this cycle.
                    result.Add(EnemyAttack.GroundStrike); result.Add(EnemyAttack.GD02);
                    if (owned.Contains(Essence.SwordWave)) result.Add(EnemyAttack.GD03);
                    if (owned.Contains(Essence.RisingSlash)) result.Add(EnemyAttack.GD04);
                    if (owned.Count == 3) result.Add(EnemyAttack.GD05);
                    break;
            }
            return result;
        }
        public static bool CanSelect(EnemyAttack attack, EnemyAttack previous, float distance,
            bool airborne, bool lineOfSight, bool shield, bool escapeRoute, bool ultimateReady)
        {
            if (IsUltimate(attack)) return ultimateReady && !IsUltimate(previous);
            if (attack == EnemyAttack.SW01 || attack == EnemyAttack.SW03) return distance >= 4;
            if (attack == EnemyAttack.SW02) return lineOfSight;
            if (attack == EnemyAttack.UP02) return previous != attack && (airborne || distance <= 8);
            if (attack == EnemyAttack.UP03 || attack == EnemyAttack.GD04) return escapeRoute;
            if (attack == EnemyAttack.GD02) return shield && distance <= GuardHeavyReach;
            return true;
        }
        public static string FusionSummary(IReadOnlyList<Essence> order)
        {
            var names = new List<string>(); foreach (var e in order) names.Add(Name(e));
            return names.Count == 0 ? "없음" : string.Join(" → ", names);
        }
    }
}
