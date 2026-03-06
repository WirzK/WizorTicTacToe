using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class ButtonDropEffect : MonoBehaviour
{
    [Header("按钮和作用对象")]
    public Button triggerButton;   
    public Transform targetObject; 

    [Header("动画模式开关")]
    [Tooltip("启用：先上升→后加速无限下坠（按钮仅可点击一次）；禁用：上升后复位（按钮可重复用）")]
    public bool enableRiseThenFall = true;

    [Header("上升动画参数")]
    public float moveUpDistance = 50f;    
    public float riseDuration = 0.3f;     
    public bool riseSmoothly = true;     

    [Header("坠落动画参数（启用无限下坠时生效）")]
    public float gravityAcceleration = 200f; 
    public float initialFallSpeed = 0f;     

    [Header("其他对象缩放配置")]
    public Transform[] otherObjectsToScale; 
    public float scaleDownDuration;   
    public bool scaleSmoothly = true;   

    [Header("通用配置")]
    public bool disableButtonAfterClick = true; // 点击后临时禁用按钮
    public bool isUIObject = true;             

    // 私有变量
    private Vector3 targetInitPos;         
    private RectTransform targetRect;      
    private Vector3[] otherObjectsInitScale; 
    private Coroutine fallCoroutine;       // 下坠协程

    private void Awake()
    {
        // 防御性校验
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
    }

    #region 初始状态缓存
    private void CacheTargetInitPosition()
    {
        if (isUIObject)
        {
            targetRect = targetObject.GetComponent<RectTransform>();
            if (targetRect == null)
            {
                Debug.LogError("目标对象是UI但无RectTransform组件！", this);
                enabled = false;
                return;
            }
            targetInitPos = targetRect.anchoredPosition;
        }
        else
        {
            targetInitPos = targetObject.position;
        }
    }

    private void CacheOtherObjectsInitScale()
    {
        if (otherObjectsToScale == null || otherObjectsToScale.Length == 0)
        {
            Debug.LogWarning("未配置需要缩放的其他对象！", this);
            return;
        }

        otherObjectsInitScale = new Vector3[otherObjectsToScale.Length];
        for (int i = 0; i < otherObjectsToScale.Length; i++)
        {
            if (otherObjectsToScale[i] != null)
            {
                otherObjectsInitScale[i] = otherObjectsToScale[i].localScale;
            }
            else
            {
                Debug.LogError($"第{i}个缩放对象为空！", this);
            }
        }
    }
    #endregion

    #region 按钮点击逻辑

    private void OnButtonClick()
    {
        if (disableButtonAfterClick)
        {
            triggerButton.interactable = false;
        }

        if (enableRiseThenFall)
        {
            fallCoroutine = StartCoroutine(PlayRiseThenInfiniteFallAnimation());
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
                targetInitPos,
                targetInitPos + new Vector3(0, moveUpDistance, 0),
                progress
            );
            UpdateTargetPosition(targetPos);

            riseElapsed += Time.deltaTime;
            yield return null;
        }
        UpdateTargetPosition(targetInitPos + new Vector3(0, moveUpDistance, 0));

        float fallTime = 0f;
        float currentFallSpeed = initialFallSpeed;
        Vector3 currentPos = targetObject.position;
        if (isUIObject) currentPos = targetRect.anchoredPosition;

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
                targetInitPos,
                targetInitPos + new Vector3(0, moveUpDistance, 0),
                progress
            );
            UpdateTargetPosition(targetPos);

            riseElapsed += Time.deltaTime;
            yield return null;
        }
        UpdateTargetPosition(targetInitPos + new Vector3(0, moveUpDistance, 0));

        float fallBackDuration = riseDuration * 0.8f;
        float fallElapsed = 0f;
        while (fallElapsed < fallBackDuration)
        {
            float t = fallElapsed / fallBackDuration;
            float progress = riseSmoothly ? Mathf.SmoothStep(0, 1, t) : t;
            Vector3 targetPos = Vector3.Lerp(
                targetInitPos + new Vector3(0, moveUpDistance, 0),
                targetInitPos,
                progress
            );
            UpdateTargetPosition(targetPos);

            fallElapsed += Time.deltaTime;
            yield return null;
        }
        UpdateTargetPosition(targetInitPos);

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
        // 强制缩到0，防止残留
        ForceOtherObjectsToZeroScale();

        // 复位模式下，缩放对象也复位
        if (!enableRiseThenFall)
        {
            yield return new WaitForSeconds(0.1f); // 延迟复位，避免动画卡顿
            ResetOtherObjectsScale();
        }
    }


    private void UpdateOtherObjectsScale(float progress)
    {
        if (otherObjectsToScale == null || otherObjectsInitScale == null) return;

        float clampedProgress = Mathf.Clamp01(progress);
        for (int i = 0; i < otherObjectsToScale.Length; i++)
        {
            if (otherObjectsToScale[i] == null) continue;

            Vector3 currentScale = Vector3.Lerp(
                otherObjectsInitScale[i],
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
        if (otherObjectsToScale == null || otherObjectsInitScale == null) return;
        for (int i = 0; i < otherObjectsToScale.Length; i++)
        {
            if (otherObjectsToScale[i] == null) continue;
            otherObjectsToScale[i].localScale = otherObjectsInitScale[i];
        }
    }

    #region 通用工具方法
    private void UpdateTargetPosition(Vector3 targetPos)
    {
        if (isUIObject && targetRect != null)
        {
            targetRect.anchoredPosition = targetPos;
        }
        else if (targetObject != null)
        {
            targetObject.position = targetPos;
        }
    }

    public void ResetState()
    {
        if (fallCoroutine != null)
        {
            StopCoroutine(fallCoroutine);
            fallCoroutine = null;
        }
        if (triggerButton != null)
        {
            triggerButton.interactable = true;
        }

        UpdateTargetPosition(targetInitPos);
    }
    #endregion
}