using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [System.Serializable]
    public class GameButtonGroup
    {
        [Header("基础操作按钮")]
        public Button restartBtn;
        public Button adjustBtn;
        public Button backBtn;

        [Header("胜利界面按钮")]
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

    [Header("游戏按钮组")]
    public GameButtonGroup gameButtons;

    // 游戏核心状态（公有字段保持原样）
    public bool isPlayerTurn = true;
    public bool isGameActive = false;
    public int stepCount = 0;
    public float gameTime = 0f;
    private bool _isTimerStarted = false;
    private Coroutine _gameTimerCoroutine;
    private int[] _winningTileIndexes;
    public string startSceneName;
    public string adjustSceneName;

    private List<Coroutine> _breathAnimCoroutines = new List<Coroutine>();

    // 棋盘数据：0=空，1=玩家(X)，2=AI(O)
    private int[] _board = new int[9];
    public int difficulty = 1; // 难度：1-简单 2-中等 3-困难
    [Range(0f, 1f)]
    public float hardAIMinimaxProbability = 0.9f;

    // 胜利条件：行、列、对角线的索引组合
    private int[][] _winConditions = new int[][]
    {
        new int[] {0,1,2}, new int[] {3,4,5}, new int[] {6,7,8},
        new int[] {0,3,6}, new int[] {1,4,7}, new int[] {2,5,8},
        new int[] {0,4,8}, new int[] {2,4,6}
    };

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        difficulty = SaveManager.instance.diff;
    }

    private void Start()
    {
        StartGame();
        BindAllButtons();
    }

    // 绑定所有按钮事件
    private void BindAllButtons()
    {
        gameButtons.restartBtn.onClick.AddListener(StartGame);
        gameButtons.adjustBtn.onClick.AddListener(AdjustDiff);
        gameButtons.backBtn.onClick.AddListener(BackToStartScene);

        gameButtons.winRestartBtn.onClick.AddListener(StartGame);
        gameButtons.winAdjustBtn.onClick.AddListener(AdjustDiff);
        gameButtons.winBackBtn.onClick.AddListener(BackToStartScene);

        gameButtons.drawRestartBtn.onClick.AddListener(StartGame);
        gameButtons.drawAdjustBtn.onClick.AddListener(AdjustDiff);
        gameButtons.drawBackBtn.onClick.AddListener(BackToStartScene);

        gameButtons.loseRestartBtn.onClick.AddListener(StartGame);
        gameButtons.loseAdjustBtn.onClick.AddListener(AdjustDiff);
        gameButtons.loseBackBtn.onClick.AddListener(BackToStartScene);
    }

    // 开始/重启游戏
    public void StartGame()
    {
        ResetBoard();
        UIManager.instance.HideAllResultPanels();
        isGameActive = true;
        isPlayerTurn = true;
        _isTimerStarted = false;
        UIManager.instance.ActivateTiles(true);
    }

    // 游戏计时器
    private IEnumerator GameTimer()
    {
        while (isGameActive)
        {
            gameTime += Time.deltaTime;
            UIManager.instance.UpdateTimer(gameTime);
            yield return null;
        }
    }

    // 玩家落子逻辑
    public void PlayerMove(int tileIndex)
    {
        if (!isGameActive || !isPlayerTurn || _board[tileIndex] != 0) return;

        // 首次落子启动计时器
        if (!_isTimerStarted)
        {
            _gameTimerCoroutine = StartCoroutine(GameTimer());
            _isTimerStarted = true;
        }

        _board[tileIndex] = 1;
        stepCount++;
        UIManager.instance.UpdateStep(stepCount);
        UIManager.instance.UpdateTile(tileIndex, 1);

        if (CheckWin(1))
        {
            EndGame(true); // 玩家胜利
            return;
        }

        if (CheckDraw())
        {
            EndGame(false, true); // 平局
            return;
        }

        isPlayerTurn = false;
        StartCoroutine(AIMove());
    }

    // AI落子逻辑（带思考延迟）
    private IEnumerator AIMove()
    {
        yield return new WaitForSeconds(0.2f); // 模拟AI思考延迟
        int aiIndex = GetBestMove();

        _board[aiIndex] = 2;
        UIManager.instance.UpdateTile(aiIndex, 2);
        UIManager.instance.UpdateStep(stepCount);

        if (CheckWin(2))
        {
            EndGame(false); // AI胜利
            yield break;
        }

        if (CheckDraw())
        {
            EndGame(false, true);
            yield break;
        }

        isPlayerTurn = true;
    }

    // 根据难度获取AI最佳落子位置
    private int GetBestMove()
    {
        return difficulty switch
        {
            1 => GetRandomMove(),
            2 => GetMediumMove(),
            3 => GetHardMove(),
            _ => GetRandomMove()
        };
    }

    // 简单难度：随机落子
    private int GetRandomMove()
    {
        List<int> emptyTiles = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            if (_board[i] == 0) emptyTiles.Add(i);
        }

        return emptyTiles[UnityEngine.Random.Range(0, emptyTiles.Count)];
    }

    // 中等难度：优先阻挡玩家胜利
    private int GetMediumMove()
    {
        // 检查是否能阻挡玩家
        for (int i = 0; i < 9; i++)
        {
            if (_board[i] == 0)
            {
                _board[i] = 1;
                if (CheckWinForAI(1))
                {
                    _board[i] = 0;
                    return i;
                }
                _board[i] = 0;
            }
        }
        return GetRandomMove();
    }

    // 困难难度：90%概率使用Minimax算法，10%随机（可配置）
    private int GetHardMove()
    {
        float randomValue = UnityEngine.Random.Range(0f, 1f);
        if (randomValue <= hardAIMinimaxProbability)
        {
            int bestScore = int.MinValue;
            int bestMove = -1;

            for (int i = 0; i < 9; i++)
            {
                if (_board[i] == 0)
                {
                    _board[i] = 2;
                    int score = Minimax(_board, 0, false);
                    _board[i] = 0;
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
            return GetRandomMove();
        }
    }

    // Minimax算法核心
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

    // 检查胜利（带记录胜利棋子索引）
    private bool CheckWin(int player)
    {
        foreach (var condition in _winConditions)
        {
            if (_board[condition[0]] == player &&
                _board[condition[1]] == player &&
                _board[condition[2]] == player)
            {
                _winningTileIndexes = condition;
                return true;
            }
        }
        return false;
    }

    // AI专用胜利检查（无需记录索引）
    private bool CheckWinForAI(int player)
    {
        foreach (var condition in _winConditions)
        {
            if (_board[condition[0]] == player &&
                _board[condition[1]] == player &&
                _board[condition[2]] == player)
            {
                return true;
            }
        }
        return false;
    }

    // 检查平局
    private bool CheckDraw()
    {
        foreach (int tile in _board)
        {
            if (tile == 0) return false;
        }
        return true;
    }

    // 结束游戏
    private void EndGame(bool playerWin, bool isDraw = false)
    {
        isGameActive = false;
        UIManager.instance.ActivateTiles(false);

        // 停止计时器
        if (_isTimerStarted && _gameTimerCoroutine != null)
        {
            StopCoroutine(_gameTimerCoroutine);
            _gameTimerCoroutine = null;
            _isTimerStarted = false;
        }

        if (isDraw)
        {
            UIManager.instance.ShowResult(playerWin, isDraw, gameTime, stepCount);
        }
        else
        {
            StartCoroutine(WinAnimationAndSettlement(playerWin));
        }
    }

    // 胜利棋子呼吸动画 + 结算
    private IEnumerator WinAnimationAndSettlement(bool playerWin)
    {
        if (_winningTileIndexes == null || _winningTileIndexes.Length != 3)
        {
            UIManager.instance.ShowResult(playerWin, false, gameTime, stepCount);
            yield break;
        }

        // 依次播放胜利棋子呼吸动画
        for (int i = 0; i < _winningTileIndexes.Length; i++)
        {
            int tileIndex = _winningTileIndexes[i];
            Coroutine animCoroutine = StartCoroutine(BreathTileScale(tileIndex));
            _breathAnimCoroutines.Add(animCoroutine);

            if (i < _winningTileIndexes.Length - 1)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }

        yield return new WaitForSeconds(1f);
        UIManager.instance.ShowResult(playerWin, false, gameTime, stepCount);
    }

    // 棋子呼吸缩放动画
    private IEnumerator BreathTileScale(int tileIndex)
    {
        Transform tileTransform = UIManager.instance.GetTileTransform(tileIndex);
        if (tileTransform == null) yield break;

        float breathCycle = 2f;
        Vector3 originalScale = tileTransform.localScale;
        float minScale = 0.75f;
        float maxScale = 1.25f;
        float scaleRange = maxScale - minScale;

        while (true)
        {
            float t = Mathf.PingPong(Time.time * (1f / breathCycle), 1f);
            float currentScale = minScale + (t * scaleRange);
            tileTransform.localScale = new Vector3(currentScale, currentScale, originalScale.z);

            yield return null;
        }
    }

    // 重置棋盘状态
    public void ResetBoard()
    {
        // 停止所有呼吸动画
        foreach (Coroutine coroutine in _breathAnimCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        _breathAnimCoroutines.Clear();

        // 恢复胜利棋子缩放
        if (_winningTileIndexes != null)
        {
            foreach (int tileIndex in _winningTileIndexes)
            {
                Transform tileTransform = UIManager.instance.GetTileTransform(tileIndex);
                if (tileTransform != null) tileTransform.localScale = Vector3.one;
            }
        }

        // 重置核心数据
        Array.Clear(_board, 0, _board.Length);
        stepCount = 0;
        gameTime = 0f;
        _isTimerStarted = false;
        isGameActive = false;
        _winningTileIndexes = null;
        _gameTimerCoroutine = null;

        // 更新UI
        UIManager.instance.ResetTiles();
        UIManager.instance.UpdateTimer(0f);
        UIManager.instance.UpdateStep(0);
    }

    // 返回开始场景
    public void BackToStartScene()
    {
        StopAllGameProcess();
        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
        Debug.Log($"切换至开始场景：{startSceneName}");
    }

    // 跳转到难度调整场景
    public void AdjustDiff()
    {
        StopAllGameProcess();
        SceneManager.LoadScene(adjustSceneName, LoadSceneMode.Single);
        Debug.Log($"切换至难度调整场景：{adjustSceneName}");
    }

    // 停止所有游戏进程（通用方法）
    private void StopAllGameProcess()
    {
        isGameActive = false;

        // 停止计时器
        if (_gameTimerCoroutine != null)
        {
            StopCoroutine(_gameTimerCoroutine);
            _gameTimerCoroutine = null;
        }

        // 停止所有动画
        foreach (Coroutine coroutine in _breathAnimCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        _breathAnimCoroutines.Clear();
    }
}