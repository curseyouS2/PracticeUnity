using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState gameState = new GameState();

    [Header("Data")]
    private LocationDataWrapper locationData;
    private EndingDataWrapper endingData;

    [Header("Managers")]
    public UIManager uiManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadGameData();
        InitializeGame();
    }

    private void LoadGameData()
    {
        // JSON 파일 로드
        TextAsset locationJson = Resources.Load<TextAsset>("Data/LocationData");
        TextAsset endingJson = Resources.Load<TextAsset>("Data/EndingData");

        locationData = JsonUtility.FromJson<LocationDataWrapper>(locationJson.text);
        endingData = JsonUtility.FromJson<EndingDataWrapper>(endingJson.text);
    }

    private void InitializeGame()
    {
        gameState = new GameState();
        uiManager.UpdateAllUI();
        ShowAvailableLocations();
    }

    public void ShowAvailableLocations()
    {
        List<Location> available = GetAvailableLocations();
        uiManager.DisplayLocations(available);
    }

    public List<Location> GetAvailableLocations()
    {
        return locationData.locations
            .Where(loc => loc.availableTime.Contains(gameState.currentTimeSlot))
            .ToList();
    }

    public void ExecuteActivity(string locationId, string activityId)
    {
        // 활동 찾기
        Location location = locationData.locations.Find(l => l.id == locationId);
        Activity activity = location.activities.Find(a => a.id == activityId);

        // 조건 체크
        if (!CheckRequirements(activity))
        {
            uiManager.ShowMessage("조건을 만족하지 못합니다!");
            return;
        }

        // 효율 계산
        float efficiency = 1.0f;
        if (gameState.stats.fatigue >= 70)
        {
            efficiency = 0.5f;
            uiManager.ShowWarning("너무 피곤합니다! 효율이 50% 감소합니다.");
        }

        // 스탯 변화 적용
        ApplyStatChanges(activity.statChanges, efficiency);
        gameState.stats.money -= activity.cost;

        // 피로도 한계 체크
        if (gameState.stats.fatigue >= 100)
        {
            gameState.conditions.isExhausted = true;
            uiManager.ShowMessage("한계입니다! 다음 턴은 반드시 휴식해야 합니다.");
        }

        // 결과 표시
        uiManager.ShowActivityResult(activity, efficiency);

        // 시간 진행
        AdvanceTime();
    }

    private bool CheckRequirements(Activity activity)
    {
        // 강제 휴식 체크
        if (gameState.conditions.isExhausted && activity.id != "sleep")
        {
            return false;
        }

        // 필요 스탯 체크
        if (activity.requiredStats != null)
        {
            foreach (var req in activity.requiredStats)
            {
                int currentValue = GetStatValue(req.Key);
                if (currentValue < req.Value)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void ApplyStatChanges(StatChanges changes, float efficiency)
    {
        gameState.stats.intelligence += Mathf.RoundToInt(changes.intelligence * efficiency);
        gameState.stats.charm += Mathf.RoundToInt(changes.charm * efficiency);
        gameState.stats.courage += Mathf.RoundToInt(changes.courage * efficiency);
        gameState.stats.fatigue += changes.fatigue;
        gameState.stats.money += changes.money;

        // 스탯 한계 설정
        gameState.stats.intelligence = Mathf.Clamp(gameState.stats.intelligence, 0, 999);
        gameState.stats.charm = Mathf.Clamp(gameState.stats.charm, 0, 999);
        gameState.stats.courage = Mathf.Clamp(gameState.stats.courage, 0, 999);
        gameState.stats.fatigue = Mathf.Clamp(gameState.stats.fatigue, 0, 100);
    }

    private int GetStatValue(string statName)
    {
        switch (statName)
        {
            case "intelligence": return gameState.stats.intelligence;
            case "charm": return gameState.stats.charm;
            case "courage": return gameState.stats.courage;
            case "money": return gameState.stats.money;
            default: return 0;
        }
    }

    private void AdvanceTime()
    {
        if (gameState.currentTimeSlot == "afternoon")
        {
            gameState.currentTimeSlot = "night";
            uiManager.ShowMessage("밤이 되었습니다.");
        }
        else
        {
            gameState.currentDay++;
            gameState.currentTimeSlot = "afternoon";

            // 자동 피로 회복
            gameState.stats.fatigue = Mathf.Max(0, gameState.stats.fatigue - 10);
            gameState.conditions.isExhausted = false;

            uiManager.ShowMessage($"Day {gameState.currentDay} 아침입니다!");

            // 게임 종료 체크
            if (gameState.currentDay > gameState.maxDays)
            {
                ShowEnding();
                return;
            }
        }

        uiManager.UpdateAllUI();
        ShowAvailableLocations();
    }

    private void ShowEnding()
    {
        // 우선순위 순으로 정렬
        var sortedEndings = endingData.endings.OrderBy(e => e.priority).ToList();

        foreach (var ending in sortedEndings)
        {
            if (CheckEndingConditions(ending))
            {
                uiManager.DisplayEnding(ending);
                return;
            }
        }

        // 기본 엔딩
        var defaultEnding = sortedEndings.Last();
        uiManager.DisplayEnding(defaultEnding);
    }

    private bool CheckEndingConditions(Ending ending)
    {
        if (ending.conditions == null || ending.conditions.Count == 0)
        {
            return true; // 조건 없는 엔딩 (기본 엔딩)
        }

        foreach (var condition in ending.conditions)
        {
            int currentValue = GetStatValue(condition.Key);
            if (currentValue < condition.Value)
            {
                return false;
            }
        }

        return true;
    }
}