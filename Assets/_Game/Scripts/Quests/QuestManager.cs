using System.Collections.Generic;
using BecomingLegend;
using BecomingLegend.Core;
using UnityEngine;

namespace BecomingLegend.Quests
{
    public class QuestManager : MonoBehaviour
    {
        private readonly List<QuestDefinition> activeQuests = new();
        private readonly List<QuestDefinition> completedQuests = new();

        public IReadOnlyList<QuestDefinition> ActiveQuests => activeQuests;
        public IReadOnlyList<QuestDefinition> CompletedQuests => completedQuests;

        public void ActivateQuest(QuestDefinition quest)
        {
            if (activeQuests.Contains(quest)) return;
            quest.SetState(QuestState.Active);
            activeQuests.Add(quest);
        }

        public void CompleteQuest(QuestDefinition quest)
        {
            if (!activeQuests.Contains(quest)) return;
            quest.SetState(QuestState.Completed);
            activeQuests.Remove(quest);
            completedQuests.Add(quest);
        }

        public bool IsQuestActive(QuestDefinition quest)
        {
            return activeQuests.Contains(quest);
        }

        public bool IsQuestCompleted(QuestDefinition quest)
        {
            return completedQuests.Contains(quest);
        }
    }
}
