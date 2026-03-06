using UnityEngine;
using System.Collections;

/// <summary>
/// 自动禁用GameObject脚本
/// 功能：在指定延迟时间后将自身设为Disable，支持自动执行/手动触发
/// 技术策划适配：参数可视化、防重复执行、清晰日志
/// </summary>
public class AutoDisableObject : MonoBehaviour
{
    [Min(0f)]
    public float disableDelay = 2f;

    public bool autoExecuteOnStart = true;

    private bool isCounting = false;

    private void Start()
    {
        if (autoExecuteOnStart)
        {
            StartDisableCountdown();
        }
    }

    public void StartDisableCountdown()
    {
        if (isCounting || disableDelay < 0 || !gameObject.activeSelf)
        {
            Debug.LogWarning($"[AutoDisable] 禁用倒计时触发失败：对象={gameObject.name}，重复执行={isCounting}，延迟={disableDelay}，当前激活状态={gameObject.activeSelf}");
            return;
        }

        StartCoroutine(DisableAfterDelayCoroutine());
    }
    private IEnumerator DisableAfterDelayCoroutine()
    {
        isCounting = true;

        yield return new WaitForSeconds(disableDelay);

        gameObject.SetActive(false);
        isCounting = false;
    }
    private void OnEnable()
    {
        isCounting = false;
    }
}