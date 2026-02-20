using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 중앙 패널 - 기본: 활동 + 캐릭터 + 이동 버튼 / 이동 모드: 장소 목록 + 뒤로 버튼
/// </summary>
public class MainPanel : MonoBehaviour
{
    public event Action<DataTable.LocationTable, DataTable.ActivityTable> OnActivitySelected;
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

    private DataTable.LocationTable currentLocation;
    private bool isInMoveMode;
    private Stack<DataTable.LocationTable> locationHistory = new Stack<DataTable.LocationTable>();

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

    private void HandleLocationClicked(DataTable.LocationTable location)
    {
        if (currentLocation != null)
            locationHistory.Push(currentLocation);

        LocationManager.Instance?.SetLocation(location);
        isInMoveMode = false;

        if (location.entryScript != null && location.entryScript.Count > 0)
        {
            string script = location.entryScript[UnityEngine.Random.Range(0, location.entryScript.Count)];
            ShowMessage(script);
        }

        DisplayCurrentLocation();
    }

    private void HandleActivityClicked(DataTable.ActivityTable activity)
    {
        OnActivitySelected?.Invoke(currentLocation, activity);
    }

    private void HandleCharacterClicked(CharacterSO character)
    {
        OnCharacterTalkSelected?.Invoke(character);
    }

    private void HandleMoveButtonClicked()
    {
        isInMoveMode = true;
        ShowMoveScreen();
    }

    private void HandleBackClicked()
    {
        if (locationHistory.Count > 0)
        {
            var previousLocation = locationHistory.Pop();
            LocationManager.Instance?.SetLocation(previousLocation);
        }

        isInMoveMode = false;
        DisplayCurrentLocation();
    }

    #endregion

    #region Display

    /// <summary>
    /// 현재 장소 기반으로 활동 → 캐릭터 → 이동 버튼 표시
    /// </summary>
    public void DisplayCurrentLocation()
    {
        if (LocationManager.Instance == null) return;

        currentLocation = LocationManager.Instance.NowLocation;
        if (currentLocation == null) return;

        isInMoveMode = false;
        ClearContainer();

        // 1. 활동 (activities는 ID 목록 → Dictionary 조회)
        if (currentLocation.activities != null && currentLocation.activities.Count > 0)
        {
            var actDict = DataTable.ActivityTable.GetDictionary();
            foreach (var actId in currentLocation.activities)
            {
                if (actDict.TryGetValue(actId, out var activity))
                    CreateActivityButton(activity);
            }
        }

        // 2. 캐릭터
        if (CharacterManager.Instance != null)
        {
            var characters = CharacterManager.Instance.GetCharactersAt(currentLocation.id);
            foreach (var character in characters)
                CreateCharacterButton(character);
        }

        // 3. 이동 버튼 (connectedLocations가 있을 때만)
        if (currentLocation.connectedLocations != null && currentLocation.connectedLocations.Count > 0)
        {
            CreateMoveButton();
        }
    }

    /// <summary>
    /// 이동 모드 화면 - connectedLocations(ID 목록) 버튼 + 뒤로 버튼
    /// </summary>
    private void ShowMoveScreen()
    {
        ClearContainer();

        if (currentLocation?.connectedLocations != null)
        {
            int currentTime = RoutineManager.Instance != null ? RoutineManager.Instance.CurrentTimeMinutes : 0;
            var locDict = DataTable.LocationTable.GetDictionary();
            foreach (var locId in currentLocation.connectedLocations)
            {
                if (locId == currentLocation.id) continue;
                if (locDict.TryGetValue(locId, out var loc) && LocationManager.IsAvailableAt(loc, currentTime))
                    CreateLocationButton(loc);
            }
        }

        CreateBackButton();
    }

    /// <summary>
    /// 모든 이용 가능한 장소 표시 (게임 시작 시 사용)
    /// </summary>
    public void DisplayLocations(List<DataTable.LocationTable> locations)
    {
        ClearContainer();
        currentLocation = null;

        if (locations == null) return;

        foreach (var location in locations)
            CreateLocationButton(location);
    }

    #endregion

    #region Button Creation

    private void CreateLocationButton(DataTable.LocationTable location)
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

    private void CreateActivityButton(DataTable.ActivityTable activity)
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

    private void CreateMoveButton()
    {
        GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);

        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = "<color=#88aaff>▶ 이동</color>";

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(HandleMoveButtonClicked);
        }
    }

    private void CreateBackButton()
    {
        GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);

        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = "<color=#aaaaaa>◀ 뒤로</color>";

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(HandleBackClicked);
        }
    }

    #endregion

    #region Messages

    public void ShowActivityResult(DataTable.ActivityTable activity, float efficiency)
    {
        string result = $"<b>[{activity.activityName}]</b> 완료!\n";

        // activityScript 랜덤 출력 (List<string>)
        if (activity.activityScript != null && activity.activityScript.Count > 0)
        {
            string script = activity.activityScript[UnityEngine.Random.Range(0, activity.activityScript.Count)];
            result += script + "\n";
        }
        else if (!string.IsNullOrEmpty(activity.description))
        {
            result += activity.description + "\n";
        }

        if (efficiency < 1.0f)
            result += $"<color=yellow>(효율 {efficiency * 100:F0}%)</color>\n";

        string costText = GetStatChangeText(activity.statCost);
        if (!string.IsNullOrEmpty(costText))
            result += $"<color=#ff8888>소비: {costText}</color>\n";

        string rewardText = GetStatChangeText(activity.statReward);
        if (!string.IsNullOrEmpty(rewardText))
            result += $"<color=#88ff88>획득: {rewardText}</color>\n";

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

    public void SelectLocation(DataTable.LocationTable location)
    {
        if (location == null) return;
        currentLocation = location;
        DisplayCurrentLocation();
    }

    public DataTable.LocationTable CurrentLocation => currentLocation;

    private void ClearContainer()
    {
        if (buttonContainer == null) return;
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
    }

    private string BuildActivityLabel(DataTable.ActivityTable activity, bool canExecute)
    {
        string label = $"<b>{activity.activityName}</b>";

        List<string> info = new List<string>();
        if (activity.durationMinutes > 0)
            info.Add(FormatDuration(activity.durationMinutes));
        if (activity.statCost != null && activity.statCost.money > 0)
            info.Add($"{activity.statCost.money:N0}원");

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

    private string GetLocationTooltip(DataTable.LocationTable location)
    {
        if (location == null) return "";
        string tooltip = $"<b>{location.locationName}</b>\n";
        if (!string.IsNullOrEmpty(location.description))
            tooltip += $"{location.description}\n";
        tooltip += $"<color=#888888>영업시간: {LocationManager.GetAvailableTimeText(location)}</color>";
        return tooltip;
    }

    private string GetActivityTooltip(DataTable.ActivityTable activity, bool canExecute)
    {
        if (activity == null) return "";

        string tooltip = $"<b>{activity.activityName}</b>\n";

        if (!string.IsNullOrEmpty(activity.description))
            tooltip += $"{activity.description}\n\n";

        tooltip += $"<color=#888888>소요 시간: {FormatDuration(activity.durationMinutes)}</color>\n";

        string costText = GetStatChangeText(activity.statCost);
        if (!string.IsNullOrEmpty(costText))
            tooltip += $"<color=#ff8888>소비: {costText}</color>\n";

        string rewardText = GetStatChangeText(activity.statReward);
        if (!string.IsNullOrEmpty(rewardText))
            tooltip += $"<color=#88ff88>획득: {rewardText}</color>\n";

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
