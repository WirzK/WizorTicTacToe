// SaveData.cs - 存档数据模型（可序列化）
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // 存档基础信息
    public string saveName = "未命名存档"; // 存档名
    public bool isEmpty = true;            // 是否为空存档

    // 游戏纪录
    public float bestTime = float.MaxValue; // 最快通关时间（初始为极大值）
    public int bestStep = int.MaxValue;     // 最短通关步数（初始为极大值）

    // 重置存档为初始状态（用于删除存档/新建空存档）
    public void Reset()
    {
        saveName = "未命名存档";
        isEmpty = true;
        bestTime = float.MaxValue;
        bestStep = int.MaxValue;
    }
}