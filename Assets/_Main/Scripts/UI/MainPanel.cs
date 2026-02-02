using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 중앙 패널 - 장소 선택, 활동 선택, 결과 메시지 표시
/// </summary>
public class MainPanel : MonoBehaviour
{
    public event Action<LocationSO, ActivitySO> OnActivitySelected;
    public event Action<CharacterSO> OnCharacterTalkSelected;

    [Header("Location Buttons")]
    [SerializeField] private Transform locationButtonContainer;
    [SerializeField] private GameObject locationButtonPrefab;

    [Header("Character Buttons")]
    [SerializeField] private Transform characterButtonContainer;
    [SerializeField] private GameObject characterButtonPrefab;

    [Header("Activity Buttons")]
    [SerializeField] private Transform activityButtonContainer;
    [SerializeField] private GameObject activityButtonPrefab;

    [Header("Result Message")]
    [SerializeField] private TextMeshProUGUI resultMessageText;
    [SerializeField] private float messageDisplayDuration = 3f;

    [Header("Colors")]
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color unavailableColor = Color.gray;

    private LocationSO currentLocation;

    public void DisplayLocations(List<LocationSO> locations)
    {
        ClearContainer(locationButtonContainer);
        ClearContainer(characterButtonContainer);
        ClearContainer(activityButtonContainer);
        currentLocation = null;

        if (locations == null) return;

        foreach (var location in locations)
        {
            CreateLocationButton(location);
        }
    }

    private void CreateLocationButton(LocationSO location)
    {
        GameObject btnObj = Instantiate(locationButtonPrefab, locationButtonContainer);
        Button btn = btnObj.GetComponent<Button>();
        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

        btnText.text = location.locationName;

        var loc = location;
        btn.onClick.AddListener(() => OnLocationClicked(loc));
    }

    private void OnLocationClicked(LocationSO location)
    {
        currentLocation = location;
        DisplayCharactersAt(location);
        DisplayActivities(location.activities);
    }

    /// <summary>
    /// 현재 장소에 있는 캐릭터 표시
    /// </summary>
    private void DisplayCharactersAt(LocationSO location)
    {
        ClearContainer(characterButtonContainer);

        if (CharacterManager.Instance == null) return;

        var characters = CharacterManager.Instance.GetCharactersAt(location.id);

        foreach (var character in characters)
        {
            CreateCharacterButton(character);
        }
    }

    private void CreateCharacterButton(CharacterSO character)
    {
        if (characterButtonContainer == null || characterButtonPrefab == null) return;

        GameObject btnObj = Instantiate(characterButtonPrefab, characterButtonContainer);
        Button btn = btnObj.GetComponent<Button>();
        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        Image btnImage = btnObj.transform.Find("Portrait")?.GetComponent<Image>();

        // 초상화 설정
        if (btnImage != null && character.portrait != null)
        {
            btnImage.sprite = character.portrait;
        }

        // 텍스트 설정
        if (btnText != null)
        {
            string affectionLevel = StatusManager.Instance?.Relationship.GetAffectionLevelName(character.id) ?? "";
            btnText.text = $"<b>{character.characterName}</b>\n<size=80%>{affectionLevel}</size>\n<color=#88ff88>대화하기</color>";
        }

        // 대화 가능 여부 체크
        bool canTalk = CharacterManager.Instance.CanTalkTo(character, currentLocation.id);
        btn.interactable = canTalk;

        var chara = character;
        btn.onClick.AddListener(() => OnCharacterClicked(chara));
    }

    private void OnCharacterClicked(CharacterSO character)
    {
        OnCharacterTalkSelected?.Invoke(character);
    }

    public void DisplayActivities(List<ActivitySO> activities)
    {
        ClearContainer(activityButtonContainer);

        if (activities == null) return;

        foreach (var activity in activities)
        {
            CreateActivityButton(activity);
        }
    }

    private void CreateActivityButton(ActivitySO activity)
    {
        GameObject btnObj = Instantiate(activityButtonPrefab, activityButtonContainer);
        Button btn = btnObj.GetComponent<Button>();
        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

        bool canExecute = StatusManager.Instance != null &&
                          StatusManager.Instance.CanExecuteActivity(activity);

        // 버튼 텍스트 구성
        string btnLabel = BuildActivityLabel(activity, canExecute);
        btnText.text = btnLabel;

        // 실행 불가 시 비활성화
        btn.interactable = canExecute;
        if (!canExecute)
        {
            var colors = btn.colors;
            colors.disabledColor = unavailableColor;
            btn.colors = colors;
        }

        var act = activity;
        btn.onClick.AddListener(() => OnActivityClicked(act));
    }

    private string BuildActivityLabel(ActivitySO activity, bool canExecute)
    {
        string label = $"<b>{activity.activityName}</b>\n";

        if (!string.IsNullOrEmpty(activity.description))
            label += $"{activity.description}\n";

        // 소요 시간 표시
        label += $"<color=#888888>소요: {FormatDuration(activity.durationMinutes)}</color>\n";

        label += GetStatChangeText(activity.statChanges);

        if (activity.cost > 0)
            label += $"\n비용: {activity.cost:N0}원";

        if (!canExecute)
        {
            string reason = StatusManager.Instance?.GetActivityBlockReason(activity) ?? "실행 불가";
            label += $"\n<color=red>[{reason}]</color>";
        }

        return label;
    }

    private string FormatDuration(int minutes)
    {
        if (minutes >= 60)
        {
            int hours = minutes / 60;
            int mins = minutes % 60;
            return mins > 0 ? $"{hours}시간 {mins}분" : $"{hours}시간";
        }
        return $"{minutes}분";
    }

    private string GetStatChangeText(StatChanges changes)
    {
        if (changes == null) return "";

        var parts = new List<string>();

        if (changes.intelligence != 0) parts.Add($"지력 {changes.intelligence:+#;-#;0}");
        if (changes.charm != 0) parts.Add($"매력 {changes.charm:+#;-#;0}");
        if (changes.courage != 0) parts.Add($"용기 {changes.courage:+#;-#;0}");
        if (changes.fatigue != 0) parts.Add($"피로 {changes.fatigue:+#;-#;0}");
        if (changes.money != 0) parts.Add($"돈 {changes.money:+#;-#;0}원");
        if (changes.physical != 0) parts.Add($"체력 {changes.physical:+#;-#;0}");
        if (changes.mental != 0) parts.Add($"정신력 {changes.mental:+#;-#;0}");

        return string.Join(" ", parts);
    }

    private void OnActivityClicked(ActivitySO activity)
    {
        OnActivitySelected?.Invoke(currentLocation, activity);
    }

    public void ShowActivityResult(ActivitySO activity, float efficiency)
    {
        string result = $"<b>[{activity.activityName}]</b> 완료!\n";

        if (!string.IsNullOrEmpty(activity.description))
            result += activity.description + "\n";

        if (efficiency < 1.0f)
            result += $"<color=yellow>(효율 {efficiency * 100:F0}%)</color>\n";

        result += GetStatChangeText(activity.statChanges);

        ShowMessage(result);

        // 활동 버튼 갱신
        if (currentLocation != null)
            DisplayActivities(currentLocation.activities);
    }

    public void ShowMessage(string message)
    {
        if (resultMessageText != null)
            resultMessageText.text = message;
    }

    public void ShowWarning(string warning)
    {
        ShowMessage($"<color=yellow>⚠ {warning}</color>");
    }

    public void ShowError(string error)
    {
        ShowMessage($"<color=red>✖ {error}</color>");
    }

    public void ClearMessage()
    {
        if (resultMessageText != null)
            resultMessageText.text = "";
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 현재 선택된 장소의 활동 목록 갱신
    /// </summary>
    public void RefreshActivities()
    {
        if (currentLocation != null)
        {
            DisplayCharactersAt(currentLocation);
            DisplayActivities(currentLocation.activities);
        }
    }

    public LocationSO CurrentLocation => currentLocation;
}
