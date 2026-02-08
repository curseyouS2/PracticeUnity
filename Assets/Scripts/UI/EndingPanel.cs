using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 엔딩 화면 표시
/// </summary>
public class EndingPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image endingImage;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    public void Show(EndingSO ending)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = ending.endingName;

        if (descriptionText != null)
            descriptionText.text = ending.description;

        if (endingImage != null && ending.endingImage != null)
            endingImage.sprite = ending.endingImage;
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnRestartClicked()
    {
        Hide();
        GameManager.Instance?.RestartGame();
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
