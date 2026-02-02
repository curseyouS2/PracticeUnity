using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인벤토리 관리 - 아이템 추가/제거/사용
/// </summary>
public class InventoryController
{
    public event Action OnInventoryChanged;
    public event Action<ItemSO, int> OnItemAdded;
    public event Action<ItemSO, int> OnItemRemoved;
    public event Action<ItemSO> OnItemUsed;

    private Dictionary<ItemSO, int> items = new Dictionary<ItemSO, int>();

    public IReadOnlyDictionary<ItemSO, int> Items => items;
    public int TotalItemCount => items.Count;

    public InventoryController()
    {
        items = new Dictionary<ItemSO, int>();
    }

    /// <summary>
    /// 아이템 추가
    /// </summary>
    public bool AddItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (items.ContainsKey(item))
        {
            int newAmount = Mathf.Min(items[item] + amount, item.maxStack);
            int actualAdded = newAmount - items[item];
            items[item] = newAmount;

            if (actualAdded > 0)
            {
                OnItemAdded?.Invoke(item, actualAdded);
                OnInventoryChanged?.Invoke();
            }
        }
        else
        {
            items[item] = Mathf.Min(amount, item.maxStack);
            OnItemAdded?.Invoke(item, items[item]);
            OnInventoryChanged?.Invoke();
        }

        return true;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    public bool RemoveItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (!items.ContainsKey(item)) return false;

        if (items[item] <= amount)
        {
            int removed = items[item];
            items.Remove(item);
            OnItemRemoved?.Invoke(item, removed);
        }
        else
        {
            items[item] -= amount;
            OnItemRemoved?.Invoke(item, amount);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 아이템 사용 (소비 아이템)
    /// </summary>
    public bool UseItem(ItemSO item)
    {
        if (item == null) return false;
        if (!items.ContainsKey(item)) return false;
        if (!item.isConsumable) return false;

        // 효과 적용
        if (item.useEffect != null && StatusManager.Instance != null)
        {
            StatusManager.Instance.ApplyActivityEffect(item.useEffect);
        }

        OnItemUsed?.Invoke(item);

        // 소비
        RemoveItem(item, 1);

        return true;
    }

    /// <summary>
    /// 특정 아이템 보유 수량
    /// </summary>
    public int GetItemCount(ItemSO item)
    {
        if (item == null) return 0;
        return items.ContainsKey(item) ? items[item] : 0;
    }

    /// <summary>
    /// 특정 아이템 보유 여부
    /// </summary>
    public bool HasItem(ItemSO item, int amount = 1)
    {
        return GetItemCount(item) >= amount;
    }

    /// <summary>
    /// 특정 타입의 아이템 목록
    /// </summary>
    public List<KeyValuePair<ItemSO, int>> GetItemsByType(ItemType type)
    {
        var result = new List<KeyValuePair<ItemSO, int>>();

        foreach (var pair in items)
        {
            if (pair.Key.itemType == type)
            {
                result.Add(pair);
            }
        }

        return result;
    }

    /// <summary>
    /// 모든 아이템 목록 (UI 표시용)
    /// </summary>
    public List<InventorySlotData> GetAllSlots()
    {
        var slots = new List<InventorySlotData>();

        foreach (var pair in items)
        {
            slots.Add(new InventorySlotData
            {
                item = pair.Key,
                count = pair.Value
            });
        }

        return slots;
    }

    /// <summary>
    /// 인벤토리 초기화
    /// </summary>
    public void Clear()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
    }
}

/// <summary>
/// 인벤토리 슬롯 데이터 (UI 전달용)
/// </summary>
[System.Serializable]
public class InventorySlotData
{
    public ItemSO item;
    public int count;
}
