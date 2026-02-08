using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 개별 스탯 게이지 UI (프리팹에 부착)
/// StatusText + StatusSlider로 구성
/// </summary>
public class StatusObject : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Slider statusSlider;

    [Header("Settings")]
    [SerializeField] private string label;
    [SerializeField] private int maxValue = 100;
    [SerializeField] private bool inverseColor;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;

    public void SetValue(int value)
    {
        if (statusSlider != null)
            statusSlider.value = Mathf.Clamp01(value / (float)maxValue);

        // if (statusText != null)
        //     statusText.text = $"{label}: {value}";

        UpdateBarColor(value);
    }

    private void UpdateBarColor(int value)
    {
        if (statusSlider == null || statusSlider.fillRect == null) return;

        var image = statusSlider.fillRect.GetComponent<Image>();
        if (image == null) return;

        if (inverseColor)
        {
            // 피로도: 높을수록 위험
            if (value >= 90) image.color = dangerColor;
            else if (value >= 70) image.color = warningColor;
            else image.color = normalColor;
        }
        else
        {
            // 체력/스탯: 낮을수록 위험
            if (value <= 20) image.color = dangerColor;
            else if (value <= 40) image.color = warningColor;
            else image.color = normalColor;
        }
    }
}
