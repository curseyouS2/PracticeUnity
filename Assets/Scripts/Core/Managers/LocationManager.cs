using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 장소 상태 관리 - 현재 위치, 이동, 이용 가능 장소 조회
/// </summary>
public class LocationManager : MonoBehaviour
{
    public static LocationManager Instance { get; private set; }

    [Header("Location Data")]
    [SerializeField] private LocationSO nowLocation;
    [SerializeField] private List<LocationSO> locations;

    public LocationSO NowLocation => nowLocation;
    public List<LocationSO> AllLocations => locations;

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
        // 장소 데이터가 없으면 Resources에서 자동 로드
        if (locations == null || locations.Count == 0)
        {
            locations = new List<LocationSO>(Resources.LoadAll<LocationSO>("Locations"));
            Debug.Log($"[LocationManager] Resources에서 {locations.Count}개의 장소 자동 로드");
        }

        // 세이브 데이터에서 현재 위치 설정
        if (!string.IsNullOrEmpty(startLocationId))
        {
            SetLocation(startLocationId);
        }
        else if (nowLocation == null)
        {
            // 기본값: "home" ID를 가진 장소 또는 첫 번째 장소
            nowLocation = locations.Find(loc => loc.id == "home") ?? locations.FirstOrDefault();
            if (nowLocation != null)
                Debug.Log($"[LocationManager] 기본 위치 설정: {nowLocation.locationName}");
        }
    }

    /// <summary>
    /// 특정 시간에 이용 가능한 장소 목록
    /// </summary>
    public List<LocationSO> GetAvailableLocations(int currentTimeMinutes)
    {
        return locations
            .Where(loc => loc.IsAvailableAt(currentTimeMinutes))
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

        var location = locations.Find(loc => loc.id == locationId);
        if (location != null)
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
    /// 현재 위치 설정 (LocationSO로)
    /// </summary>
    public void SetLocation(LocationSO location)
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

#if UNITY_EDITOR
    public void SetLocations(List<LocationSO> newLocations)
    {
        locations = newLocations;
    }
#endif
}
