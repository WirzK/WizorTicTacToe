using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ChangeScene : MonoBehaviour
{
    [Header("按钮绑定")]
    public Button btn;

    [Header("场景配置")]
    [Tooltip("目标场景名")]
    public string targetSceneName = "NewScene";
    [Tooltip("场景加载模式")]
    public LoadSceneMode loadMode = LoadSceneMode.Single;

    [Header("延迟配置")]
    [Tooltip("是否启用场景切换延迟")]
    public bool isDelayEnabled = false;
    [Tooltip("延迟时间")]
    public float delayTime = 1f; // 默认延迟1秒

    private void Awake()
    {
        CheckButtonBinding();
    }

    private void Start()
    {
        if (btn != null)
        {
            btn.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {

        // 判断是否启用延迟：启用则走协程延迟加载，否则立即加载
        if (isDelayEnabled)
        {
            StartCoroutine(LoadSceneAfterDelay());
        }
        else
        {
            LoadTargetScene();
        }
    }
    private IEnumerator LoadSceneAfterDelay()
    {
        Debug.Log($"将在{delayTime}秒后跳转到场景：{targetSceneName}");
        yield return new WaitForSeconds(delayTime); 
        LoadTargetScene(); 
    }

    private void LoadTargetScene()
    {
        SceneManager.LoadScene(targetSceneName, loadMode);
        Debug.Log($"跳转到场景：{targetSceneName}，加载模式：{loadMode}");
    }

    private void CheckButtonBinding()
    {
        if (btn == null)
        {
            Debug.LogWarning("[SceneSwitcher] 按钮未绑定");
        }
    }
}