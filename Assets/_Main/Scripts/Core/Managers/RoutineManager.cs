using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 일과 스케줄 전체 관리
/// 실제 시간 기반 (분 단위), 활동별 소요 시간 적용
/// </summary>
public class RoutineManager : MonoBehaviour
{
    public static RoutineManager Instance { get; private set; }

    public event Action<int> OnTimeChanged;           // 시간 변경 시
    public event Action<int> OnDayChanged;            // 날짜 변경 시
    public event Action OnGameEnd;
    public event Action<ActivitySO, float> OnActivityExecuted;

    [Header("Location Data")]
    [SerializeField] private List<LocationSO> locations;

    [Header("Time Settings")]
    [SerializeField] private int startTimeMinutes = 960;      // 16:00 (방과후)
    [SerializeField] private int sleepTimeMinutes = 1440;     // 24:00 (자정)
    [SerializeField] private int maxDays = 30;

    private int currentDay = 1;
    private int currentTimeMinutes;  // 0 ~ 1439 (00:00 ~ 23:59)

    public int CurrentDay => currentDay;
    public int CurrentTimeMinutes => currentTimeMinutes;
    public int MaxDays => maxDays;
    public bool IsGameOver => currentDay > maxDays;
    public string CurrentTimeString => TimeUtility.MinutesToTimeString(currentTimeMinutes);
    public DayOfWeek CurrentDayOfWeek => GetDayOfWeek();

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
        currentDay = startDay;
        currentTimeMinutes = startTime ?? startTimeMinutes;
    }

    /// <summary>
    /// 현재 시간에 이용 가능한 장소 목록
    /// </summary>
    public List<LocationSO> GetAvailableLocations()
    {
        return locations
            .Where(loc => loc.IsAvailableAt(currentTimeMinutes))
            .ToList();
    }

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

        // 시간 진행 (활동 소요 시간만큼)
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

    /// <summary>
    /// 시간 진행 (분 단위)
    /// </summary>
    public void AdvanceTime(int minutes)
    {
        currentTimeMinutes += minutes;

        // 자정(또는 설정된 수면 시간)을 넘기면 다음 날로
        if (currentTimeMinutes >= sleepTimeMinutes)
        {
            AdvanceDay();
        }
        else
        {
            OnTimeChanged?.Invoke(currentTimeMinutes);
        }
    }

    /// <summary>
    /// 다음 날로 진행
    /// </summary>
    private void AdvanceDay()
    {
        currentDay++;
        currentTimeMinutes = startTimeMinutes; // 다음 날 시작 시간

        // 일일 처리
        StatusManager.Instance.ProcessDayEnd();

        OnDayChanged?.Invoke(currentDay);
        OnTimeChanged?.Invoke(currentTimeMinutes);

        // 게임 종료 체크
        if (currentDay > maxDays)
        {
            OnGameEnd?.Invoke();
        }
    }

    /// <summary>
    /// 현재 시간 텍스트 (HH:MM 형식)
    /// </summary>
    public string GetTimeDisplayText()
    {
        return CurrentTimeString;
    }

    /// <summary>
    /// 날짜 텍스트
    /// </summary>
    public string GetDayDisplayText()
    {
        return $"Day {currentDay}/{maxDays}";
    }

    /// <summary>
    /// 남은 시간 (분)
    /// </summary>
    public int GetRemainingMinutes()
    {
        return sleepTimeMinutes - currentTimeMinutes;
    }

    /// <summary>
    /// 남은 시간 텍스트
    /// </summary>
    public string GetRemainingTimeText()
    {
        int remaining = GetRemainingMinutes();
        int hours = remaining / 60;
        int mins = remaining % 60;

        if (hours > 0)
            return $"{hours}시간 {mins}분 남음";
        else
            return $"{mins}분 남음";
    }

    /// <summary>
    /// 활동 종료 예상 시간
    /// </summary>
    public string GetEndTimeText(ActivitySO activity)
    {
        int endTime = currentTimeMinutes + activity.durationMinutes;
        return TimeUtility.MinutesToTimeString(endTime);
    }

    /// <summary>
    /// 시간대 이름 반환
    /// </summary>
    public string GetTimePeriodName()
    {
        if (currentTimeMinutes >= 360 && currentTimeMinutes < 720)
            return "아침";
        else if (currentTimeMinutes >= 720 && currentTimeMinutes < 1080)
            return "낮";
        else if (currentTimeMinutes >= 1080 && currentTimeMinutes < 1260)
            return "저녁";
        else
            return "밤";
    }

    /// <summary>
    /// 현재 요일 반환 (0=월, 1=화, 2=수, 3=목, 4=금, 5=토, 6=일)
    /// </summary>
    public DayOfWeek GetDayOfWeek()
    {
        return (DayOfWeek)((currentDay - 1) % 7);
    }

    /// <summary>
    /// 요일 이름 반환
    /// </summary>
    public string GetDayOfWeekName()
    {
        return GetDayOfWeek() switch
        {
            DayOfWeek.Monday => "월요일",
            DayOfWeek.Tuesday => "화요일",
            DayOfWeek.Wednesday => "수요일",
            DayOfWeek.Thursday => "목요일",
            DayOfWeek.Friday => "금요일",
            DayOfWeek.Saturday => "토요일",
            DayOfWeek.Sunday => "일요일",
            _ => ""
        };
    }

    /// <summary>
    /// 주말 여부
    /// </summary>
    public bool IsWeekend()
    {
        var day = GetDayOfWeek();
        return day == DayOfWeek.Saturday || day == DayOfWeek.Sunday;
    }

    /// <summary>
    /// 강제 수면 (하루 종료)
    /// </summary>
    public void Sleep()
    {
        AdvanceDay();
    }

    /// <summary>
    /// 강제 휴식 (탈진 상태일 때)
    /// </summary>
    public void ForceRest()
    {
        // 휴식 활동 찾아서 실행
        foreach (var location in locations)
        {
            var restActivity = location.activities?.Find(a => a.id == "rest" || a.id == "sleep");
            if (restActivity != null)
            {
                ExecuteActivity(location, restActivity);
                return;
            }
        }

        // 휴식 활동을 못 찾으면 다음 날로
        Sleep();
    }

#if UNITY_EDITOR
    public void SetLocations(List<LocationSO> newLocations)
    {
        locations = newLocations;
    }
#endif
}
