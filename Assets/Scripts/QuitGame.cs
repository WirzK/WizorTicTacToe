using UnityEngine;
using UnityEngine.UI; 

public class QuitGame: MonoBehaviour
{
    //给按钮用的
    public void Quit()
    {
#if UNITY_EDITOR
        Debug.Log("点击了退出按钮");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Start()
    {
        Button quitButton = GetComponent<Button>();
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(Quit);
        }
        else
        {
            Debug.LogError("当前物体没有挂载Button组件");
        }
    }
}