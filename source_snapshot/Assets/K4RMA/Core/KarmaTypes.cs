namespace KarmaPrototype
{
    // Essence values are serialized in existing assets; keep these IDs stable.
    public enum Essence { SwordWave = 0, RisingSlash = 1, Guard = 2 }
    public enum RunPhase { Title, Combat, Gate, Choosing, Transition, Victory, Defeat, Story, Controls, GuardianFallen, PlayerFallen }
    public enum EnemyAttack
    {
        GroundStrike = 0, SWBasic = 1,
        SW01 = 13, SW02, SW03, SW04, SW05,
        UP01, UP02, UP03, UP04, UP05,
        GD01, GD02, GD03, GD04, GD05
    }
}
