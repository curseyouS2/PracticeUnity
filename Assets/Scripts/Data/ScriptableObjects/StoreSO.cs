using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Store", menuName = "Game/Store")]
public class StoreSO : LocationSO
{
    [Header("Shop Inventory")]
    public List<ShopItemEntry> shopItems;
}

[System.Serializable]
public class ShopItemEntry
{
    public string itemId;
    public int price;
}
