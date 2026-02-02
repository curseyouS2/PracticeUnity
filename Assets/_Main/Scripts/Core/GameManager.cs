using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 게임 전체 흐름 관리 (초기화, 엔딩)
/// 세부 로직은 StatusManager, RoutineManager에 위임
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private StatusManager statusManager;
    [SerializeField] private RoutineManager routineManager;
    [SerializeField] private UIManager uiManager;

    [Header("Ending Data")]
    [SerializeField] private List<EndingSO> endings;

    [Header("Game State")]
    public GameState gameState = new GameState();

    public StatusManager Status => statusManager;
    public RoutineManager Routine => routineManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        // 매니저 초기화
        statusManager.Initialize(gameState);
        routineManager.Initialize(gameState.currentDay);

        // 이벤트 연결
        SubscribeToEvents();

        // UI 초기화
        uiManager.Initialize();
        uiManager.UpdateAllUI();
        ShowAvailableLocations();
    }

    private void SubscribeToEvents()
    {
        // StatusManager 이벤트
        statusManager.OnStatusUpdated += () => uiManager.UpdateAllUI();

        // RoutineManager 이벤트
        routineManager.OnTimeChanged += OnTimeChanged;
        routineManager.OnDayChanged += OnDayChanged;
        routineManager.OnGameEnd += ShowEnding;
        routineManager.OnActivityExecuted += OnActivityExecuted;
    }

    private void OnTimeChanged(int currentTimeMinutes)
    {
        uiManager.UpdateAllUI();
        ShowAvailableLocations();
    }

    private void OnDayChanged(int newDay)
    {
        uiManager.ShowMessage($"Day {newDay} 아침입니다!");
        SyncGameState();
    }

    private void OnActivityExecuted(ActivitySO activity, float efficiency)
    {
        uiManager.ShowActivityResult(activity, efficiency);
        SyncGameState();
    }

    public void ShowAvailableLocations()
    {
        var locations = routineManager.GetAvailableLocations();
        uiManager.DisplayLocations(locations);
    }

    /// <summary>
    /// 활동 실행 (UIManager에서 호출)
    /// </summary>
    public void ExecuteActivity(LocationSO location, ActivitySO activity)
    {
        // 실행 가능 여부 체크
        if (!routineManager.CanExecuteActivity(activity))
        {
            string reason = routineManager.GetActivityBlockReason(activity);
            uiManager.ShowMessage(reason);
            return;
        }

        // 효율 경고
        float efficiency = statusManager.CalculateTotalEfficiency();
        if (efficiency < 1.0f)
        {
            uiManager.ShowWarning($"컨디션이 좋지 않습니다! 효율이 {efficiency * 100:F0}%입니다.");
        }

        // 활동 실행
        routineManager.ExecuteActivity(location, activity);
    }

    private void ShowEnding()
    {
        if (endings == null || endings.Count == 0)
        {
            Debug.LogWarning("No endings configured!");
            return;
        }

        // 우선순위 순 정렬
        var sortedEndings = endings.OrderBy(e => e.priority).ToList();

        foreach (var ending in sortedEndings)
        {
            if (CheckEndingConditions(ending))
            {
                uiManager.DisplayEnding(ending);
                return;
            }
        }

        // 기본 엔딩
        uiManager.DisplayEnding(sortedEndings.Last());
    }

    private bool CheckEndingConditions(EndingSO ending)
    {
        if (ending.conditions == null || ending.conditions.Count == 0)
        {
            return true;
        }

        foreach (var condition in ending.conditions)
        {
            int currentValue = statusManager.GetStatValue(condition.statType);
            if (currentValue < condition.minValue)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// GameState 동기화 (저장/로드용)
    /// </summary>
    private void SyncGameState()
    {
        gameState.currentDay = routineManager.CurrentDay;
        gameState.condition = statusManager.Condition.CurrentCondition;
    }

    public void RestartGame()
    {
        gameState = new GameState();
        InitializeGame();
    }
}
