using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;


[RequireComponent(typeof(Button))]
public class SaveSlotManager : MonoBehaviour
{
    [Header("基础配置")]
    public int slotIndex; // 当前存档位索引（0/1/2）
    public SaveManager saveManager;
    public UIMover uiMover;

    [Header("非空存档UI（存档存在时显示）")]
    public TextMeshProUGUI txtSaveName;

    [Header("简单难度")]
    public TextMeshProUGUI txtDiff1_Name;   
    public TextMeshProUGUI txtDiff1_Time;  
    public TextMeshProUGUI txtDiff1_Step;    

    [Header("中等难度")]
    public TextMeshProUGUI txtDiff2_Name;  
    public TextMeshProUGUI txtDiff2_Time;   
    public TextMeshProUGUI txtDiff2_Step;  

    [Header("困难难度）")]
    public TextMeshProUGUI txtDiff3_Name;   
    public TextMeshProUGUI txtDiff3_Time;   
    public TextMeshProUGUI txtDiff3_Step;   

    public Button btnDelete;           
    public Button btnStart;             
    public GameObject deleteConfirmPanel; 

    [Header("空存档UI")]
    public GameObject emptyObj1;        
    public GameObject emptyObj2;       
    public Button btnCreate;           
    public TMP_InputField inputSaveName;
    public Button btnConfirmCreate;    

    private SaveData currentSaveData;  
    private bool isDeletePanelShow = false;

    private void Awake()
    {

        InitButtonEvents();
        if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
    }

    private void Start()
    {
        saveManager = SaveManager.instance;
        RefreshSlotDisplay();
    }

    private void InitButtonEvents()
    {
        if (btnDelete != null) btnDelete.onClick.AddListener(ShowDeleteConfirm);
        if (btnStart != null) btnStart.onClick.AddListener(OnStartGame);

        if (btnCreate != null) btnCreate.onClick.AddListener(OnClickCreate);
        if (btnConfirmCreate != null) btnConfirmCreate.onClick.AddListener(OnConfirmCreate);
    }
    public void RefreshSlotDisplay()
    {
        currentSaveData = saveManager.LoadSave(slotIndex);

        if (currentSaveData.isEmpty)
        {
            SetNonEmptyUIActive(false);
            SetEmptyUIActive(true);
        }
        else
        {
            SetNonEmptyUIActive(true);
            SetEmptyUIActive(false);
            UpdateSaveRecordDisplay();
        }
    }

    private void SetNonEmptyUIActive(bool isActive)
    {
        if (txtSaveName != null) txtSaveName.gameObject.SetActive(isActive);

        if (txtDiff1_Name != null) txtDiff1_Name.gameObject.SetActive(isActive);
        if (txtDiff1_Time != null) txtDiff1_Time.gameObject.SetActive(isActive);
        if (txtDiff1_Step != null) txtDiff1_Step.gameObject.SetActive(isActive);

        if (txtDiff2_Name != null) txtDiff2_Name.gameObject.SetActive(isActive);
        if (txtDiff2_Time != null) txtDiff2_Time.gameObject.SetActive(isActive);
        if (txtDiff2_Step != null) txtDiff2_Step.gameObject.SetActive(isActive);

        if (txtDiff3_Name != null) txtDiff3_Name.gameObject.SetActive(isActive);
        if (txtDiff3_Time != null) txtDiff3_Time.gameObject.SetActive(isActive);
        if (txtDiff3_Step != null) txtDiff3_Step.gameObject.SetActive(isActive);

        if (btnDelete != null) btnDelete.gameObject.SetActive(isActive);
        if (btnStart != null) btnStart.gameObject.SetActive(isActive);
    }

    private void SetEmptyUIActive(bool isActive)
    {
        if (emptyObj1 != null) emptyObj1.SetActive(isActive);
        if (emptyObj2 != null) emptyObj2.SetActive(false); 
        if (inputSaveName != null) inputSaveName.gameObject.SetActive(false);
    }

    private void UpdateSaveRecordDisplay()
    {
        if (txtSaveName != null) txtSaveName.text = currentSaveData.saveName;

        UpdateSingleDiffDisplay(1, txtDiff1_Name, txtDiff1_Time, txtDiff1_Step);
        UpdateSingleDiffDisplay(2, txtDiff2_Name, txtDiff2_Time, txtDiff2_Step);
        UpdateSingleDiffDisplay(3, txtDiff3_Name, txtDiff3_Time, txtDiff3_Step);
    }

    private void UpdateSingleDiffDisplay(int diff, TextMeshProUGUI txtName, TextMeshProUGUI txtTime, TextMeshProUGUI txtStep)
    {
        if (txtName == null || txtTime == null || txtStep == null) return;

        float time = 0;
        int step = 0;
        string diffName = "";

        switch (diff)
        {
            case 1:
                time = currentSaveData.diff1BestTime;
                step = currentSaveData.diff1BestStep;
                diffName = "Easy"; 
                break;
            case 2:
                time = currentSaveData.diff2BestTime;
                step = currentSaveData.diff2BestStep;
                diffName = "Normal";
                break;
            case 3:
                time = currentSaveData.diff3BestTime;
                step = currentSaveData.diff3BestStep;
                diffName = "Hard";
                break;
        }

        txtName.text = diffName;

        if (time == float.MaxValue || step == int.MaxValue)
        {
            txtTime.text = "Best Time: None";
            txtStep.text = "Best Steps: None";
        }
        else
        {
            txtTime.text = $"Best Time: {time:F1}s";
            txtStep.text = $"Best Steps: {step}";
        }
    }

    #region 非空存档交互逻辑
    private void ShowDeleteConfirm()
    {
        if (currentSaveData.isEmpty)
        {
            Debug.LogWarning($"存档位{slotIndex}为空，无需删除");
            return;
        }

        saveManager.tempDeleteSlotIndex = slotIndex;

        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(true);
            isDeletePanelShow = true;
        }
    }

    private void OnStartGame()
    {
        saveManager.currentSaveIndex = slotIndex;
        uiMover.UIMove();
        StartCoroutine(LoadSceneWithDelay("SelectScene", 1f));
    }

    private IEnumerator LoadSceneWithDelay(string sceneName, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        SceneManager.LoadScene(sceneName);
    }
    #endregion

    #region 空存档交互逻辑
    private void OnClickCreate()
    {
        if (emptyObj1 != null) emptyObj1.SetActive(false);
        if (emptyObj2 != null) emptyObj2.SetActive(true);
        if (inputSaveName != null)
        {
            inputSaveName.gameObject.SetActive(true);
            inputSaveName.text = "";
        }
    }

    private void OnConfirmCreate()
    {
        string newName = inputSaveName.text.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("Save name cannot be empty!");
            return;
        }
        currentSaveData = new SaveData();
        currentSaveData.isEmpty = false;
        currentSaveData.saveName = newName;
        currentSaveData.ResetDiffRecords();

        saveManager.SaveSave(slotIndex, currentSaveData);
        if (emptyObj2 != null) emptyObj2.SetActive(false);
        if (inputSaveName != null) inputSaveName.gameObject.SetActive(false);
        RefreshSlotDisplay();
    }
    #endregion
}