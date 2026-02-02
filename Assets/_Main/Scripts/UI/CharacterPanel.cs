using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 우측 패널 - 캐릭터 이미지, 상태, 대사, 인벤토리 표시
/// </summary>
public class CharacterPanel : MonoBehaviour
{
    [Header("Character Display")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Header("Condition")]
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private Image conditionIcon;

    [Header("Dialogue")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Inventory")]
    [SerializeField] private Transform inventoryContainer;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private int maxVisibleSlots = 8;

    [Header("Dialogue Settings")]
    [SerializeField] private string[] normalDialogues = { "오늘은 뭘 할까요?", "좋은 하루예요!", "열심히 해봐요!" };
    [SerializeField] private string[] tiredDialogues = { "좀 쉬고 싶네요.", "피곤해요...", "오늘은 힘드네요." };
    [SerializeField] private string[] exhaustedDialogues = { "너무 힘들어요...", "더 이상 못하겠어요...", "쉬어야 해요..." };
    [SerializeField] private string[] happyDialogues = { "기분이 좋아요!", "오늘 뭐든 할 수 있을 것 같아요!", "최고의 하루예요!" };
    [SerializeField] private string[] sadDialogues = { "마음이 무거워요...", "기분이 별로예요.", "힘이 안 나요." };

    private List<InventorySlotUI> slotPool = new List<InventorySlotUI>();

    private void Start()
    {
        // 인벤토리 변경 이벤트 구독
        if (StatusManager.Instance != null)
        {
            StatusManager.Instance.OnInventoryUpdated += UpdateInventory;
        }

        InitializeSlotPool();
    }

    private void OnDestroy()
    {
        if (StatusManager.Instance != null)
        {
            StatusManager.Instance.OnInventoryUpdated -= UpdateInventory;
        }
    }

    private void InitializeSlotPool()
    {
        if (inventoryContainer == null || inventorySlotPrefab == null) return;

        // 슬롯 풀 생성
        for (int i = 0; i < maxVisibleSlots; i++)
        {
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryContainer);
            InventorySlotUI slot = slotObj.GetComponent<InventorySlotUI>();

            if (slot != null)
            {
                slot.OnSlotClicked += OnInventorySlotClicked;
                slot.Clear();
                slotPool.Add(slot);
            }
        }
    }

    public void UpdatePanel()
    {
        UpdateCondition();
        UpdateDialogue();
        UpdateInventory();
    }

    private void UpdateCondition()
    {
        var status = StatusManager.Instance;
        if (status == null) return;

        var condition = status.Condition;
        conditionText.text = $"상태: {condition.GetConditionDisplayName()}";

        // 컨디션에 따른 색상 변경
        conditionText.color = GetConditionColor(condition.CurrentCondition);
    }

    private void UpdateDialogue()
    {
        var status = StatusManager.Instance;
        if (status == null) return;

        var health = status.Health.CurrentHealth;
        var condition = status.Condition.CurrentCondition;

        string dialogue = GetDialogueByState(health.fatigue, condition);
        dialogueText.text = dialogue;
    }

    /// <summary>
    /// 인벤토리 UI 갱신
    /// </summary>
    public void UpdateInventory()
    {
        var status = StatusManager.Instance;
        if (status == null || status.Inventory == null) return;

        var items = status.Inventory.GetAllSlots();

        // 모든 슬롯 초기화
        foreach (var slot in slotPool)
        {
            slot.Clear();
        }

        // 아이템 표시
        for (int i = 0; i < items.Count && i < slotPool.Count; i++)
        {
            slotPool[i].SetItem(items[i].item, items[i].count);
        }
    }

    private void OnInventorySlotClicked(ItemSO item)
    {
        if (item == null) return;

        // 소비 아이템이면 사용
        if (item.isConsumable)
        {
            bool used = StatusManager.Instance?.Inventory.UseItem(item) ?? false;

            if (used)
            {
                ShowDialogue($"{item.itemName}을(를) 사용했습니다!");
            }
        }
        else
        {
            // 사용 불가 아이템은 설명만 표시
            ShowDialogue(item.description);
        }
    }

    private string GetDialogueByState(int fatigue, ConditionType condition)
    {
        // 컨디션 우선
        switch (condition)
        {
            case ConditionType.Happy:
                return GetRandomDialogue(happyDialogues);
            case ConditionType.Sad:
            case ConditionType.Depressed:
                return GetRandomDialogue(sadDialogues);
            case ConditionType.Exhausted:
                return GetRandomDialogue(exhaustedDialogues);
        }

        // 피로도 기반
        if (fatigue >= 90)
            return GetRandomDialogue(exhaustedDialogues);
        else if (fatigue >= 70)
            return GetRandomDialogue(tiredDialogues);
        else
            return GetRandomDialogue(normalDialogues);
    }

    private string GetRandomDialogue(string[] dialogues)
    {
        if (dialogues == null || dialogues.Length == 0)
            return "";

        return dialogues[Random.Range(0, dialogues.Length)];
    }

    private Color GetConditionColor(ConditionType condition)
    {
        return condition switch
        {
            ConditionType.Happy => Color.green,
            ConditionType.Exhausted or ConditionType.Sick or ConditionType.Injured => Color.red,
            ConditionType.Sad or ConditionType.Depressed => Color.gray,
            _ => Color.white
        };
    }

    public void SetCharacterImage(Sprite sprite)
    {
        if (characterImage != null && sprite != null)
            characterImage.sprite = sprite;
    }

    public void SetCharacterName(string name)
    {
        if (characterNameText != null)
            characterNameText.text = name;
    }

    /// <summary>
    /// 특정 대사 직접 표시
    /// </summary>
    public void ShowDialogue(string dialogue)
    {
        if (dialogueText != null)
            dialogueText.text = dialogue;
    }
}
