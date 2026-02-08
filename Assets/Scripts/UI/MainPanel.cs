using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 중앙 패널 - 단일 컨테이너에 활동 → 캐릭터 → 장소 순으로 버튼 표시
/// </summary>
public class MainPanel : MonoBehaviour
{
    public event Action<LocationSO, ActivitySO> OnActivitySelected;
    public event Action<CharacterSO> OnCharacterTalkSelected;

    [Header("Button Container")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Header("Result Message")]
    [SerializeField] private TextMeshProUGUI resultMessageText;

    [Header("Colors")]
    [SerializeField] private Color unavailableColor = Color.gray;

    private LocationSO currentLocation;

    private void OnEnable()
    {
        LocationButton.OnLocationClicked += HandleLocationClicked;
        LocationButton.OnActivityClicked += HandleActivityClicked;
        LocationButton.OnCharacterClicked += HandleCharacterClicked;
    }

    private void OnDisable()
    {
        LocationButton.OnLocationClicked -= HandleLocationClicked;
        LocationButton.OnActivityClicked -= HandleActivityClicked;
        LocationButton.OnCharacterClicked -= HandleCharacterClicked;
    }

    private void Start()
    {
        HideTooltip();
    }

    #region Event Handlers

    private void HandleLocationClicked(LocationSO location)
    {
        LocationManager.Instance?.SetLocation(location);
        DisplayCurrentLocation();
    }

    private void HandleActivityClicked(ActivitySO activity)
    {
        OnActivitySelected?.Invoke(currentLocation, activity);
    }

    private void HandleCharacterClicked(CharacterSO character)
    {
        OnCharacterTalkSelected?.Invoke(character);
    }

    #endregion

    #region Display

    /// <summary>
    /// 현재 장소 기반으로 활동 → 캐릭터 → 이동 가능 장소 순으로 표시
    /// </summary>
    public void DisplayCurrentLocation()
    {
        if (LocationManager.Instance == null) return;

        currentLocation = LocationManager.Instance.NowLocation;
        if (currentLocation == null) return;

        ClearContainer();

        // 1. 활동
        if (currentLocation.activities != null)
        {
            foreach (var activity in currentLocation.activities)
                CreateActivityButton(activity);
        }

        // 2. 캐릭터
        if (CharacterManager.Instance != null)
        {
            var characters = CharacterManager.Instance.GetCharactersAt(currentLocation.id);
            foreach (var character in characters)
                CreateCharacterButton(character);
        }

        // 3. 이동 가능 장소
        if (currentLocation.connectedLocations != null)
        {
            int currentTime = RoutineManager.Instance != null ? RoutineManager.Instance.CurrentTimeMinutes : 0;
            foreach (var loc in currentLocation.connectedLocations)
            {
                if (loc != currentLocation && loc.IsAvailableAt(currentTime))
                    CreateLocationButton(loc);
            }
        }
    }

    /// <summary>
    /// 모든 이용 가능한 장소 표시 (게임 시작 시 사용)
    /// </summary>
    public void DisplayLocations(List<LocationSO> locations)
    {
        ClearContainer();
        currentLocation = null;

        if (locations == null) return;

        foreach (var location in locations)
            CreateLocationButton(location);
    }

    #endregion

    #region Button Creation

    private void CreateLocationButton(LocationSO location)
    {
        GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
        LocationButton locBtn = btnObj.GetComponent<LocationButton>();
        if (locBtn == null)
            locBtn = btnObj.AddComponent<LocationButton>();

        locBtn.SetupAsLocation(location);

        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = $"<color=#88aaff>▶ {location.locationName}</color>";

        AddTooltipEvents(btnObj, () => GetLocationTooltip(location));
    }

    private void CreateActivityButton(ActivitySO activity)
    {
        GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
        LocationButton locBtn = btnObj.GetComponent<LocationButton>();
        if (locBtn == null)
            locBtn = btnObj.AddComponent<LocationButton>();

        locBtn.SetupAsActivity(activity);

        bool canExecute = GameManager.Instance?.ActivityService?.CanExecuteActivity(activity) ?? false;

        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = BuildActivityLabel(activity, canExecute);

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = canExecute;
            if (!canExecute)
            {
                var colors = btn.colors;
                colors.disabledColor = unavailableColor;
                btn.colors = colors;
            }
        }

        AddTooltipEvents(btnObj, () => GetActivityTooltip(activity, canExecute));
    }

    private void CreateCharacterButton(CharacterSO character)
    {
        GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
        LocationButton locBtn = btnObj.GetComponent<LocationButton>();
        if (locBtn == null)
            locBtn = btnObj.AddComponent<LocationButton>();

        locBtn.SetupAsCharacter(character);

        bool canTalk = CharacterManager.Instance.CanTalkTo(character, currentLocation.id);

        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
        {
            string affectionLevel = StatusManager.Instance?.Relationship.GetAffectionLevelName(character.id) ?? "";
            btnText.text = $"<color=#88ff88>💬 {character.characterName}</color>\n<size=80%>{affectionLevel}</size>";
        }

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
            btn.interactable = canTalk;
    }

    #endregion

    #region Messages

    public void ShowActivityResult(ActivitySO activity, float efficiency)
    {
        string result = $"<b>[{activity.activityName}]</b> 완료!\n";

        if (!string.IsNullOrEmpty(activity.description))
            result += activity.description + "\n";

        if (efficiency < 1.0f)
            result += $"<color=yellow>(효율 {efficiency * 100:F0}%)</color>\n";

        result += GetStatChangeText(activity.statChanges);
        ShowMessage(result);

        DisplayCurrentLocation();
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

    #endregion

    #region Helpers

    public void RefreshActivities()
    {
        DisplayCurrentLocation();
    }

    public void SelectLocation(LocationSO location)
    {
        if (location == null) return;
        currentLocation = location;
        DisplayCurrentLocation();
    }

    public LocationSO CurrentLocation => currentLocation;

    private void ClearContainer()
    {
        if (buttonContainer == null) return;
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
    }

    private string BuildActivityLabel(ActivitySO activity, bool canExecute)
    {
        string label = $"<b>{activity.activityName}</b>";

        List<string> info = new List<string>();
        if (activity.durationMinutes > 0)
            info.Add(FormatDuration(activity.durationMinutes));
        if (activity.cost > 0)
            info.Add($"{activity.cost:N0}원");

        if (info.Count > 0)
            label += $"\n<size=80%><color=#888888>{string.Join(" | ", info)}</color></size>";

        if (!canExecute)
            label += "\n<color=red>[실행 불가]</color>";

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

    private string GetLocationTooltip(LocationSO location)
    {
        if (location == null) return "";
        string tooltip = $"<b>{location.locationName}</b>\n";
        if (!string.IsNullOrEmpty(location.description))
            tooltip += $"{location.description}\n";
        tooltip += $"<color=#888888>영업시간: {location.GetAvailableTimeText()}</color>";
        return tooltip;
    }

    private string GetActivityTooltip(ActivitySO activity, bool canExecute)
    {
        if (activity == null) return "";

        string tooltip = $"<b>{activity.activityName}</b>\n";

        if (!string.IsNullOrEmpty(activity.description))
            tooltip += $"{activity.description}\n\n";

        tooltip += $"<color=#888888>소요 시간: {FormatDuration(activity.durationMinutes)}</color>\n";

        string statChanges = GetStatChangeText(activity.statChanges);
        if (!string.IsNullOrEmpty(statChanges))
            tooltip += $"<color=#88ff88>{statChanges}</color>\n";

        if (activity.cost > 0)
            tooltip += $"<color=#ffff88>비용: {activity.cost:N0}원</color>\n";

        if (!canExecute)
        {
            string reason = GameManager.Instance?.ActivityService?.GetActivityBlockReason(activity) ?? "실행 불가";
            tooltip += $"\n<color=red>[{reason}]</color>";
        }

        return tooltip;
    }

    #endregion

    #region Tooltip System

    private void AddTooltipEvents(GameObject buttonObj, Func<string> getTooltipText)
    {
        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = buttonObj.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) => ShowTooltip(getTooltipText()));
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) => HideTooltip());
        trigger.triggers.Add(entryExit);
    }

    private void ShowTooltip(string text)
    {
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipText.text = text;
            tooltipPanel.SetActive(true);
        }
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    #endregion
}
