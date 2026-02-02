using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 좌측 패널 - 날짜/시간, 스탯, 피로도, 소지금 표시
/// </summary>
public class StatusPanel : MonoBehaviour
{
    [Header("Time Info")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI remainingTimeText;

    [Header("Stats")]
    [SerializeField] private Slider intelligenceBar;
    [SerializeField] private TextMeshProUGUI intelligenceText;
    [SerializeField] private Slider charmBar;
    [SerializeField] private TextMeshProUGUI charmText;
    [SerializeField] private Slider courageBar;
    [SerializeField] private TextMeshProUGUI courageText;

    [Header("Health")]
    [SerializeField] private Slider fatigueBar;
    [SerializeField] private TextMeshProUGUI fatigueText;
    [SerializeField] private Slider physicalBar;
    [SerializeField] private TextMeshProUGUI physicalText;
    [SerializeField] private Slider mentalBar;
    [SerializeField] private TextMeshProUGUI mentalText;

    [Header("Money")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;

    public void UpdatePanel()
    {
        UpdateTimeInfo();
        UpdateStats();
        UpdateHealth();
        UpdateMoney();
    }

    private void UpdateTimeInfo()
    {
        var routine = RoutineManager.Instance;
        if (routine == null) return;

        dayText.text = routine.GetDayDisplayText();
        timeText.text = $"{routine.GetTimeDisplayText()} ({routine.GetTimePeriodName()})";

        if (remainingTimeText != null)
            remainingTimeText.text = routine.GetRemainingTimeText();
    }

    private void UpdateStats()
    {
        var status = StatusManager.Instance;
        if (status == null) return;

        var stats = status.Status.CurrentStats;

        SetStatBar(intelligenceBar, intelligenceText, "지력", stats.intelligence, 100);
        SetStatBar(charmBar, charmText, "매력", stats.charm, 100);
        SetStatBar(courageBar, courageText, "용기", stats.courage, 100);
    }

    private void UpdateHealth()
    {
        var status = StatusManager.Instance;
        if (status == null) return;

        var health = status.Health.CurrentHealth;

        // 피로도 (높을수록 나쁨)
        if (fatigueBar != null)
        {
            fatigueBar.value = health.fatigue / 100f;
            if (fatigueText != null)
                fatigueText.text = $"피로도: {health.fatigue}";

            UpdateBarColor(fatigueBar, health.fatigue, true);
        }

        // 체력
        if (physicalBar != null)
        {
            physicalBar.value = health.physical / 100f;
            if (physicalText != null)
                physicalText.text = $"체력: {health.physical}";

            UpdateBarColor(physicalBar, health.physical, false);
        }

        // 정신력
        if (mentalBar != null)
        {
            mentalBar.value = health.mental / 100f;
            if (mentalText != null)
                mentalText.text = $"정신력: {health.mental}";

            UpdateBarColor(mentalBar, health.mental, false);
        }
    }

    private void UpdateMoney()
    {
        var status = StatusManager.Instance;
        if (status == null) return;

        var stats = status.Status.CurrentStats;
        moneyText.text = $"소지금: {stats.money:N0}원";
    }

    private void SetStatBar(Slider bar, TextMeshProUGUI text, string label, int value, int maxValue)
    {
        if (bar != null)
            bar.value = value / (float)maxValue;

        if (text != null)
            text.text = $"{label}: {value}";
    }

    private void UpdateBarColor(Slider bar, int value, bool inverseLogic)
    {
        if (bar == null || bar.fillRect == null) return;

        var image = bar.fillRect.GetComponent<Image>();
        if (image == null) return;

        if (inverseLogic)
        {
            // 피로도: 높을수록 위험
            if (value >= 90)
                image.color = dangerColor;
            else if (value >= 70)
                image.color = warningColor;
            else
                image.color = normalColor;
        }
        else
        {
            // 체력/정신력: 낮을수록 위험
            if (value <= 20)
                image.color = dangerColor;
            else if (value <= 40)
                image.color = warningColor;
            else
                image.color = normalColor;
        }
    }
}
