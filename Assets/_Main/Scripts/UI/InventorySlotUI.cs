using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// 인벤토리 슬롯 UI - 아이템 아이콘 + 수량 표시
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    public event Action<ItemSO> OnSlotClicked;

    [Header("UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private GameObject countBackground;

    [Header("Empty State")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Color emptyColor = new Color(1, 1, 1, 0.3f);
    [SerializeField] private Color filledColor = Color.white;

    private ItemSO currentItem;
    private int currentCount;

    public ItemSO CurrentItem => currentItem;

    private void Awake()
    {
        Clear();
    }

    /// <summary>
    /// 슬롯에 아이템 설정
    /// </summary>
    public void SetItem(ItemSO item, int count)
    {
        currentItem = item;
        currentCount = count;

        if (item != null && count > 0)
        {
            // 아이콘 표시
            if (iconImage != null)
            {
                iconImage.sprite = item.icon != null ? item.icon : emptySlotSprite;
                iconImage.color = filledColor;
            }

            // 수량 표시
            UpdateCountDisplay(count);
        }
        else
        {
            Clear();
        }
    }

    /// <summary>
    /// 수량만 업데이트
    /// </summary>
    public void UpdateCount(int count)
    {
        currentCount = count;
        UpdateCountDisplay(count);
    }

    private void UpdateCountDisplay(int count)
    {
        if (countText != null)
        {
            // 1개면 수량 숨김, 2개 이상이면 표시
            bool showCount = count > 1;

            if (countBackground != null)
                countBackground.SetActive(showCount);

            countText.gameObject.SetActive(showCount);
            countText.text = count.ToString();
        }
    }

    /// <summary>
    /// 슬롯 비우기
    /// </summary>
    public void Clear()
    {
        currentItem = null;
        currentCount = 0;

        if (iconImage != null)
        {
            iconImage.sprite = emptySlotSprite;
            iconImage.color = emptyColor;
        }

        if (countText != null)
            countText.gameObject.SetActive(false);

        if (countBackground != null)
            countBackground.SetActive(false);
    }

    /// <summary>
    /// 클릭 처리
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            OnSlotClicked?.Invoke(currentItem);
        }
    }
}
