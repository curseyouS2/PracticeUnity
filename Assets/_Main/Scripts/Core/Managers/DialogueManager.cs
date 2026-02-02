using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 대화 진행 관리
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public event Action<DialogueSO, CharacterSO> OnDialogueStarted;
    public event Action<DialogueLine> OnLineChanged;
    public event Action<List<DialogueChoice>> OnChoicesShown;
    public event Action OnDialogueEnded;

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
            // 대화가 없으면 기본 대사만 표시
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

        // 효과 적용
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

            // 다음 대화가 있으면 연결
            if (choice.nextDialogue != null)
            {
                currentDialogue = choice.nextDialogue;
                currentLineIndex = -1; // NextLine에서 0이 됨
            }
            else
            {
                currentLineIndex = currentDialogue.lines.Count; // 종료 준비
            }
        }
        else if (choice.nextDialogue != null)
        {
            // 다음 대화로 이동
            StartDialogue(currentCharacter, choice.nextDialogue);
        }
        else
        {
            // 대화 종료
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

        // 화자 이름이 비어있으면 캐릭터 이름 사용
        if (string.IsNullOrEmpty(line.speakerName) && currentCharacter != null)
        {
            line.speakerName = currentCharacter.characterName;
        }

        // 초상화가 비어있으면 캐릭터 초상화 사용
        if (line.speakerPortrait == null && currentCharacter != null)
        {
            line.speakerPortrait = currentCharacter.portrait;
        }

        OnLineChanged?.Invoke(line);
    }

    private void ApplyChoiceEffects(DialogueChoice choice)
    {
        if (currentCharacter == null) return;

        // 호감도 변화
        if (choice.affectionChange != 0)
        {
            StatusManager.Instance?.Relationship.AddAffection(
                currentCharacter.id,
                choice.affectionChange
            );
        }

        // 스탯 변화
        if (choice.statChange != null)
        {
            StatusManager.Instance?.ApplyActivityEffect(choice.statChange);
        }
    }

    private void EndDialogue()
    {
        if (!isDialogueActive) return;

        // 대화 완료 보상
        if (currentDialogue != null)
        {
            // 호감도 보상
            if (currentDialogue.affectionReward != 0 && currentCharacter != null)
            {
                StatusManager.Instance?.Relationship.AddAffection(
                    currentCharacter.id,
                    currentDialogue.affectionReward
                );
            }

            // 스탯 보상
            if (currentDialogue.statReward != null)
            {
                StatusManager.Instance?.ApplyActivityEffect(currentDialogue.statReward);
            }

            // 일회성 대화 표시
            if (currentDialogue.oneTimeOnly)
            {
                StatusManager.Instance?.Relationship.MarkDialogueViewed(currentDialogue.id);
            }

            // 시간 진행
            if (currentDialogue.durationMinutes > 0)
            {
                RoutineManager.Instance?.AdvanceTime(currentDialogue.durationMinutes);
            }
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
