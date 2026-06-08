namespace BecomingLegend
{
    public enum StatType
    {
        Strength,
        Swiftness,
        Vitality
    }

    public enum ClassType
    {
        Knight,
        AxeWarrior,
        Assassin
    }

    public enum ActorTeam
    {
        Player,
        Enemy,
        Neutral
    }

    public enum DamageType
    {
        Physical,
        Magical,
        True
    }

    public enum QuestState
    {
        Inactive,
        Active,
        Completed,
        Failed
    }

    public enum QuestObjectiveType
    {
        DefeatEnemies,
        Explore,
        Interact,
        TrainStat,
        CollectItems
    }

    public enum GameState
    {
        Boot,
        MainMenu,
        Playing,
        Paused,
        Combat,
        Dialogue,
        GameOver
    }
}
