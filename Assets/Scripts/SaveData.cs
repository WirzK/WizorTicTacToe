using UnityEngine;

[System.Serializable]
public class SaveData
{
    public string saveName = "未命名存档";
    public bool isEmpty = true;

    public float diff1BestTime = float.MaxValue;
    public int diff1BestStep = int.MaxValue;
    public float diff2BestTime = float.MaxValue;
    public int diff2BestStep = int.MaxValue;
    public float diff3BestTime = float.MaxValue;
    public int diff3BestStep = int.MaxValue;

    public int diff4BestWinStreak = 0;

    public void Reset()
    {
        saveName = "未命名存档";
        isEmpty = true;
        ResetDiffRecords();
    }

    public void ResetDiffRecords()
    {
        diff1BestTime = float.MaxValue;
        diff1BestStep = int.MaxValue;
        diff2BestTime = float.MaxValue;
        diff2BestStep = int.MaxValue;
        diff3BestTime = float.MaxValue;
        diff3BestStep = int.MaxValue;

        // 重置无尽模式记录
        diff4BestWinStreak = 0;
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
                Debug.LogError($"无效难度值：{difficulty}（传统难度仅支持1-3）");
                break;
        }
    }

    public void UpdateEndlessRecord(int newWinStreak)
    {
        if (newWinStreak > diff4BestWinStreak)
        {
            diff4BestWinStreak = newWinStreak;
        }
    }

    public void UpdateRecordByDiff(int difficulty, float newTime = 0f, int newStep = 0, int newWinStreak = 0)
    {
        if (difficulty >= 1 && difficulty <= 3)
        {
            UpdateDiffRecord(difficulty, newTime, newStep);
        }
        else if (difficulty == 4)
        {
            UpdateEndlessRecord(newWinStreak);
        }
        else
        {
            Debug.LogError($"无效难度值：{difficulty}（仅支持1-4，4=无尽模式）");
        }
    }
}