using System;

/// <summary>
/// 시간 관련 유틸리티 (포맷팅, 변환)
/// </summary>
public static class TimeUtility
{
    public const int MINUTES_PER_DAY = 1440; // 24 * 60

    #region 변환

    public static int HoursToMinutes(int hours, int minutes = 0)
    {
        return hours * 60 + minutes;
    }

    public static (int hours, int minutes) MinutesToTime(int totalMinutes)
    {
        return ((totalMinutes / 60) % 24, totalMinutes % 60);
    }

    #endregion

    #region 포맷팅

    public static string MinutesToTimeString(int totalMinutes)
    {
        int hours = (totalMinutes / 60) % 24;
        int mins = totalMinutes % 60;
        return $"{hours:D2}:{mins:D2}";
    }

    public static string FormatDayDisplay(int day, int maxDays)
    {
        return $"Day {day}/{maxDays}";
    }

    public static string FormatRemainingTime(int remainingMinutes)
    {
        int hours = remainingMinutes / 60;
        int mins = remainingMinutes % 60;

        if (hours > 0)
            return $"{hours}시간 {mins}분 남음";
        else
            return $"{mins}분 남음";
    }

    public static string GetTimePeriodName(int timeMinutes)
    {
        if (timeMinutes >= 360 && timeMinutes < 720)
            return "아침";
        else if (timeMinutes >= 720 && timeMinutes < 1080)
            return "낮";
        else if (timeMinutes >= 1080 && timeMinutes < 1260)
            return "저녁";
        else
            return "밤";
    }

    public static string GetDayOfWeekName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
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

    #endregion
}
