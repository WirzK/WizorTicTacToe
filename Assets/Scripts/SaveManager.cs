// SaveManager.cs - 全局存档管理（跨场景常驻）
using UnityEngine;
using UnityEngine.SceneManagement; // 新增：场景管理命名空间

public class SaveManager : MonoBehaviour
{
    // 全局单例实例
    public static SaveManager instance;

    // 存档配置
    public const int SAVE_SLOT_COUNT = 3; // 固定3个存档槽
    private const string SAVE_KEY_PREFIX = "TicTacToe_Save_"; // 存档Key前缀

    // 当前选中的存档索引（0/1/2，全局生效）
    public int currentSaveIndex = 0;

    // 所有存档数据（全局可访问）
    public SaveData[] allSaveDatas = new SaveData[SAVE_SLOT_COUNT];

    private void Awake()
    {
        // 1. 单例+跨场景常驻核心逻辑
        if (instance == null)
        {
            instance = this;
            // 关键：标记该对象跨场景不销毁
            DontDestroyOnLoad(gameObject);
            // 初始化存档数据
            InitAllSaves();
            // 监听场景切换事件（可选，确保场景切换后数据同步）
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("SaveManager已初始化，跨场景常驻");
        }
        else
        {
            // 2. 防止重复创建：如果已有实例，销毁新的
            if (instance != this)
            {
                Destroy(gameObject);
                Debug.Log("检测到重复的SaveManager，已销毁");
            }
        }
    }

    // 场景加载完成后回调（可选，确保新场景中存档数据正常）
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"场景{scene.name}加载完成，存档数据保持不变");
        // 可选：场景切换后重新刷新存档数据（防止异常）
        // InitAllSaves();
    }

    /// <summary>
    /// 初始化所有存档（首次启动/场景切换时调用）
    /// </summary>
    public void InitAllSaves()
    {
        for (int i = 0; i < SAVE_SLOT_COUNT; i++)
        {
            allSaveDatas[i] = LoadSave(i); // 加载对应索引的存档
        }
    }

    /// <summary>
    /// 加载指定索引的存档
    /// </summary>
    /// <param name="saveIndex">存档槽索引（0/1/2）</param>
    public SaveData LoadSave(int saveIndex)
    {
        // 校验索引合法性
        if (saveIndex < 0 || saveIndex >= SAVE_SLOT_COUNT)
        {
            Debug.LogError($"存档索引{saveIndex}超出范围（仅支持0-{SAVE_SLOT_COUNT - 1}）");
            return new SaveData();
        }

        // 从PlayerPrefs读取Json字符串
        string saveKey = $"{SAVE_KEY_PREFIX}{saveIndex}";
        string saveJson = PlayerPrefs.GetString(saveKey, "");

        // 解析Json（首次加载时初始化空存档）
        SaveData saveData = new SaveData();
        if (!string.IsNullOrEmpty(saveJson))
        {
            saveData = JsonUtility.FromJson<SaveData>(saveJson);
        }
        else
        {
            saveData.Reset(); // 首次加载：初始化空存档
            SaveSave(saveIndex, saveData); // 保存初始状态
        }

        return saveData;
    }

    /// <summary>
    /// 保存指定索引的存档
    /// </summary>
    /// <param name="saveIndex">存档槽索引</param>
    /// <param name="saveData">要保存的存档数据</param>
    public void SaveSave(int saveIndex, SaveData saveData)
    {
        // 校验索引合法性
        if (saveIndex < 0 || saveIndex >= SAVE_SLOT_COUNT)
        {
            Debug.LogError($"存档索引{saveIndex}超出范围（仅支持0-{SAVE_SLOT_COUNT - 1}）");
            return;
        }

        // 转换为Json并保存到PlayerPrefs
        string saveKey = $"{SAVE_KEY_PREFIX}{saveIndex}";
        string saveJson = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(saveKey, saveJson);
        PlayerPrefs.Save(); // 立即保存到本地

        // 更新全局存档数据
        allSaveDatas[saveIndex] = saveData;
    }

    /// <summary>
    /// 删除指定索引的存档（重置为空存档）
    /// </summary>
    /// <param name="saveIndex">存档槽索引</param>
    public void DeleteSave(int saveIndex)
    {
        // 校验索引合法性
        if (saveIndex < 0 || saveIndex >= SAVE_SLOT_COUNT)
        {
            Debug.LogError($"存档索引{saveIndex}超出范围（仅支持0-{SAVE_SLOT_COUNT - 1}）");
            return;
        }

        // 重置存档数据
        SaveData emptySave = new SaveData();
        emptySave.Reset();
        SaveSave(saveIndex, emptySave);

        Debug.Log($"存档{saveIndex + 1}已删除，重置为空存档");
    }

    /// <summary>
    /// 更新当前选中存档的纪录（胜利时调用）
    /// </summary>
    /// <param name="newTime">新的通关时间</param>
    /// <param name="newStep">新的通关步数</param>
    public void UpdateCurrentSaveRecord(float newTime, int newStep)
    {
        SaveData currentSave = allSaveDatas[currentSaveIndex];

        // 标记存档为非空
        currentSave.isEmpty = false;

        // 检查并更新最快时间
        bool isTimeRecord = newTime < currentSave.bestTime;
        if (isTimeRecord)
        {
            currentSave.bestTime = newTime;
        }

        // 检查并更新最短步数
        bool isStepRecord = newStep < currentSave.bestStep;
        if (isStepRecord)
        {
            currentSave.bestStep = newStep;
        }

        // 保存更新后的存档
        SaveSave(currentSaveIndex, currentSave);
    }

    /// <summary>
    /// 获取当前选中的存档数据（全局快捷访问）
    /// </summary>
    public SaveData GetCurrentSave()
    {
        return allSaveDatas[currentSaveIndex];
    }

    // 销毁时移除场景监听（避免内存泄漏）
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}