using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店UI调试工具 - 帮助诊断显示问题
/// 使用方法：将此脚本挂到任何GameObject上，在运行时点击按钮调试
/// </summary>
public class ShopUIDebugger : MonoBehaviour
{
    [Header("调试目标")]
    public ShopNumberCardSlot numberCardSlot;
    public ShopFormulaCardSlot formulaCardSlot;
    public ShopItem<NumberCardInstance> testNumberCard;
    public ShopItem<FormulaCardData> testFormulaCard;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 600));

        GUILayout.Label("=== 商店UI调试工具 ===", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });

        GUILayout.Space(10);

        // 数字卡测试
        GUILayout.Label("【数字卡调试】");
        if (GUILayout.Button("1. 检查NumberCardUIFactory", GUILayout.Height(30)))
        {
            DebugNumberCardFactory();
        }

        if (GUILayout.Button("2. 检查ShopNumberCardSlot绑定", GUILayout.Height(30)))
        {
            DebugNumberCardSlot();
        }

        if (GUILayout.Button("3. 测试生成数字卡", GUILayout.Height(30)))
        {
            TestGenerateNumberCard();
        }

        GUILayout.Space(20);

        // 公式卡测试
        GUILayout.Label("【公式卡调试】");
        if (GUILayout.Button("4. 检查FormulaCardUI", GUILayout.Height(30)))
        {
            DebugFormulaCardUI();
        }

        if (GUILayout.Button("5. 检查ShopFormulaCardSlot绑定", GUILayout.Height(30)))
        {
            DebugFormulaCardSlot();
        }

        if (GUILayout.Button("6. 测试生成公式卡", GUILayout.Height(30)))
        {
            TestGenerateFormulaCard();
        }

        GUILayout.Space(20);

        // 全局测试
        GUILayout.Label("【全局调试】");
        if (GUILayout.Button("7. 刷新商店UI", GUILayout.Height(30)))
        {
            UIManager.Instance.RefreshShopUI();
            Debug.Log("商店UI已刷新");
        }

        if (GUILayout.Button("8. 打印所有配置", GUILayout.Height(30)))
        {
            PrintAllConfigs();
        }

        GUILayout.EndArea();
    }

    void DebugNumberCardFactory()
    {
        var factory = UIManager.Instance.numberCardLibrary;

        if (factory == null)
        {
            Debug.LogError(" UIManager.numberCardLibrary 为 null");
            return;
        }

        Debug.Log("NumberCardUIFactory 存在");

        // 检查是否包含所有布局类型的prefab
        var types = new[] {
            NumberCardLayoutType.Single,
            NumberCardLayoutType.Add_AB,
            NumberCardLayoutType.Multiply_AB,
            NumberCardLayoutType.Composite_AB
        };

        foreach (var type in types)
        {
            var prefab = factory.GetPrefab(type);
            if (prefab == null)
            {
                Debug.LogError($"找不到布局类型 {type} 的 Prefab");
            }
            else
            {
                Debug.Log($"{type} Prefab: {prefab.name}");

                // 检查prefab上的视图组件
                var view = prefab.GetComponent<NumberCardLayoutView>();
                if (view == null)
                {
                    Debug.LogWarning($"{type} Prefab 缺少 NumberCardLayoutView 组件");
                }
                else
                {
                    Debug.Log($"{type} Prefab 包含 {view.GetType().Name}");
                }
            }
        }
    }

    void DebugNumberCardSlot()
    {
        if (numberCardSlot == null)
        {
            Debug.LogError("numberCardSlot 未指定");
            return;
        }

        Debug.Log("=== ShopNumberCardSlot 配置检查 ===");

        var slotScript = numberCardSlot.GetComponent<ShopNumberCardSlot>();
        if (slotScript == null)
        {
            Debug.LogError("找不到 ShopNumberCardSlot 脚本");
            return;
        }

        // 使用反射检查字段
        var cardContentRoot = typeof(ShopNumberCardSlot).GetField("cardContentRoot");
        var priceText = typeof(ShopNumberCardSlot).GetField("priceText");
        var buyButton = typeof(ShopNumberCardSlot).GetField("buyButton");
        var numberCardLibrary = typeof(ShopNumberCardSlot).GetField("numberCardLibrary");

        CheckField(slotScript, cardContentRoot, "cardContentRoot");
        CheckField(slotScript, priceText, "priceText");
        CheckField(slotScript, buyButton, "buyButton");
        CheckField(slotScript, numberCardLibrary, "numberCardLibrary");
    }

    void DebugFormulaCardUI()
    {
        var formulaCardPrefab = UIManager.Instance.formulaCardPrefab;

        if (formulaCardPrefab == null)
        {
            Debug.LogError("UIManager.formulaCardPrefab 为 null");
            return;
        }

        Debug.Log($" formulaCardPrefab 存在: {formulaCardPrefab.name}");

        // 检查是否包含FormulaCardUI
        var formulaUI = formulaCardPrefab.GetComponent<FormulaCardUI>();
        if (formulaUI == null)
        {
            Debug.LogError(" formulaCardPrefab 缺少 FormulaCardUI 组件");

            // 尝试在子物体中查找
            formulaUI = formulaCardPrefab.GetComponentInChildren<FormulaCardUI>();
            if (formulaUI != null)
            {
                Debug.LogWarning($" FormulaCardUI 在子物体中: {formulaUI.gameObject.name}");
            }
        }
        else
        {
            Debug.Log($" formulaCardPrefab 包含 FormulaCardUI");

            // 检查FormulaCardUI的字段
            CheckField(formulaUI, typeof(FormulaCardUI).GetField("formulaArea"), "formulaArea");
            CheckField(formulaUI, typeof(FormulaCardUI).GetField("textPrefab"), "textPrefab");
            CheckField(formulaUI, typeof(FormulaCardUI).GetField("slotPrefab"), "slotPrefab");
        }
    }

    void DebugFormulaCardSlot()
    {
        if (formulaCardSlot == null)
        {
            Debug.LogError(" formulaCardSlot 未指定");
            return;
        }

        Debug.Log("=== ShopFormulaCardSlot 配置检查 ===");

        var slotScript = formulaCardSlot.GetComponent<ShopFormulaCardSlot>();
        if (slotScript == null)
        {
            Debug.LogError("找不到 ShopFormulaCardSlot 脚本");
            return;
        }

        // 检查字段
        CheckField(slotScript, typeof(ShopFormulaCardSlot).GetField("cardContentRoot"), "cardContentRoot");
        CheckField(slotScript, typeof(ShopFormulaCardSlot).GetField("priceText"), "priceText");
        CheckField(slotScript, typeof(ShopFormulaCardSlot).GetField("buyButton"), "buyButton");
        CheckField(slotScript, typeof(ShopFormulaCardSlot).GetField("formulaCardPrefab"), "formulaCardPrefab");
    }

    void TestGenerateNumberCard()
    {
        if (ShopManager.Instance == null || ShopManager.Instance.shopNumberCards == null)
        {
            Debug.LogError("ShopManager 未初始化");
            return;
        }

        if (ShopManager.Instance.shopNumberCards.Count == 0)
        {
            Debug.LogError("商店数字卡列表为空");
            return;
        }

        var item = ShopManager.Instance.shopNumberCards[0];
        if (item == null || item.cardData == null)
        {
            Debug.Log(" 槽位0已锁定（这是正常的）");
            return;
        }

        Debug.Log($"槽位0数字卡: {item.cardData.cardData.cardName}");
        Debug.Log($"  布局类型: {item.cardData.cardData.layoutType}");
        Debug.Log($"  价格: {item.price}");
    }

    void TestGenerateFormulaCard()
    {
        if (ShopManager.Instance == null || ShopManager.Instance.shopFormulaCards == null)
        {
            Debug.LogError("ShopManager 未初始化");
            return;
        }

        if (ShopManager.Instance.shopFormulaCards.Count == 0)
        {
            Debug.LogError(" 商店公式卡列表为空");
            return;
        }

        var item = ShopManager.Instance.shopFormulaCards[0];
        if (item == null || item.cardData == null)
        {
            Debug.Log("槽位0已锁定（这是正常的）");
            return;
        }

        Debug.Log($" 槽位0公式卡: {item.cardData.Name}");
        Debug.Log($" 公式: {item.cardData.Pattern}");
        Debug.Log($" 所需数量: {item.cardData.RequiredCount}");
        Debug.Log($" 价格: {item.price}");
    }

    void PrintAllConfigs()
    {
        Debug.Log("=== 所有配置信息 ===");

        Debug.Log("【UIManager】");
        Debug.Log($"  numberCardLibrary: {(UIManager.Instance.numberCardLibrary != null ? "✅" : "❌")}");
        Debug.Log($"  formulaCardPrefab: {(UIManager.Instance.formulaCardPrefab != null ? "✅" : "❌")}");
        Debug.Log($"  shopNumberCardPrefab: {(UIManager.Instance.shopNumberCardPrefab != null ? "✅" : "❌")}");
        Debug.Log($"  shopFormulaCardPrefab: {(UIManager.Instance.shopFormulaCardPrefab != null ? "✅" : "❌")}");
        Debug.Log($"  shopNumberArea: {(UIManager.Instance.shopNumberArea != null ? "✅" : "❌")}");
        Debug.Log($"  shopFormulaArea: {(UIManager.Instance.shopFormulaArea != null ? "✅" : "❌")}");

        Debug.Log("【ShopManager】");
        if (ShopManager.Instance != null)
        {
            Debug.Log($"  numberCardCount: {ShopManager.Instance.numberCardCount}");
            Debug.Log($"  formulaCardCount: {ShopManager.Instance.formulaCardCount}");
            Debug.Log($"  shopNumberCards: {ShopManager.Instance.shopNumberCards.Count}");
            Debug.Log($"  shopFormulaCards: {ShopManager.Instance.shopFormulaCards.Count}");
        }
    }

    void CheckField(object obj, System.Reflection.FieldInfo field, string fieldName)
    {
        if (field == null)
        {
            Debug.LogWarning($"字段 {fieldName} 不存在");
            return;
        }

        var value = field.GetValue(obj);
        if (value == null)
        {
            Debug.LogError($"{fieldName} 未设置 (null)");
        }
        else
        {
            Debug.Log($"{fieldName} 已设置: {value}");
        }
    }
}
