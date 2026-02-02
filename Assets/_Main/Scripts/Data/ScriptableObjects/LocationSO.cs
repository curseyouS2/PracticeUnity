using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Location", menuName = "Game/Location")]
public class LocationSO : ScriptableObject
{
    public string id;
    public string locationName;
    [TextArea(2, 4)]
    public string description;

    [Header("Availability (24시간 기준)")]
    [Tooltip("이용 가능 시작 시간 (예: 9시 = 540분)")]
    public int openTimeMinutes = 540;  // 09:00
    [Tooltip("이용 가능 종료 시간 (예: 21시 = 1260분)")]
    public int closeTimeMinutes = 1260; // 21:00

    [Header("Activities")]
    public List<ActivitySO> activities;

    [Header("Visuals")]
    public Sprite locationIcon;
    public Sprite backgroundImage;

    /// <summary>
    /// 특정 시간에 이용 가능한지 확인
    /// </summary>
    public bool IsAvailableAt(int currentTimeMinutes)
    {
        // 자정을 넘어가는 경우 처리 (예: 22:00 ~ 02:00)
        if (closeTimeMinutes < openTimeMinutes)
        {
            return currentTimeMinutes >= openTimeMinutes || currentTimeMinutes < closeTimeMinutes;
        }

        return currentTimeMinutes >= openTimeMinutes && currentTimeMinutes < closeTimeMinutes;
    }

    /// <summary>
    /// 이용 가능 시간 텍스트 (예: "09:00 ~ 21:00")
    /// </summary>
    public string GetAvailableTimeText()
    {
        return $"{MinutesToTimeString(openTimeMinutes)} ~ {MinutesToTimeString(closeTimeMinutes)}";
    }

    private string MinutesToTimeString(int minutes)
    {
        int hours = (minutes / 60) % 24;
        int mins = minutes % 60;
        return $"{hours:D2}:{mins:D2}";
    }
}

/// <summary>
/// 시간 유틸리티
/// </summary>
public static class TimeUtility
{
    public const int MINUTES_PER_DAY = 1440; // 24 * 60

    public static int HoursToMinutes(int hours, int minutes = 0)
    {
        return hours * 60 + minutes;
    }

    public static string MinutesToTimeString(int totalMinutes)
    {
        int hours = (totalMinutes / 60) % 24;
        int mins = totalMinutes % 60;
        return $"{hours:D2}:{mins:D2}";
    }

    public static (int hours, int minutes) MinutesToTime(int totalMinutes)
    {
        return ((totalMinutes / 60) % 24, totalMinutes % 60);
    }
}
