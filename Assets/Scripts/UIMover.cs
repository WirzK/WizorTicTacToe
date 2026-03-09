using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIMover : MonoBehaviour
{
    public float startPosX;
    public float targetPosX;
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