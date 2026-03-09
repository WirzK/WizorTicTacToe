using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum EndlessGamePhase
{
    Purchase,
    Battle,
    Settlement
}

public class EndlessManager : MonoBehaviour
{
    #region 单例
    public static EndlessManager instance;
    #endregion

    #region 配置项
    [Header("按钮配置")]
    public EndlessButtonGroup endlessButtons;

    [Header("游戏阶段配置")]
    public EndlessGamePhase currentPhase;
    public float phaseSwitchDelay = 1f;

    [Header("资源初始配置")]
    public int initialHp = 3;
    public int initialMoney = 30;
    public int currentHp;
    public int currentMoney;
    public int totalMoney = 0;

    [Header("连胜相关配置")]
    public int winStreak = 0;
    public int maxWinStreak = 0;
    public string startSceneName;

    [Header("动态难度配置")]
    public int difficultyLevel = 1;
    public float currentMinimaxProb;

    [Header("难度分段配置")]
    public int easyDifficultyMax = 5;
    public int normalDifficultyMax = 10;
    public int hardDifficultyStart = 11;
    public int hardDifficultyMax = 15;
    public float easyMinimaxProb = 0f;
    public float normalMinimaxProb = 0f;
    public float hardStartMinimaxProb = 0.6f;
    public float hardMaxMinimaxProb = 1f;

    [Header("战斗状态")]
    public bool isPlayerTurn = true;
    public bool isGameActive = false;
    public int stepCount = 0;
    public float gameTime = 0f;
    private bool _isFirstRound = true;
    #endregion

    #region 战斗核心数据
    private int[] _board = new int[9];
    private int[][] _winConditions = new int[][]
    {
        new int[] {0,1,2}, new int[] {3,4,5}, new int[] {6,7,8},
        new int[] {0,3,6}, new int[] {1,4,7}, new int[] {2,5,8},
        new int[] {0,4,8}, new int[] {2,4,6}
    };
    private int[] _winningTiles;
    private List<Coroutine> _breathAnimCoroutines = new List<Coroutine>();
    private int[] _priorityPositions = new int[] { 4, 0, 2, 6, 8, 1, 3, 5, 7 };
    #endregion

    #region 按钮
    [System.Serializable]
    public class EndlessButtonGroup
    {
        [Header("基础功能按钮")]
        public Button restartBtn;
        public Button backBtn;

        [Header("结算界面按钮")]
        public Button winContinueBtn;
        public Button drawContinueBtn;
        public Button loseContinueBtn;
        public Button endelessRestartBtn;
        public Button endelessBackBtn;

        [Header("阶段切换按钮")]
        public Button shopContinueBtn;
    }
    #endregion

    #region 生命周期
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadEndlessData();
        StartEndlessGame();
        BindAllButtonEvents();
    }
    #endregion

    #region 存档数据初始化
    private void LoadEndlessData()
    {
        if (SaveManager.instance != null)
        {
            maxWinStreak = SaveManager.instance.GetCurrentEndlessBestWinStreak();
        }
        else
        {
            maxWinStreak = 0;
        }

        difficultyLevel = 1;
        currentMinimaxProb = CalculateMinimaxProbability(difficultyLevel);
    }

    private void SaveMaxWinStreak()
    {
        if (winStreak > maxWinStreak)
        {
            maxWinStreak = winStreak;

            if (SaveManager.instance != null)
            {
                SaveManager.instance.UpdateCurrentSaveEndlessRecord(maxWinStreak);
            }
        }
    }

    public void StartEndlessGame()
    {
        winStreak = 0;
        difficultyLevel = 1;
        currentMinimaxProb = CalculateMinimaxProbability(difficultyLevel);

        currentHp = initialHp;
        currentMoney = initialMoney;
        _isFirstRound = true;

        ResetBoard();
        EndlessUIManager.instance.HideAllResultPanels();

        if (ShopManager.instance != null)
        {
            ShopManager.instance.ResetShopManager();
        }

        SwitchToBattlePhase();
    }
    #endregion

    #region 按钮事件绑定
    private void BindAllButtonEvents()
    {
        BindBaseFunctionButtons();
        BindSettlementButtons();
        BindPhaseSwitchButtons();
    }

    private void BindBaseFunctionButtons()
    {
        if (endlessButtons.restartBtn != null)
            endlessButtons.restartBtn.onClick.AddListener(EndThisGame);

        if (endlessButtons.backBtn != null)
            endlessButtons.backBtn.onClick.AddListener(BackToStartScene);
    }

    private void BindSettlementButtons()
    {
        if (endlessButtons.winContinueBtn != null)
            endlessButtons.winContinueBtn.onClick.AddListener(SwitchToPurchasePhase);

        if (endlessButtons.drawContinueBtn != null)
            endlessButtons.drawContinueBtn.onClick.AddListener(SwitchToPurchasePhase);

        if (endlessButtons.loseContinueBtn != null)
            endlessButtons.loseContinueBtn.onClick.AddListener(SwitchToPurchasePhase);

        if (endlessButtons.endelessRestartBtn != null)
            endlessButtons.endelessRestartBtn.onClick.AddListener(StartEndlessGame);

        if (endlessButtons.endelessBackBtn != null)
            endlessButtons.endelessBackBtn.onClick.AddListener(BackToStartScene);
    }

    private void BindPhaseSwitchButtons()
    {
        if (endlessButtons.shopContinueBtn != null)
            endlessButtons.shopContinueBtn.onClick.AddListener(SwitchToBattlePhase);
    }
    #endregion

    #region 游戏流程控制
    public void SwitchToPurchasePhase()
    {
        currentPhase = EndlessGamePhase.Purchase;
        isGameActive = false;

        EndlessUIManager.instance.ShowPurchasePhase();
        EndlessUIManager.instance.UpdateEndlessResources(
            currentHp,
            currentMoney,
            winStreak,
            maxWinStreak
        );

        endlessButtons.shopContinueBtn.interactable = true;
    }

    public void SwitchToBattlePhase()
    {
        if (!_isFirstRound)
        {
            EndlessUIManager.instance.HideShopPanels();
        }
        else
        {
            _isFirstRound = false;
        }

        StartCoroutine(SwitchPhaseCoroutine(EndlessGamePhase.Battle));
    }

    public void SwitchToSettlementPhase()
    {
        StartCoroutine(SwitchPhaseCoroutine(EndlessGamePhase.Settlement));
    }

    private IEnumerator SwitchPhaseCoroutine(EndlessGamePhase targetPhase)
    {
        yield return new WaitForSeconds(phaseSwitchDelay);

        currentPhase = targetPhase;

        if (targetPhase == EndlessGamePhase.Battle)
        {
            InitBattlePhase();
        }
        else if (targetPhase == EndlessGamePhase.Settlement)
        {
            ProcessSettlement();
        }
    }

    private void InitBattlePhase()
    {
        ShopManager.instance.currentItem = EndlessItemType.None;
        ResetBoard();
        EndlessUIManager.instance.HideAllResultPanels();

        isGameActive = true;
        isPlayerTurn = true;
        stepCount = 0;
        gameTime = 0f;

        EndlessUIManager.instance.ActivateTiles(true);
        EndlessUIManager.instance.UpdateEndlessResources(
            currentHp,
            currentMoney,
            winStreak,
            maxWinStreak
        );
    }
    #endregion

    #region 战斗核心逻辑
    public void PlayerMove(int tileIndex)
    {
        if (currentPhase != EndlessGamePhase.Battle || !isGameActive || !isPlayerTurn || _board[tileIndex] != 0)
            return;

        _board[tileIndex] = 1;
        stepCount++;

        EndlessUIManager.instance.UpdateStep(stepCount);
        EndlessUIManager.instance.UpdateTile(tileIndex, 1);

        if (CheckWin(1))
        {
            EndBattle(true);
            return;
        }
        if (CheckDraw())
        {
            EndBattle(false, true);
            return;
        }

        isPlayerTurn = false;
        StartCoroutine(AIMoveCoroutine());
    }

    private IEnumerator AIMoveCoroutine()
    {
        yield return new WaitForSeconds(0.2f);

        int aiIndex = GetBestMove();

        _board[aiIndex] = 2;
        EndlessUIManager.instance.UpdateTile(aiIndex, 2);
        EndlessUIManager.instance.UpdateStep(stepCount);

        if (CheckWin(2))
        {
            EndBattle(false);
            yield break;
        }
        if (CheckDraw())
        {
            EndBattle(false, true);
            yield break;
        }

        isPlayerTurn = true;
    }

    private int GetBestMove()
    {
        if (difficultyLevel <= easyDifficultyMax)
        {
            return GetDefensiveRandomMove();
        }
        else if (difficultyLevel <= normalDifficultyMax)
        {
            return GetNormalDifficultyMove();
        }
        else
        {
            float randomValue = UnityEngine.Random.Range(0f, 1f);
            if (randomValue <= currentMinimaxProb)
            {
                return GetMinimaxBestMove();
            }
            else
            {
                return GetNormalDifficultyMove();
            }
        }
    }

    private int GetNormalDifficultyMove()
    {
        int winMove = GetWinningMove(2);
        if (winMove != -1) return winMove;

        int blockMove = GetWinningMove(1);
        if (blockMove != -1) return blockMove;

        if (_board[4] == 0) return 4;

        List<int> emptyCorners = new List<int>();
        foreach (int corner in new int[] { 0, 2, 6, 8 })
        {
            if (_board[corner] == 0) emptyCorners.Add(corner);
        }
        if (emptyCorners.Count > 0)
        {
            return emptyCorners[UnityEngine.Random.Range(0, emptyCorners.Count)];
        }

        List<int> emptyEdges = new List<int>();
        foreach (int edge in new int[] { 1, 3, 5, 7 })
        {
            if (_board[edge] == 0) emptyEdges.Add(edge);
        }
        if (emptyEdges.Count > 0)
        {
            return emptyEdges[UnityEngine.Random.Range(0, emptyEdges.Count)];
        }

        return GetRandomMove();
    }

    private int GetWinningMove(int player)
    {
        foreach (var condition in _winConditions)
        {
            int count = 0;
            int emptyIndex = -1;
            foreach (int index in condition)
            {
                if (_board[index] == player) count++;
                else if (_board[index] == 0) emptyIndex = index;
            }

            if (count == 2 && emptyIndex != -1)
            {
                return emptyIndex;
            }
        }
        return -1;
    }

    private int GetMinimaxBestMove()
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

        return bestMove >= 0 ? bestMove : GetNormalDifficultyMove();
    }

    private int GetDefensiveRandomMove()
    {
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

    private int GetRandomMove()
    {
        var emptyTiles = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            if (_board[i] == 0) emptyTiles.Add(i);
        }
        return emptyTiles[UnityEngine.Random.Range(0, emptyTiles.Count)];
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
        foreach (var condition in _winConditions)
        {
            if (_board[condition[0]] == player && _board[condition[1]] == player && _board[condition[2]] == player)
            {
                _winningTiles = condition;
                return true;
            }
        }
        return false;
    }

    private bool CheckWinForAI(int player)
    {
        foreach (var condition in _winConditions)
        {
            if (_board[condition[0]] == player && _board[condition[1]] == player && _board[condition[2]] == player)
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckDraw()
    {
        foreach (int tile in _board)
        {
            if (tile == 0) return false;
        }
        return true;
    }

    private void EndBattle(bool playerWin, bool isDraw = false)
    {
        isGameActive = false;
        EndlessUIManager.instance.ActivateTiles(false);

        if (isDraw)
        {
            SwitchToSettlementPhase();
        }
        else
        {
            StartCoroutine(WinBreathAnimationAndSettlement(playerWin));
        }
    }

    private IEnumerator WinBreathAnimationAndSettlement(bool playerWin)
    {
        if (_winningTiles != null && _winningTiles.Length == 3)
        {
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
        }

        yield return new WaitForSeconds(1f);
        SwitchToSettlementPhase();
    }

    private IEnumerator BreathTileScale(int tileIndex)
    {
        Transform tileTransform = EndlessUIManager.instance.GetTileTransform(tileIndex);
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
            if (coroutine != null) StopCoroutine(coroutine);
        }
        _breathAnimCoroutines.Clear();

        if (_winningTiles != null)
        {
            foreach (int tileIndex in _winningTiles)
            {
                Transform tileTransform = EndlessUIManager.instance.GetTileTransform(tileIndex);
                if (tileTransform != null) tileTransform.localScale = Vector3.one;
            }
        }
        EndlessUIManager.instance.ResetTiles();

        Array.Clear(_board, 0, _board.Length);
        stepCount = 0;
        gameTime = 0f;
        _winningTiles = null;
    }
    #endregion

    #region 结算逻辑
    private void ProcessSettlement()
    {
        bool originalWin = CheckWin(1);
        bool originalDraw = CheckDraw();
        bool finalWin = originalWin;
        bool finalDraw = originalDraw;
        bool isRevive = false;

        if (ShopManager.instance != null)
        {
            var (itemWin, itemDraw, itemRevive) = ShopManager.instance.ApplyItemEffect(originalWin, originalDraw);
            finalWin = itemWin;
            finalDraw = itemDraw;
            isRevive = itemRevive;
        }

        if (finalWin)
        {
            ProcessWinSettlement();
        }
        else if (finalDraw)
        {
            ProcessDrawSettlement();
        }
        else
        {
            ProcessLoseSettlement(isRevive);
        }

        EndlessUIManager.instance.UpdateEndlessResources(
            currentHp,
            currentMoney,
            winStreak,
            maxWinStreak
        );

        if (ShopManager.instance != null)
        {
            ShopManager.instance.isOneTimeItemUsed = false;
            ShopManager.instance.usedItem = EndlessItemType.None;
        }
    }

    private void ProcessWinSettlement()
    {
        int winAdd = (ShopManager.instance != null &&
                     ShopManager.instance.isOneTimeItemUsed &&
                     ShopManager.instance.usedItem == EndlessItemType.DoubleWinCount) ? 2 : 1;
        winStreak += winAdd;

        IncreaseDifficulty();

        int moneyReward = Mathf.Max(9 - stepCount, 4);
        totalMoney += moneyReward;

        if (ShopManager.instance != null)
        {
            ShopManager.instance.AddMoney(moneyReward);
        }

        EndlessUIManager.instance.ShowEndlessResult(0, winStreak, currentHp, moneyReward, stepCount);
    }

    private void ProcessDrawSettlement()
    {
        EndlessUIManager.instance.ShowEndlessResult(1, winStreak, currentHp, 0, 0);
    }

    private void ProcessLoseSettlement(bool isRevive)
    {
        if (!isRevive)
        {
            ReduceHp();
            ReduceDifficulty();
        }
        else
        {
            ReduceDifficulty();
        }

        if (currentHp <= 0)
        {
            EndThisGame();
        }
        else
        {
            EndlessUIManager.instance.ShowEndlessResult(2, winStreak, currentHp, 0, 0);
        }
    }

    public void ReduceHp(int amount = 1)
    {
        currentHp = Mathf.Max(currentHp - amount, 0);
        EndlessUIManager.instance.UpdateEndlessResources(currentHp, currentMoney, winStreak, maxWinStreak);
    }

    private void IncreaseDifficulty()
    {
        difficultyLevel += 1;
        currentMinimaxProb = CalculateMinimaxProbability(difficultyLevel);
        Debug.Log($"难度{difficultyLevel}级，Minimax概率：{currentMinimaxProb:P0}");
    }

    private void ReduceDifficulty()
    {
        difficultyLevel = Mathf.Max(difficultyLevel - 1, 1);
        currentMinimaxProb = CalculateMinimaxProbability(difficultyLevel);
        Debug.Log($"难度{difficultyLevel}级，Minimax概率：{currentMinimaxProb:P0}");
    }

    private float CalculateMinimaxProbability(int level)
    {
        if (level <= easyDifficultyMax)
        {
            return easyMinimaxProb;
        }
        else if (level <= normalDifficultyMax)
        {
            return normalMinimaxProb;
        }
        else if (level <= hardDifficultyMax)
        {
            float ratio = (float)(level - hardDifficultyStart) / (hardDifficultyMax - hardDifficultyStart);
            return Mathf.Lerp(hardStartMinimaxProb, hardMaxMinimaxProb, ratio);
        }
        else
        {
            return hardMaxMinimaxProb;
        }
    }

    private void EndThisGame()
    {
        bool breakRecord = winStreak > maxWinStreak;
        SaveMaxWinStreak();
        EndlessUIManager.instance.ShowEndlessGameOver(winStreak, maxWinStreak, breakRecord);
    }
    #endregion

    #region 场景切换
    public void BackToStartScene()
    {
        isGameActive = false;

        foreach (Coroutine coroutine in _breathAnimCoroutines)
            StopCoroutine(coroutine);
        _breathAnimCoroutines.Clear();

        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
        Debug.Log($"去{startSceneName}");
    }
    #endregion
}