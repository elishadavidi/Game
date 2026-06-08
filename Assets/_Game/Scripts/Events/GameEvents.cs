using BecomingLegend;
using BecomingLegend.Combat;

namespace BecomingLegend.Events
{
    public readonly struct StatChangedEvent
    {
        public readonly StatType StatType;
        public readonly float OldValue;
        public readonly float NewValue;
        public readonly object Source;

        public StatChangedEvent(StatType statType, float oldValue, float newValue, object source)
        {
            StatType = statType;
            OldValue = oldValue;
            NewValue = newValue;
            Source = source;
        }
    }

    public readonly struct DamageDealtEvent
    {
        public readonly DamageResult Damage;

        public DamageDealtEvent(DamageResult damage) => Damage = damage;
    }

    public readonly struct EntityDiedEvent
    {
        public readonly object Entity;

        public EntityDiedEvent(object entity) => Entity = entity;
    }

    public readonly struct LevelUpEvent
    {
        public readonly object Entity;
        public readonly int NewLevel;

        public LevelUpEvent(object entity, int newLevel)
        {
            Entity = entity;
            NewLevel = newLevel;
        }
    }

    public readonly struct QuestStateChangedEvent
    {
        public readonly object Quest;
        public readonly QuestState NewState;

        public QuestStateChangedEvent(object quest, QuestState newState)
        {
            Quest = quest;
            NewState = newState;
        }
    }

    public readonly struct TrainingLoggedEvent
    {
        public readonly string ActivityName;
        public readonly float Intensity;
        public readonly StatType PrimaryStat;
        public readonly float StatGain;

        public TrainingLoggedEvent(string activityName, float intensity, StatType primaryStat, float statGain)
        {
            ActivityName = activityName;
            Intensity = intensity;
            PrimaryStat = primaryStat;
            StatGain = statGain;
        }
    }

    public readonly struct GameStateChangedEvent
    {
        public readonly GameState PreviousState;
        public readonly GameState NewState;

        public GameStateChangedEvent(GameState previous, GameState newState)
        {
            PreviousState = previous;
            NewState = newState;
        }
    }
}
