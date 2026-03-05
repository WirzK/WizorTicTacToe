using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("核心UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI stepText;
    public Button[] tileButtons;
    public Transform[] tiles; // 存储棋盘按钮的Transform，方便动画使用

    [Header("三个独立结算面板")]
    public GameObject winPanel;    // 胜利面板
    public GameObject losePanel;   // 失败面板
    public GameObject drawPanel;   // 平局面板

    [Header("胜利面板专属UI（显示时间/步数/破纪录）")]
    public TextMeshProUGUI winTimeText;    // 胜利面板-用时文本
    public TextMeshProUGUI winStepText;    // 胜利面板-步数文本
    public TextMeshProUGUI winRecordText;  // 胜利面板-破纪录提示文本

    // 存档键名（用于存储最快时间和最短步数纪录）
    private const string BEST_TIME_KEY = "TicTacToe_BestTime";
    private const string BEST_STEP_KEY = "TicTacToe_BestStep";

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // 初始隐藏所有结算面板
        HideAllResultPanels();
    }
    private void Start()
    {
    
    }
    // 更新计时器（格式化为分:秒）
    public void UpdateTimer(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = string.Format("Time {0:00}:{1:00}", minutes, seconds);
    }

    // 更新计步器
    public void UpdateStep(int step)
    {
        stepText.text = $"Steps:{step}";
    }

    // 更新棋盘按钮显示（玩家/AI落子）
    public void UpdateTile(int index, int player)
    {
        TileButton btn = tileButtons[index].GetComponent<TileButton>();
        if (btn == null) return;
        else if (player == 2)
            btn.SetTileSpriteAndDisable();
    }

    // 激活/禁用棋盘按钮
    public void ActivateTiles(bool active)
    {
        foreach (var btn in tileButtons)
        {
            btn.interactable = active;
        }
    }

    // 重置所有棋盘按钮
    public void ResetTiles()
    {
        foreach (var btn in tileButtons)
        {
            TileButton tileBtn = btn.GetComponent<TileButton>();
            if (tileBtn != null)
                tileBtn.ResetTile(); // 调用TileButton的重置方法
        }
    }

    // 高亮胜利的三个按钮（放大动画）
    public void HighlightWinningTiles(int[] indices)
    {
        foreach (int index in indices)
        {
            StartCoroutine(ScaleTileCoroutine(tileButtons[index].transform, 1.2f, 0.3f));
        }
    }

    // 按钮缩放协程
    private IEnumerator ScaleTileCoroutine(Transform tile, float targetScale, float duration)
    {
        Vector3 startScale = tile.localScale;
        float timer = 0f;
        while (timer < duration)
        {
            tile.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        tile.localScale = Vector3.one * targetScale;
    }

    // 核心：显示对应结算面板（胜利/失败/平局）
    public void ShowResult(bool playerWin, bool isDraw, float time, int steps)
    {
        // 先隐藏所有面板
        HideAllResultPanels();

        if (isDraw)
        {
            // 显示平局面板
            drawPanel.SetActive(true);
        }
        else if (playerWin)
        {
            // 显示胜利面板 + 计算破纪录 + 填充时间/步数
            winPanel.SetActive(true);
            UpdateWinPanel(time, steps);
        }
        else
        {
            // 显示失败面板
            losePanel.SetActive(true);
        }
    }

    // 胜利面板专属：更新时间、步数、破纪录信息（仅显示，存档逻辑交给SaveManager）
    private void UpdateWinPanel(float gameTime, int stepCount)
    {
        // 获取当前选中的存档数据
        SaveData currentSave = SaveManager.instance.GetCurrentSave();

        // 1. 格式化时间
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        string timeStr = string.Format("{0:00}:{1:00}", minutes, seconds);

        // 2. 计算是否破纪录
        bool isTimeRecord = gameTime < currentSave.bestTime;
        bool isStepRecord = stepCount < currentSave.bestStep;

        // 3. 更新胜利面板文本
        winTimeText.text = $"time:{timeStr}";
        winStepText.text = $"steps:{stepCount}";

        // 4. 显示破纪录提示
        string recordTips = "";
        if (isTimeRecord && isStepRecord)
        {
            recordTips = "New time record and step record!";
        }
        else if (isTimeRecord)
        {
            recordTips = $"New time record! \n Old record:{FormatTime(currentSave.bestTime)}";
        }
        else if (isStepRecord)
        {
            recordTips = $"New steps record! \n Old record:{currentSave.bestStep}步";
        }
        else
        {
            recordTips = $"Time record:{FormatTime(currentSave.bestTime)} | Step record:{currentSave.bestStep}步";
        }
        winRecordText.text = recordTips;
    }


    // 辅助方法：格式化时间（用于显示最佳时间）
    private string FormatTime(float time)
    {
        if (time == float.MaxValue) return "--:--"; // 无纪录时显示
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // 隐藏所有结算面板
    public void HideAllResultPanels()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (drawPanel != null) drawPanel.SetActive(false);
    }
    public Transform GetTileTransform(int tileIndex)
    {
        // 假设你有一个存储所有棋盘按钮的数组tiles（长度9）
        if (tiles != null && tileIndex >= 0 && tileIndex < tiles.Length)
        {
            return tiles[tileIndex].transform;
        }
        return null;
    }

    // 重开游戏
    public void OnRestartButton()
    {
        HideAllResultPanels();
        GameManager.instance.ResetBoard();
        GameManager.instance.StartGame(GameManager.instance.difficulty);
    }

    // 退出游戏
    public void OnExitButton()
    {
        // 跳转到开始页面或退出应用
        Application.Quit();
    }
}