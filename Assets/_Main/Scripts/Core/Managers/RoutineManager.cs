using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 일과 스케줄 전체 관리
/// TimeController로 시간 관리 위임, 장소/활동 실행 담당
/// </summary>
public class RoutineManager : MonoBehaviour
{
    public static RoutineManager Instance { get; private set; }

    public event Action OnGameEnd;
    public event Action<ActivitySO, float> OnActivityExecuted;

    [Header("Location Data")]
    [SerializeField] private List<LocationSO> locations;

    [Header("Time Settings")]
    [SerializeField] private int startTimeMinutes = 960;      // 16:00 (방과후)
    [SerializeField] private int sleepTimeMinutes = 1440;     // 24:00 (자정)
    [SerializeField] private int maxDays = 30;

    public TimeController Time { get; private set; }

    // Facade 프로퍼티 (기존 API 유지)
    public int CurrentDay => Time.CurrentDay;
    public int CurrentTimeMinutes => Time.CurrentTimeMinutes;
    public int MaxDays => Time.MaxDays;
    public bool IsGameOver => Time.IsGameOver;
    public string CurrentTimeString => Time.CurrentTimeString;
    public DayOfWeek CurrentDayOfWeek => Time.CurrentDayOfWeek;

    // 이벤트 전달
    public event Action<int> OnTimeChanged
    {
        add => Time.OnTimeChanged += value;
        remove => Time.OnTimeChanged -= value;
    }

    public event Action<int> OnDayChanged
    {
        add => Time.OnDayChanged += value;
        remove => Time.OnDayChanged -= value;
    }

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

    public void Initialize(int startDay = 1, int? startTime = null)
    {
        Time = new TimeController(
            startDay,
            startTime ?? startTimeMinutes,
            sleepTimeMinutes,
            maxDays
        );

        // 게임 종료 체크를 위한 이벤트 연결
        Time.OnDayChanged += CheckGameEnd;
    }

    private void OnDestroy()
    {
        if (Time != null)
        {
            Time.OnDayChanged -= CheckGameEnd;
        }
    }

    private void CheckGameEnd(int day)
    {
        if (day > maxDays)
        {
            OnGameEnd?.Invoke();
        }
    }

    #region 장소 관리

    /// <summary>
    /// 현재 시간에 이용 가능한 장소 목록
    /// </summary>
    public List<LocationSO> GetAvailableLocations()
    {
        return locations
            .Where(loc => loc.IsAvailableAt(Time.CurrentTimeMinutes))
            .ToList();
    }

    #endregion

    #region 활동 관리

    /// <summary>
    /// 특정 장소의 실행 가능한 활동 목록
    /// </summary>
    public List<ActivitySO> GetAvailableActivities(LocationSO location)
    {
        if (location == null || location.activities == null)
            return new List<ActivitySO>();

        return location.activities
            .Where(act => CanExecuteActivity(act))
            .ToList();
    }

    /// <summary>
    /// 활동 실행
    /// </summary>
    public bool ExecuteActivity(LocationSO location, ActivitySO activity)
    {
        if (!CanExecuteActivity(activity))
        {
            return false;
        }

        // 비용 지불
        if (activity.cost > 0)
        {
            StatusManager.Instance.Status.SpendMoney(activity.cost);
        }

        // 효과 적용
        float efficiency = StatusManager.Instance.CalculateTotalEfficiency();
        StatusManager.Instance.ApplyActivityEffect(activity.statChanges);

        // 이벤트 발생
        OnActivityExecuted?.Invoke(activity, efficiency);

        // 시간 진행
        AdvanceTime(activity.durationMinutes);

        return true;
    }

    /// <summary>
    /// 활동 실행 가능 여부
    /// </summary>
    public bool CanExecuteActivity(ActivitySO activity)
    {
        return StatusManager.Instance.CanExecuteActivity(activity);
    }

    /// <summary>
    /// 활동 불가 사유
    /// </summary>
    public string GetActivityBlockReason(ActivitySO activity)
    {
        return StatusManager.Instance.GetActivityBlockReason(activity);
    }

    #endregion

    #region 시간 관리

    /// <summary>
    /// 시간 진행 (분 단위)
    /// </summary>
    public void AdvanceTime(int minutes)
    {
        bool dayChanged = Time.AdvanceTime(minutes);

        if (dayChanged)
        {
            // 일일 처리
            StatusManager.Instance.ProcessDayEnd();
        }
    }

    /// <summary>
    /// 강제 수면 (하루 종료)
    /// </summary>
    public void Sleep()
    {
        StatusManager.Instance.ProcessDayEnd();
        Time.AdvanceDay();
    }

    /// <summary>
    /// 강제 휴식 (탈진 상태일 때)
    /// </summary>
    public void ForceRest()
    {
        foreach (var location in locations)
        {
            var restActivity = location.activities?.Find(a => a.id == "rest" || a.id == "sleep");
            if (restActivity != null)
            {
                ExecuteActivity(location, restActivity);
                return;
            }
        }

        Sleep();
    }

    #endregion

    #region 포맷팅 (TimeUtility로 위임)

    public string GetTimeDisplayText() => Time.CurrentTimeString;
    public string GetDayDisplayText() => TimeUtility.FormatDayDisplay(Time.CurrentDay, Time.MaxDays);
    public int GetRemainingMinutes() => Time.GetRemainingMinutes();
    public string GetRemainingTimeText() => TimeUtility.FormatRemainingTime(Time.GetRemainingMinutes());
    public string GetTimePeriodName() => TimeUtility.GetTimePeriodName(Time.CurrentTimeMinutes);
    public string GetDayOfWeekName() => TimeUtility.GetDayOfWeekName(Time.CurrentDayOfWeek);
    public string GetEndTimeText(ActivitySO activity) => TimeUtility.MinutesToTimeString(Time.CurrentTimeMinutes + activity.durationMinutes);
    public bool IsWeekend() => Time.IsWeekend();

    #endregion

#if UNITY_EDITOR
    public void SetLocations(List<LocationSO> newLocations)
    {
        locations = newLocations;
    }
#endif
}
