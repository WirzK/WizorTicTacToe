using UnityEngine;
using UnityEngine.UI;

public class ButtonPanelController : MonoBehaviour
{
    [Header("面板和按钮引用")]
    public GameObject Panel;
    public Button closeButton;
    public Button openButton;

    private Vector3 _panelShowScale = Vector3.one;
    private Vector3 _panelHideScale = Vector3.zero;

    private void Start()
    {
        // 初始化面板为隐藏状态（仅缩放，保持激活）
        if (Panel != null)
        {
            Panel.transform.localScale = _panelHideScale;
            Panel.SetActive(true);
        }

        // 绑定按钮点击事件
        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenPanel);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    /// <summary>
    /// 打开面板（缩放至原尺寸）
    /// </summary>
    public void OpenPanel()
    {
        if (Panel != null)
        {
            Panel.transform.localScale = _panelShowScale;
        }
    }

    /// <summary>
    /// 关闭面板（缩放到0）
    /// </summary>
    public void ClosePanel()
    {
        if (Panel != null)
        {
            Panel.transform.localScale = _panelHideScale;
        }
    }
}