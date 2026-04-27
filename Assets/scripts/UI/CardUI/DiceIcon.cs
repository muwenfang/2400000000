using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 骰子图标管理器 - 根据骰子面数分类，返回对应的图标
/// </summary>
public class DiceIconManager : MonoBehaviour
{
    public static DiceIconManager Instance { get; private set; }

    [System.Serializable]
    public class DiceIconSet
    {
        public string typeName;
        public int maxSides;
        public Sprite icon;
    }

    [SerializeField]
    private List<DiceIconSet> diceIconSets = new List<DiceIconSet>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        InitializeDefaultIcons();
    }

    /// <summary>
    /// 初始化默认的骰子图标配置
    /// 在编辑器中可以手动配置，这里仅作为默认值
    /// </summary>
    private void InitializeDefaultIcons()
    {
        if (diceIconSets.Count == 0)
        {
            diceIconSets.Add(new DiceIconSet { typeName = "D4 (Type 1)", maxSides = 6 });
            diceIconSets.Add(new DiceIconSet { typeName = "D8 (Type 2)", maxSides = 10 });
            diceIconSets.Add(new DiceIconSet { typeName = "D12 (Type 3)", maxSides = 14 });
            diceIconSets.Add(new DiceIconSet { typeName = "D16 (Type 4)", maxSides = 18 });
            diceIconSets.Add(new DiceIconSet { typeName = "D20 (Type 5)", maxSides = 100 });
        }
    }

    /// <summary>
    /// 根据骰子面数获取对应的图标
    /// </summary>
    public Sprite GetDiceIcon(int diceSides)
    {
        foreach (var iconSet in diceIconSets)
        {
            if (diceSides <= iconSet.maxSides)
            {
                return iconSet.icon;
            }
        }

        Debug.LogWarning($"[DiceIconManager] 未找到面数为 {diceSides} 的骰子图标，返回 null");
        return null;
    }

    /// <summary>
    /// 根据骰子面数获取图标类型（1-5）
    /// </summary>
    public int GetDiceType(int diceSides)
    {
        for (int i = 0; i < diceIconSets.Count; i++)
        {
            if ( diceSides <= diceIconSets[i].maxSides)
            {
                return i + 1; // 返回1-5的类型号
            }
        }
        return -1;
    }

    /// <summary>
    /// 设置骰子图标集合（在编辑器中调用）
    /// </summary>
    public void SetDiceIconSets(List<DiceIconSet> iconSets)
    {
        diceIconSets = iconSets;
    }

    /// <summary>
    /// 获取所有骰子图标配置（用于编辑器显示）
    /// </summary>
    public List<DiceIconSet> GetAllDiceIconSets()
    {
        return diceIconSets;
    }
}
