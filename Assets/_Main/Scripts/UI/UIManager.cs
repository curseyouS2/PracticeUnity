using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Left Panel - Stats")]
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI timeText;
    public Slider intelligenceBar;
    public TextMeshProUGUI intelligenceText;
    public Slider charmBar;
    public TextMeshProUGUI charmText;
    public Slider courageBar;
    public TextMeshProUGUI courageText;
    public Slider fatigueBar;
    public TextMeshProUGUI fatigueText;
    public TextMeshProUGUI moneyText;

    [Header("Center Panel - Activities")]
    public Transform locationButtonContainer;
    public GameObject locationButtonPrefab;
    public Transform activityButtonContainer;
    public GameObject activityButtonPrefab;
    public TextMeshProUGUI resultMessageText;

    [Header("Right Panel - Character")]
    public Image characterImage;
    public TextMeshProUGUI conditionText;
    public TextMeshProUGUI dialogueText;

    [Header("Ending Panel")]
    public GameObject endingPanel;
    public TextMeshProUGUI endingTitleText;
    public TextMeshProUGUI endingDescriptionText;

    private string currentLocationId;

    public void UpdateAllUI()
    {
        UpdateStatsPanel();
        UpdateCharacterPanel();
    }

    private void UpdateStatsPanel()
    {
        GameState state = GameManager.Instance.gameState;

        dayText.text = $"Day {state.currentDay}/{state.maxDays}";
        timeText.text = state.currentTimeSlot == "afternoon" ? "방과 후" : "밤";

        intelligenceBar.value = state.stats.intelligence / 100f;
        intelligenceText.text = $"지력: {state.stats.intelligence}";

        charmBar.value = state.stats.charm / 100f;
        charmText.text = $"매력: {state.stats.charm}";

        courageBar.value = state.stats.courage / 100f;
        courageText.text = $"용기: {state.stats.courage}";

        fatigueBar.value = state.stats.fatigue / 100f;
        fatigueText.text = $"피로도: {state.stats.fatigue}";

        // 피로도 색상 변경
        if (state.stats.fatigue >= 90)
            fatigueBar.fillRect.GetComponent<Image>().color = Color.red;
        else if (state.stats.fatigue >= 70)
            fatigueBar.fillRect.GetComponent<Image>().color = Color.yellow;
        else
            fatigueBar.fillRect.GetComponent<Image>().color = Color.green;

        moneyText.text = $"소지금: {state.stats.money}원";
    }

    public void DisplayLocations(List<Location> locations)
    {
        // 기존 버튼 제거
        foreach (Transform child in locationButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // 장소 버튼 생성
        foreach (var location in locations)
        {
            GameObject btnObj = Instantiate(locationButtonPrefab, locationButtonContainer);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            btnText.text = location.name;
            string locId = location.id; // 클로저 문제 방지
            btn.onClick.AddListener(() => OnLocationSelected(locId));
        }
    }

    private void OnLocationSelected(string locationId)
    {
        currentLocationId = locationId;
        Location location = GameManager.Instance.GetAvailableLocations()
            .Find(l => l.id == locationId);

        DisplayActivities(location.activities);
    }

    private void DisplayActivities(List<Activity> activities)
    {
        // 기존 버튼 제거
        foreach (Transform child in activityButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // 활동 버튼 생성
        foreach (var activity in activities)
        {
            GameObject btnObj = Instantiate(activityButtonPrefab, activityButtonContainer);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            // 버튼 텍스트 구성
            string btnLabel = $"{activity.name}\n{activity.description}\n";
            btnLabel += GetStatChangeText(activity.statChanges);
            if (activity.cost > 0) btnLabel += $"\n비용: {activity.cost}원";

            btnText.text = btnLabel;

            string actId = activity.id;
            btn.onClick.AddListener(() => OnActivitySelected(actId));
        }
    }

    private string GetStatChangeText(StatChanges changes)
    {
        string text = "";
        if (changes.intelligence != 0) text += $"지력 {changes.intelligence:+#;-#;0} ";
        if (changes.charm != 0) text += $"매력 {changes.charm:+#;-#;0} ";
        if (changes.courage != 0) text += $"용기 {changes.courage:+#;-#;0} ";
        if (changes.fatigue != 0) text += $"피로 {changes.fatigue:+#;-#;0} ";
        if (changes.money != 0) text += $"돈 {changes.money:+#;-#;0}원";
        return text;
    }

    private void OnActivitySelected(string activityId)
    {
        GameManager.Instance.ExecuteActivity(currentLocationId, activityId);
    }

    public void ShowActivityResult(Activity activity, float efficiency)
    {
        string result = $"[{activity.name}] 완료!\n";
        result += activity.description + "\n";
        if (efficiency < 1.0f)
        {
            result += $"(효율 {efficiency * 100}%)\n";
        }
        result += GetStatChangeText(activity.statChanges);

        resultMessageText.text = result;
    }

    public void ShowMessage(string message)
    {
        resultMessageText.text = message;
    }

    public void ShowWarning(string warning)
    {
        resultMessageText.text = $"<color=yellow>⚠ {warning}</color>";
    }

    private void UpdateCharacterPanel()
    {
        GameState state = GameManager.Instance.gameState;

        // 컨디션 텍스트
        if (state.conditions.isExhausted)
            conditionText.text = "상태: 탈진";
        else if (state.stats.fatigue >= 70)
            conditionText.text = "상태: 피곤함";
        else
            conditionText.text = "상태: 보통";

        // 대사 (피로도에 따라)
        if (state.stats.fatigue >= 90)
            dialogueText.text = "너무 힘들어요...";
        else if (state.stats.fatigue >= 70)
            dialogueText.text = "좀 쉬고 싶네요.";
        else
            dialogueText.text = "오늘은 뭘 할까요?";
    }

    public void DisplayEnding(Ending ending)
    {
        endingPanel.SetActive(true);
        endingTitleText.text = ending.name;
        endingDescriptionText.text = ending.description;
    }
}