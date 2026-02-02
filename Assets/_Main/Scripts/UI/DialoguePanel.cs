using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 대화 UI 패널
/// </summary>
public class DialoguePanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Character Display")]
    [SerializeField] private Image characterPortrait;
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Header("Dialogue")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continueIndicator;

    [Header("Choices")]
    [SerializeField] private GameObject choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    [Header("Affection Display")]
    [SerializeField] private TextMeshProUGUI affectionText;
    [SerializeField] private GameObject affectionChangePopup;
    [SerializeField] private TextMeshProUGUI affectionChangeText;

    private List<GameObject> spawnedChoiceButtons = new List<GameObject>();
    private bool waitingForChoice = false;

    private void Start()
    {
        // 이벤트 구독
        var dm = DialogueManager.Instance;
        if (dm != null)
        {
            dm.OnDialogueStarted += OnDialogueStarted;
            dm.OnLineChanged += OnLineChanged;
            dm.OnChoicesShown += OnChoicesShown;
            dm.OnDialogueEnded += OnDialogueEnded;
        }

        // 호감도 변경 이벤트
        if (StatusManager.Instance?.Relationship != null)
        {
            StatusManager.Instance.Relationship.OnAffectionChanged += OnAffectionChanged;
        }

        Hide();
    }

    private void OnDestroy()
    {
        var dm = DialogueManager.Instance;
        if (dm != null)
        {
            dm.OnDialogueStarted -= OnDialogueStarted;
            dm.OnLineChanged -= OnLineChanged;
            dm.OnChoicesShown -= OnChoicesShown;
            dm.OnDialogueEnded -= OnDialogueEnded;
        }

        if (StatusManager.Instance?.Relationship != null)
        {
            StatusManager.Instance.Relationship.OnAffectionChanged -= OnAffectionChanged;
        }
    }

    private void Update()
    {
        // 클릭/스페이스로 다음 대사
        if (panelRoot.activeSelf && !waitingForChoice)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                DialogueManager.Instance?.NextLine();
            }
        }
    }

    private void OnDialogueStarted(DialogueSO dialogue, CharacterSO character)
    {
        Show();
        waitingForChoice = false;
        ClearChoices();

        if (character != null)
        {
            if (characterPortrait != null)
                characterPortrait.sprite = character.portrait;

            if (characterNameText != null)
                characterNameText.text = character.characterName;

            UpdateAffectionDisplay(character.id);
        }
    }

    private void OnLineChanged(DialogueLine line)
    {
        waitingForChoice = false;
        ClearChoices();

        if (characterPortrait != null && line.speakerPortrait != null)
            characterPortrait.sprite = line.speakerPortrait;

        if (characterNameText != null && !string.IsNullOrEmpty(line.speakerName))
            characterNameText.text = line.speakerName;

        if (dialogueText != null)
            dialogueText.text = line.text;

        if (continueIndicator != null)
            continueIndicator.SetActive(!line.hasChoices);
    }

    private void OnChoicesShown(List<DialogueChoice> choices)
    {
        waitingForChoice = true;
        ClearChoices();

        if (continueIndicator != null)
            continueIndicator.SetActive(false);

        if (choicesContainer == null || choiceButtonPrefab == null) return;

        choicesContainer.SetActive(true);

        for (int i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            int index = i;

            GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer.transform);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
            {
                string label = choice.choiceText;

                // 호감도 변화 힌트
                if (choice.affectionChange > 0)
                    label += $" <color=green>(♥+{choice.affectionChange})</color>";
                else if (choice.affectionChange < 0)
                    label += $" <color=red>(♥{choice.affectionChange})</color>";

                btnText.text = label;
            }

            btn.onClick.AddListener(() => OnChoiceSelected(index));
            spawnedChoiceButtons.Add(btnObj);
        }
    }

    private void OnChoiceSelected(int index)
    {
        waitingForChoice = false;
        ClearChoices();
        DialogueManager.Instance?.SelectChoice(index);
    }

    private void OnDialogueEnded()
    {
        Hide();
    }

    private void OnAffectionChanged(string characterId, int newValue)
    {
        var currentChar = DialogueManager.Instance?.CurrentCharacter;
        if (currentChar != null && currentChar.id == characterId)
        {
            UpdateAffectionDisplay(characterId);
        }
    }

    private void UpdateAffectionDisplay(string characterId)
    {
        if (affectionText == null) return;

        var relationship = StatusManager.Instance?.Relationship;
        if (relationship == null) return;

        int affection = relationship.GetAffection(characterId);
        string levelName = relationship.GetAffectionLevelName(characterId);

        affectionText.text = $"♥ {affection} ({levelName})";
    }

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        ClearChoices();
    }

    private void ClearChoices()
    {
        foreach (var btn in spawnedChoiceButtons)
        {
            Destroy(btn);
        }
        spawnedChoiceButtons.Clear();

        if (choicesContainer != null)
            choicesContainer.SetActive(false);
    }

    /// <summary>
    /// 호감도 변화 팝업 표시
    /// </summary>
    public void ShowAffectionChange(int change)
    {
        if (affectionChangePopup == null || affectionChangeText == null) return;

        affectionChangePopup.SetActive(true);

        if (change > 0)
            affectionChangeText.text = $"<color=green>♥ +{change}</color>";
        else
            affectionChangeText.text = $"<color=red>♥ {change}</color>";

        // 일정 시간 후 숨김
        CancelInvoke(nameof(HideAffectionPopup));
        Invoke(nameof(HideAffectionPopup), 1.5f);
    }

    private void HideAffectionPopup()
    {
        if (affectionChangePopup != null)
            affectionChangePopup.SetActive(false);
    }
}
