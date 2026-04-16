using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowMyFormula : MonoBehaviour
{
    [Header("容器引用")]
    public Transform contentRoot;

    [Header("滚动配置")]
    public ScrollRect scrollRect; // 关键：需要绑定 ScrollRect 组件

    [Header("显示设置")]
    public float cardScale = 1.0f;

    [Header("删卡功能配置")]
    [Tooltip("删除公式卡按钮 - 从Inspector拖入")]
    public Button deleteFormulaCardButton;

    private List<Button> activeDeletionButtons = new List<Button>();

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red; // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色

    private void OnEnable()
    {
        InitializeScrollRect();
        RefreshAllCards();

        // 根据当前的SelectionMode激活对应的button
        ActivateButtonsBasedOnMode();
    }
    private void OnDisable()
    {
        // 禁用删卡按钮
        if (deleteFormulaCardButton != null)
            deleteFormulaCardButton.gameObject.SetActive(false);

        // 清除激活的按钮监听
        foreach (var btn in activeDeletionButtons)
        {
            if (btn != null)
                btn.onClick.RemoveAllListeners();
        }
        activeDeletionButtons.Clear();
    }

    /// <summary>
    /// 根据当前选择模式激活对应的button
    /// </summary>
    private void ActivateButtonsBasedOnMode()
    {
        var mode = CardSelectionManager.Instance.GetCurrentMode();

        // 删卡模式：激活公式卡的删除按钮
        if (mode == CardSelectionManager.SelectionMode.RemoveCard)
        {
            ActivateFormulaCardDeletionButtons();
        }
        else
        {
            //不在删除模式时，禁用所有按钮
            DeactivateAllDeletionButtons();
        }
    }
    /// <summary>
    /// 禁用所有删除按钮（当不在删除模式时）
    /// </summary>
    private void DeactivateAllDeletionButtons()
    {
        foreach (var btn in activeDeletionButtons)
        {
            if (btn != null)
            {
                btn.gameObject.SetActive(false);
                btn.onClick.RemoveAllListeners();
            }
        }
        activeDeletionButtons.Clear();
    }
    /// <summary>
    /// 激活公式卡的删除按钮
    /// </summary>
    private void ActivateFormulaCardDeletionButtons()
    {
        var deck = CardManager.Instance.formulaCardDeck;

        // 清除之前的按钮监听
        foreach (var btn in activeDeletionButtons)
        {
            if (btn != null)
                btn.onClick.RemoveAllListeners();
        }
        activeDeletionButtons.Clear();

        int activatedCount = 0;

        // 获取所有公式卡UI组件
        FormulaCardUI[] formulaCardUIs = contentRoot.GetComponentsInChildren<FormulaCardUI>();

        foreach (var formulaUI in formulaCardUIs)
        {
            if (formulaUI == null) continue;


            // 查找或添加删除按钮
            Button deleteBtn = formulaUI.GetComponent<Button>();
            if (deleteBtn == null)
            {
                deleteBtn = formulaUI.GetComponentInChildren<Button>();
            }
            // 搜索Button，包括被禁用的脚本
            deleteBtn = FindButtonIncludingDisabled(formulaUI.gameObject);

            // 清除之前的监听
            deleteBtn.onClick.RemoveAllListeners();

            // 获取公式卡数据
            FormulaCardData formulaData = formulaUI.GetFormulaCardData();
            if (formulaData != null)
            {
                if (!deleteBtn.enabled)
                {
                    deleteBtn.enabled = true;
                }

                //激活Button所在的物体（关键！）
                Transform buttonTransform = deleteBtn.transform;
                while (buttonTransform != null)
                {
                    if (!buttonTransform.gameObject.activeSelf)
                    {
                        buttonTransform.gameObject.SetActive(true);
                        Debug.Log($"[ShowMyFormula] 激活了物体");
                    }

                    // 检查是否到达了FormulaUI或其他容器
                    if (buttonTransform.gameObject.GetComponent<FormulaCardUI>() != null)
                    {
                        //Debug.Log($"[ShowMyFormula] 已到达FormulaCardUI容器");
                        break;
                    }

                    buttonTransform = buttonTransform.parent;
                }

                //确保Button可交互
                if (!deleteBtn.interactable)
                {
                    deleteBtn.interactable = true;
                }

                //添加删除回调
                FormulaCardData cardData = formulaData;
                deleteBtn.onClick.AddListener(() => OnFormulaCardDeleteSelected(cardData));

                // 记录按钮
                activeDeletionButtons.Add(deleteBtn);

                activatedCount++;

                Debug.Log($"[ShowMyFormula] 成功激活公式卡删除按钮");
            }
            else
            {
                Debug.LogError($"[ShowMyFormula] 无法获取FormulaCardData：{formulaUI.gameObject.name}");
            }
        
        }
    }
    /// <summary>
    /// 关键方法：搜索Button，包括脚本被禁用的情况
    /// 这个方法比GetComponentInChildren更全面
    /// </summary>
    private Button FindButtonIncludingDisabled(GameObject parent)
    {
        if (parent == null) return null;

        // 方案1：先在自己身上找
        Button btn = parent.GetComponent<Button>();
        if (btn != null)
        {
            Debug.Log($"[ShowMyFormula] 在'{parent.name}'本身找到Button");
            return btn;
        }

        // 方案2：在激活的子物体中找
        btn = parent.GetComponentInChildren<Button>(false);  // false=只找激活的
        if (btn != null)
        {
            Debug.Log($"[ShowMyFormula] 在'{parent.name}'的激活子物体中找到Button");
            return btn;
        }

        // 方案3：在所有子物体中找，包括非激活的
        btn = parent.GetComponentInChildren<Button>(true);  // true=包括非激活的
        if (btn != null)
        {
            Debug.Log($"[ShowMyFormula] 在'{parent.name}'的非激活子物体中找到Button");
            return btn;
        }

        // 方案4：递归搜索所有后代（处理嵌套很深的情况）
        foreach (Transform child in parent.transform)
        {
            btn = FindButtonIncludingDisabled(child.gameObject);
            if (btn != null)
            {
                Debug.Log($"[ShowMyFormula] 在'{parent.name}'的深层子物体中找到Button");
                return btn;
            }
        }

        return null;
    }
    /// <summary>
    /// 处理公式卡删除选择
    /// </summary>
    private void OnFormulaCardDeleteSelected(FormulaCardData selectedFormula)
    {
        if (selectedFormula == null)
        {
            Debug.LogError("[ShowMyFormula] 选择的公式卡为空");
            return;
        }

        // 触发CardSelectionManager的回调
        CardSelectionManager.Instance.OnCardSelected(selectedFormula);

        // 执行删卡逻辑
        ExecuteFormulaCardDeletion(selectedFormula);
    }

    /// <summary>
    /// 执行公式卡删除逻辑
    /// </summary>
    private void ExecuteFormulaCardDeletion(FormulaCardData cardToDelete)
    {
        // 数量判定移到点击时判断
        if (!PlayerCardInventory.Instance.CanRemoveFormulaCard())
        {
            Debug.LogWarning($"[ShowMyFormula] 无法删除公式卡：最少需要保留 {PlayerCardInventory.Instance.minFormulaCardCount} 张");
            return;
        }

        // 使用约束检查来删除卡牌
        bool deleted = PlayerCardInventory.Instance.RemoveFormulaCard(cardToDelete);

        if (deleted)
        {
            // 通知ShopManager更新统计
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnCardDeleted(cardToDelete);
            }

            // 刷新显示
            RefreshAllCards();
        }
        else
        {
            Debug.LogWarning("[ShowMyFormula] 删除公式卡失败");
        }
    }
    /// <summary>
    /// 初始化滚轮支持
    /// </summary>
    void InitializeScrollRect()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<ScrollRect>();
        }
        if (scrollRect == null)
        {
            // 尝试在父物体的父物体找
            Transform parent = contentRoot.parent;
            if (parent != null)
            {
                scrollRect = parent.GetComponent<ScrollRect>();
            }
        }

        // 确保 contentRoot 有 LayoutElement
        LayoutElement le = contentRoot.GetComponent<LayoutElement>();
        if (le == null)
        {
            le = contentRoot.gameObject.AddComponent<LayoutElement>();
        }
        le.preferredWidth = -1;      // 不设置宽度约束
        le.preferredHeight = -1;     // 不设置高度约束（由内容决定）
        le.flexibleHeight = 1;       // 允许灵活高度

        // 找到或创建 ScrollRect
        if (scrollRect == null)
        {
            // 尝试在当前物体找
            scrollRect = GetComponent<ScrollRect>();
        }

        // 如果没找到，自动创建
        if (scrollRect == null && contentRoot != null)
        {
            Transform scrollParent = contentRoot.parent;
            if (scrollParent != null)
            {
                scrollRect = scrollParent.GetComponent<ScrollRect>();

                if (scrollRect == null)
                {
                    scrollRect = scrollParent.gameObject.AddComponent<ScrollRect>();
                    scrollRect.content = (RectTransform)contentRoot;
                    scrollRect.horizontal = false;      // 禁用水平滚动
                    scrollRect.vertical = true;         // 启用垂直滚动
                    scrollRect.movementType = ScrollRect.MovementType.Elastic;
                    scrollRect.elasticity = 0.1f;
                    scrollRect.scrollSensitivity = 15;   // 调整滚动灵敏度

                    Image image = scrollParent.GetComponent<Image>();
                    if (image == null)
                    {
                        image = scrollParent.gameObject.AddComponent<Image>();
                        image.color = new Color(1, 1, 1, 0.01f);
                    }

                    Debug.Log("[ShowMyCard] 自动创建了 ScrollRect 组件");
                }
            }
        }

        //确保 contentRoot 父物体的 RectTransform 配置正确
        RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        if (scrollRectTransform != null)
        {
            // 设置 ScrollRect 的大小约束
            LayoutElement scrollLE = scrollRect.GetComponent<LayoutElement>();
            if (scrollLE == null)
            {
                scrollLE = scrollRect.gameObject.AddComponent<LayoutElement>();
            }
            scrollLE.preferredHeight = 600;  //设置滚动区域的高度（可根据需要调整）
        }

    }
    public void RefreshAllCards()
    {
        // 1. 清理旧卡牌
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        GenerateFormulaCards();
        
        // 3. 强制重建布局
        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
        }

        if (scrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
        }
        // 刷新后重新激活按钮
        ActivateButtonsBasedOnMode();
    }
    /// <summary>
    /// 通用方法：设置文本内容和颜色
    /// </summary>
    void SetPartDisplay(Text textComp, NumberComponent component, int currentValue)
    {
        if (textComp == null || component == null) return;

        if (component.isDice)
        {
            // 骰子显示：~面数~ (黄色)
            textComp.text = $"~{component.diceSides}~";
            textComp.color = diceColor;
        }
        else if (component.isIncremental)
        {
            // 递增显示：{当前值} (绿色) - 这里使用了实例里的 currentValue
            textComp.text = $"{{{currentValue}}}";
            textComp.color = incrementalColor;
        }
        else
        {
            // 普通显示：数值 (黑色)
            textComp.text = currentValue.ToString();
            textComp.color = normalColor;
        }
    }

    void GenerateFormulaCards()
    {
        // 从PlayerCardInventory读取实时数据（与数字卡一致）
        var deck = PlayerCardInventory.Instance.GetAllFormulaCards();
        var prefab = UIManager.Instance.formulaCardPrefab;

        if (deck == null || prefab == null) return;

        foreach (var data in deck)
        {
            GameObject go = Instantiate(prefab, contentRoot);
            go.transform.localScale = UnityEngine.Vector3.one * cardScale;

            go.SetActive(true);

            var view = go.GetComponent<FormulaCardUI>();
            if (view != null) view.Bind(data);
        }
    }

    // 多多益善：显示公式卡并添加按钮
    public void ShowCardsForMoreMoreBetter()
    {
        // 清空旧卡片
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        // 生成卡片
        GenerateFormulaCards();

        // 获取所有卡片UI
        FormulaCardUI[] cards = contentRoot.GetComponentsInChildren<FormulaCardUI>();

        foreach (FormulaCardUI cardUI in cards)
        {
            // 获取卡片数据
            FormulaCardData data = cardUI.GetFormulaCardData();
            if (data == null) continue;

            // 找按钮（用你项目里现成的方法）
            Button btn = FindButtonIncludingDisabled(cardUI.gameObject);
            if (btn == null) continue;

            // 启用按钮
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            btn.enabled = true;

            // 清除旧事件
            btn.onClick.RemoveAllListeners();

            // 绑定点击：直接选择这张公式卡
            btn.onClick.AddListener(() =>
            {
                CardSelectionManager.Instance.OnCardSelected(data);
            });
        }

        // 刷新UI
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
    }
}
