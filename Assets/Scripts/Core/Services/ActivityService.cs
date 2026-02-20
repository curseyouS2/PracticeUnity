using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 활동 실행 로직 통합 - 검증, 비용 지불, 효과 적용, 시간 진행을 한 곳에서 처리
/// 의존성을 생성자로 주입받아 static Instance 참조를 사용하지 않음
/// </summary>
public class ActivityService
{
    public event Action<DataTable.ActivityTable, float> OnActivityExecuted;

    private readonly StatusManager statusManager;
    private readonly RoutineManager routineManager;

    public ActivityService(StatusManager statusManager, RoutineManager routineManager)
    {
        this.statusManager = statusManager;
        this.routineManager = routineManager;
    }

    /// <summary>
    /// 활동 실행. 성공 여부와 실패 사유를 반환
    /// </summary>
    public (bool success, string message) ExecuteActivity(DataTable.LocationTable location, DataTable.ActivityTable activity)
    {
        // 1. 시간 검증
        if (activity.durationMinutes > routineManager.Time.GetRemainingMinutes())
        {
            string reason = $"시간이 부족합니다. (필요: {activity.durationMinutes}분, 남은 시간: {routineManager.Time.GetRemainingMinutes()}분)";
            return (false, reason);
        }

        // 2. 스탯/비용/컨디션/아이템 검증
        if (!statusManager.CanExecuteActivity(activity))
        {
            string reason = statusManager.GetActivityBlockReason(activity);
            return (false, reason);
        }

        // 3. 효율 계산 (효과 적용 전)
        float efficiency = statusManager.CalculateTotalEfficiency();

        // 4. 비용 지불 (스탯)
        statusManager.ApplyStatCost(activity.statCost);

        // 5. 아이템 비용 소비
        if (activity.itemCost != null)
        {
            foreach (var cost in activity.itemCost)
            {
                var item = statusManager.GetItemById(cost.itemId);
                if (item != null)
                    statusManager.Inventory.RemoveItem(item, cost.amount);
            }
        }

        // 6. 보상 적용 (스탯)
        statusManager.ApplyActivityEffect(activity.statReward);

        // 7. 아이템 보상 지급
        if (activity.itemReward != null)
        {
            foreach (var reward in activity.itemReward)
            {
                var item = statusManager.GetItemById(reward.itemId);
                if (item != null)
                    statusManager.Inventory.AddItem(item, reward.amount);
            }
        }

        // 8. 시간 진행
        routineManager.AdvanceTime(activity.durationMinutes);

        // 9. 이벤트 발생
        OnActivityExecuted?.Invoke(activity, efficiency);

        return (true, null);
    }

    /// <summary>
    /// 활동 실행 가능 여부
    /// </summary>
    public bool CanExecuteActivity(DataTable.ActivityTable activity)
    {
        if (activity.durationMinutes > routineManager.Time.GetRemainingMinutes())
            return false;

        return statusManager.CanExecuteActivity(activity);
    }

    /// <summary>
    /// 활동 불가 사유
    /// </summary>
    public string GetActivityBlockReason(DataTable.ActivityTable activity)
    {
        if (activity.durationMinutes > routineManager.Time.GetRemainingMinutes())
            return $"시간이 부족합니다. (필요: {activity.durationMinutes}분, 남은 시간: {routineManager.Time.GetRemainingMinutes()}분)";

        return statusManager.GetActivityBlockReason(activity);
    }

    /// <summary>
    /// 강제 휴식 (탈진 상태일 때)
    /// </summary>
    public void ForceRest(LocationManager locationManager)
    {
        if (locationManager != null)
        {
            var actDict = DataTable.ActivityTable.GetDictionary();
            foreach (var location in locationManager.AllLocations)
            {
                foreach (var actId in location.activities)
                {
                    if ((actId == "act_rest" || actId == "act_sleep") && actDict.TryGetValue(actId, out var restActivity))
                    {
                        ExecuteActivity(location, restActivity);
                        return;
                    }
                }
            }
        }

        routineManager.Sleep();
    }
}
