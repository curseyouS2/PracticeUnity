using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 활동 실행 로직 통합 - 검증, 비용 지불, 효과 적용, 시간 진행을 한 곳에서 처리
/// 의존성을 생성자로 주입받아 static Instance 참조를 사용하지 않음
/// </summary>
public class ActivityService
{
    public event Action<ActivitySO, float> OnActivityExecuted;

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
    public (bool success, string message) ExecuteActivity(LocationSO location, ActivitySO activity)
    {
        // 1. 시간 검증
        if (activity.durationMinutes > routineManager.Time.GetRemainingMinutes())
        {
            string reason = $"시간이 부족합니다. (필요: {activity.durationMinutes}분, 남은 시간: {routineManager.Time.GetRemainingMinutes()}분)";
            return (false, reason);
        }

        // 2. 스탯/비용/컨디션 검증
        if (!statusManager.CanExecuteActivity(activity))
        {
            string reason = statusManager.GetActivityBlockReason(activity);
            return (false, reason);
        }

        // 3. 효율 계산 (효과 적용 전)
        float efficiency = statusManager.CalculateTotalEfficiency();

        // 4. 비용 지불
        if (activity.cost > 0)
            statusManager.Status.SpendMoney(activity.cost);

        // 5. 효과 적용
        statusManager.ApplyActivityEffect(activity.statChanges);

        // 6. 시간 진행
        routineManager.AdvanceTime(activity.durationMinutes);

        // 7. 이벤트 발생
        OnActivityExecuted?.Invoke(activity, efficiency);

        return (true, null);
    }

    /// <summary>
    /// 활동 실행 가능 여부
    /// </summary>
    public bool CanExecuteActivity(ActivitySO activity)
    {
        if (activity.durationMinutes > routineManager.Time.GetRemainingMinutes())
            return false;

        return statusManager.CanExecuteActivity(activity);
    }

    /// <summary>
    /// 활동 불가 사유
    /// </summary>
    public string GetActivityBlockReason(ActivitySO activity)
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
            foreach (var location in locationManager.AllLocations)
            {
                var restActivity = location.activities?.Find(a => a.id == "rest" || a.id == "sleep");
                if (restActivity != null)
                {
                    ExecuteActivity(location, restActivity);
                    return;
                }
            }
        }

        routineManager.Sleep();
    }
}
