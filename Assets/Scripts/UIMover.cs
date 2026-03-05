using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI位置缓动控制（适配RectTransform）
/// 功能：将脚本所在UI的X坐标先瞬间设为-700，再1秒匀速移回-200
/// </summary>
public class UIMover : MonoBehaviour
{
    [Header("缓动配置")]
    [Tooltip("初始偏移X（瞬间移动的目标值）")]
    public float startPosX;
    [Tooltip("目标偏移X（缓动终点）")]
    public float targetPosX;
    [Tooltip("缓动时长（秒）")]
    public float moveDuration;

    // 核心组件引用
    private RectTransform _rectTransform;
    // 标记：是否正在缓动，避免重复调用
    private bool _isMoving = false;

    private void Awake()
    {
        //防御性判空
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("[UIPositionTweener] 脚本未挂载在带RectTransform的UI对象上！");
            enabled = false;
        }
    }
    public void Start()
    {
        PlayPositionTween();
    }
    public void PlayPositionTween()
    {
        // 避免重复执行（提升体验，防止多次点击卡顿）
        if (_isMoving || _rectTransform == null) return;

        // 第一步：瞬间将X设为startPosX（-700）
        Vector2 currentAnchoredPos = _rectTransform.anchoredPosition;
        currentAnchoredPos.x = startPosX;
        _rectTransform.anchoredPosition = currentAnchoredPos;

        // 第二步：启动协程，1秒内匀速移回targetPosX（-200）
        StartCoroutine(MoveToTargetCoroutine());
    }

    /// <summary>
    /// 协程：匀速缓动到目标位置
    /// </summary>
    private IEnumerator MoveToTargetCoroutine()
    {
        _isMoving = true;
        float elapsedTime = 0f;
        Vector2 startAnchoredPos = _rectTransform.anchoredPosition;
        Vector2 targetAnchoredPos = new Vector2(targetPosX, startAnchoredPos.y);

        // 匀速插值逻辑（Time.deltaTime保证帧率无关）
        while (elapsedTime < moveDuration)
        {
            // 计算进度（0→1）
            float t = elapsedTime / moveDuration;
            // 匀速插值（Mathf.Lerp保证线性移动）
            _rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPos, targetAnchoredPos, t);

            elapsedTime += Time.deltaTime;
            yield return null; // 等待下一帧
        }

        _rectTransform.anchoredPosition = targetAnchoredPos;
        _isMoving = false;
    }
}