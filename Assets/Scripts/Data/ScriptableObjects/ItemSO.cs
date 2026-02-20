using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Game/Item")]
public class ItemSO : ScriptableObject
{
    public string id;
    public string itemName;
    [TextArea(2, 4)]
    public string description;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Properties")]
    public ItemType itemType;
    public int maxStack = 99;
    public bool isConsumable = true;

    [Header("Effects (사용 시 효과)")]
    public StatChanges useEffect;
}

[GoogleSheet.Core.Type.UGS(typeof(ItemType))]
public enum ItemType
{
    Consumable,     // 소비 아이템
    Equipment,      // 장비
    KeyItem,        // 중요 아이템
    Material        // 재료
}
