using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 대화 진행 관리 - 순수 대화 흐름만 담당
/// 게임 상태 변경은 이벤트를 통해 외부에서 처리
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // 대화 흐름 이벤트
    public event Action<DialogueSO, CharacterSO> OnDialogueStarted;
    public event Action<DialogueLine> OnLineChanged;
    public event Action<List<DialogueChoice>> OnChoicesShown;
    public event Action OnDialogueEnded;

    // 게임 상태 변경 요청 이벤트 (GameManager에서 구독하여 처리)
    public event Action<string, int> OnAffectionChangeRequested;
    public event Action<StatChanges> OnStatChangeRequested;
    public event Action<string> OnDialogueViewedRequested;
    public event Action<int> OnTimeAdvanceRequested;

    private DialogueSO currentDialogue;
    private CharacterSO currentCharacter;
    private int currentLineIndex;
    private bool isDialogueActive;

    public bool IsDialogueActive => isDialogueActive;
    public CharacterSO CurrentCharacter => currentCharacter;

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
    /// 대화 시작
    /// </summary>
    public void StartDialogue(CharacterSO character, DialogueSO dialogue)
    {
        if (isDialogueActive) return;
        if (dialogue == null)
        {
            StartSimpleDialogue(character, character.GetRandomDefaultDialogue());
            return;
        }

        currentCharacter = character;
        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;

        OnDialogueStarted?.Invoke(dialogue, character);

        ShowCurrentLine();
    }

    /// <summary>
    /// 단순 대사 시작 (한 줄)
    /// </summary>
    public void StartSimpleDialogue(CharacterSO character, string text)
    {
        currentCharacter = character;
        currentDialogue = null;
        isDialogueActive = true;

        var simpleLine = new DialogueLine
        {
            speakerName = character.characterName,
            text = text,
            speakerPortrait = character.portrait,
            hasChoices = false
        };

        OnDialogueStarted?.Invoke(null, character);
        OnLineChanged?.Invoke(simpleLine);
    }

    /// <summary>
    /// 다음 대사로 진행
    /// </summary>
    public void NextLine()
    {
        if (!isDialogueActive) return;

        // 단순 대사 모드
        if (currentDialogue == null)
        {
            EndDialogue();
            return;
        }

        // 현재 줄에 선택지가 있으면 선택 대기
        if (currentLineIndex < currentDialogue.lines.Count)
        {
            var currentLine = currentDialogue.lines[currentLineIndex];
            if (currentLine.hasChoices && currentLine.choices != null && currentLine.choices.Count > 0)
            {
                OnChoicesShown?.Invoke(currentLine.choices);
                return;
            }
        }

        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.lines.Count)
        {
            EndDialogue();
        }
        else
        {
            ShowCurrentLine();
        }
    }

    /// <summary>
    /// 선택지 선택
    /// </summary>
    public void SelectChoice(int choiceIndex)
    {
        if (!isDialogueActive || currentDialogue == null) return;
        if (currentLineIndex >= currentDialogue.lines.Count) return;

        var currentLine = currentDialogue.lines[currentLineIndex];
        if (!currentLine.hasChoices || currentLine.choices == null) return;
        if (choiceIndex < 0 || choiceIndex >= currentLine.choices.Count) return;

        var choice = currentLine.choices[choiceIndex];

        // 효과 요청 (이벤트로 위임)
        ApplyChoiceEffects(choice);

        // 반응 대사가 있으면 표시
        if (!string.IsNullOrEmpty(choice.responseText))
        {
            var responseLine = new DialogueLine
            {
                speakerName = currentCharacter.characterName,
                text = choice.responseText,
                speakerPortrait = currentCharacter.portrait,
                hasChoices = false
            };
            OnLineChanged?.Invoke(responseLine);

            if (choice.nextDialogue != null)
            {
                currentDialogue = choice.nextDialogue;
                currentLineIndex = -1;
            }
            else
            {
                currentLineIndex = currentDialogue.lines.Count;
            }
        }
        else if (choice.nextDialogue != null)
        {
            StartDialogue(currentCharacter, choice.nextDialogue);
        }
        else
        {
            EndDialogue();
        }
    }

    private void ShowCurrentLine()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.lines.Count)
        {
            EndDialogue();
            return;
        }

        var line = currentDialogue.lines[currentLineIndex];

        if (string.IsNullOrEmpty(line.speakerName) && currentCharacter != null)
        {
            line.speakerName = currentCharacter.characterName;
        }

        if (line.speakerPortrait == null && currentCharacter != null)
        {
            line.speakerPortrait = currentCharacter.portrait;
        }

        OnLineChanged?.Invoke(line);
    }

    private void ApplyChoiceEffects(DialogueChoice choice)
    {
        if (currentCharacter == null) return;

        if (choice.affectionChange != 0)
            OnAffectionChangeRequested?.Invoke(currentCharacter.id, choice.affectionChange);

        if (choice.statChange != null)
            OnStatChangeRequested?.Invoke(choice.statChange);
    }

    private void EndDialogue()
    {
        if (!isDialogueActive) return;

        // 대화 완료 보상 요청 (이벤트로 위임)
        if (currentDialogue != null)
        {
            if (currentDialogue.affectionReward != 0 && currentCharacter != null)
                OnAffectionChangeRequested?.Invoke(currentCharacter.id, currentDialogue.affectionReward);

            if (currentDialogue.statReward != null)
                OnStatChangeRequested?.Invoke(currentDialogue.statReward);

            if (currentDialogue.oneTimeOnly)
                OnDialogueViewedRequested?.Invoke(currentDialogue.id);

            if (currentDialogue.durationMinutes > 0)
                OnTimeAdvanceRequested?.Invoke(currentDialogue.durationMinutes);
        }

        isDialogueActive = false;
        currentDialogue = null;
        currentCharacter = null;
        currentLineIndex = 0;

        OnDialogueEnded?.Invoke();
    }

    /// <summary>
    /// 대화 강제 종료
    /// </summary>
    public void ForceEndDialogue()
    {
        isDialogueActive = false;
        currentDialogue = null;
        currentCharacter = null;
        OnDialogueEnded?.Invoke();
    }
}
