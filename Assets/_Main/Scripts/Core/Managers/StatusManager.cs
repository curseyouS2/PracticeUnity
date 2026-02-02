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

    public void Initialize(GameState gameState)
    {
        Status = new StatusController(gameState.stats);
        Health = new HealthController(gameState.health);
        Condition = new ConditionController(gameState.condition);
        Inventory = new InventoryController();
        Relationship = new RelationshipController();

        // 이벤트 연결
        Status.OnStatsChanged += _ => OnStatusUpdated?.Invoke();
        Health.OnHealthChanged += _ => OnStatusUpdated?.Invoke();
        Health.OnExhausted += HandleExhaustion;
        Condition.OnConditionChanged += _ => OnStatusUpdated?.Invoke();
        Inventory.OnInventoryChanged += () => OnInventoryUpdated?.Invoke();
    }

    /// <summary>
    /// 활동으로 인한 스탯 변화 적용
    /// </summary>
    public void ApplyActivityEffect(StatChanges changes)
    {
        float efficiency = CalculateTotalEfficiency();

        Status.ApplyChanges(changes, efficiency);
        Health.ApplyChanges(changes, efficiency);

        OnStatusUpdated?.Invoke();
    }

    /// <summary>
    /// 활동 실행 가능 여부 확인
    /// </summary>
    public bool CanExecuteActivity(ActivitySO activity)
    {
        // 컨디션 체크
        if (!Condition.CanPerformActivity(activity))
        {
            return false;
        }

        // 비용 체크
        if (!Status.CanAfford(activity.cost))
        {
            return false;
        }

        // 요구 스탯 체크
        if (activity.requirements != null)
        {
            foreach (var req in activity.requirements)
            {
                if (!MeetsRequirement(req))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 활동 실행 불가 사유 반환
    /// </summary>
    public string GetActivityBlockReason(ActivitySO activity)
    {
        if (!Condition.CanPerformActivity(activity))
        {
            return "현재 상태로는 이 활동을 할 수 없습니다.";
        }

        if (!Status.CanAfford(activity.cost))
        {
            return "소지금이 부족합니다.";
        }

        if (activity.requirements != null)
        {
            foreach (var req in activity.requirements)
            {
                if (!MeetsRequirement(req))
                {
                    return $"{req.statType} 스탯이 부족합니다. (필요: {req.minValue})";
                }
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
