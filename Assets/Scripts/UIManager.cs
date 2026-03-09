using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("菜单和棋盘")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI stepText;
    public TextMeshProUGUI diffText;
    public Button[] tileButtons;
    public Transform[] tiles;

    [Header("结算面板")]
    public GameObject winPanel; 
    public GameObject losePanel; 
    public GameObject drawPanel;  

    public TextMeshProUGUI winTimeText;   
    public TextMeshProUGUI winStepText;   
    public TextMeshProUGUI winRecordText;


    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        HideAllResultPanels();
    }

    private void Start()
    {
        switch (GameManager.instance.difficulty)
        {
            case 1:
                diffText.text = "Easy";
                break;
            case 2:
                diffText.text = "Normal";
                break;
            case 3:
                diffText.text = "Hard";
                break;
            case 4:
                diffText.text = "Endless";
                break;
        }
    }

    public void UpdateTimer(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = string.Format("Time {0:00}:{1:00}", minutes, seconds);
    }

    public void UpdateStep(int step)
    {
        stepText.text = $"Steps:{step}";
    }

    public void UpdateTile(int index, int player)
    {
        TileButton btn = tileButtons[index].GetComponent<TileButton>();
        if (btn == null) return;
        else if (player == 2)
            btn.SetTileSpriteAndDisable();
    }

    public void ActivateTiles(bool active)
    {
        foreach (var btn in tileButtons)
        {
            btn.interactable = active;
        }
    }

    public void ResetTiles()
    {
        foreach (var btn in tileButtons)
        {
            TileButton tileBtn = btn.GetComponent<TileButton>();
            if (tileBtn != null)
                tileBtn.ResetTile(); 
        }
    }

    public void HighlightWinningTiles(int[] indices)
    {
        foreach (int index in indices)
        {
            StartCoroutine(ScaleTileCoroutine(tileButtons[index].transform, 1.2f, 0.3f));
        }
    }

    // 缩放棋子
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

    public void ShowResult(bool playerWin, bool isDraw, float time, int steps)
    {
        HideAllResultPanels();

        if (isDraw)
        {
            drawPanel.SetActive(true);
        }
        else if (playerWin)
        {
            // 显示胜利面板 + 计算破纪录 + 填充时间步数
            winPanel.SetActive(true);
            UpdateWinPanel(time, steps);
        }
        else
        {
            losePanel.SetActive(true);
        }
    }

    private void UpdateWinPanel(float gameTime, int stepCount)
    {
        if (GameManager.instance == null || SaveManager.instance == null)
        {
            Debug.LogError("GameManager/SaveManager未初始化！");
            return;
        }
        int currentDifficulty = GameManager.instance.difficulty;
        SaveData currentSave = SaveManager.instance.GetCurrentSave();
        if (currentSave == null) return;

        float bestTime = float.MaxValue;
        int bestStep = int.MaxValue;
        switch (currentDifficulty)
        {
            case 1:
                bestTime = currentSave.diff1BestTime;
                bestStep = currentSave.diff1BestStep;
                break;
            case 2:
                bestTime = currentSave.diff2BestTime;
                bestStep = currentSave.diff2BestStep;
                break;
            case 3:
                bestTime = currentSave.diff3BestTime;
                bestStep = currentSave.diff3BestStep;
                break;
            default:
                return;
        }

        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        string timeStr = string.Format("{0:00}:{1:00}", minutes, seconds);

        bool isTimeRecord = gameTime < bestTime && bestTime != float.MaxValue;
        bool isStepRecord = stepCount < bestStep && bestStep != int.MaxValue;
        bool isFirstWin = bestTime == float.MaxValue || bestStep == int.MaxValue; // 首次通关

        winTimeText.text = $"Time:{timeStr}";
        winStepText.text = $"Steps:{stepCount}";

        string recordTips = "";
        if (isFirstWin)
        {
            recordTips = " First win on this difficulty!";
        }
        else if (isTimeRecord && isStepRecord)
        {
            recordTips = "New time record and step record!";
        }
        else if (isTimeRecord)
        {
            recordTips = $"New time record! \n Old record:{FormatTime(bestTime)}";
        }
        else if (isStepRecord)
        {
            recordTips = $"New steps record! \n Old record:{bestStep} steps";
        }
        else
        {
            recordTips = $"Best Time:{FormatTime(bestTime)} | Best Steps:{bestStep} steps";
        }
        winRecordText.text = recordTips;
        SaveManager.instance.UpdateCurrentSaveDiffRecord(currentDifficulty, gameTime, stepCount);
    }

    private string FormatTime(float time)
    {
        if (time == float.MaxValue) return "--:--"; // 无纪录
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
        if (tiles != null && tileIndex >= 0 && tileIndex < tiles.Length)
        {
            return tiles[tileIndex].transform;
        }
        return null;
    }
}