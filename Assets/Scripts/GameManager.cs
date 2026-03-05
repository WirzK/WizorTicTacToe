using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    //按钮
    #region
    [System.Serializable] // 必须加这个，否则Inspector里看不到这个类的内容
    public class GameButtonGroup
    {
        [Header("基础状态按钮")] // 可选：给按钮分组加子标题，更清晰
        public Button restartBtn;
        public Button adjustBtn;
        public Button backBtn;

        [Header("胜利界面按钮")] // 子标题分隔不同状态的按钮
        public Button winRestartBtn;
        public Button winAdjustBtn;
        public Button winBackBtn;

        [Header("平局界面按钮")]
        public Button drawRestartBtn;
        public Button drawAdjustBtn;
        public Button drawBackBtn;

        [Header("失败界面按钮")]
        public Button loseRestartBtn;
        public Button loseAdjustBtn;
        public Button loseBackBtn;
    }

    [Header("游玩界面按钮")]
    public GameButtonGroup gameButtons;
    #endregion

    // 游戏状态
    public bool isPlayerTurn = true;
    public bool isGameActive = false;
    public int stepCount = 0;
    public float gameTime = 0f;
    private bool isTimerStarted = false;
    private Coroutine _timerCoroutine;
    private int[] _winningTiles;
    public string startSceneName = "StartMenu"; // 替换成你实际的开始界面场景名

    // 新增：保存所有呼吸动画的协程引用（用于重置时停止）
    private List<Coroutine> _breathAnimCoroutines = new List<Coroutine>();

    // 棋盘数据：0=空，1=玩家(X)，2=AI(O)
    private int[] board = new int[9];
    public int difficulty = 1; // 1=简单，2=中等，3=困难

    // 胜利条件：所有行、列、对角线的索引组合
    private int[][] winConditions = new int[][]
    {
        new int[] {0,1,2}, new int[] {3,4,5}, new int[] {6,7,8}, // 行
        new int[] {0,3,6}, new int[] {1,4,7}, new int[] {2,5,8}, // 列
        new int[] {0,4,8}, new int[] {2,4,6} // 对角线
    };
    [Range(0f, 1f)] // 限制0-1之间，方便调试
    public float hardAIMinimaxProbability = 0.9f; // 0.9=90%概率用Minimax，10%随机

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartGame(difficulty);
        // 按钮事件绑定
        #region
        gameButtons.restartBtn.onClick.AddListener(RestartGame);
        gameButtons.adjustBtn.onClick.AddListener(BackToSettingScene);
        gameButtons.backBtn.onClick.AddListener(BackToStartScene);
        gameButtons.winRestartBtn.onClick.AddListener(RestartGame);
        gameButtons.winAdjustBtn.onClick.AddListener(BackToSettingScene);
        gameButtons.winBackBtn.onClick.AddListener(BackToStartScene);
        gameButtons.drawRestartBtn.onClick.AddListener(RestartGame);
        gameButtons.drawAdjustBtn.onClick.AddListener(BackToSettingScene);
        gameButtons.drawBackBtn.onClick.AddListener(BackToStartScene);
        gameButtons.loseRestartBtn.onClick.AddListener(RestartGame);
        gameButtons.loseAdjustBtn.onClick.AddListener(BackToSettingScene);
        gameButtons.loseBackBtn.onClick.AddListener(BackToStartScene);
        #endregion
    }

    // 初始化游戏（难度由难度选择页传入）
    public void StartGame(int diff)
    {
        difficulty = diff;
        ResetBoard(); // 重置棋盘和状态
        UIManager.instance.HideAllResultPanels();
        isGameActive = true;
        isPlayerTurn = true;
        isTimerStarted = false;
        UIManager.instance.ActivateTiles(true); // 激活所有棋盘按钮
    }
    public void RestartGame()//留给按钮用的无参版本
    {
        ResetBoard(); // 重置棋盘和状态

        isGameActive = true;
        isPlayerTurn = true;
        isTimerStarted = false;
        UIManager.instance.ActivateTiles(true); // 激活所有棋盘按钮
    }

    // 计时器协程（仅玩家第一次点击后启动）
    private IEnumerator TimerCoroutine()
    {
        while (isGameActive)
        {
            gameTime += Time.deltaTime;
            UIManager.instance.UpdateTimer(gameTime);
            yield return null;
        }
    }

    // 玩家下棋（核心修改：第一次点击时启动计时器）
    public void PlayerMove(int tileIndex)
    {
        // 校验：游戏未激活/非玩家回合/格子已占 → 直接返回
        if (!isGameActive || !isPlayerTurn || board[tileIndex] != 0) return;

        // 关键：玩家第一次点击时启动计时器
        if (!isTimerStarted)
        {
            // 保存协程引用，方便后续停止
            _timerCoroutine = StartCoroutine(TimerCoroutine());
            isTimerStarted = true; // 标记计时器已启动，避免重复启动
        }

        // 玩家落子逻辑
        board[tileIndex] = 1;
        stepCount++;
        UIManager.instance.UpdateStep(stepCount);
        UIManager.instance.UpdateTile(tileIndex, 1); // 补充：显示玩家的X

        // 检查玩家胜利
        if (CheckWin(1))
        {
            EndGame(true); // 玩家胜利
            return;
        }
        // 检查平局
        if (CheckDraw())
        {
            EndGame(false, true); // 平局
            return;
        }

        // 切换到AI回合
        isPlayerTurn = false;
        StartCoroutine(AIMoveCoroutine());
    }

    //用0.2s的停顿模拟思考
    private IEnumerator AIMoveCoroutine()
    {
        yield return new WaitForSeconds(0.2f);
        int aiIndex = GetBestMove();

        // AI落子逻辑
        board[aiIndex] = 2;
        stepCount++;
        UIManager.instance.UpdateTile(aiIndex, 2); // 显示O
        UIManager.instance.UpdateStep(stepCount);

        // 检查AI胜利
        if (CheckWin(2))
        {
            EndGame(false); // AI胜利，传入false表示玩家未胜利
            yield break;
        }
        // 检查平局
        if (CheckDraw())
        {
            EndGame(false, true); // 平局，传入isDraw=true
            yield break;
        }

        // 切换回玩家回合
        isPlayerTurn = true;
    }

    // AI决策：根据难度选择策略
    private int GetBestMove()
    {
        switch (difficulty)
        {
            case 1: return GetRandomMove(); // 简单：随机
            case 2: return GetMediumMove(); // 中等：优先阻止玩家
            case 3: return GetHardMove();   // 困难：Minimax算法
            default: return GetRandomMove();
        }
    }

    // 简单AI：随机选择空位置
    private int GetRandomMove()
    {
        var emptyTiles = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == 0) emptyTiles.Add(i);
        }

        return emptyTiles[UnityEngine.Random.Range(0, emptyTiles.Count)];
    }

    // 中等AI：优先阻止玩家即将连成一线的位置，否则随机
    private int GetMediumMove()
    {
        // 优先阻止玩家胜利
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == 0)
            {
                board[i] = 1;
                if (CheckWinForAI(1)) // 专用的胜利检查（不触发动画）
                {
                    board[i] = 0;
                    return i;
                }
                board[i] = 0;
            }
        }
        // 否则随机
        return GetRandomMove();
    }

    // 困难AI：Minimax算法（确保不败）
    // 困难AI：90% Minimax算法，10% 随机（概率可调整）
    private int GetHardMove()
    {
        // 随机生成0-1之间的数，判断是否使用Minimax
        float randomValue = UnityEngine.Random.Range(0f, 1f);
        if (randomValue <= hardAIMinimaxProbability)
        {
            // 概率内：使用Minimax算法（原逻辑）
            int bestScore = int.MinValue;
            int bestMove = -1;

            for (int i = 0; i < 9; i++)
            {
                if (board[i] == 0)
                {
                    board[i] = 2;
                    int score = Minimax(board, 0, false);
                    board[i] = 0;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = i;
                    }
                }
            }
            return bestMove;
        }
        else
        {
            // 概率外：随机落子
            return GetRandomMove();
        }
    }

    // Minimax递归函数
    private int Minimax(int[] board, int depth, bool isMaximizing)
    {
        if (CheckWinForAI(2)) return 10 - depth;
        if (CheckWinForAI(1)) return -10 + depth;
        if (CheckDraw()) return 0;

        if (isMaximizing)
        {
            int bestScore = int.MinValue;
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == 0)
                {
                    board[i] = 2;
                    int score = Minimax(board, depth + 1, false);
                    board[i] = 0;
                    bestScore = Math.Max(score, bestScore);
                }
            }
            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == 0)
                {
                    board[i] = 1;
                    int score = Minimax(board, depth + 1, true);
                    board[i] = 0;
                    bestScore = Math.Min(score, bestScore);
                }
            }
            return bestScore;
        }
    }

    // 检查胜负（带动画记录）
    private bool CheckWin(int player)
    {
        foreach (var condition in winConditions)
        {
            if (board[condition[0]] == player &&
                board[condition[1]] == player &&
                board[condition[2]] == player)
            {
                _winningTiles = condition; // 保存获胜的棋子索引
                return true;
            }
        }
        return false;
    }

    // 仅检查胜负（无动画，供AI决策用）
    private bool CheckWinForAI(int player)
    {
        foreach (var condition in winConditions)
        {
            if (board[condition[0]] == player &&
                board[condition[1]] == player &&
                board[condition[2]] == player)
            {
                return true;
            }
        }
        return false;
    }

    // 检查平局
    private bool CheckDraw()
    {
        foreach (int tile in board)
        {
            if (tile == 0) return false;
        }
        return true;
    }

    // 游戏结束（核心修改：区分平局/胜利处理）
    private void EndGame(bool playerWin, bool isDraw = false)
    {
        isGameActive = false;
        UIManager.instance.ActivateTiles(false);

        // 正确停止计时器
        if (isTimerStarted && _timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
            isTimerStarted = false;
        }

        // 逻辑分支：平局直接结算，胜利先播呼吸动画再结算
        if (isDraw)
        {
            // 平局：直接结算
            UIManager.instance.ShowResult(playerWin, isDraw, gameTime, stepCount);
        }
        else
        {
            // 胜利/失败：先启动呼吸动画，等待1s后结算（动画持续到下一局）
            StartCoroutine(WinBreathAnimationAndSettlement(playerWin));
        }
    }

    // 新增：获胜棋子呼吸动画 + 延时结算（动画持续到下一局）
    private IEnumerator WinBreathAnimationAndSettlement(bool playerWin)
    {
        if (_winningTiles == null || _winningTiles.Length != 3)
        {
            // 无获胜棋子，直接结算
            UIManager.instance.ShowResult(playerWin, false, gameTime, stepCount);
            yield break;
        }

        // 逐个启动棋子呼吸动画（间隔0.2s）
        for (int i = 0; i < _winningTiles.Length; i++)
        {
            int tileIndex = _winningTiles[i];
            // 启动呼吸动画并保存协程引用
            Coroutine animCoroutine = StartCoroutine(BreathTileScale(tileIndex));
            _breathAnimCoroutines.Add(animCoroutine);
            // 间隔0.2s再启动下一个
            if (i < _winningTiles.Length - 1)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }

        // 等待1s后执行结算逻辑（动画仍持续播放）
        yield return new WaitForSeconds(1f);

        // 执行结算逻辑
        UIManager.instance.ShowResult(playerWin, false, gameTime, stepCount);
    }

    // 核心修改：呼吸动画（循环缩放，持续到下一局）
    private IEnumerator BreathTileScale(int tileIndex)
    {
        Transform tileTransform = UIManager.instance.GetTileTransform(tileIndex);
        if (tileTransform == null) yield break;

        float breathCycle = 2f; // 呼吸周期（0.75→1.25→0.75 耗时2秒）
        Vector3 originalScale = tileTransform.localScale;
        float minScale = 0.75f;
        float maxScale = 1.25f;
        float scaleRange = maxScale - minScale; // 缩放范围

        // 无限循环呼吸，直到被手动停止
        while (true)
        {
            // Mathf.PingPong：在0→breathCycle→0之间循环，值范围[0, breathCycle]
            float t = Mathf.PingPong(Time.time * (1f / breathCycle), 1f);
            // 线性插值计算当前缩放值（0→1对应min→max）
            float currentScale = minScale + (t * scaleRange);
            tileTransform.localScale = new Vector3(currentScale, currentScale, originalScale.z);

            yield return null;
        }
    }

    // 重置棋盘（重开游戏时调用，核心修改：停止所有呼吸动画）
    public void ResetBoard()
    {
        // 1. 停止所有呼吸动画
        foreach (Coroutine coroutine in _breathAnimCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        _breathAnimCoroutines.Clear(); // 清空协程引用列表

        // 2. 恢复所有获胜棋子的原始缩放
        if (_winningTiles != null)
        {
            foreach (int tileIndex in _winningTiles)
            {
                Transform tileTransform = UIManager.instance.GetTileTransform(tileIndex);
                if (tileTransform != null)
                {
                    tileTransform.localScale = Vector3.one; // 恢复1倍缩放（可改为你的原始缩放）
                }
            }
        }

        // 3. 重置棋盘数据和状态
        Array.Clear(board, 0, board.Length);
        stepCount = 0;
        gameTime = 0f;
        isTimerStarted = false;
        isGameActive = false;
        _winningTiles = null;
        _timerCoroutine = null;

        // 4. 更新UI
        UIManager.instance.ResetTiles();
        UIManager.instance.UpdateTimer(0f);
        UIManager.instance.UpdateStep(0);
    }
    public void BackToStartScene()
    {
        // 停止所有游戏逻辑（避免加载场景时协程/动画还在运行）
        isGameActive = false;
        // 停止计时器协程
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
        // 停止所有呼吸动画
        foreach (Coroutine coroutine in _breathAnimCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        _breathAnimCoroutines.Clear();

        // 核心：加载指定的开始界面场景（Single模式=关闭当前场景，只保留目标场景）
        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);

        Debug.Log($"跳转到开始界面：{startSceneName}");
    }
    //去调整难度
    public void BackToSettingScene()
    {
        // 停止所有游戏逻辑（避免加载场景时协程/动画还在运行）
        isGameActive = false;
        // 停止计时器协程
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
        // 停止所有呼吸动画
        foreach (Coroutine coroutine in _breathAnimCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        _breathAnimCoroutines.Clear();

        // 核心：加载指定的开始界面场景（Single模式=关闭当前场景，只保留目标场景）
        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);

        Debug.Log($"跳转到开始界面：{startSceneName}");
    }
}