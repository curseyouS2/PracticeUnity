using UnityEngine;
using System;

/// <summary>
/// 체력(physical, mental, fatigue) 관리
/// </summary>
public class HealthController
{
    public event Action<Health> OnHealthChanged;
    public event Action OnExhausted;
    public event Action OnCriticalHealth;

    private Health health;

    public Health CurrentHealth => health;
    public bool IsExhausted => health.fatigue >= 100;
    public bool IsTired => health.fatigue >= 70;

    public HealthController(Health initialHealth)
    {
        health = initialHealth;
    }

    public void ApplyChanges(StatChanges changes, float efficiency = 1f)
    {
        health.physical += Mathf.RoundToInt(changes.physical * efficiency);
        health.mental += Mathf.RoundToInt(changes.mental * efficiency);
        health.fatigue += changes.fatigue; // 피로도는 효율 미적용

        ClampHealth();
        CheckHealthStatus();
        OnHealthChanged?.Invoke(health);
    }

    public void AddFatigue(int amount)
    {
        health.fatigue += amount;
        ClampHealth();
        CheckHealthStatus();
        OnHealthChanged?.Invoke(health);
    }

    public void RecoverFatigue(int amount)
    {
        health.fatigue = Mathf.Max(0, health.fatigue - amount);
        OnHealthChanged?.Invoke(health);
    }

    public void RecoverHealth(int physicalAmount, int mentalAmount)
    {
        health.physical = Mathf.Min(100, health.physical + physicalAmount);
        health.mental = Mathf.Min(100, health.mental + mentalAmount);
        OnHealthChanged?.Invoke(health);
    }

    public void DailyRecovery()
    {
        RecoverFatigue(10);
    }

    public float GetEfficiency()
    {
        if (health.fatigue >= 90) return 0.3f;
        if (health.fatigue >= 70) return 0.5f;
        if (health.fatigue >= 50) return 0.8f;
        return 1.0f;
    }

    public int GetHealthValue(StatType statType)
    {
        return statType switch
        {
            StatType.Physical => health.physical,
            StatType.Mental => health.mental,
            StatType.Fatigue => health.fatigue,
            _ => 0
        };
    }

    private void ClampHealth()
    {
        health.physical = Mathf.Clamp(health.physical, 0, 100);
        health.mental = Mathf.Clamp(health.mental, 0, 100);
        health.fatigue = Mathf.Clamp(health.fatigue, 0, 100);
    }

    private void CheckHealthStatus()
    {
        if (health.fatigue >= 100)
        {
            OnExhausted?.Invoke();
        }

        if (health.physical <= 20 || health.mental <= 20)
        {
            OnCriticalHealth?.Invoke();
        }
    }
}
