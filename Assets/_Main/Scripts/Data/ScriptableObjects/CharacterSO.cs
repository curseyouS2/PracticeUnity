using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Character", menuName = "Game/Character")]
public class CharacterSO : ScriptableObject
{
    public string id;
    public string characterName;

    [Header("Visuals")]
    public Sprite portrait;
    public Sprite fullBodyImage;

    [Header("Default Dialogues (평소 대사)")]
    [TextArea(2, 3)]
    public string[] defaultDialogues;

    [Header("Schedules (등장 스케줄)")]
    public List<CharacterSchedule> schedules;

    /// <summary>
    /// 특정 요일/시간/장소에 이 캐릭터가 있는지 확인
    /// </summary>
    public bool IsAvailableAt(string locationId, DayOfWeek dayOfWeek, int timeMinutes)
    {
        if (schedules == null) return false;

        foreach (var schedule in schedules)
        {
            if (schedule.IsActiveAt(locationId, dayOfWeek, timeMinutes))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 현재 조건에 맞는 스케줄 반환
    /// </summary>
    public CharacterSchedule GetActiveSchedule(string locationId, DayOfWeek dayOfWeek, int timeMinutes)
    {
        if (schedules == null) return null;

        foreach (var schedule in schedules)
        {
            if (schedule.IsActiveAt(locationId, dayOfWeek, timeMinutes))
            {
                return schedule;
            }
        }

        return null;
    }

    /// <summary>
    /// 랜덤 기본 대사 반환
    /// </summary>
    public string GetRandomDefaultDialogue()
    {
        if (defaultDialogues == null || defaultDialogues.Length == 0)
            return $"{characterName}: ...";

        return defaultDialogues[Random.Range(0, defaultDialogues.Length)];
    }
}

/// <summary>
/// 캐릭터 등장 스케줄
/// </summary>
[System.Serializable]
public class CharacterSchedule
{
    [Header("When")]
    public List<DayOfWeek> activeDays;
    public int startTimeMinutes = 960;   // 16:00
    public int endTimeMinutes = 1200;    // 20:00

    [Header("Where")]
    public string locationId;

    [Header("Dialogue")]
    public DialogueSO dialogue;

    [Header("Requirements")]
    [Tooltip("필요 호감도 (이 이상이어야 등장)")]
    public int requiredAffection = 0;

    /// <summary>
    /// 이 스케줄이 특정 조건에서 활성화되는지 확인
    /// </summary>
    public bool IsActiveAt(string locId, DayOfWeek dayOfWeek, int timeMinutes)
    {
        // 장소 체크
        if (locationId != locId) return false;

        // 요일 체크
        if (activeDays != null && activeDays.Count > 0)
        {
            if (!activeDays.Contains(dayOfWeek)) return false;
        }

        // 시간 체크
        if (timeMinutes < startTimeMinutes || timeMinutes >= endTimeMinutes)
            return false;

        return true;
    }

    /// <summary>
    /// 스케줄 시간 텍스트 (예: "16:00 ~ 20:00")
    /// </summary>
    public string GetTimeRangeText()
    {
        return $"{TimeUtility.MinutesToTimeString(startTimeMinutes)} ~ {TimeUtility.MinutesToTimeString(endTimeMinutes)}";
    }
}
