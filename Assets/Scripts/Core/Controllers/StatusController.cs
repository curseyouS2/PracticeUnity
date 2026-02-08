using UnityEngine;
using System;

/// <summary>
/// 기본 스탯(지력, 매력, 용기, 도덕성, 돈) 관리
/// </summary>
public class StatusController
{
    public event Action<Stats> OnStatsChanged;

    private Stats stats;

    public Stats CurrentStats => stats;

    public StatusController(Stats initialStats)
    {
        stats = initialStats;
    }

    public void ApplyChanges(StatChanges changes, float efficiency = 1f)
    {
        stats.intelligence += Mathf.RoundToInt(changes.intelligence * efficiency);
        stats.charm += Mathf.RoundToInt(changes.charm * efficiency);
        stats.courage += Mathf.RoundToInt(changes.courage * efficiency);
        stats.moral += Mathf.RoundToInt(changes.moral * efficiency);
        stats.money += Mathf.RoundToInt(changes.money * efficiency);

        ClampStats();
        OnStatsChanged?.Invoke(stats);
    }

    public void AddMoney(int amount)
    {
        stats.money += amount;
        OnStatsChanged?.Invoke(stats);
    }

    public void SpendMoney(int amount)
    {
        stats.money -= amount;
        OnStatsChanged?.Invoke(stats);
    }

    public bool CanAfford(int cost)
    {
        return stats.money >= cost;
    }

    public int GetStatValue(StatType statType)
    {
        return statType switch
        {
            StatType.Intelligence => stats.intelligence,
            StatType.Charm => stats.charm,
            StatType.Courage => stats.courage,
            StatType.Moral => stats.moral,
            StatType.Money => stats.money,
            _ => 0
        };
    }

    public void SetStat(StatType statType, int value)
    {
        switch (statType)
        {
            case StatType.Intelligence: stats.intelligence = value; break;
            case StatType.Charm: stats.charm = value; break;
            case StatType.Courage: stats.courage = value; break;
            case StatType.Moral: stats.moral = value; break;
            case StatType.Money: stats.money = value; break;
        }
        ClampStats();
        OnStatsChanged?.Invoke(stats);
    }

    private void ClampStats()
    {
        stats.intelligence = Mathf.Clamp(stats.intelligence, 0, 999);
        stats.charm = Mathf.Clamp(stats.charm, 0, 999);
        stats.courage = Mathf.Clamp(stats.courage, 0, 999);
        stats.moral = Mathf.Clamp(stats.moral, 0, 100);
    }
}
