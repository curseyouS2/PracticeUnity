using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 장소 상태 관리 - 현재 위치, 이동, 이용 가능 장소 조회
/// </summary>
public class LocationManager : MonoBehaviour
{
    public static LocationManager Instance { get; private set; }

    private DataTable.LocationTable nowLocation;
    private List<DataTable.LocationTable> locations;

    public DataTable.LocationTable NowLocation => nowLocation;
    public List<DataTable.LocationTable> AllLocations => locations;

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

    public void Initialize(string startLocationId = null)
    {
        locations = DataTable.LocationTable.GetList();
        Debug.Log($"[LocationManager] DataTable에서 {locations.Count}개의 장소 로드");

        if (!string.IsNullOrEmpty(startLocationId) && SetLocation(startLocationId))
        {
            // startLocationId로 위치 설정 성공
        }
        else
        {
            nowLocation = locations.Find(loc => loc.id == "loc_home") ?? locations.FirstOrDefault();
            if (nowLocation != null)
                Debug.Log($"[LocationManager] 기본 위치로 설정: {nowLocation.locationName}");
        }
    }

    /// <summary>
    /// 특정 시간에 이용 가능한 장소 목록
    /// </summary>
    public List<DataTable.LocationTable> GetAvailableLocations(int currentTimeMinutes)
    {
        return locations
            .Where(loc => IsAvailableAt(loc, currentTimeMinutes))
            .ToList();
    }

    /// <summary>
    /// 현재 위치 설정 (LocationID로)
    /// </summary>
    public bool SetLocation(string locationId)
    {
        if (string.IsNullOrEmpty(locationId))
        {
            Debug.LogWarning("[LocationManager] 유효하지 않은 LocationID");
            return false;
        }

        var dict = DataTable.LocationTable.GetDictionary();
        if (dict.TryGetValue(locationId, out var location))
        {
            nowLocation = location;
            Debug.Log($"[LocationManager] 현재 위치 설정: {nowLocation.locationName} ({locationId})");
            return true;
        }
        else
        {
            Debug.LogWarning($"[LocationManager] LocationID '{locationId}'를 찾을 수 없습니다");
            return false;
        }
    }

    /// <summary>
    /// 현재 위치 설정 (LocationTable로)
    /// </summary>
    public void SetLocation(DataTable.LocationTable location)
    {
        if (location != null)
        {
            nowLocation = location;
            Debug.Log($"[LocationManager] 현재 위치 설정: {nowLocation.locationName}");
        }
    }

    /// <summary>
    /// 현재 위치의 ID 반환
    /// </summary>
    public string GetCurrentLocationId()
    {
        return nowLocation != null ? nowLocation.id : null;
    }

    /// <summary>
    /// 현재 위치 이름 반환
    /// </summary>
    public string GetLocationName()
    {
        return nowLocation != null ? nowLocation.locationName : "???";
    }

    /// <summary>
    /// 장소 입장 조건 충족 여부 확인
    /// </summary>
    public bool CanEnterLocation(DataTable.LocationTable location)
    {
        if (location == null) return false;
        if (StatusManager.Instance == null) return true;

        if (location.entryConditions != null)
        {
            foreach (var req in location.entryConditions)
            {
                if (StatusManager.Instance.GetStatValue(req.statType) < req.minValue)
                    return false;
            }
        }

        if (location.entryItemConditions != null)
        {
            foreach (var req in location.entryItemConditions)
            {
                if (!StatusManager.Instance.Inventory.HasItem(StatusManager.Instance.GetItemById(req.itemId), req.amount))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 장소 입장 불가 사유 반환
    /// </summary>
    public string GetEntryBlockReason(DataTable.LocationTable location)
    {
        if (location == null) return "장소 정보가 없습니다.";
        if (StatusManager.Instance == null) return null;

        if (location.entryConditions != null)
        {
            foreach (var req in location.entryConditions)
            {
                if (StatusManager.Instance.GetStatValue(req.statType) < req.minValue)
                    return $"{req.statType} 스탯이 부족합니다. (필요: {req.minValue})";
            }
        }

        if (location.entryItemConditions != null)
        {
            foreach (var req in location.entryItemConditions)
            {
                var item = StatusManager.Instance.GetItemById(req.itemId);
                if (!StatusManager.Instance.Inventory.HasItem(item, req.amount))
                    return $"아이템 '{item?.itemName}'이(가) 부족합니다. (필요: {req.amount})";
            }
        }

        return null;
    }

    public static bool IsAvailableAt(DataTable.LocationTable loc, int currentTimeMinutes)
    {
        if (loc.closeTimeMinutes < loc.openTimeMinutes)
            return currentTimeMinutes >= loc.openTimeMinutes || currentTimeMinutes < loc.closeTimeMinutes;
        return currentTimeMinutes >= loc.openTimeMinutes && currentTimeMinutes < loc.closeTimeMinutes;
    }

    public static string GetAvailableTimeText(DataTable.LocationTable loc)
    {
        return $"{TimeUtility.MinutesToTimeString(loc.openTimeMinutes)} ~ {TimeUtility.MinutesToTimeString(loc.closeTimeMinutes)}";
    }
}
