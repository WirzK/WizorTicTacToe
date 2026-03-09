using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonDropEffect : MonoBehaviour
{
    [Header("按钮和作用对象")]
    public Button triggerButton;
    public Transform targetObject;


    [Header("上升动画参数")]
    public float moveUpDistance = 50f;
    public float riseDuration = 0.3f;
    public bool riseSmoothly = true;

    [Header("坠落动画参数（需启用下坠）")]
    public bool enableRiseThenFall = true;
    public float gravityAcceleration = 200f;
    public float initialFallSpeed = 0f;

    [Header("其他对象缩放配置")]
    public Transform[] otherObjectsToScale;
    public float scaleDownDuration;
    public bool scaleSmoothly = true;

    [Header("通用配置")]
    private bool disableButtonAfterClick;
    public bool isUIObject = true;

    private Vector3 _targetInitPos;
    private RectTransform _targetRect;
    private Vector3[] _otherObjectsInitScale;
    private Coroutine _fallCoroutine;

    private void Awake()
    {
        if (triggerButton == null)
        {
            Debug.LogError("请绑定触发按钮！", this);
            enabled = false;
            return;
        }
        if (targetObject == null)
        {
            Debug.LogError("请绑定坠落动画的目标对象！", this);
            return;
        }

        CacheTargetInitPosition();
        CacheOtherObjectsInitScale();
        triggerButton.onClick.AddListener(OnButtonClick);
        disableButtonAfterClick = enableRiseThenFall;
    }

    #region 初始状态缓存
    private void CacheTargetInitPosition()
    {
        if (isUIObject)
        {
            _targetRect = targetObject.GetComponent<RectTransform>();
            if (_targetRect == null)
            {
                Debug.LogError("目标对象是UI但无RectTransform组件！", this);
                enabled = false;
                return;
            }
            _targetInitPos = _targetRect.anchoredPosition;
        }
        else
        {
            _targetInitPos = targetObject.position;
        }
    }

    private void CacheOtherObjectsInitScale()
    {
        if (otherObjectsToScale == null || otherObjectsToScale.Length == 0)
        {
            Debug.LogWarning("未配置需要缩放的其他对象！", this);
            return;
        }

        _otherObjectsInitScale = new Vector3[otherObjectsToScale.Length];
        for (int i = 0; i < otherObjectsToScale.Length; i++)
        {
            if (otherObjectsToScale[i] != null)
            {
                _otherObjectsInitScale[i] = otherObjectsToScale[i].localScale;
            }
            else
            {
                Debug.LogError($"第{i}个缩放对象为空！", this);
            }
        }
    }
    #endregion

    #region 按钮点击逻辑
    public void OnButtonClick()
    {
        if (disableButtonAfterClick)
        {
            triggerButton.interactable = false;
        }

        if (enableRiseThenFall)
        {
            _fallCoroutine = StartCoroutine(PlayRiseThenInfiniteFallAnimation());
            StartCoroutine(PlayScaleDownAnimation());
        }
        else
        {
            StartCoroutine(PlayResetableRiseFallAnimation());
        }
    }
    #endregion

    #region 动画协程（启用无限下坠模式）
    private IEnumerator PlayRiseThenInfiniteFallAnimation()
    {
        float riseElapsed = 0f;
        while (riseElapsed < riseDuration)
        {
            float t = riseElapsed / riseDuration;
            float progress = riseSmoothly ? Mathf.SmoothStep(0, 1, t) : t;
            Vector3 targetPos = Vector3.Lerp(
                _targetInitPos,
                _targetInitPos + new Vector3(0, moveUpDistance, 0),
                progress
            );
            UpdateTargetPosition(targetPos);

            riseElapsed += Time.deltaTime;
            yield return null;
        }
        UpdateTargetPosition(_targetInitPos + new Vector3(0, moveUpDistance, 0));

        float fallTime = 0f;
        float currentFallSpeed = initialFallSpeed;
        Vector3 currentPos = targetObject.position;
        if (isUIObject) currentPos = _targetRect.anchoredPosition;

        while (true)
        {
            currentFallSpeed += gravityAcceleration * Time.deltaTime;
            float fallDelta = -currentFallSpeed * Time.deltaTime;
            currentPos += new Vector3(0, fallDelta, 0);

            UpdateTargetPosition(currentPos);

            fallTime += Time.deltaTime;
            yield return null;
        }
    }
    #endregion

    private IEnumerator PlayResetableRiseFallAnimation()
    {
        float riseElapsed = 0f;
        while (riseElapsed < riseDuration)
        {
            float t = riseElapsed / riseDuration;
            float progress = riseSmoothly ? Mathf.SmoothStep(0, 1, t) : t;
            Vector3 targetPos = Vector3.Lerp(
                _targetInitPos,
                _targetInitPos + new Vector3(0, moveUpDistance, 0),
                progress
            );
            UpdateTargetPosition(targetPos);

            riseElapsed += Time.deltaTime;
            yield return null;
        }
        UpdateTargetPosition(_targetInitPos + new Vector3(0, moveUpDistance, 0));

        float fallBackDuration = riseDuration * 0.8f;
        float fallElapsed = 0f;
        while (fallElapsed < fallBackDuration)
        {
            float t = fallElapsed / fallBackDuration;
            float progress = riseSmoothly ? Mathf.SmoothStep(0, 1, t) : t;
            Vector3 targetPos = Vector3.Lerp(
                _targetInitPos + new Vector3(0, moveUpDistance, 0),
                _targetInitPos,
                progress
            );
            UpdateTargetPosition(targetPos);

            fallElapsed += Time.deltaTime;
            yield return null;
        }
        UpdateTargetPosition(_targetInitPos);

        ResetState();
    }

    private IEnumerator PlayScaleDownAnimation()
    {
        float scaleElapsed = 0f;
        while (scaleElapsed < scaleDownDuration)
        {
            float t = scaleElapsed / scaleDownDuration;
            float progress = scaleSmoothly ? Mathf.SmoothStep(0, 1, t) : t;
            UpdateOtherObjectsScale(progress);

            scaleElapsed += Time.deltaTime;
            yield return null;
        }
        ForceOtherObjectsToZeroScale();

        if (!enableRiseThenFall)
        {
            yield return new WaitForSeconds(0.1f);
            ResetOtherObjectsScale();
        }
    }

    private void UpdateOtherObjectsScale(float progress)
    {
        if (otherObjectsToScale == null || _otherObjectsInitScale == null) return;

        float clampedProgress = Mathf.Clamp01(progress);
        for (int i = 0; i < otherObjectsToScale.Length; i++)
        {
            if (otherObjectsToScale[i] == null) continue;

            Vector3 currentScale = Vector3.Lerp(
                _otherObjectsInitScale[i],
                Vector3.zero,
                clampedProgress
            );
            otherObjectsToScale[i].localScale = currentScale;
        }
    }

    private void ForceOtherObjectsToZeroScale()
    {
        if (otherObjectsToScale == null) return;
        foreach (var obj in otherObjectsToScale)
        {
            if (obj != null) obj.localScale = Vector3.zero;
        }
    }

    private void ResetOtherObjectsScale()
    {
        if (otherObjectsToScale == null || _otherObjectsInitScale == null) return;
        for (int i = 0; i < otherObjectsToScale.Length; i++)
        {
            if (otherObjectsToScale[i] == null) continue;
            otherObjectsToScale[i].localScale = _otherObjectsInitScale[i];
        }
    }

    #region 通用工具方法
    private void UpdateTargetPosition(Vector3 targetPos)
    {
        if (isUIObject && _targetRect != null)
        {
            _targetRect.anchoredPosition = targetPos;
        }
        else if (targetObject != null)
        {
            targetObject.position = targetPos;
        }
    }

    public void ResetState()
    {
        if (_fallCoroutine != null)
        {
            StopCoroutine(_fallCoroutine);
            _fallCoroutine = null;
        }

        UpdateTargetPosition(_targetInitPos);
    }
    #endregion
}