using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class DiffButtonController : MonoBehaviour
{
    [Header("按钮")]
    public Button btnEasy;       
    public Button btnNormal;
    public Button btnHard;
    public RectTransform animTargetEasy;   
    public RectTransform animTargetNormal;
    public RectTransform animTargetHard; 

    [Header("上移动画参数")]
    public float moveUpDistance = 50f;  
    public float moveUpDuration = 0.3f;  

    [Header("下落物理参数")]
    public float gravityAcceleration = 800f;
    public float dropStopThreshold = 800f;
    public float initialDropVelocity = 0f;

    [Header("缩小动画参数")]
    public float scaleDownTime = 1f;

    [Header("场景配置")]
    public string nextSceneName = "PlayScene";
    public SaveManager saveManager;

    private Vector3 animTargetEasyInitPos;
    private Vector3 animTargetNormalInitPos;
    private Vector3 animTargetHardInitPos;

    private void Awake()
    {
        saveManager = SaveManager.instance;

        if (saveManager == null)
        {
            Debug.LogError("【DiffButtonController】未找到SaveManager单例！");
            enabled = false;
            return;
        }

        animTargetEasyInitPos = animTargetEasy.anchoredPosition;
        animTargetNormalInitPos = animTargetNormal.anchoredPosition;
        animTargetHardInitPos = animTargetHard.anchoredPosition;

        btnEasy.onClick.AddListener(() => OnDifficultyButtonClick(btnEasy, 1));
        btnNormal.onClick.AddListener(() => OnDifficultyButtonClick(btnNormal, 2));
        btnHard.onClick.AddListener(() => OnDifficultyButtonClick(btnHard, 3));
    }

    public void Start()
    {

    }

    private void OnDifficultyButtonClick(Button clickedBtn, int difficulty)
    {
        // 禁用所有按钮，防止重复点击
        btnEasy.interactable = false;
        btnNormal.interactable = false;
        btnHard.interactable = false;

        saveManager.diff = difficulty;
        Debug.Log($"已选择难度：{(DifficultyType)difficulty}（值：{difficulty}）");

        StartCoroutine(ExecuteButtonAnimation(clickedBtn));
    }

    private IEnumerator ExecuteButtonAnimation(Button clickedBtn)
    {
        // 获取被点击按钮对应的动画目标和初始位置
        RectTransform clickedTargetRect = null;
        Vector3 initPos = Vector3.zero;

        if (clickedBtn == btnEasy)
        {
            clickedTargetRect = animTargetEasy;
            initPos = animTargetEasyInitPos;
        }
        else if (clickedBtn == btnNormal)
        {
            clickedTargetRect = animTargetNormal;
            initPos = animTargetNormalInitPos;
        }
        else if (clickedBtn == btnHard)
        {
            clickedTargetRect = animTargetHard;
            initPos = animTargetHardInitPos;
        }

        if (clickedTargetRect == null)
        {
            Debug.LogError("【DiffButtonController】被点击按钮对应的动画目标为空");
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < moveUpDuration)
        {
            float t = elapsedTime / moveUpDuration;
            clickedTargetRect.anchoredPosition = Vector3.Lerp(initPos, initPos + new Vector3(0, moveUpDistance, 0), Mathf.SmoothStep(0, 1, t));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        clickedTargetRect.anchoredPosition = initPos + new Vector3(0, moveUpDistance, 0);

        float currentDropVelocity = initialDropVelocity;
        float totalDropDistance = 0f; 
        Vector3 currentPos = clickedTargetRect.anchoredPosition;

        RectTransform[] otherTargets = GetOtherAnimationTargets(clickedBtn);
        Vector3[] otherTargetsInitScale = new Vector3[2];
        for (int i = 0; i < otherTargets.Length; i++)
        {
            if (otherTargets[i] != null)
            {
                otherTargetsInitScale[i] = otherTargets[i].localScale;
            }
        }

        while (totalDropDistance < dropStopThreshold)
        {
            float dropDelta = currentDropVelocity * Time.deltaTime;
            totalDropDistance += Mathf.Abs(dropDelta);
            currentDropVelocity += gravityAcceleration * Time.deltaTime;

            currentPos.y -= dropDelta;
            clickedTargetRect.anchoredPosition = currentPos;

            float scaleElapsed = Time.time - (Time.time - scaleDownTime); 
            float scaleT = Mathf.Clamp01(scaleElapsed / scaleDownTime);
            for (int i = 0; i < otherTargets.Length; i++)
            {
                if (otherTargets[i] != null)
                {
                    otherTargets[i].localScale = Vector3.Lerp(otherTargetsInitScale[i], Vector3.zero, Mathf.SmoothStep(0, 1, scaleT));
                }
            }

            yield return null;
        }


        foreach (RectTransform target in otherTargets)
        {
            if (target != null)
            {
                target.localScale = Vector3.zero;
            }
        }

        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene(nextSceneName);
    }


    private RectTransform[] GetOtherAnimationTargets(Button clickedBtn)
    {
        if (clickedBtn == btnEasy)
        {
            return new RectTransform[] { animTargetNormal, animTargetHard };
        }
        else if (clickedBtn == btnNormal)
        {
            return new RectTransform[] { animTargetEasy, animTargetHard };
        }
        else 
        {
            return new RectTransform[] { animTargetEasy, animTargetNormal };
        }
    }


    private Button[] GetOtherButtons(Button clickedBtn)
    {
        if (clickedBtn == btnEasy)
        {
            return new Button[] { btnNormal, btnHard };
        }
        else if (clickedBtn == btnNormal)
        {
            return new Button[] { btnEasy, btnHard };
        }
        else 
        {
            return new Button[] { btnEasy, btnNormal };
        }
    }


    public enum DifficultyType
    {
        Easy = 1,    
        Normal = 2,  
        Hard = 3     
    }
}