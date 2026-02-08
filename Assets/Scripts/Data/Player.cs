using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어의 모든 저장 가능한 상태를 담는 데이터 클래스
/// JSON 직렬화를 통해 저장/불러오기에 사용
/// </summary>
[Serializable]
public class Player
{
    public string playerName = "플레이어";

    public GameState gameState = new GameState();
    // 관계 (JsonUtility가 Dictionary 미지원이라 List 사용)
    public List<RelationshipData> relationships = new List<RelationshipData>();
    public List<string> viewedDialogues = new List<string>();

    // 인벤토리
    public List<InventoryData> inventory = new List<InventoryData>();
}

[Serializable]
public class RelationshipData
{
    public string characterId;
    public int affection;
}

[Serializable]
public class InventoryData
{
    public string itemId;
    public int count;
}
