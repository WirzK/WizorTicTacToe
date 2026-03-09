using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class EndlessUIManager : MonoBehaviour
{
    #region 单例
    public static EndlessUIManager instance;
    #endregion

    #region 配置项
    [Header("核心UI - 瓷砖操作")]
    public Button[] tileButtons;
    public Transform[] tiles;
    public TextMeshProUGUI stepText;

    [Header("核心UI - 资源显示")]
    public Image[] hpImages;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI winStreakText;

    [Header("核心UI - 道具提示")]
    public Image propsImage;

    [Header("结算面板 - 基础胜负")]
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject drawPanel;
    public GameObject gameOverPanel;

    [Header("结算面板 - 无尽模式专属")]
    public GameObject endlessWinPanel;
    public TextMeshProUGUI winCurrentStreakText;
    public TextMeshProUGUI winResultStepText;
    public TextMeshProUGUI winResultObtainText;
    public TextMeshProUGUI winResultMoneyText;

    public GameObject endlessDrawPanel;
    public TextMeshProUGUI drawCurrentStreakText;
    public Image[] drawResultHpImages;
    public TextMeshProUGUI drawResultMoneyText;

    public GameObject endlessLosePanel;
    public TextMeshProUGUI loseCurrentStreakText;
    public Image[] loseResultHpImages;
    public TextMeshProUGUI loseResultMoneyText;

    [Header("结算面板 - 游戏结束详情")]
    public GameObject endlessResultPanel;
    public TextMeshProUGUI totalMoneyText;
    public TextMeshProUGUI totalPropsText;
    public TextMeshProUGUI finalWinStreakText;
    public TextMeshProUGUI maxWinStreakGameOverText;
    public TextMeshProUGUI breakOrNotText;
    public Button replayBtn;
    public Button returnBtn;

    [Header("商店面板 - 双面板滑入配置")]
    public GameObject leftShopPanel;
    public GameObject rightShopPanel;
    public float panelMoveDuration = 0.5f;

    [Header("商店面板 - 左侧位置配置")]
    public Vector2 leftPanelStartPos;
    public Vector2 leftPanelTargetPos;

    [Header("商店面板 - 右侧位置配置")]
    public Vector2 rightPanelStartPos;
    public Vector2 rightPanelTargetPos;

    [Header("商店面板 - 道具图标")]
    public Sprite[] itemIcons;
    #endregion

    #region 生命周期
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        InitPanelDefaultState();
        InitHpImagesDefaultState();
        InitPropsImageDefaultState();
        BindEndlessButtons();
    }

    private void Start()
    {

    }
    #endregion

    #region 初始化辅助函数
    private void InitPanelDefaultState()
    {
        if (leftShopPanel != null) leftShopPanel.SetActive(false);
        if (rightShopPanel != null) rightShopPanel.SetActive(false);
        if (endlessResultPanel != null) endlessResultPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void InitHpImagesDefaultState()
    {
        if (hpImages != null && hpImages.Length >= 3)
        {
            for (int i = 0; i < hpImages.Length; i++)
            {
                hpImages[i].gameObject.SetActive(true);
            }
        }
    }

    private void InitPropsImageDefaultState()
    {
        if (propsImage != null) propsImage.gameObject.SetActive(false);
    }
    #endregion

    #region 商店面板核心功能
    public void ShowPurchasePhase(bool show = true)
    {
        HideUsedItemIcon();

        if (leftShopPanel == null || rightShopPanel == null) return;

        if (show)
        {
            if (ShopManager.instance?.shopDetailText != null)
            {
                ShopManager.instance.shopDetailText.text = "Click on the item to view details and use it.";
            }

            ShowLeftShopPanel();
            ShowRightShopPanel();
        }
        else
        {
            HideShopPanels();
        }
    }

    private void ShowLeftShopPanel()
    {
        HideAllResultPanels();
        leftShopPanel.SetActive(true);
        RectTransform leftRect = leftShopPanel.GetComponent<RectTransform>();
        if (leftRect != null)
        {
            leftRect.anchoredPosition = leftPanelStartPos;
            StartCoroutine(MovePanelCoroutine(leftRect, leftPanelTargetPos, panelMoveDuration));
        }
    }

    private void ShowRightShopPanel()
    {
        rightShopPanel.SetActive(true);
        RectTransform rightRect = rightShopPanel.GetComponent<RectTransform>();
        ShopManager.instance.shopDetailText.text = "Click on the item to view details and use it.";
        if (rightRect != null)
        {
            rightRect.anchoredPosition = rightPanelStartPos;
            StartCoroutine(MovePanelCoroutine(rightRect, rightPanelTargetPos, panelMoveDuration));
        }
    }

    public void HideShopPanels()
    {
        if (leftShopPanel == null || rightShopPanel == null) return;

        HideLeftShopPanel();
        HideRightShopPanel();
        ResetShopSelectionState();
    }

    private void HideLeftShopPanel()
    {
        RectTransform leftRect = leftShopPanel.GetComponent<RectTransform>();
        if (leftRect != null)
        {
            StartCoroutine(MovePanelCoroutine(leftRect, leftPanelStartPos, panelMoveDuration, () =>
            {
                leftShopPanel.SetActive(false);
            }));
        }
    }

    private void HideRightShopPanel()
    {
        RectTransform rightRect = rightShopPanel.GetComponent<RectTransform>();
        if (rightRect != null)
        {
            StartCoroutine(MovePanelCoroutine(rightRect, rightPanelStartPos, panelMoveDuration, () =>
            {
                rightShopPanel.SetActive(false);
            }));
        }
    }

    private void ResetShopSelectionState()
    {
        if (ShopManager.instance != null)
        {
            ShopManager.instance.selectedItemType = EndlessItemType.None;
            if (ShopManager.instance.shopDetailText != null)
            {
                ShopManager.instance.shopDetailText.text = "Click on the item to view details and use it.";
            }
        }
    }

    private IEnumerator MovePanelCoroutine(RectTransform rect, Vector2 targetPos, float duration, System.Action onComplete = null)
    {
        if (rect == null) yield break;

        Vector2 startPos = rect.anchoredPosition;
        float timer = 0f;

        while (timer < duration)
        {
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = targetPos;
        onComplete?.Invoke();
    }
    #endregion

    #region 道具提示显示/隐藏
    public void ShowUsedItemIcon(EndlessItemType itemType)
    {
        if (propsImage == null || ShopManager.instance?.shopItemButtons == null || ShopManager.instance.shopItemButtons.Length < 5)
            return;

        Button targetButton = GetTargetShopButtonByItemType(itemType);
        if (targetButton == null) return;

        Image buttonImage = targetButton.GetComponent<Image>();
        if (buttonImage != null && buttonImage.sprite != null)
        {
            propsImage.sprite = buttonImage.sprite;
            propsImage.SetNativeSize();
            propsImage.gameObject.SetActive(true);
        }
    }

    private Button GetTargetShopButtonByItemType(EndlessItemType itemType)
    {
        switch (itemType)
        {
            case EndlessItemType.DoubleWinCount:
                return ShopManager.instance.shopItemButtons[0];
            case EndlessItemType.DrawAsWin:
                return ShopManager.instance.shopItemButtons[1];
            case EndlessItemType.LoseNotEnd:
                return ShopManager.instance.shopItemButtons[2];
            case EndlessItemType.DoubleMoney:
                return ShopManager.instance.shopItemButtons[3];
            case EndlessItemType.RestoreHp:
                return ShopManager.instance.shopItemButtons[4];
            default:
                return null;
        }
    }

    public void HideUsedItemIcon()
    {
        if (propsImage != null)
        {
            propsImage.gameObject.SetActive(false);
            propsImage.sprite = null;
        }
    }
    #endregion

    #region 基础UI更新 - 瓷砖操作
    public void UpdateStep(int step)
    {
        if (stepText != null) stepText.text = $"Step: {step}";
    }

    public void UpdateTile(int index, int player)
    {
        TileButton btn = tileButtons[index].GetComponent<TileButton>();
        if (btn == null) return;

        if (player == 1)
        {
            btn.SetPlayerTileSpriteAndDisable();
        }
        else if (player == 2)
        {
            btn.SetTileSpriteAndDisable();
        }
    }

    public void ActivateTiles(bool active)
    {
        foreach (var btn in tileButtons)
        {
            if (btn != null) btn.interactable = active;
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
            if (index >= 0 && index < tileButtons.Length)
            {
                StartCoroutine(ScaleTileCoroutine(tileButtons[index].transform, 1.2f, 0.3f));
            }
        }
    }

    private IEnumerator ScaleTileCoroutine(Transform tile, float targetScale, float duration)
    {
        if (tile == null) yield break;

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

    public Transform GetTileTransform(int tileIndex)
    {
        if (tiles != null && tileIndex >= 0 && tileIndex < tiles.Length)
        {
            return tiles[tileIndex].transform;
        }
        return null;
    }
    #endregion

    #region 基础UI更新 - 资源与结算
    public void UpdateEndlessResources(int hp, int money, int winStreak, int maxWinStreak)
    {
        UpdateHpImages(hp);

        if (moneyText != null)
        {
            moneyText.text = $"{money}";
        }

        if (winStreakText != null)
        {
            winStreakText.text = $"Win Streak: {winStreak}";
        }
    }

    private void UpdateHpImages(int hp)
    {
        if (hpImages != null && hpImages.Length >= 3)
        {
            for (int i = 0; i < 3; i++)
            {
                bool isActive = i < hp;
                if (hpImages[i] != null)
                {
                    StartCoroutine(FadeImageCoroutine(hpImages[i], isActive ? 1f : 0f, 0.2f, isActive));
                }
            }
        }
    }

    private IEnumerator FadeImageCoroutine(Image img, float targetAlpha, float duration, bool setActive = false)
    {
        if (img == null) yield break;

        img.gameObject.SetActive(true);
        Color startColor = img.color;
        float startAlpha = startColor.a;
        float timer = 0f;

        while (timer < duration)
        {
            startColor.a = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            img.color = startColor;
            timer += Time.deltaTime;
            yield return null;
        }

        startColor.a = targetAlpha;
        img.color = startColor;

        if (targetAlpha == 0f)
        {
            img.gameObject.SetActive(false);
        }
        else
        {
            img.gameObject.SetActive(setActive);
        }
    }

    public void HideAllResultPanels()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (drawPanel != null) drawPanel.SetActive(false);

        if (endlessWinPanel != null) endlessWinPanel.SetActive(false);
        if (endlessDrawPanel != null) endlessDrawPanel.SetActive(false);
        if (endlessLosePanel != null) endlessLosePanel.SetActive(false);
    }

    public void ShowEndlessResult(int result, int winStreak, int hp, int moneyReward, int stepCount)
    {
        HideAllResultPanels();
        if (EndlessManager.instance == null) return;

        switch (result)
        {
            case 0:
                ShowWinResultPanel(winStreak, stepCount, moneyReward);
                break;
            case 1:
                ShowDrawResultPanel(winStreak, hp);
                break;
            default:
                ShowLoseResultPanel(winStreak, hp);
                break;
        }
    }

    private void ShowWinResultPanel(int winStreak, int stepCount, int moneyReward)
    {
        if (endlessWinPanel != null) endlessWinPanel.SetActive(true);

        if (winCurrentStreakText != null) winCurrentStreakText.text = $"Win Streak: {winStreak}";
        if (winResultStepText != null) winResultStepText.text = $"Step: {stepCount}";
        if (winResultObtainText != null) winResultObtainText.text = $"You Got 9 - {stepCount} = {moneyReward}$";
        if (winResultMoneyText != null) winResultMoneyText.text = $"Total: {EndlessManager.instance.currentMoney}$";
    }

    private void ShowDrawResultPanel(int winStreak, int hp)
    {
        if (endlessDrawPanel != null) endlessDrawPanel.SetActive(true);

        if (drawCurrentStreakText != null) drawCurrentStreakText.text = $"Win Streak: {winStreak}";
        UpdateResultHpImages(drawResultHpImages, hp);
        if (drawResultMoneyText != null) drawResultMoneyText.text = $"Total: {EndlessManager.instance.currentMoney}$";
    }

    private void ShowLoseResultPanel(int winStreak, int hp)
    {
        if (endlessLosePanel != null) endlessLosePanel.SetActive(true);

        if (loseCurrentStreakText != null) loseCurrentStreakText.text = $"Win Streak: {winStreak}";
        UpdateResultHpImages(loseResultHpImages, hp);
        if (loseResultMoneyText != null) loseResultMoneyText.text = $"Total: {EndlessManager.instance.currentMoney}$";
    }

    private void UpdateResultHpImages(Image[] hpImages, int hp)
    {
        if (hpImages != null && hpImages.Length >= 3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (hpImages[i] != null) hpImages[i].gameObject.SetActive(i < hp);
            }
        }
    }

    public void ShowEndlessGameOver(int currentWin, int maxWin, bool breakRecord)
    {
        if (gameOverPanel == null || EndlessManager.instance == null || ShopManager.instance == null) return;

        HideAllResultPanels();
        gameOverPanel.SetActive(true);

        if (totalMoneyText != null) totalMoneyText.text = $"Total Money Earned: {EndlessManager.instance.totalMoney}$";
        if (totalPropsText != null) totalPropsText.text = $"Total Props Earned: {ShopManager.instance.totalProps}";
        if (finalWinStreakText != null) finalWinStreakText.text = $"Final Win Streak: {currentWin}";
        if (maxWinStreakGameOverText != null) maxWinStreakGameOverText.text = $"Best: {maxWin}";
        if (breakOrNotText != null) breakOrNotText.text = breakRecord ? "Good Job!" : "Try Again!";
    }
    #endregion

    #region 按钮绑定
    private void BindEndlessButtons()
    {
        if (replayBtn != null)
        {
            replayBtn.onClick.AddListener(() =>
            {
                if (EndlessManager.instance != null) EndlessManager.instance.StartEndlessGame();
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
            });
        }

        if (returnBtn != null)
        {
            returnBtn.onClick.AddListener(() =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            });
        }
    }
    #endregion
}