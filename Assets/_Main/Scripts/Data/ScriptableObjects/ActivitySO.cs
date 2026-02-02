using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Activity", menuName = "Game/Activity")]
public class ActivitySO : ScriptableObject
{
    public string id;
    public string activityName;
    [TextArea(2, 4)]
    public string description;

    [Header("Time")]
    [Tooltip("소요 시간 (분 단위, 예: 60 = 1시간)")]
    public int durationMinutes = 60;

    [Header("Cost")]
    public int cost;

    [Header("Stat Changes")]
    public StatChanges statChanges;

    [Header("Requirements")]
    public List<StatRequirement> requirements;
}

[System.Serializable]
public class StatRequirement
{
    public StatType statType;
    public int minValue;
}

public enum StatType
{
    Intelligence,
    Charm,
    Courage,
    Moral,
    Money,
    Physical,
    Mental,
    Fatigue
}
