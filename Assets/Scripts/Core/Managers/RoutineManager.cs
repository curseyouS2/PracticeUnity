using UnityEngine;
using System;

/// <summary>
/// 시간 관리 전용
/// TimeController를 소유하고 시간 진행, 하루 종료 처리 담당
/// </summary>
public class RoutineManager : MonoBehaviour
{
    public static RoutineManager Instance { get; private set; }

    public event Action OnGameEnd;
    public event Action<int> OnDayEndProcessing;

    [Header("Time Settings")]
    [SerializeField] private int startTimeMinutes = 360;      // 06:00 (기상)
    [SerializeField] private int sleepTimeMinutes = 1440;     // 24:00 (자정)
    [SerializeField] private int maxDays = 30;

    public TimeController Time { get; private set; }

    // Facade 프로퍼티
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

    #region 시간 관리

    /// <summary>
    /// 시간 진행 (분 단위)
    /// </summary>
    public void AdvanceTime(int minutes)
    {
        bool dayChanged = Time.AdvanceTime(minutes);

        if (dayChanged)
        {
            OnDayEndProcessing?.Invoke(Time.CurrentDay);
        }
    }

    /// <summary>
    /// 강제 수면 (하루 종료)
    /// </summary>
    public void Sleep()
    {
        OnDayEndProcessing?.Invoke(Time.CurrentDay);
        Time.AdvanceDay();
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
}
