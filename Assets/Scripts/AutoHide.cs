using UnityEngine;
using System.Collections;

/// <summary>
/// 自动禁用GameObject
/// 延迟指定时间后将自身设为非激活状态，支持启动自动执行/手动触发两种模式
/// </summary>
public class AutoDisableObject : MonoBehaviour
{
    [Min(0f)]
    public float disableDelay = 2f;          // 禁用延迟时间
    public bool autoExecuteOnStart = true;   // Start时自动执行

    private bool isCounting = false;

    private void Start()
    {
        if (autoExecuteOnStart)
        {
            StartDisableCountdown();
        }
    }

    /// <summary>
    /// 启动禁用倒计时（手动触发入口）
    /// </summary>
    public void StartDisableCountdown()
    {
        // 防重复执行 + 检查对象当前激活状态
        if (isCounting || !gameObject.activeSelf)
        {
            Debug.LogWarning($"[AutoDisable] 禁用倒计时触发失败：对象「{gameObject.name}」，重复执行={isCounting}，当前激活状态={gameObject.activeSelf}");
            return;
        }

        StartCoroutine(DisableAfterDelayCoroutine());
    }

    /// <summary>
    /// 延迟禁用协程（核心逻辑）
    /// </summary>
    private IEnumerator DisableAfterDelayCoroutine()
    {
        isCounting = true;
        yield return new WaitForSeconds(disableDelay);

        gameObject.SetActive(false);
        isCounting = false;
    }

    /// <summary>
    /// 对象重新激活时重置倒计时标记
    /// </summary>
    private void OnEnable()
    {
        isCounting = false;
    }
}