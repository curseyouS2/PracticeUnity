using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Location", menuName = "Game/Location")]
public class LocationSO : ScriptableObject
{
    public string id;
    public string locationName;
    [TextArea(2, 4)]
    public string description;

    [Header("Availability (24시간 기준)")]
    [Tooltip("이용 가능 시작 시간 (예: 9시 = 540분)")]
    public int openTimeMinutes = 540;  // 09:00
    [Tooltip("이용 가능 종료 시간 (예: 21시 = 1260분)")]
    public int closeTimeMinutes = 1260; // 21:00

    [Header("Activities")]
    public List<ActivitySO> activities;

    [Header("Connected Locations")]
    [Tooltip("이 장소에서 이동 가능한 장소 목록")]
    public List<LocationSO> connectedLocations;

    [Header("Visuals")]
    public Sprite locationIcon;
    public Sprite backgroundImage;

    /// <summary>
    /// 특정 시간에 이용 가능한지 확인
    /// </summary>
    public bool IsAvailableAt(int currentTimeMinutes)
    {
        // 자정을 넘어가는 경우 처리 (예: 22:00 ~ 02:00)
        if (closeTimeMinutes < openTimeMinutes)
        {
            return currentTimeMinutes >= openTimeMinutes || currentTimeMinutes < closeTimeMinutes;
        }

        return currentTimeMinutes >= openTimeMinutes && currentTimeMinutes < closeTimeMinutes;
    }

    /// <summary>
    /// 이용 가능 시간 텍스트 (예: "09:00 ~ 21:00")
    /// </summary>
    public string GetAvailableTimeText()
    {
        return $"{TimeUtility.MinutesToTimeString(openTimeMinutes)} ~ {TimeUtility.MinutesToTimeString(closeTimeMinutes)}";
    }
}
