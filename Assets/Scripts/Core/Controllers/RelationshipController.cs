using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터별 호감도/친밀도 관리
/// </summary>
public class RelationshipController
{
    public event Action<string, int> OnAffectionChanged;
    public event Action<string, int> OnAffectionLevelUp;

    private Dictionary<string, int> affections = new Dictionary<string, int>();
    private HashSet<string> viewedDialogues = new HashSet<string>();

    // 호감도 레벨 구간
    public static readonly int[] AffectionLevels = { 0, 20, 50, 100, 200 };

    public RelationshipController()
    {
        affections = new Dictionary<string, int>();
        viewedDialogues = new HashSet<string>();
    }

    /// <summary>
    /// 캐릭터 호감도 반환
    /// </summary>
    public int GetAffection(string characterId)
    {
        return affections.ContainsKey(characterId) ? affections[characterId] : 0;
    }

    /// <summary>
    /// 호감도 레벨 반환 (0~4)
    /// </summary>
    public int GetAffectionLevel(string characterId)
    {
        int affection = GetAffection(characterId);

        for (int i = AffectionLevels.Length - 1; i >= 0; i--)
        {
            if (affection >= AffectionLevels[i])
                return i;
        }

        return 0;
    }

    /// <summary>
    /// 호감도 추가
    /// </summary>
    public void AddAffection(string characterId, int amount)
    {
        int oldLevel = GetAffectionLevel(characterId);

        if (!affections.ContainsKey(characterId))
            affections[characterId] = 0;

        affections[characterId] = Mathf.Max(0, affections[characterId] + amount);

        int newLevel = GetAffectionLevel(characterId);

        OnAffectionChanged?.Invoke(characterId, affections[characterId]);

        if (newLevel > oldLevel)
        {
            OnAffectionLevelUp?.Invoke(characterId, newLevel);
        }
    }

    /// <summary>
    /// 호감도 설정
    /// </summary>
    public void SetAffection(string characterId, int value)
    {
        affections[characterId] = Mathf.Max(0, value);
        OnAffectionChanged?.Invoke(characterId, value);
    }

    /// <summary>
    /// 호감도 조건 충족 여부
    /// </summary>
    public bool MeetsAffectionRequirement(string characterId, int required)
    {
        return GetAffection(characterId) >= required;
    }

    /// <summary>
    /// 대화 이미 본 적 있는지 확인
    /// </summary>
    public bool HasViewedDialogue(string dialogueId)
    {
        return viewedDialogues.Contains(dialogueId);
    }

    /// <summary>
    /// 대화 봤음 표시
    /// </summary>
    public void MarkDialogueViewed(string dialogueId)
    {
        viewedDialogues.Add(dialogueId);
    }

    /// <summary>
    /// 본 대화 목록 반환 (저장용)
    /// </summary>
    public HashSet<string> GetViewedDialogues()
    {
        return new HashSet<string>(viewedDialogues);
    }

    /// <summary>
    /// 호감도 레벨 이름
    /// </summary>
    public string GetAffectionLevelName(string characterId)
    {
        return GetAffectionLevel(characterId) switch
        {
            0 => "모르는 사이",
            1 => "아는 사이",
            2 => "친구",
            3 => "절친",
            4 => "특별한 사이",
            _ => "???"
        };
    }

    /// <summary>
    /// 모든 호감도 데이터 반환 (저장용)
    /// </summary>
    public Dictionary<string, int> GetAllAffections()
    {
        return new Dictionary<string, int>(affections);
    }

    /// <summary>
    /// 호감도 데이터 로드 (불러오기용)
    /// </summary>
    public void LoadAffections(Dictionary<string, int> data)
    {
        affections = new Dictionary<string, int>(data);
    }
}
