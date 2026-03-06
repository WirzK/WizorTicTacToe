using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    public const int SAVE_SLOT_COUNT = 3; 
    private const string SAVE_KEY_PREFIX = "TicTacToe_Save_";

    public int currentSaveIndex = 0;

    public SaveData[] allSaveDatas = new SaveData[SAVE_SLOT_COUNT];

    public int tempDeleteSlotIndex = -1;

    public int diff = 1;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitAllSaves();
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("SaveManager已初始化，跨场景常驻");
        }
        else
        {
            if (instance != this)
            {
                Destroy(gameObject);
                Debug.Log("检测到重复的SaveManager，已销毁");
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"场景{scene.name}加载完成，存档数据保持不变");
    }
    public void InitAllSaves()
    {
        for (int i = 0; i < SAVE_SLOT_COUNT; i++)
        {
            allSaveDatas[i] = LoadSave(i);
        }
    }
    public SaveData LoadSave(int saveIndex)
    {
        if (saveIndex < 0 || saveIndex >= SAVE_SLOT_COUNT)
        {
            Debug.LogError($"存档索引{saveIndex}超出范围（仅支持0-{SAVE_SLOT_COUNT - 1}）");
            SaveData emptySave = new SaveData();
            emptySave.Reset();
            return emptySave;
        }

        string saveKey = $"{SAVE_KEY_PREFIX}{saveIndex}";
        string saveJson = PlayerPrefs.GetString(saveKey, "");

        SaveData saveData = new SaveData();
        if (!string.IsNullOrEmpty(saveJson))
        {
            saveData = JsonUtility.FromJson<SaveData>(saveJson);
            if (saveData.diff1BestTime == 0) saveData.ResetDiffRecords();
        }
        else
        {
            saveData.Reset();
            SaveSave(saveIndex, saveData);
        }

        return saveData;
    }

    public void SaveSave(int saveIndex, SaveData saveData)
    {
        if (saveIndex < 0 || saveIndex >= SAVE_SLOT_COUNT)
        {
            Debug.LogError($"存档索引{saveIndex}超出范围（仅支持0-{SAVE_SLOT_COUNT - 1}）");
            return;
        }

        string saveKey = $"{SAVE_KEY_PREFIX}{saveIndex}";
        string saveJson = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(saveKey, saveJson);
        PlayerPrefs.Save();

        allSaveDatas[saveIndex] = saveData;
    }

    public void DeleteSave(int saveIndex)
    {
        if (saveIndex < 0 || saveIndex >= SAVE_SLOT_COUNT)
        {
            Debug.LogError($"存档索引{saveIndex}超出范围（仅支持0-{SAVE_SLOT_COUNT - 1}）");
            return;
        }

        SaveData emptySave = new SaveData();
        emptySave.Reset();
        SaveSave(saveIndex, emptySave);

        Debug.Log($"存档{saveIndex + 1}已删除，重置为空存档");
    }
    //给按钮用的
    public void ConfirmDeleteShared()
    {
        if (tempDeleteSlotIndex >= 0 && tempDeleteSlotIndex < SAVE_SLOT_COUNT)
        {
            DeleteSave(tempDeleteSlotIndex);
            foreach (SaveSlotManager slot in FindObjectsOfType<SaveSlotManager>())
            {
                slot.RefreshSlotDisplay();
            }
            tempDeleteSlotIndex = -1;
        }
        foreach (SaveSlotManager slot in FindObjectsOfType<SaveSlotManager>())
        {
            if (slot.deleteConfirmPanel != null)
            {
                slot.deleteConfirmPanel.SetActive(false);
            }
        }
    }

    public void CancelDeleteShared()
    {
        tempDeleteSlotIndex = -1;
        foreach (SaveSlotManager slot in FindObjectsOfType<SaveSlotManager>())
        {
            if (slot.deleteConfirmPanel != null)
            {
                slot.deleteConfirmPanel.SetActive(false);
            }
        }
    }

    public void UpdateCurrentSaveDiffRecord(int difficulty, float newTime, int newStep)
    {
        SaveData currentSave = allSaveDatas[currentSaveIndex];
        currentSave.isEmpty = false; 
        currentSave.UpdateDiffRecord(difficulty, newTime, newStep); 
        SaveSave(currentSaveIndex, currentSave);
    }

    public SaveData GetCurrentSave()
    {
        return allSaveDatas[currentSaveIndex];
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}