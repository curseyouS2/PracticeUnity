using System;
using System.Collections.Generic;

/// <summary>
/// 상태 이상(컨디션) 관리
/// </summary>
public class ConditionController
{
    public event Action<ConditionType> OnConditionChanged;
    public event Action<ConditionType> OnConditionAdded;
    public event Action<ConditionType> OnConditionRemoved;

    private ConditionType currentCondition;
    private Dictionary<ConditionType, int> conditionDurations;

    public ConditionType CurrentCondition => currentCondition;
    public bool HasNegativeCondition => currentCondition == ConditionType.Exhausted ||
                                         currentCondition == ConditionType.Sick ||
                                         currentCondition == ConditionType.Injured ||
                                         currentCondition == ConditionType.Depressed;

    public ConditionController(ConditionType initialCondition = ConditionType.None)
    {
        currentCondition = initialCondition;
        conditionDurations = new Dictionary<ConditionType, int>();
    }

    public void SetCondition(ConditionType condition, int duration = -1)
    {
        if (currentCondition != condition)
        {
            var oldCondition = currentCondition;
            currentCondition = condition;

            if (duration > 0)
            {
                conditionDurations[condition] = duration;
            }

            if (oldCondition != ConditionType.None)
            {
                OnConditionRemoved?.Invoke(oldCondition);
            }

            if (condition != ConditionType.None)
            {
                OnConditionAdded?.Invoke(condition);
            }

            OnConditionChanged?.Invoke(condition);
        }
    }

    public void ClearCondition()
    {
        SetCondition(ConditionType.None);
    }

    public void ProcessDayEnd()
    {
        if (conditionDurations.ContainsKey(currentCondition))
        {
            conditionDurations[currentCondition]--;

            if (conditionDurations[currentCondition] <= 0)
            {
                conditionDurations.Remove(currentCondition);
                ClearCondition();
            }
        }
    }

    public bool CanPerformActivity(ActivitySO activity)
    {
        // 탈진 상태면 휴식만 가능
        if (currentCondition == ConditionType.Exhausted)
        {
            return activity.id == "sleep" || activity.id == "rest";
        }

        // 부상 상태면 격렬한 활동 불가 (activity에 태그 추가 필요)
        if (currentCondition == ConditionType.Injured)
        {
            // 추후 activity에 태그 시스템 추가 시 확장
        }

        return true;
    }

    public string GetConditionDisplayName()
    {
        return currentCondition switch
        {
            ConditionType.None => "보통",
            ConditionType.Exhausted => "탈진",
            ConditionType.Sick => "아픔",
            ConditionType.Injured => "부상",
            ConditionType.Happy => "행복",
            ConditionType.Sad => "슬픔",
            ConditionType.Depressed => "우울",
            _ => "알 수 없음"
        };
    }

    public float GetConditionEfficiencyModifier()
    {
        return currentCondition switch
        {
            ConditionType.Happy => 1.2f,
            ConditionType.Sad => 0.8f,
            ConditionType.Depressed => 0.5f,
            ConditionType.Sick => 0.6f,
            ConditionType.Injured => 0.7f,
            _ => 1.0f
        };
    }
}
