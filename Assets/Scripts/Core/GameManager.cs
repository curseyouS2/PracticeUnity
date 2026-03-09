using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UGS;
/// <summary>
/// 게임 전체 흐름 관리 (초기화, 엔딩)
/// ActivityService를 통해 활동 실행, 각 매니저에 세부 로직 위임
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private StatusManager statusManager;
    [SerializeField] private RoutineManager routineManager;
    [SerializeField] private LocationManager locationManager;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private UIManager uiManager;

    [Header("Ending Data")]
    [SerializeField] private List<EndingSO> endings;

    public ActivityService ActivityService { get; private set; }
    private UIRefreshScheduler refreshScheduler;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        UnityGoogleSheet.LoadAllData();
    }

    private void Start()
    {
        InitializeGame();
    }

    /// <summary>
    /// 게임 시작 - 세이브가 있으면 불러오기, 없으면 새 플레이어 생성
    /// </summary>
    private void InitializeGame()
    {
        Player player = SaveLoadManager.Load() ?? new Player();
        StartWithPlayer(player);
    }

    /// <summary>
    /// Player 데이터를 기반으로 게임 시작
    /// </summary>
    private void StartWithPlayer(Player player)
    {
        // 매니저 참조가 없으면 자동 탐색
        statusManager ??= FindAnyObjectByType<StatusManager>();
        routineManager ??= FindAnyObjectByType<RoutineManager>();
        locationManager ??= FindAnyObjectByType<LocationManager>();
        dialogueManager ??= FindAnyObjectByType<DialogueManager>();
        uiManager ??= FindAnyObjectByType<UIManager>();

        // 엔딩 데이터가 없으면 Resources에서 자동 로드
        if (endings == null || endings.Count == 0)
        {
            endings = new List<EndingSO>(Resources.LoadAll<EndingSO>("Endings"));
            Debug.Log($"[GameManager] Resources에서 {endings.Count}개의 엔딩 자동 로드");
        }

        // 매니저 초기화 (Player 데이터 분배)
        if (statusManager != null)
            statusManager.Initialize(player);
        else
            Debug.LogError("[GameManager] StatusManager를 찾을 수 없습니다!");

        if (routineManager != null)
            routineManager.Initialize(player.gameState.currentDay, player.gameState.currentTime);
        else
            Debug.LogError("[GameManager] RoutineManager를 찾을 수 없습니다!");

        if (locationManager != null)
            locationManager.Initialize(player.gameState.currentLocationId);
        else
            Debug.LogError("[GameManager] LocationManager를 찾을 수 없습니다!");

        // ActivityService 생성 (DI)
        if (statusManager != null && routineManager != null)
            ActivityService = new ActivityService(statusManager, routineManager);

        // UIRefreshScheduler 설정
        refreshScheduler = GetComponent<UIRefreshScheduler>();
        if (refreshScheduler == null)
            refreshScheduler = gameObject.AddComponent<UIRefreshScheduler>();
        refreshScheduler.Initialize(() =>
        {
            uiManager?.UpdateAllUI();
            if (locationManager != null && locationManager.NowLocation != null)
                uiManager?.SelectCurrentLocation();
            else
                ShowAvailableLocations();
        });

        // 이벤트 연결
        SubscribeToEvents();

        // UI 초기화
        if (uiManager != null)
        {
            uiManager.Initialize();
            uiManager.UpdateAllUI();
        }
        else
        {
            Debug.LogWarning("[GameManager] UIManager를 찾을 수 없습니다.");
        }

        // 현재 위치가 있으면 바로 해당 장소 표시, 없으면 전체 장소 목록
        if (locationManager != null && locationManager.NowLocation != null)
            uiManager?.SelectCurrentLocation();
        else
            ShowAvailableLocations();

        Debug.Log($"[GameManager] 게임 초기화 완료 - Day {routineManager?.CurrentDay}, " +
                  $"이용 가능 장소 {locationManager?.GetAvailableLocations(routineManager?.CurrentTimeMinutes ?? 0)?.Count ?? 0}개");
    }

    private void SubscribeToEvents()
    {
        // 모든 상태 변경 → 스케줄러를 통해 프레임당 1회 갱신
        if (statusManager != null)
            statusManager.OnStatusUpdated += () => refreshScheduler.RequestRefresh();

        // LocationManager 이벤트
        if (locationManager != null)
            locationManager.OnLocationChanged += _ => refreshScheduler.RequestRefresh();

        // RoutineManager 이벤트
        if (routineManager != null)
        {
            routineManager.OnTimeChanged += _ => refreshScheduler.RequestRefresh();
            routineManager.OnDayChanged += OnDayChanged;
            routineManager.OnGameEnd += ShowEnding;
            routineManager.OnDayEndProcessing += _ => statusManager?.ProcessDayEnd();
        }

        // ActivityService 이벤트
        if (ActivityService != null)
        {
            ActivityService.OnActivityExecuted += OnActivityExecuted;
        }

        // DialogueManager 이벤트 (게임 상태 변경 요청 처리)
        if (dialogueManager != null)
        {
            dialogueManager.OnAffectionChangeRequested += (charId, amount) =>
                statusManager?.Relationship.AddAffection(charId, amount);

            dialogueManager.OnStatChangeRequested += (changes) =>
                statusManager?.ApplyActivityEffect(changes);

            dialogueManager.OnDialogueViewedRequested += (dialogueId) =>
                statusManager?.Relationship.MarkDialogueViewed(dialogueId);

            dialogueManager.OnTimeAdvanceRequested += (minutes) =>
                routineManager?.AdvanceTime(minutes);
        }
    }

    private void OnDayChanged(int newDay)
    {
        uiManager?.ShowMessage($"Day {newDay} 아침입니다!");
    }

    private void OnActivityExecuted(DataTable.ActivityTable activity, float efficiency)
    {
        uiManager?.ShowActivityResult(activity, efficiency);
    }

    public void ShowAvailableLocations()
    {
        if (locationManager == null || routineManager == null || uiManager == null) return;

        var locations = locationManager.GetAvailableLocations(routineManager.CurrentTimeMinutes);
        uiManager.DisplayLocations(locations);
    }

    /// <summary>
    /// 활동 실행 (UIManager에서 호출)
    /// </summary>
    public void ExecuteActivity(DataTable.LocationTable location, DataTable.ActivityTable activity)
    {
        if (ActivityService == null) return;

        var (success, message) = ActivityService.ExecuteActivity(location, activity);
        if (!success)
        {
            uiManager?.ShowMessage(message);
        }
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

    #region Save / Load

    /// <summary>
    /// 현재 상태를 Player 객체로 수집
    /// </summary>
    public Player CollectPlayerData()
    {
        var player = new Player();

        statusManager?.ExportToPlayer(player);

        if (routineManager != null)
        {
            player.gameState.currentDay = routineManager.CurrentDay;
            player.gameState.currentTime = routineManager.CurrentTimeMinutes;
        }

        if (locationManager != null)
        {
            player.gameState.currentLocationId = locationManager.GetCurrentLocationId();
        }

        return player;
    }

    /// <summary>
    /// 게임 저장
    /// </summary>
    public void SaveGame()
    {
        var player = CollectPlayerData();
        if (SaveLoadManager.Save(player))
            uiManager?.ShowMessage("저장 완료!");
        else
            uiManager?.ShowMessage("저장에 실패했습니다.");
    }

    /// <summary>
    /// 세이브 파일에서 불러오기
    /// </summary>
    public void LoadGame()
    {
        var player = SaveLoadManager.Load();
        if (player != null)
        {
            StartWithPlayer(player);
            uiManager?.ShowMessage($"불러오기 완료! (Day {player.gameState.currentDay})");
        }
        else
        {
            uiManager?.ShowMessage("세이브 데이터가 없습니다.");
        }
    }

    /// <summary>
    /// 새 게임 시작
    /// </summary>
    public void NewGame()
    {
        SaveLoadManager.DeleteSave();
        StartWithPlayer(new Player());
    }

    /// <summary>
    /// 게임 재시작 (현재 세이브 무시)
    /// </summary>
    public void RestartGame()
    {
        StartWithPlayer(new Player());
    }

    #endregion
}
