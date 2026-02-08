using UnityEngine;
using System;
using System.IO;

/// <summary>
/// Player 데이터를 JSON으로 저장/불러오기
/// 저장 경로: Application.persistentDataPath/save/player.json
/// </summary>
public static class SaveLoadManager
{
    private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "save");
    private const string SaveFileName = "player.json";
    private static string SaveFilePath => Path.Combine(SaveDirectory, SaveFileName);

    /// <summary>
    /// Player 데이터를 JSON 파일로 저장
    /// </summary>
    public static bool Save(Player player)
    {
        try
        {
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);

            string json = JsonUtility.ToJson(player, true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"[SaveLoadManager] 저장 완료: {SaveFilePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadManager] 저장 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// JSON 파일에서 Player 데이터 불러오기
    /// 세이브 파일이 없으면 null 반환
    /// </summary>
    public static Player Load()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("[SaveLoadManager] 세이브 파일 없음");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            var player = JsonUtility.FromJson<Player>(json);
            Debug.Log($"[SaveLoadManager] 불러오기 완료: Day {player.gameState.currentDay}");
            return player;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoadManager] 불러오기 실패: {e.Message}");
            return null;
        }
    }

    public static bool HasSaveData()
    {
        return File.Exists(SaveFilePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("[SaveLoadManager] 세이브 삭제 완료");
        }
    }
}
