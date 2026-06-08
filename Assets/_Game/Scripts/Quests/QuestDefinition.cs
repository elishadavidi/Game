using System.Collections.Generic;
using BecomingLegend;
using BecomingLegend.Core;
using UnityEngine;

namespace BecomingLegend.Quests
{
    [CreateAssetMenu(menuName = "Becoming Legend/Quests/Quest Definition", fileName = "NewQuest")]
    public class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string questName;
        [SerializeField] [TextArea(2, 5)] private string description;
        [SerializeField] private List<Objective> objectives = new();
        [SerializeField] private int xpReward;
        [SerializeField] private string nextQuestId;

        public string QuestName => questName;
        public string Description => description;
        public IReadOnlyList<Objective> Objectives => objectives;
        public int XPReward => xpReward;
        public QuestState State { get; private set; }

        public void SetState(QuestState newState)
        {
            State = newState;
        }

        [System.Serializable]
        public class Objective
        {
            public QuestObjectiveType type;
            public string targetId;
            public int requiredCount;
            public int currentCount;

            public bool IsComplete => currentCount >= requiredCount;
        }
    }
}
