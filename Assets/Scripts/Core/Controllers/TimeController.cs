using System;

/// <summary>
/// 시간/날짜 상태 관리
/// </summary>
public class TimeController
{
    public event Action<int> OnTimeChanged;
    public event Action<int> OnDayChanged;

    private int currentDay;
    private int currentTimeMinutes;
    private readonly int startTimeMinutes;
    private readonly int sleepTimeMinutes;
    private readonly int maxDays;

    public int CurrentDay => currentDay;
    public int CurrentTimeMinutes => currentTimeMinutes;
    public int MaxDays => maxDays;
    public DayOfWeek CurrentDayOfWeek => GetDayOfWeek();
    public bool IsGameOver => currentDay > maxDays;
    public string CurrentTimeString => TimeUtility.MinutesToTimeString(currentTimeMinutes);

    public TimeController(int startDay, int startTime, int sleepTime, int maxDays)
    {
        this.currentDay = startDay;
        this.currentTimeMinutes = startTime;
        this.startTimeMinutes = startTime;
        this.sleepTimeMinutes = sleepTime;
        this.maxDays = maxDays;
    }

    /// <summary>
    /// 시간 진행 (분 단위)
    /// </summary>
    /// <returns>날짜가 변경되었으면 true</returns>
    public bool AdvanceTime(int minutes)
    {
        currentTimeMinutes += minutes;

        if (currentTimeMinutes >= sleepTimeMinutes)
        {
            AdvanceDay();
            return true;
        }
        else
        {
            OnTimeChanged?.Invoke(currentTimeMinutes);
            return false;
        }
    }

    /// <summary>
    /// 다음 날로 진행
    /// </summary>
    public void AdvanceDay()
    {
        currentDay++;
        currentTimeMinutes = startTimeMinutes;

        OnDayChanged?.Invoke(currentDay);
        OnTimeChanged?.Invoke(currentTimeMinutes);
    }

    /// <summary>
    /// 남은 시간 (분)
    /// </summary>
    public int GetRemainingMinutes()
    {
        return sleepTimeMinutes - currentTimeMinutes;
    }

    /// <summary>
    /// 현재 요일 반환
    /// </summary>
    public DayOfWeek GetDayOfWeek()
    {
        return (DayOfWeek)((currentDay - 1) % 7);
    }

    /// <summary>
    /// 시간대 반환
    /// </summary>
    public TimePeriod GetTimePeriod()
    {
        if (currentTimeMinutes >= 360 && currentTimeMinutes < 720)
            return TimePeriod.Morning;
        else if (currentTimeMinutes >= 720 && currentTimeMinutes < 1080)
            return TimePeriod.Afternoon;
        else if (currentTimeMinutes >= 1080 && currentTimeMinutes < 1260)
            return TimePeriod.Evening;
        else
            return TimePeriod.Night;
    }

    /// <summary>
    /// 주말 여부
    /// </summary>
    public bool IsWeekend()
    {
        var day = GetDayOfWeek();
        return day == DayOfWeek.Saturday || day == DayOfWeek.Sunday;
    }
}

/// <summary>
/// 시간대 열거형
/// </summary>
public enum TimePeriod
{
    Morning,    // 아침 (06:00 ~ 12:00)
    Afternoon,  // 낮 (12:00 ~ 18:00)
    Evening,    // 저녁 (18:00 ~ 21:00)
    Night       // 밤 (21:00 ~ 06:00)
}
