using BecomingLegend;
using BecomingLegend.Combat;
using BecomingLegend.Quests;
using BecomingLegend.Training;
using UnityEngine;

namespace BecomingLegend.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private TrainingManager trainingManager;

        public GameState CurrentState { get; private set; } = GameState.Boot;
        public static GameManager Instance { get; private set; }

        public CombatManager Combat => combatManager;
        public QuestManager Quests => questManager;
        public TrainingManager Training => trainingManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            if (combatManager == null) combatManager = GetComponent<CombatManager>();
            if (combatManager == null) combatManager = gameObject.AddComponent<CombatManager>();
            if (questManager == null) questManager = GetComponent<QuestManager>();
            if (trainingManager == null) trainingManager = GetComponent<TrainingManager>();
            CurrentState = GameState.MainMenu;
        }

        public void StartGame()
        {
            CurrentState = GameState.Playing;
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
