using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 캐릭터의 모든 상태를 통합 관리하는 매니저
/// StatusController, HealthController, ConditionController, InventoryController를 조율
/// </summary>
public class StatusManager : MonoBehaviour
{
    public static StatusManager Instance { get; private set; }

    public event Action OnStatusUpdated;
    public event Action OnInventoryUpdated;

    public StatusController Status { get; private set; }
    public HealthController Health { get; private set; }
    public ConditionController Condition { get; private set; }
    public InventoryController Inventory { get; private set; }
    public RelationshipController Relationship { get; private set; }

    private Dictionary<string, ItemSO> itemRegistry = new Dictionary<string, ItemSO>();

    public ItemSO GetItemById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return itemRegistry.TryGetValue(id, out var item) ? item : null;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize() => Initialize(new Player());

    public void Initialize(Player player)
    {
        Status = new StatusController(player.gameState.stats);
        Health = new HealthController(player.gameState.health);
        Condition = new ConditionController(player.gameState.condition);
        Inventory = new InventoryController();
        Relationship = new RelationshipController();

        // 관계 데이터 로드
        foreach (var r in player.relationships)
            Relationship.SetAffection(r.characterId, r.affection);

        foreach (var d in player.viewedDialogues)
            Relationship.MarkDialogueViewed(d);

        // 인벤토리 로드
        LoadInventory(player.inventory);

        // 이벤트 연결
        Status.OnStatsChanged += _ => OnStatusUpdated?.Invoke();
        Health.OnHealthChanged += _ => OnStatusUpdated?.Invoke();
        Health.OnExhausted += HandleExhaustion;
        Condition.OnConditionChanged += _ => OnStatusUpdated?.Invoke();
        Inventory.OnInventoryChanged += () => OnInventoryUpdated?.Invoke();
        Inventory.OnItemEffectRequested += (changes) => ApplyActivityEffect(changes);
    }

    /// <summary>
    /// 현재 상태를 Player 객체에 내보내기 (저장용)
    /// </summary>
    public void ExportToPlayer(Player player)
    {
        player.gameState.stats = Status.CurrentStats;
        player.gameState.health = Health.CurrentHealth;
        player.gameState.condition = Condition.CurrentCondition;

        // 관계
        player.relationships = new List<RelationshipData>();
        foreach (var kvp in Relationship.GetAllAffections())
        {
            player.relationships.Add(new RelationshipData
            {
                characterId = kvp.Key,
                affection = kvp.Value
            });
        }

        // 본 대화
        player.viewedDialogues = new List<string>(Relationship.GetViewedDialogues());

        // 인벤토리
        player.inventory = new List<InventoryData>();
        foreach (var slot in Inventory.GetAllSlots())
        {
            player.inventory.Add(new InventoryData
            {
                itemId = slot.item.id,
                count = slot.count
            });
        }
    }

    private void LoadInventory(List<InventoryData> inventoryData)
    {
        // itemId 조회용 레지스트리 항상 빌드
        itemRegistry.Clear();
        foreach (var item in Resources.LoadAll<ItemSO>(""))
            itemRegistry[item.id] = item;

        if (inventoryData == null || inventoryData.Count == 0) return;

        foreach (var data in inventoryData)
        {
            if (itemRegistry.TryGetValue(data.itemId, out var item))
                Inventory.AddItem(item, data.count);
            else
                Debug.LogWarning($"[StatusManager] 아이템 '{data.itemId}'를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 활동으로 인한 스탯 변화 적용
    /// </summary>
    public void ApplyActivityEffect(StatChanges changes)
    {
        float efficiency = CalculateTotalEfficiency();

        Status.ApplyChanges(changes, efficiency);
        Health.ApplyChanges(changes, efficiency);
        // OnStatusUpdated는 각 컨트롤러의 이벤트 체인으로 이미 발생하므로 중복 호출 제거
    }

    /// <summary>
    /// 스탯 비용 차감 (양수 입력값을 음수로 변환하여 적용, efficiency 미적용)
    /// </summary>
    public void ApplyStatCost(StatChanges cost)
    {
        if (cost == null) return;

        var negated = new StatChanges
        {
            intelligence = -cost.intelligence,
            charm = -cost.charm,
            courage = -cost.courage,
            moral = -cost.moral,
            money = -cost.money,
            fatigue = -cost.fatigue,
            physical = -cost.physical,
            mental = -cost.mental
        };

        Status.ApplyChanges(negated);
        Health.ApplyChanges(negated);
    }

    /// <summary>
    /// 활동 실행 가능 여부 확인
    /// </summary>
    public bool CanExecuteActivity(DataTable.ActivityTable activity)
    {
        if (!Condition.CanPerformActivity(activity))
            return false;

        // 비용 체크 (money)
        if (activity.statCost != null && activity.statCost.money > 0)
        {
            if (!Status.CanAfford(activity.statCost.money))
                return false;
        }

        // 스탯 조건 체크
        if (activity.statConditions != null)
        {
            foreach (var req in activity.statConditions)
            {
                if (!MeetsRequirement(req))
                    return false;
            }
        }

        // 아이템 조건 체크
        if (activity.itemConditions != null)
        {
            foreach (var req in activity.itemConditions)
            {
                if (!Inventory.HasItem(GetItemById(req.itemId), req.amount))
                    return false;
            }
        }

        // 아이템 비용 보유 체크
        if (activity.itemCost != null)
        {
            foreach (var req in activity.itemCost)
            {
                if (!Inventory.HasItem(GetItemById(req.itemId), req.amount))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 활동 실행 불가 사유 반환
    /// </summary>
    public string GetActivityBlockReason(DataTable.ActivityTable activity)
    {
        if (!Condition.CanPerformActivity(activity))
            return "현재 상태로는 이 활동을 할 수 없습니다.";

        if (activity.statCost != null && activity.statCost.money > 0)
        {
            if (!Status.CanAfford(activity.statCost.money))
                return "소지금이 부족합니다.";
        }

        if (activity.statConditions != null)
        {
            foreach (var req in activity.statConditions)
            {
                if (!MeetsRequirement(req))
                    return $"{req.statType} 스탯이 부족합니다. (필요: {req.minValue})";
            }
        }

        if (activity.itemConditions != null)
        {
            foreach (var req in activity.itemConditions)
            {
                var item = GetItemById(req.itemId);
                if (!Inventory.HasItem(item, req.amount))
                    return $"아이템 '{item?.itemName}'이(가) 부족합니다. (필요: {req.amount})";
            }
        }

        if (activity.itemCost != null)
        {
            foreach (var req in activity.itemCost)
            {
                var item = GetItemById(req.itemId);
                if (!Inventory.HasItem(item, req.amount))
                    return $"아이템 '{item?.itemName}'이(가) 부족합니다. (필요: {req.amount})";
            }
        }

        return null;
    }

    public float CalculateTotalEfficiency()
    {
        float healthEfficiency = Health.GetEfficiency();
        float conditionModifier = Condition.GetConditionEfficiencyModifier();

        return healthEfficiency * conditionModifier;
    }

    public void ProcessDayEnd()
    {
        Health.DailyRecovery();
        Condition.ProcessDayEnd();

        // 탈진 상태 해제
        if (Condition.CurrentCondition == ConditionType.Exhausted && !Health.IsExhausted)
        {
            Condition.ClearCondition();
        }

        OnStatusUpdated?.Invoke();
    }

    public int GetStatValue(StatType statType)
    {
        return statType switch
        {
            StatType.Intelligence or
            StatType.Charm or
            StatType.Courage or
            StatType.Moral or
            StatType.Money => Status.GetStatValue(statType),

            StatType.Physical or
            StatType.Mental or
            StatType.Fatigue => Health.GetHealthValue(statType),

            _ => 0
        };
    }

    private bool MeetsRequirement(StatRequirement req)
    {
        int currentValue = GetStatValue(req.statType);
        return currentValue >= req.minValue;
    }

    private void HandleExhaustion()
    {
        Condition.SetCondition(ConditionType.Exhausted);
    }
}
