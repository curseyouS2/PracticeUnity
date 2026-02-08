using UnityEngine;
using TMPro;

/// <summary>
/// 좌측 패널 - 날짜/시간, 스탯, 피로도, 소지금 표시
/// 각 수치는 StatusObject 프리팹을 통해 표시 (moral 제외)
/// </summary>
public class StatusPanel : MonoBehaviour
{
    [Header("Time Info")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timeText;
    // [SerializeField] private TextMeshProUGUI remainingTimeText;
    [SerializeField] private TextMeshProUGUI locationText;

    [Header("Stats")]
    [SerializeField] private StatusObject intelligenceGauge;
    [SerializeField] private StatusObject charmGauge;
    [SerializeField] private StatusObject courageGauge;

    [Header("Health")]
    [SerializeField] private StatusObject physicalGauge;
    [SerializeField] private StatusObject mentalGauge;
    [SerializeField] private StatusObject fatigueGauge;

    [Header("Money")]
    [SerializeField] private TextMeshProUGUI moneyText;

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
        locationText.text = LocationManager.Instance != null
            ? LocationManager.Instance.GetLocationName()
            : "???";
        // if (remainingTimeText != null)
        //     remainingTimeText.text = routine.GetRemainingTimeText();
    }

    private void UpdateStats()
    {
        var status = StatusManager.Instance;
        if (status == null) return;

        var stats = status.Status.CurrentStats;

        intelligenceGauge.SetValue(stats.intelligence);
        charmGauge.SetValue(stats.charm);
        courageGauge.SetValue(stats.courage);
    }

    private void UpdateHealth()
    {
        var status = StatusManager.Instance;
        if (status == null) return;

        var health = status.Health.CurrentHealth;

        physicalGauge.SetValue(health.physical);
        mentalGauge.SetValue(health.mental);
        fatigueGauge.SetValue(health.fatigue);
    }

    private void UpdateMoney()
    {
        var status = StatusManager.Instance;
        if (status == null) return;

        var stats = status.Status.CurrentStats;
        moneyText.text = stats.money.ToString();
    }
}
