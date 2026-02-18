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

    [Header("Script")]
    [Tooltip("활동 시 로그에 출력되는 텍스트 (랜덤 1개 출력)")]
    public string[] activityScript;

    [Header("Cost (양수로 입력 → 내부에서 차감)")]
    public StatChanges statCost;
    public List<ItemRequirement> itemCost;

    [Header("Reward")]
    public StatChanges statReward;
    public List<ItemReward> itemReward;

    [Header("Conditions")]
    public List<StatRequirement> statConditions;
    public List<ItemRequirement> itemConditions;
}

[System.Serializable]
public class StatRequirement
{
    public StatType statType;
    public int minValue;
}

[System.Serializable]
public class ItemRequirement
{
    public ItemSO item;
    public int amount = 1;
}

[System.Serializable]
public class ItemReward
{
    public ItemSO item;
    public int amount = 1;
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
