using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Game/Dialogue")]
public class DialogueSO : ScriptableObject
{
    public string id;

    [Header("Type")]
    public DialogueType dialogueType = DialogueType.Simple;

    [Header("Simple Dialogue (단순 대사)")]
    public List<DialogueLine> lines;

    [Header("Requirements")]
    [Tooltip("필요 호감도 (이 이상이어야 이 대화 진행)")]
    public int requiredAffection = 0;

    [Header("Rewards")]
    [Tooltip("대화 완료 시 호감도 변화")]
    public int affectionReward = 1;
    [Tooltip("대화 완료 시 스탯 변화")]
    public StatChanges statReward;

    [Header("Flags")]
    [Tooltip("이 대화는 한 번만 볼 수 있음")]
    public bool oneTimeOnly = false;
    [Tooltip("대화에 소요되는 시간 (분)")]
    public int durationMinutes = 30;
}

public enum DialogueType
{
    Simple,     // 단순 대사 나열
    Branching   // 선택지 분기
}

/// <summary>
/// 대화 한 줄
/// </summary>
[System.Serializable]
public class DialogueLine
{
    [Tooltip("화자 이름 (비워두면 캐릭터 이름 사용)")]
    public string speakerName;

    [TextArea(2, 4)]
    public string text;

    [Tooltip("이 대사에서 표시할 표정/이미지")]
    public Sprite speakerPortrait;

    [Header("Branching (선택지)")]
    public bool hasChoices = false;
    public List<DialogueChoice> choices;
}

/// <summary>
/// 대화 선택지
/// </summary>
[System.Serializable]
public class DialogueChoice
{
    [Tooltip("선택지 텍스트")]
    public string choiceText;

    [Header("Effects")]
    [Tooltip("이 선택지 선택 시 호감도 변화")]
    public int affectionChange = 0;
    [Tooltip("이 선택지 선택 시 스탯 변화")]
    public StatChanges statChange;

    [Header("Next")]
    [Tooltip("이 선택지 선택 후 이어질 대화 (없으면 대화 종료)")]
    public DialogueSO nextDialogue;

    [Header("Response")]
    [TextArea(2, 3)]
    [Tooltip("선택 직후 캐릭터 반응 대사")]
    public string responseText;
}
