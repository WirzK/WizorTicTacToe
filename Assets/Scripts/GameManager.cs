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
    [System.Serializable]
    public class GameButtonGroup
    {
        [Header("基础状态按钮")] 
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
    public string startSceneName;
    public string adjustSceneName;

    private List<Coroutine> _breathAnimCoroutines = new List<Coroutine>();

    // 棋盘数据：0=空，1=玩家(X)，2=AI(O)
    private int[] board = new int[9];
    public int difficulty = 1; // 1=简单，2=中等，3=困难

    // 胜利条件：所有行、列、对角线的索引组合
    private int[][] winConditions = new int[][]
    {
        new int[] {0,1,2}, new int[] {3,4,5}, new int[] {6,7,8}, 
        new int[] {0,3,6}, new int[] {1,4,7}, new int[] {2,5,8}, 
        new int[] {0,4,8}, new int[] {2,4,6} 
    };
    [Range(0f, 1f)] // 限制0-1之间
    public float hardAIMinimaxProbability = 0.9f;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        difficulty = SaveManager.instance.diff;
    }

    private void Start()
    {
        StartGame();
        #region
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
        #endregion
    }

    public void StartGame()
    {
        ResetBoard();
        UIManager.instance.HideAllResultPanels();
        isGameActive = true;
        isPlayerTurn = true;
        isTimerStarted = false;
        UIManager.instance.ActivateTiles(true);
    }

    private IEnumerator TimerCoroutine()
    {
        while (isGameActive)
        {
            gameTime += Time.deltaTime;
            UIManager.instance.UpdateTimer(gameTime);
            yield return null;
        }
    }

    public void PlayerMove(int tileIndex)
    {
        if (!isGameActive || !isPlayerTurn || board[tileIndex] != 0) return;

        if (!isTimerStarted)
        {
            _timerCoroutine = StartCoroutine(TimerCoroutine());//第一次落子启动计时器
            isTimerStarted = true; 
        }

        board[tileIndex] = 1;
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
        StartCoroutine(AIMoveCoroutine());
    }

    private IEnumerator AIMoveCoroutine()
    {
        yield return new WaitForSeconds(0.2f);//假装思考
        int aiIndex = GetBestMove();

        board[aiIndex] = 2;
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

    private int GetBestMove()
    {
        switch (difficulty)
        {
            case 1: return GetRandomMove(); 
            case 2: return GetMediumMove(); 
            case 3: return GetHardMove();   
            default: return GetRandomMove();
        }
    }

    private int GetRandomMove()
    {
        var emptyTiles = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == 0) emptyTiles.Add(i);
        }

        return emptyTiles[UnityEngine.Random.Range(0, emptyTiles.Count)];
    }

    private int GetMediumMove()
    {
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == 0)
            {
                board[i] = 1;
                if (CheckWinForAI(1))
                {
                    board[i] = 0;
                    return i;
                }
                board[i] = 0;
            }
        }
        return GetRandomMove();
    }

    // 困难AI：90% Minimax算法，10% 随机（概率可调整）
    private int GetHardMove()
    {
        float randomValue = UnityEngine.Random.Range(0f, 1f);
        if (randomValue <= hardAIMinimaxProbability)
        {
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
            return GetRandomMove();
        }
    }

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

    private bool CheckWin(int player)
    {
        foreach (var condition in winConditions)
        {
            if (board[condition[0]] == player &&
                board[condition[1]] == player &&
                board[condition[2]] == player)
            {
                _winningTiles = condition;
                return true;
            }
        }
        return false;
    }

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

    private void EndGame(bool playerWin, bool isDraw = false)
    {
        isGameActive = false;
        UIManager.instance.ActivateTiles(false);

        if (isTimerStarted && _timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
            isTimerStarted = false;
        }

        if (isDraw)
        {
            UIManager.instance.ShowResult(playerWin, isDraw, gameTime, stepCount);
        }
        else
        {
            StartCoroutine(WinBreathAnimationAndSettlement(playerWin));
        }
    }

    private IEnumerator WinBreathAnimationAndSettlement(bool playerWin)
    {
        if (_winningTiles == null || _winningTiles.Length != 3)
        {
            UIManager.instance.ShowResult(playerWin, false, gameTime, stepCount);
            yield break;
        }

        for (int i = 0; i < _winningTiles.Length; i++)
        {
            int tileIndex = _winningTiles[i];
            Coroutine animCoroutine = StartCoroutine(BreathTileScale(tileIndex));
            _breathAnimCoroutines.Add(animCoroutine);
            if (i < _winningTiles.Length - 1)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }

        yield return new WaitForSeconds(1f);

        UIManager.instance.ShowResult(playerWin, false, gameTime, stepCount);
    }

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

    public void ResetBoard()
    {
        foreach (Coroutine coroutine in _breathAnimCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        _breathAnimCoroutines.Clear();

        if (_winningTiles != null)
        {
            foreach (int tileIndex in _winningTiles)
            {
                Transform tileTransform = UIManager.instance.GetTileTransform(tileIndex);
                if (tileTransform != null)
                {
                    tileTransform.localScale = Vector3.one;
                }
            }
        }

        Array.Clear(board, 0, board.Length);
        stepCount = 0;
        gameTime = 0f;
        isTimerStarted = false;
        isGameActive = false;
        _winningTiles = null;
        _timerCoroutine = null;

        UIManager.instance.ResetTiles();
        UIManager.instance.UpdateTimer(0f);
        UIManager.instance.UpdateStep(0);
    }
    public void BackToStartScene()
    {
        isGameActive = false;
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
        foreach (Coroutine coroutine in _breathAnimCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        _breathAnimCoroutines.Clear();

        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);

        Debug.Log($"跳转到开始界面：{startSceneName}");
    }
    public void BackToBeginScene()
    {
        isGameActive = false;
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
        foreach (Coroutine coroutine in _breathAnimCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        _breathAnimCoroutines.Clear();
        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);

        Debug.Log($"跳转到开始界面：{startSceneName}");
    }
    public void AdjustDiff()
    {
        isGameActive = false;
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
        foreach (Coroutine coroutine in _breathAnimCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        _breathAnimCoroutines.Clear();
        SceneManager.LoadScene(adjustSceneName, LoadSceneMode.Single);

        Debug.Log($"跳转到难度界面：{adjustSceneName}");
    }
}