using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 여러 UI 갱신 요청을 프레임당 1회로 통합
/// 활동 실행 시 발생하는 다중 이벤트(SpendMoney, ApplyEffect, TimeChanged 등)에 의한
/// 중복 UI 갱신을 방지
/// </summary>
public class UIRefreshScheduler : MonoBehaviour
{
    private bool isDirty;
    private Action refreshAction;

    public void Initialize(Action onRefresh)
    {
        refreshAction = onRefresh;
    }

    /// <summary>
    /// UI 갱신 요청. 같은 프레임 내 여러 번 호출해도 1회만 실행됨
    /// </summary>
    public void RequestRefresh()
    {
        if (!isDirty)
        {
            isDirty = true;
            StartCoroutine(RefreshAtEndOfFrame());
        }
    }

    private IEnumerator RefreshAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        isDirty = false;
        refreshAction?.Invoke();
    }
}
