using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌选择管理器 
/// 根据模式选择性激活卡牌
/// </summary>
public class CardSelectionManager : MonoBehaviour
{
    public static CardSelectionManager Instance;

    [Header("两个卡牌界面")]
    [Tooltip("数字卡界面 - 由 ShowMyCard 管理")]
    public RectTransform numberCardPanel;

    [Tooltip("公式卡界面 - 由 ShowMyCard 管理")]
    public RectTransform formulaCardPanel;

    [Tooltip("祝福界面 - 由 ShowMyCard 管理")]
    public RectTransform blessingPanel;

    public enum SelectionMode
    {
        CardCheat,      // 老千祝福：只能选择数字卡
        RemoveCard,     // 删除卡牌：可以选择数字卡和公式卡
        WishCoinSelect  // 许愿币祝福：只能选择祝福
    }

    private SelectionMode currentMode;
    private Action<object> selectionCallback;  // 选择完成后的回调
    private List<Button> activeSelectionButtons = new List<Button>();  // 当前激活的选择按钮

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 开启卡牌选择模式
    /// </summary>
    /// <param name="mode">选择模式</param>
    /// <param name="callback">选择完成后的回调</param>
    public void StartCardSelection(SelectionMode mode, Action<object> callback)
    {
        if (callback == null)
        {
            Debug.LogError("[CardSelectionManager] 回调为空");
            return;
        }

        currentMode = mode;
        selectionCallback = callback;
        activeSelectionButtons.Clear();

        Debug.Log($"[CardSelectionManager] 开启卡牌选择模式：{mode}");

        // 根据模式显示对应的界面并激活按钮
        switch (mode)
        {
            case SelectionMode.CardCheat:
                // 老千模式：只显示数字卡
                ShowNumberCardPanelOnly();
                break;

            case SelectionMode.RemoveCard:
                // 删卡模式：显示数字卡和公式卡
                ShowNumberAndFormulaCardPanels();
                break;

            case SelectionMode.WishCoinSelect:
                // 许愿模式：只显示祝福
                ShowBlessingPanelOnly();
                break;
        }

        // 激活按钮
        ActivateSelectionButtons();
    }

    private void ShowBlessingPanelOnly()
    {
        // 隐藏公式卡界面
        if (formulaCardPanel != null)
            formulaCardPanel.gameObject.SetActive(false);

        // 隐藏数字卡界面
        if (numberCardPanel != null)
        {
            numberCardPanel.gameObject.SetActive(false);
        }
        blessingPanel.gameObject.SetActive(true);
    }


    /// <summary>
    /// 老千模式：只显示数字卡界面
    /// </summary>
    private void ShowNumberCardPanelOnly()
    {
        Debug.Log("[CardSelectionManager] 老千模式：只显示数字卡界面");

        // 隐藏公式卡界面
        if (formulaCardPanel != null)
            formulaCardPanel.gameObject.SetActive(false);

        // 显示数字卡界面
        if (numberCardPanel != null)
        {
            numberCardPanel.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("[CardSelectionManager] numberCardPanel 未赋值");
        }

        // 通过 UIManager 显示卡牌界面
        if (UIManager.Instance != null && UIManager.Instance.myNumberCardPanel != null)
        {
            UIManager.Instance.ShowPanel(UIManager.Instance.myNumberCardPanel);
            Debug.Log("[CardSelectionManager] 通过 UIManager 显示卡牌界面");
        }
    }

    /// <summary>
    /// 删卡模式：显示数字卡和公式卡界面
    /// </summary>
    private void ShowNumberAndFormulaCardPanels()
    {
        Debug.Log("[CardSelectionManager] 删卡模式：显示数字卡和公式卡界面");

        // 显示数字卡和公式卡
        if (numberCardPanel != null)
            numberCardPanel.gameObject.SetActive(true);
        if (formulaCardPanel != null)
            formulaCardPanel.gameObject.SetActive(true);

        // 通过 UIManager 显示卡牌界面
        if (UIManager.Instance != null && UIManager.Instance.myNumberCardPanel != null)
        {
            UIManager.Instance.ShowPanel(UIManager.Instance.myNumberCardPanel);
            Debug.Log("[CardSelectionManager] 通过 UIManager 显示卡牌界面");
        }
    }

    /// <summary>
    /// 激活卡牌上的所有选择按钮
    /// 根据模式选择性地激活
    /// </summary>
    private void ActivateSelectionButtons()
    {
        List<RectTransform> panelsToCheck = new List<RectTransform>();

        // 根据模式决定检查哪些面板
        switch (currentMode)
        {
            case SelectionMode.CardCheat:
                // 老千模式：只检查数字卡面板
                if (numberCardPanel != null && numberCardPanel.gameObject.activeSelf)
                    panelsToCheck.Add(numberCardPanel);
                break;

            case SelectionMode.RemoveCard:
                // 删卡模式：检查数字卡和公式卡面板
                if (numberCardPanel != null && numberCardPanel.gameObject.activeSelf)
                    panelsToCheck.Add(numberCardPanel);
                if (formulaCardPanel != null && formulaCardPanel.gameObject.activeSelf)
                    panelsToCheck.Add(formulaCardPanel);
                break;        
                
            case SelectionMode.WishCoinSelect:
                // 选择祝福的模式
                if (blessingPanel != null && blessingPanel.gameObject.activeSelf)
                    panelsToCheck.Add(blessingPanel);        
                break;
        }

        if (panelsToCheck.Count == 0)
        {
            Debug.LogWarning("[CardSelectionManager] 没有可用的卡牌面板");
            return;
        }

        // 为每个面板中的卡牌激活按钮
        foreach (RectTransform panel in panelsToCheck)
        {
            ActivateButtonsInPanel(panel);
        }

        Debug.Log($"[CardSelectionManager] 成功激活 {activeSelectionButtons.Count} 个选择按钮");

        if (activeSelectionButtons.Count == 0)
        {
            Debug.LogError("[CardSelectionManager] 没有成功激活任何选择按钮！");
        }
    }

    /// <summary>
    /// 在指定面板中激活所有卡牌的选择按钮
    /// </summary>
    private void ActivateButtonsInPanel(RectTransform panel)
    {
        if (panel == null) return;

        // 获取面板中所有的 PlayerController（数字卡）
        PlayerController[] cardControllers = panel.GetComponentsInChildren<PlayerController>();

        foreach (PlayerController cardController in cardControllers)
        {
            if (cardController == null || cardController.BoundCard == null)
                continue;

            // 查找 selectedButton
            Button selectedBtn = cardController.GetComponent<Button>();
            if (selectedBtn == null)
            {
                Transform btnTransform = cardController.transform.Find("selectedButton");
                if (btnTransform != null)
                {
                    selectedBtn = btnTransform.GetComponent<Button>();
                }
            }

            if (selectedBtn != null)
            {
                NumberCardInstance cardInstance = cardController.BoundCard;
                selectedBtn.onClick.AddListener(() => OnCardSelected(cardInstance));
                activeSelectionButtons.Add(selectedBtn);
                Debug.Log($"[CardSelectionManager] 已激活数字卡选择按钮：{cardController.BoundCard.cardData.cardName}");
            }

            if (currentMode == SelectionMode.WishCoinSelect)
            {
            // 遍历祝福面板内的祝福项（挂有BlessingItemUI）
                foreach (Transform child in panel)
                {
                    var blessItem = child.GetComponent<BlessingUI>();
                    if (blessItem == null || blessItem.BoundBlessing == null) continue;
        
                    Button btn = child.GetComponent<Button>() ?? child.gameObject.AddComponent<Button>();
                    btn.onClick.AddListener(() => OnWishCoinBlessingSelected(blessItem.BoundBlessing));
                }
            }   
        
        }

        // 获取面板中所有的 FormulaCardUI（公式卡 prefab）
        FormulaCardUI[] formulaCardUIs = panel.GetComponentsInChildren<FormulaCardUI>();

        foreach (FormulaCardUI formulaUI in formulaCardUIs)
        {
            if (formulaUI == null) continue;

            // 获取或添加 Button 组件
            Button formulaBtn = formulaUI.GetComponent<Button>();
            if (formulaBtn == null)
            {
                formulaBtn = formulaUI.gameObject.AddComponent<Button>();
                Debug.Log($"[CardSelectionManager] 为公式卡 prefab 添加了 Button 组件");
            }

            if (formulaBtn != null)
            {
                // 从 FormulaCardUI 获取数据
                FormulaCardData formulaData = formulaUI.GetFormulaCardData();
                if (formulaData != null)
                {
                    formulaBtn.onClick.AddListener(() => OnFormulaCardSelected(formulaData));
                    activeSelectionButtons.Add(formulaBtn);
                    Debug.Log($"[CardSelectionManager] 已激活公式卡选择按钮：{formulaData.Name}");
                }
                else
                {
                    Debug.LogWarning($"[CardSelectionManager] 无法从 FormulaCardUI 获取数据");
                }
            }
        }
    }

    /// <summary>
    /// 处理数字卡被选中
    /// </summary>
    private void OnCardSelected(NumberCardInstance selectedCard)
    {
        if (selectedCard == null)
        {
            Debug.LogError("[CardSelectionManager] 选择的数字卡为空");
            return;
        }

        Debug.Log($"[CardSelectionManager] 用户选择数字卡：{selectedCard.cardData.cardName}");
        selectionCallback?.Invoke(selectedCard);
        EndCardSelection();
    }

    /// <summary>
    /// 处理公式卡被选中
    /// </summary>
    private void OnFormulaCardSelected(FormulaCardData selectedFormula)
    {
        if (selectedFormula == null)
        {
            Debug.LogError("[CardSelectionManager] 选择的公式卡为空");
            return;
        }

        Debug.Log($"[CardSelectionManager] 用户选择公式卡：{selectedFormula.Name}");
        selectionCallback?.Invoke(selectedFormula);
        EndCardSelection();
    }
    /// <summary>
    /// 处理祝福被选中
    /// </summary>
    private void OnWishCoinBlessingSelected(BlessingData selectedBlessing)
    {
        selectionCallback?.Invoke(selectedBlessing);
        EndCardSelection();
    }
    
    /// <summary>
    /// 关闭卡牌选择模式
    /// </summary>
    public void EndCardSelection()
    {
        // 移除所有选择按钮的监听
        foreach (Button btn in activeSelectionButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
            }
        }
        activeSelectionButtons.Clear();

        // 隐藏所有卡牌面板
        if (numberCardPanel != null)
            numberCardPanel.gameObject.SetActive(false);
        if (formulaCardPanel != null)
            formulaCardPanel.gameObject.SetActive(false);
        if (blessingPanel != null)
            blessingPanel.gameObject.SetActive(false);

        // 通过 UIManager 隐藏卡牌界面
        if (UIManager.Instance != null && UIManager.Instance.myNumberCardPanel != null)
        {
            UIManager.Instance.myNumberCardPanel.SetActive(false);
            Debug.Log("[CardSelectionManager] 通过 UIManager 隐藏卡牌界面");
        }

        Debug.Log("[CardSelectionManager] 关闭卡牌选择模式");

        currentMode = SelectionMode.CardCheat;
        selectionCallback = null;
    }

    /// <summary>
    /// 取消选择（用户主动取消）
    /// </summary>
    public void CancelSelection()
    {
        Debug.Log("[CardSelectionManager] 用户取消卡牌选择");
        EndCardSelection();
    }

    /// <summary>
    /// 获取当前选择模式
    /// </summary>
    public SelectionMode GetCurrentMode()
    {
        return currentMode;
    }

    /// <summary>
    /// 检查是否处于选择模式
    /// </summary>
    public bool IsSelecting()
    {
        return (numberCardPanel != null && numberCardPanel.gameObject.activeSelf) ||
               (formulaCardPanel != null && formulaCardPanel.gameObject.activeSelf);
    }
}