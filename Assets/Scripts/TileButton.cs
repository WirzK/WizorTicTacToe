using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;

// 每次reset就直接重开Scene，保证重置状态
public class TileButton : MonoBehaviour, IPointerClickHandler
{
    private int tileIndex;          // 当前按钮对应的棋盘索引（0-8）
    public Sprite playerImage; 
    public Sprite aiImage;
    public Sprite emptyImage;
    public GameObject tileImage;
    private Button selfButton;      // 按钮自身组件（用于控制是否可点击）
    private void Awake()
    {
        selfButton = GetComponent<Button>();     // 每个按钮的Button组件独立
    }
    private void Start()
    {
        tileIndex = transform.GetSiblingIndex(); // 每个按钮的索引独立

        playerImage = Resources.Load<Sprite>("Images/o");
        aiImage = Resources.Load<Sprite>("Images/x");
        emptyImage = Resources.Load<Sprite>("Images/empty");
    }

    // 鼠标点击触发（仅当前按钮响应）
    public void OnPointerClick(PointerEventData eventData)
    {
        // 只有满足“玩家回合、游戏激活、按钮可点击”才响应
        if (GameManager.instance.isPlayerTurn && GameManager.instance.isGameActive && selfButton.interactable)
        {
            
            GameManager.instance.PlayerMove(tileIndex);
            tileImage.SetActive(true);
            tileImage.GetComponent<Image>().sprite = playerImage; // 显示玩家棋子
            selfButton.interactable = false;
        }
    }

    //电脑落子时调用
    public void SetTileSpriteAndDisable()
    {
        selfButton.interactable = false;
        tileImage.SetActive(true);
        tileImage.GetComponent<Image>().sprite = aiImage;
    }
    public void ResetTile()
    {
        selfButton.interactable = true;
        tileImage.GetComponent<Image>().sprite = emptyImage;
        tileImage.SetActive(false);
    }

    // 可选：暴露索引供外部获取（比如GameManager需要确认按钮对应位置）
    public int GetTileIndex()
    {
        return tileIndex;
    }
}