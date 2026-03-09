using UnityEngine;
using UnityEngine.EventSystems;
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

        playerImage = Resources.Load<Sprite>("Images/o") ?? playerImage;
        aiImage = Resources.Load<Sprite>("Images/x") ?? aiImage;
        emptyImage = Resources.Load<Sprite>("Images/empty") ?? emptyImage;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!selfButton.interactable) return;

        if (GameManager.instance != null)
        {
            if (GameManager.instance.isPlayerTurn && GameManager.instance.isGameActive)
            {
                GameManager.instance.PlayerMove(tileIndex);
                UpdateTileUI(1);
                selfButton.interactable = false;
            }
        }
        else if (EndlessManager.instance != null)
        {
            var endlessMgr = EndlessManager.instance;
            if (endlessMgr.isGameActive && endlessMgr.isPlayerTurn && endlessMgr.currentPhase == EndlessGamePhase.Battle)
            {
                endlessMgr.PlayerMove(tileIndex);
            }
        }
    }

    public void SetTileSpriteAndDisable()
    {
        UpdateTileUI(2);
        selfButton.interactable = false;
    }

    public void SetPlayerTileSpriteAndDisable()
    {
        UpdateTileUI(1);
        selfButton.interactable = false;
    }

    public void ResetTile()
    {
        selfButton.interactable = true;
        tileImage.GetComponent<Image>().sprite = emptyImage;
        tileImage.SetActive(false);
    }

    private void UpdateTileUI(int playerType)
    {
        if (tileImage == null) return;

        tileImage.SetActive(true);
        if (playerType == 1) // Íæ¼Ò
        {
            tileImage.GetComponent<Image>().sprite = playerImage;
        }
        else if (playerType == 2) // AI
        {
            tileImage.GetComponent<Image>().sprite = aiImage;
        }
    }

    public int GetTileIndex()
    {
        return tileIndex;
    }
}