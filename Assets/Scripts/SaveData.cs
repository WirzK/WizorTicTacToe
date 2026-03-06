using UnityEngine;

[System.Serializable]
public class SaveData
{
    // 存档基础信息
    public string saveName = "未命名存档"; 
    public bool isEmpty = true;            

    // 分难度记录（1=简单，2=中等，3=困难）
    public float diff1BestTime = float.MaxValue; 
    public int diff1BestStep = int.MaxValue;     
    public float diff2BestTime = float.MaxValue; 
    public int diff2BestStep = int.MaxValue;    
    public float diff3BestTime = float.MaxValue; 
    public int diff3BestStep = int.MaxValue;    

    /// <summary>
    /// 重置存档为初始状态（删除存档/新建空存档时调用）
    /// </summary>
    public void Reset()
    {
        saveName = "未命名存档";
        isEmpty = true;
        ResetDiffRecords(); // 重置各难度记录
    }

    public void ResetDiffRecords()
    {
        diff1BestTime = float.MaxValue;
        diff1BestStep = int.MaxValue;
        diff2BestTime = float.MaxValue;
        diff2BestStep = int.MaxValue;
        diff3BestTime = float.MaxValue;
        diff3BestStep = int.MaxValue;
    }

    public void UpdateDiffRecord(int difficulty, float newTime, int newStep)
    {
        switch (difficulty)
        {
            case 1:
                if (newTime < diff1BestTime) diff1BestTime = newTime;
                if (newStep < diff1BestStep) diff1BestStep = newStep;
                break;
            case 2:
                if (newTime < diff2BestTime) diff2BestTime = newTime;
                if (newStep < diff2BestStep) diff2BestStep = newStep;
                break;
            case 3:
                if (newTime < diff3BestTime) diff3BestTime = newTime;
                if (newStep < diff3BestStep) diff3BestStep = newStep;
                break;
            default:
                Debug.LogError($"无效难度值：{difficulty}（仅支持1-3）");
                break;
        }
    }
}