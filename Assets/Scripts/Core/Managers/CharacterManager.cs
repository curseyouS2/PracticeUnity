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
    /// 현재 장소/시간에 있는 캐릭터 목록 (편의 메서드)
    /// </summary>
    public List<CharacterSO> GetCharactersAt(string locationId)
    {
        var routine = RoutineManager.Instance;
        if (routine == null) return new List<CharacterSO>();

        Func<string, int> getAffection = StatusManager.Instance != null
            ? (id) => StatusManager.Instance.Relationship.GetAffection(id)
            : (_) => 0;

        return GetCharactersAt(locationId, routine.CurrentDayOfWeek, routine.CurrentTimeMinutes, getAffection);
    }

    /// <summary>
    /// 특정 조건에서 있는 캐릭터 목록 (명시적 의존성)
    /// </summary>
    public List<CharacterSO> GetCharactersAt(string locationId, DayOfWeek dayOfWeek, int timeMinutes, Func<string, int> getAffection)
    {
        var result = new List<CharacterSO>();

        foreach (var character in allCharacters)
        {
            if (character.IsAvailableAt(locationId, dayOfWeek, timeMinutes))
            {
                var schedule = character.GetActiveSchedule(locationId, dayOfWeek, timeMinutes);
                if (schedule != null)
                {
                    int affection = getAffection(character.id);
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
    /// 캐릭터와 대화 가능 여부 (명시적 의존성)
    /// </summary>
    public bool CanTalkTo(CharacterSO character, string locationId, int remainingMinutes,
        Func<string, int> getAffection, Func<string, bool> hasViewedDialogue)
    {
        var routine = RoutineManager.Instance;
        if (routine == null) return false;

        var schedule = character.GetActiveSchedule(locationId, routine.CurrentDayOfWeek, routine.CurrentTimeMinutes);
        if (schedule == null) return false;

        // 대화 시간 체크
        if (schedule.dialogue != null && schedule.dialogue.durationMinutes > 0)
        {
            if (remainingMinutes < schedule.dialogue.durationMinutes)
                return false;
        }

        // 호감도 조건 체크
        if (schedule.dialogue != null)
        {
            int affection = getAffection(character.id);
            if (affection < schedule.dialogue.requiredAffection)
                return false;

            if (schedule.dialogue.oneTimeOnly && hasViewedDialogue(schedule.dialogue.id))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 캐릭터와 대화 가능 여부 (편의 메서드)
    /// </summary>
    public bool CanTalkTo(CharacterSO character, string locationId)
    {
        if (RoutineManager.Instance == null || StatusManager.Instance == null)
            return false;

        return CanTalkTo(
            character,
            locationId,
            RoutineManager.Instance.GetRemainingMinutes(),
            id => StatusManager.Instance.Relationship.GetAffection(id),
            id => StatusManager.Instance.Relationship.HasViewedDialogue(id)
        );
    }

#if UNITY_EDITOR
    public void SetCharacters(List<CharacterSO> characters)
    {
        allCharacters = characters;
    }
#endif
}
