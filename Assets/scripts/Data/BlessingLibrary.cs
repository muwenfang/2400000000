using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 祝福卡库 - 存储所有祝福卡数据
/// </summary>
[CreateAssetMenu(fileName = "BlessingLibrary", menuName = "BlessingLibrary")]
public class BlessingLibrary : ScriptableObject
{
    [Header("所有祝福卡")]
    public List<BlessingData> allBlessings = new List<BlessingData>();

    /// <summary>
    /// 初始化默认祝福卡（Context Menu for testing）
    /// </summary>
    [ContextMenu("Initialize Default Blessings")]
    public void InitializeDefaultBlessings()
    {
        allBlessings.Clear();

        // 理财大师
        allBlessings.Add(CreateBlessing(
            id: 12,
            name: "理财大师",
            description: "每回合结束时额外获得已拥有点数1%的点数（向下取整）",
            type: BlessingData.BlessingType.FinancialMaster,
            basePrice: 3000,
            isStackable: true,
            effectValue: 0.01f
        ));

        //  多多益善
        allBlessings.Add(CreateBlessing(
            id: 16,
            name: "多多益善",
            description: "选择一张填空卡并将其复制",
            type: BlessingData.BlessingType.MoreMoreBetter,
            basePrice: 500,
            isStackable: true,
            effectValue: 1f
        ));

        //  辩证主义
        allBlessings.Add(CreateBlessing(
            id: 30,
            name: "辩证主义",
            description: "每回合倍率永久+1，获得24点，所有卡牌与祝福的价格+1%",
            type: BlessingData.BlessingType.DialecticalViewpoint,
            basePrice: 2400,
            isStackable: true,
            effectValue: 1f,
            bonusPoints: 24
        ));

        Debug.Log($" 成功初始化 {allBlessings.Count} 个祝福！");
    }

    /// <summary>
    /// 创建单个祝福（运行时创建）
    /// </summary>
    private BlessingData CreateBlessing(int id, string name, string description,
        BlessingData.BlessingType type, int basePrice, bool isStackable,
        float effectValue = 0f, int bonusPoints = 0)
    {
        BlessingData blessing = ScriptableObject.CreateInstance<BlessingData>();
        blessing.blessingId = id;
        blessing.blessingName = name;
        blessing.description = description;
        blessing.blessingType = type;
        blessing.basePrice = basePrice;
        blessing.isStackable = isStackable;
        blessing.effectValue = effectValue;     //点数加成
        blessing.bonusPoints = bonusPoints;     //倍率加成
        return blessing;
    }

    /// <summary>
    /// 根据ID获取祝福
    /// </summary>
    public BlessingData GetBlessingById(int id)
    {
        return allBlessings.Find(b => b.blessingId == id);
    }

    /// <summary>
    /// 根据类型获取祝福
    /// </summary>
    public BlessingData GetBlessingByType(BlessingData.BlessingType type)
    {
        return allBlessings.Find(b => b.blessingType == type);
    }

    /// <summary>
    /// 随机获取一个祝福
    /// </summary>
    public BlessingData GetRandomBlessing()
    {
        if (allBlessings.Count == 0)
        {
            Debug.LogError("祝福库为空！");
            return null;
        }

        int randomIndex = Random.Range(0, allBlessings.Count);
        return allBlessings[randomIndex];
    }

    /// <summary>
    /// 获取所有祝福
    /// </summary>
    public List<BlessingData> GetAllBlessings()
    {
        return new List<BlessingData>(allBlessings);
    }
}
