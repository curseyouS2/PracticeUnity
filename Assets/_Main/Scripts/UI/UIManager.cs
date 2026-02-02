using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI 전체 조율 - 각 패널에 위임
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private StatusPanel statusPanel;
    [SerializeField] private CharacterPanel characterPanel;
    [SerializeField] private MainPanel mainPanel;
    [SerializeField] private EndingPanel endingPanel;
    [SerializeField] private DialoguePanel dialoguePanel;

    public StatusPanel Status => statusPanel;
    public CharacterPanel Character => characterPanel;
    public MainPanel Main => mainPanel;
    public DialoguePanel Dialogue => dialoguePanel;

    public void Initialize()
    {
        // 패널 이벤트 연결
        if (mainPanel != null)
        {
            mainPanel.OnActivitySelected += HandleActivitySelected;
            mainPanel.OnCharacterTalkSelected += HandleCharacterTalk;
        }

        // 엔딩 패널 숨김
        if (endingPanel != null)
        {
            endingPanel.Hide();
        }

        // 대화 패널 숨김
        if (dialoguePanel != null)
        {
            dialoguePanel.Hide();
        }
    }

    private void OnDestroy()
    {
        if (mainPanel != null)
        {
            mainPanel.OnActivitySelected -= HandleActivitySelected;
            mainPanel.OnCharacterTalkSelected -= HandleCharacterTalk;
        }
    }

    public void UpdateAllUI()
    {
        statusPanel?.UpdatePanel();
        characterPanel?.UpdatePanel();
    }

    public void DisplayLocations(List<LocationSO> locations)
    {
        mainPanel?.DisplayLocations(locations);
    }

    public void ShowActivityResult(ActivitySO activity, float efficiency)
    {
        mainPanel?.ShowActivityResult(activity, efficiency);
    }

    public void ShowMessage(string message)
    {
        mainPanel?.ShowMessage(message);
    }

    public void ShowWarning(string warning)
    {
        mainPanel?.ShowWarning(warning);
    }

    public void DisplayEnding(EndingSO ending)
    {
        endingPanel?.Show(ending);
    }

    private void HandleActivitySelected(LocationSO location, ActivitySO activity)
    {
        GameManager.Instance?.ExecuteActivity(location, activity);
    }

    private void HandleCharacterTalk(CharacterSO character)
    {
        if (character == null || mainPanel == null) return;

        var location = mainPanel.CurrentLocation;
        if (location == null) return;

        // 현재 스케줄의 대화 가져오기
        var schedule = CharacterManager.Instance?.GetCurrentSchedule(character, location.id);
        var dialogue = schedule?.dialogue;

        // 대화 시작
        DialogueManager.Instance?.StartDialogue(character, dialogue);
    }

    /// <summary>
    /// 모든 UI 갱신 (활동 후 호출)
    /// </summary>
    public void RefreshAll()
    {
        UpdateAllUI();
        mainPanel?.RefreshActivities();
    }
}
