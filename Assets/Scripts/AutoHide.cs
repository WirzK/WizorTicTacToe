using UnityEngine;
using System.Collections;

/// <summary>
/// 自动禁用GameObject脚本
/// 功能：在指定延迟时间后将自身设为Disable，支持自动执行/手动触发
/// 技术策划适配：参数可视化、防重复执行、清晰日志
/// </summary>
public class AutoDisableObject : MonoBehaviour
{
    [Header("自动禁用配置")]
    [Tooltip("延迟禁用的时间（秒），不能为负数")]
    [Min(0f)] // 限制最小值为0，避免无效配置
    public float disableDelay = 2f;

    [Tooltip("是否在游戏启动时自动执行禁用逻辑")]
    public bool autoExecuteOnStart = true;

    // 标记：是否正在执行禁用倒计时，避免重复触发
    private bool isCounting = false;

    private void Start()
    {
        // 启动自动执行（策划可通过Inspector开关控制）
        if (autoExecuteOnStart)
        {
            StartDisableCountdown();
        }
    }

    /// <summary>
    /// 公开方法：启动禁用倒计时（可手动调用/绑定按钮）
    /// </summary>
    public void StartDisableCountdown()
    {
        // 防御性检查：避免重复执行、延迟时间无效、对象已禁用
        if (isCounting || disableDelay < 0 || !gameObject.activeSelf)
        {
            Debug.LogWarning($"[AutoDisable] 禁用倒计时触发失败：对象={gameObject.name}，重复执行={isCounting}，延迟={disableDelay}，当前激活状态={gameObject.activeSelf}");
            return;
        }

        StartCoroutine(DisableAfterDelayCoroutine());
    }

    /// <summary>
    /// 协程：延迟指定时间后禁用自身
    /// </summary>
    private IEnumerator DisableAfterDelayCoroutine()
    {
        isCounting = true;
        Debug.Log($"[AutoDisable] 开始倒计时：{gameObject.name} 将在 {disableDelay} 秒后禁用");

        // 等待指定延迟时间
        yield return new WaitForSeconds(disableDelay);

        // 禁用自身GameObject（核心逻辑）
        gameObject.SetActive(false);
        isCounting = false;
        Debug.Log($"[AutoDisable] 已禁用：{gameObject.name}");
    }

    /// <summary>
    /// 可选：对象重新激活时重置状态（避免再次激活后无法触发）
    /// </summary>
    private void OnEnable()
    {
        isCounting = false;
    }
}