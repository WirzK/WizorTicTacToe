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
    [Tooltip("初始偏移X")]
    public float startPosX;
    [Tooltip("目标偏移X")]
    public float targetPosX;
    [Tooltip("缓动时长")]
    public float moveDuration;
    public bool moveOnStart = true;

    private RectTransform _rectTransform;
    private bool _isMoving = false;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            enabled = false;
        }
    }
    public void Start()
    {
        if(moveOnStart)
            UIMove();
    }
    public void UIMove()
    {
        if (_isMoving || _rectTransform == null) return;

        Vector2 currentAnchoredPos = _rectTransform.anchoredPosition;
        currentAnchoredPos.x = startPosX;
        _rectTransform.anchoredPosition = currentAnchoredPos;

        StartCoroutine(MoveToTargetCoroutine());
    }

    private IEnumerator MoveToTargetCoroutine()
    {
        _isMoving = true;
        float elapsedTime = 0f;
        Vector2 startAnchoredPos = _rectTransform.anchoredPosition;
        Vector2 targetAnchoredPos = new Vector2(targetPosX, startAnchoredPos.y);

        while (elapsedTime < moveDuration)
        {
            float t = elapsedTime / moveDuration;
            _rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPos, targetAnchoredPos, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _rectTransform.anchoredPosition = targetAnchoredPos;
        _isMoving = false;
    }
}