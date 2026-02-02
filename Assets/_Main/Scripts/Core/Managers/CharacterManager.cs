using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 캐릭터 등장/스케줄 관리
/// </summary>
public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    public event Action<List<CharacterSO>> OnCharactersUpdated;

    [Header("Characters")]
    [SerializeField] private List<CharacterSO> allCharacters;

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

    /// <summary>
    /// 현재 장소/시간에 있는 캐릭터 목록
    /// </summary>
    public List<CharacterSO> GetCharactersAt(string locationId)
    {
        var routine = RoutineManager.Instance;
        if (routine == null) return new List<CharacterSO>();

        return GetCharactersAt(locationId, routine.CurrentDayOfWeek, routine.CurrentTimeMinutes);
    }

    /// <summary>
    /// 특정 조건에서 있는 캐릭터 목록
    /// </summary>
    public List<CharacterSO> GetCharactersAt(string locationId, DayOfWeek dayOfWeek, int timeMinutes)
    {
        var result = new List<CharacterSO>();

        foreach (var character in allCharacters)
        {
            if (character.IsAvailableAt(locationId, dayOfWeek, timeMinutes))
            {
                // 호감도 조건 체크
                var schedule = character.GetActiveSchedule(locationId, dayOfWeek, timeMinutes);
                if (schedule != null && StatusManager.Instance != null)
                {
                    int affection = StatusManager.Instance.Relationship.GetAffection(character.id);
                    if (affection >= schedule.requiredAffection)
                    {
                        result.Add(character);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 캐릭터 ID로 캐릭터 찾기
    /// </summary>
    public CharacterSO GetCharacter(string characterId)
    {
        return allCharacters.Find(c => c.id == characterId);
    }

    /// <summary>
    /// 캐릭터의 현재 스케줄 반환
    /// </summary>
    public CharacterSchedule GetCurrentSchedule(CharacterSO character, string locationId)
    {
        var routine = RoutineManager.Instance;
        if (routine == null) return null;

        return character.GetActiveSchedule(locationId, routine.CurrentDayOfWeek, routine.CurrentTimeMinutes);
    }

    /// <summary>
    /// 캐릭터와 대화 가능 여부
    /// </summary>
    public bool CanTalkTo(CharacterSO character, string locationId)
    {
        var schedule = GetCurrentSchedule(character, locationId);
        if (schedule == null) return false;

        // 대화 시간 체크
        if (schedule.dialogue != null && schedule.dialogue.durationMinutes > 0)
        {
            int remaining = RoutineManager.Instance.GetRemainingMinutes();
            if (remaining < schedule.dialogue.durationMinutes)
                return false;
        }

        // 호감도 조건 체크
        if (schedule.dialogue != null)
        {
            int affection = StatusManager.Instance.Relationship.GetAffection(character.id);
            if (affection < schedule.dialogue.requiredAffection)
                return false;

            // 일회성 대화 체크
            if (schedule.dialogue.oneTimeOnly)
            {
                if (StatusManager.Instance.Relationship.HasViewedDialogue(schedule.dialogue.id))
                    return false;
            }
        }

        return true;
    }

#if UNITY_EDITOR
    public void SetCharacters(List<CharacterSO> characters)
    {
        allCharacters = characters;
    }
#endif
}
