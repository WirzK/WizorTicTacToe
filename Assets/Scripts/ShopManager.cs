using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 道具类型枚举
public enum EndlessItemType
{
    DoubleWinCount,  // 本局胜利胜场双倍
    DrawAsWin,       // 平局视作胜利
    LoseNotEnd,      // 失败不结束（降难度）
    DoubleMoney,     // 资金翻倍（有上限）
    RestoreHp,       // 恢复1点生命值（可多次使用，生命值≤2时可用）
    None             // 无道具（初始占位）
}

// 道具数据结构
[System.Serializable]
public class EndlessItemData
{
    public EndlessItemType itemType;
    public string itemName;
    public int itemPrice;
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    // Inspector可配置项
    [Header("基础配置")]
    public int maxHpLimit = 3; // 生命值上限
    public int totalProps = 0;

    [Header("道具系统配置")]
    public List<EndlessItemData> allItems;
    public EndlessManager endlessManager;

    [Header("商店商品UI")]
    public Button[] shopItemButtons; // 商店商品按钮数组
    public TextMeshProUGUI[] shopItemPriceTexts; // 商品价格文本数组
    public TextMeshProUGUI shopDetailText;
    public Button shopUseButton;

    [Header("购买和道具详情")]
    public Button doubleWinCountBuyBtn;
    public Button drawAsWinBuyBtn;
    public Button loseNotEndBuyBtn;
    public Button doubleMoneyBuyBtn;
    public Button restoreHpBuyBtn;
    public int upperLimit;
    public EndlessItemType currentItem;

    [Header("道具数量文本")]
    public TextMeshProUGUI doubleWinCountCountText;
    public TextMeshProUGUI drawAsWinCountText;
    public TextMeshProUGUI loseNotEndCountText;
    public TextMeshProUGUI doubleMoneyCountText;
    public TextMeshProUGUI restoreHpCountText;

    public EndlessItemType usedItem = EndlessItemType.None; // 本局使用的一次性道具
    public bool isOneTimeItemUsed; // 一次性道具是否已使用
    private Dictionary<EndlessItemType, int> itemCounts;  // 每种道具的拥有数量
    public EndlessItemType selectedItemType = EndlessItemType.None; // 临时存储选中的道具类型

    private void Awake()
    {
        // 单例模式初始化
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // 绑定全局使用按钮默认监听
        if (shopUseButton != null)
        {
            shopUseButton.onClick.AddListener(() =>
            {
                if (selectedItemType != EndlessItemType.None)
                {
                    UseItemByType(selectedItemType);
                }
                else
                {
                    Debug.LogWarning("Please select an item before clicking use!");
                }
            });
        }
        currentItem = EndlessItemType.None;
        // 初始化道具数量字典
        itemCounts = new Dictionary<EndlessItemType, int>();
        InitItemCounts();

        // 初始化默认道具
        InitDefaultItems();

        // 绑定固定购买按钮事件
        BindFixedShopButtonEvents();
    }

    private void Start()
    {

        if (EndlessManager.instance != null)
        {
            endlessManager = EndlessManager.instance;
        }
        else
        {
            Debug.LogError("ShopManager 无法找到 EndlessManager 单例！");
        }

        // 初始化商店状态
        ResetShopManager();
        InitShopItemPrices();
        BindShopButtonEvents();
        shopUseButton.interactable = false;
    }

    private void InitDefaultItems()
    {
        if (allItems.Count > 0) return;

        allItems.Add(new EndlessItemData
        {
            itemType = EndlessItemType.DoubleWinCount,
            itemName = "双倍胜场",
            itemPrice = 4
        });
        allItems.Add(new EndlessItemData
        {
            itemType = EndlessItemType.DrawAsWin,
            itemName = "平局胜",
            itemPrice = 6
        });
        allItems.Add(new EndlessItemData
        {
            itemType = EndlessItemType.LoseNotEnd,
            itemName = "失败续命",
            itemPrice = 12
        });
        allItems.Add(new EndlessItemData
        {
            itemType = EndlessItemType.DoubleMoney,
            itemName = "资金翻倍",
            itemPrice = 15
        });
        allItems.Add(new EndlessItemData
        {
            itemType = EndlessItemType.RestoreHp,
            itemName = "恢复生命",
            itemPrice = 20
        });
    }

    private void InitItemCounts()
    {
        foreach (EndlessItemType type in Enum.GetValues(typeof(EndlessItemType)))
        {
            if (type != EndlessItemType.None)
            {
                itemCounts[type] = 0;
            }
        }
    }

    private void InitShopItemPrices()
    {
        if (shopItemPriceTexts == null || shopItemPriceTexts.Length < 5) return;

        EndlessItemType[] itemTypes = new EndlessItemType[]
        {
            EndlessItemType.DoubleWinCount,
            EndlessItemType.DrawAsWin,
            EndlessItemType.LoseNotEnd,
            EndlessItemType.DoubleMoney,
            EndlessItemType.RestoreHp
        };

        for (int i = 0; i < itemTypes.Length; i++)
        {
            var itemData = GetItemDataByType(itemTypes[i]);
            if (itemData != null && i < shopItemPriceTexts.Length)
            {
                shopItemPriceTexts[i].text = $"Price: {itemData.itemPrice}";
            }
        }
    }

    private void BindFixedShopButtonEvents()
    {
        if (doubleWinCountBuyBtn != null)
            doubleWinCountBuyBtn.onClick.AddListener(() => BuyItemByType(EndlessItemType.DoubleWinCount));
        if (drawAsWinBuyBtn != null)
            drawAsWinBuyBtn.onClick.AddListener(() => BuyItemByType(EndlessItemType.DrawAsWin));
        if (loseNotEndBuyBtn != null)
            loseNotEndBuyBtn.onClick.AddListener(() => BuyItemByType(EndlessItemType.LoseNotEnd));
        if (doubleMoneyBuyBtn != null)
            doubleMoneyBuyBtn.onClick.AddListener(() => BuyItemByType(EndlessItemType.DoubleMoney));
        if (restoreHpBuyBtn != null)
            restoreHpBuyBtn.onClick.AddListener(() => BuyItemByType(EndlessItemType.RestoreHp));
    }

    private void BindShopButtonEvents()
    {
        if (shopItemButtons == null || shopItemButtons.Length < 5) return;

        shopItemButtons[0].onClick.AddListener(() =>
        {
            selectedItemType = EndlessItemType.DoubleWinCount;
            SelectItem(selectedItemType);
            UpdateShopDetail(selectedItemType);
        });
        shopItemButtons[1].onClick.AddListener(() =>
        {
            selectedItemType = EndlessItemType.DrawAsWin;
            SelectItem(selectedItemType);
            UpdateShopDetail(selectedItemType);
        });
        shopItemButtons[2].onClick.AddListener(() =>
        {
            selectedItemType = EndlessItemType.LoseNotEnd;
            SelectItem(selectedItemType);
            UpdateShopDetail(selectedItemType);
        });
        shopItemButtons[3].onClick.AddListener(() =>
        {
            selectedItemType = EndlessItemType.DoubleMoney;
            SelectItem(selectedItemType);
            UpdateShopDetail(selectedItemType);
        });
        shopItemButtons[4].onClick.AddListener(() =>
        {
            selectedItemType = EndlessItemType.RestoreHp;
            SelectItem(selectedItemType);
            UpdateShopDetail(selectedItemType);
        });
    }

    // 重置商店
    public void ResetShopManager()
    {
        isOneTimeItemUsed = false;
        usedItem = EndlessItemType.None;
        currentItem = EndlessItemType.None;
        // 重置所有道具数量为0
        ResetItemCounts();
        // 更新所有购买按钮状态
        UpdateAllShopButtonStates();
    }

    // 重置所有道具数量为0并更新UI
    private void ResetItemCounts()
    {
        foreach (EndlessItemType type in Enum.GetValues(typeof(EndlessItemType)))
        {
            if (type != EndlessItemType.None)
            {
                itemCounts[type] = 0;
                UpdateItemCountDisplay(type, 0);
            }
        }
    }

    public void UpdateShopDetail(EndlessItemType itemType)
    {
        if (shopDetailText == null) return;

        EndlessItemData itemData = GetItemDataByType(itemType);
        UpdateShopDetail(itemData);
    }

    public void UpdateShopDetail(EndlessItemData itemData)
    {
        if (shopDetailText == null || itemData == null) return;

        switch (itemData.itemType)
        {
            case EndlessItemType.DoubleWinCount:
                shopDetailText.text = "Double Win: Get 2 win streak after next win (1-use, 1/item per round, 1 round effect)";
                break;
            case EndlessItemType.DrawAsWin:
                shopDetailText.text = "Draw As Win: Count draw as win for next round (1-use, 1/item per round, 1 round effect)";
                break;
            case EndlessItemType.LoseNotEnd:
                shopDetailText.text = "Lose Not End: Next loss no chance deduction (1-use, 1/item per round, 1 round effect)";
                break;
            case EndlessItemType.DoubleMoney:
                shopDetailText.text = "Double Money: Double current money instantly (max +$20 per use)";
                break;
            case EndlessItemType.RestoreHp:
                shopDetailText.text = "One More Chance: Get 1 Chance. Usable only when Chances ≤ 2";
                break;
            default:
                shopDetailText.text = "Please select an item";
                break;
        }
    }

    // 更新道具数量显示文本
    public void UpdateItemCountDisplay(EndlessItemType itemType, int count)
    {
        switch (itemType)
        {
            case EndlessItemType.DoubleWinCount:
                if (doubleWinCountCountText != null) doubleWinCountCountText.text = $"Count: {count}";
                break;
            case EndlessItemType.RestoreHp:
                if (restoreHpCountText != null) restoreHpCountText.text = $"Count: {count}";
                break;
            case EndlessItemType.DoubleMoney:
                if (doubleMoneyCountText != null) doubleMoneyCountText.text = $"Count: {count}";
                break;
            case EndlessItemType.LoseNotEnd:
                if (loseNotEndCountText != null) loseNotEndCountText.text = $"Count: {count}";
                break;
            case EndlessItemType.DrawAsWin:
                if (drawAsWinCountText != null) drawAsWinCountText.text = $"Count: {count}";
                break;
        }
    }

    // 根据道具类型获取道具数据
    public EndlessItemData GetItemDataByType(EndlessItemType itemType)
    {
        return allItems.Find(item => item.itemType == itemType);
    }

    public int GetItemCount(EndlessItemType itemType)
    {
        if (itemCounts.ContainsKey(itemType))
        {
            return itemCounts[itemType];
        }
        return 0;
    }

    // 购买指定类型的道具
    public void BuyItemByType(EndlessItemType itemType)
    {
        if (endlessManager == null)
        {
            Debug.LogError("ShopManager: EndlessManager 未初始化！");
            return;
        }

        EndlessItemData item = GetItemDataByType(itemType);
        if (item == null)
        {
            Debug.LogWarning($"ShopManager: 未找到{itemType}对应的道具数据");
            return;
        }

        if (endlessManager.currentMoney < item.itemPrice)
        {
            Debug.Log($"无法购买{item.itemName}：资金不足（当前{endlessManager.currentMoney}，需要{item.itemPrice}）");
            return;
        }


        endlessManager.currentMoney -= item.itemPrice;
        itemCounts[itemType] += 1;
        totalProps++;

        // 更新UI
        if (EndlessUIManager.instance != null)
        {
            EndlessUIManager.instance.UpdateEndlessResources(
                endlessManager.currentHp,
                endlessManager.currentMoney,
                EndlessManager.instance.winStreak,
                EndlessManager.instance.maxWinStreak
            );
            UpdateItemCountDisplay(itemType, itemCounts[itemType]);
        }

        UpdateSingleItemButtonStates(itemType);

        if (currentItem == itemType)
            shopUseButton.interactable = GetItemCount(itemType) > 0;
        Debug.Log($"购买道具：{item.itemName}，剩余资金：{endlessManager.currentMoney}，当前拥有数量：{itemCounts[itemType]}");
    }

    public void UseItemByType(EndlessItemType itemType)
    {
        if (endlessManager == null)
        {
            Debug.LogError("ShopManager: EndlessManager 未初始化！");
            return;
        }

        if (GetItemCount(itemType) <= 0)
        {
            Debug.Log($"无法使用{itemType}：未拥有该道具");
            return;
        }

        EndlessItemData item = GetItemDataByType(itemType);
        if (item == null)
        {
            Debug.LogWarning($"ShopManager: 未找到{itemType}对应的道具数据");
            return;
        }

        switch (itemType)
        {
            case EndlessItemType.DoubleWinCount:
            case EndlessItemType.DrawAsWin:
            case EndlessItemType.LoseNotEnd:
                if (isOneTimeItemUsed)
                {
                    shopDetailText.text = "One-time use item already used this round - only one is allowed per round";
                    return;
                }
                isOneTimeItemUsed = true;
                usedItem = itemType;
                itemCounts[itemType] -= 1;
                if (EndlessUIManager.instance != null)
                {
                    EndlessUIManager.instance.ShowUsedItemIcon(itemType);
                }
                break;

            // 资金翻倍
            case EndlessItemType.DoubleMoney:
                itemCounts[itemType] -= 1;
                if (EndlessUIManager.instance != null)
                {
                    EndlessUIManager.instance.StartCoroutine(DoubleMoneyAnimation());
                    endlessManager.totalMoney += Math.Min(endlessManager.currentMoney, upperLimit);
                    endlessManager.currentMoney = Math.Min(endlessManager.currentMoney * 2, endlessManager.currentMoney + upperLimit);
                }
                UpdateAllShopButtonStates();
                break;

            // 可重复使用，给个机会
            case EndlessItemType.RestoreHp:
                if (endlessManager.currentHp >= maxHpLimit)
                {
                    shopDetailText.text = "You already have enough chances.";
                    return;
                }
                itemCounts[itemType] -= 1;
                endlessManager.currentHp = Mathf.Min(endlessManager.currentHp + 1, maxHpLimit);

                if (EndlessUIManager.instance != null)
                {
                    EndlessUIManager.instance.UpdateEndlessResources(
                        endlessManager.currentHp,
                        endlessManager.currentMoney,
                        EndlessManager.instance.winStreak,
                        EndlessManager.instance.maxWinStreak
                    );
                }
                break;

            default:
                Debug.Log($"不支持的道具类型：{itemType}");
                return;
        }

        shopUseButton.interactable = GetItemCount(itemType) > 0;
        UpdateItemCountDisplay(itemType, itemCounts[itemType]);
        Debug.Log($"使用道具：{item.itemName}，剩余数量：{itemCounts[itemType]}");
    }

    // 选中道具（用于UI预览详情，同时为全局use按钮绑定对应方法）
    public void SelectItem(EndlessItemType itemType)
    {
        EndlessItemData selectedItem = GetItemDataByType(itemType);
        if (selectedItem == null)
        {
            Debug.LogWarning($"未找到{itemType}对应的道具数据");
            return;
        }

        UpdateShopDetail(selectedItem);
        // 选中了新道具，更新全局use按钮的绑定
        BindUseButtonToItem(itemType);

        Debug.Log($"选中道具：{selectedItem.itemName}，全局use按钮已绑定该道具的使用方法");
    }

    // 为全局使用按钮绑定指定道具的使用逻辑
    public void BindUseButtonToItem(EndlessItemType itemType)
    {
        if (shopUseButton == null) return;
        currentItem = itemType;
        shopUseButton.onClick.RemoveAllListeners();

        shopUseButton.onClick.AddListener(() =>
        {
            UseItemByType(itemType);
        });

        shopUseButton.interactable = GetItemCount(itemType) > 0;
    }

    // 更新单个道具的购买按钮状态（根据资金是否足够）
    private void UpdateSingleItemButtonStates(EndlessItemType itemType)
    {
        if (endlessManager == null) return;

        doubleWinCountBuyBtn.interactable = endlessManager.currentMoney >= GetItemDataByType(EndlessItemType.DoubleWinCount).itemPrice;
        drawAsWinBuyBtn.interactable = endlessManager.currentMoney >= GetItemDataByType(EndlessItemType.DrawAsWin).itemPrice;
        loseNotEndBuyBtn.interactable = endlessManager.currentMoney >= GetItemDataByType(EndlessItemType.LoseNotEnd).itemPrice;
        doubleMoneyBuyBtn.interactable = endlessManager.currentMoney >= GetItemDataByType(EndlessItemType.DoubleMoney).itemPrice;
        restoreHpBuyBtn.interactable = endlessManager.currentMoney >= GetItemDataByType(EndlessItemType.RestoreHp).itemPrice;
    }

    // 更新所有道具的购买按钮状态
    public void UpdateAllShopButtonStates()
    {
        foreach (EndlessItemType type in Enum.GetValues(typeof(EndlessItemType)))
        {
            if (type != EndlessItemType.None)
            {
                UpdateSingleItemButtonStates(type);
            }
        }
    }

    // 增加资金
    public void AddMoney(int amount)
    {
        if (endlessManager == null)
        {
            Debug.LogError("ShopManager: EndlessManager 未初始化！");
            return;
        }

        endlessManager.currentMoney = endlessManager.currentMoney + amount;

        if (EndlessUIManager.instance != null)
        {
            EndlessUIManager.instance.UpdateEndlessResources(
                endlessManager.currentHp,
                endlessManager.currentMoney,
                EndlessManager.instance.winStreak,
                EndlessManager.instance.maxWinStreak
            );
        }

        // 资金变化后更新所有购买按钮状态
        UpdateAllShopButtonStates();
    }

    // 结算阶段应用道具效果，返回修正后的胜负结果
    public (bool finalWin, bool finalDraw, bool isRevive) ApplyItemEffect(bool originalWin, bool originalDraw)
    {
        bool finalWin = originalWin;
        bool finalDraw = originalDraw;
        bool isRevive = false;

        if (!isOneTimeItemUsed) return (finalWin, finalDraw, isRevive);

        // 根据使用的一次性道具修正胜负
        switch (usedItem)
        {
            case EndlessItemType.DoubleWinCount:
                // 仅胜利时生效（胜场双倍在EndlessManager处理）
                break;
            case EndlessItemType.DrawAsWin:
                // 平局视作胜利
                if (originalDraw)
                {
                    finalWin = true;
                    finalDraw = false;
                }
                break;
            case EndlessItemType.LoseNotEnd:
                // 失败不结束（续命）
                if (!originalWin && !originalDraw)
                {
                    isRevive = true;
                }
                break;
            // 资金翻倍和恢复生命无结算阶段效果
            case EndlessItemType.DoubleMoney:
            case EndlessItemType.RestoreHp:
                break;
        }

        return (finalWin, finalDraw, isRevive);
    }

    // 资金翻倍动画
    public IEnumerator DoubleMoneyAnimation()
    {
        if (EndlessUIManager.instance.moneyText == null) yield break;

        int currentMoney = endlessManager.currentMoney;

        int targetMoney = currentMoney + Math.Min(currentMoney, upperLimit);
        int tempMoney = currentMoney;

        if (tempMoney >= targetMoney)
        {
            EndlessUIManager.instance.moneyText.text = $"{targetMoney}";
            yield break;
        }

        float addInterval = 0.05f;
        while (tempMoney < targetMoney)
        {
            tempMoney += 1;
            if (tempMoney > targetMoney) tempMoney = targetMoney;

            EndlessUIManager.instance.moneyText.text = $"{tempMoney}";
            yield return new WaitForSeconds(addInterval);
        }
    }
}