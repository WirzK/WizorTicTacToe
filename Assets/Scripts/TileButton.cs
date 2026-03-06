using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;

public class TileButton : MonoBehaviour, IPointerClickHandler
{
    private int tileIndex;       
    public Sprite playerImage; 
    public Sprite aiImage;
    public Sprite emptyImage;
    public GameObject tileImage;
    private Button selfButton;     
    private void Awake()
    {
        selfButton = GetComponent<Button>();   
    }
    private void Start()
    {
        tileIndex = transform.GetSiblingIndex();

        playerImage = Resources.Load<Sprite>("Images/o");
        aiImage = Resources.Load<Sprite>("Images/x");
        emptyImage = Resources.Load<Sprite>("Images/empty");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 只有满足“玩家回合、游戏激活、按钮可点击”才响应
        if (GameManager.instance.isPlayerTurn && GameManager.instance.isGameActive && selfButton.interactable)
        {
            
            GameManager.instance.PlayerMove(tileIndex);
            tileImage.SetActive(true);
            tileImage.GetComponent<Image>().sprite = playerImage; 
            selfButton.interactable = false;
        }
    }
    //电脑用的
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

    public int GetTileIndex()
    {
        return tileIndex;
    }
}