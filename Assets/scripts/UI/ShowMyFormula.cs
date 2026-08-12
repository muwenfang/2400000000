using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
public class ShowMyFormula : MonoBehaviour, ISelectablePanel
{
    [Header("��������")]
    public Transform contentRoot;

    [Header("��������")]
    public ScrollRect scrollRect; // �ؼ�����Ҫ�� ScrollRect ���

    [Header("��ʾ����")]
    public float cardScale = 1.0f;

    [Header("ɾ����������")]
    [Tooltip("ɾ����ʽ����ť - ��Inspector����")]
    public Button deleteFormulaCardButton;

    public GameObject deletionCostPanel;
    public Text deletionCostText;
    public BigInteger deletionCost = 200;

    // 反向映射：GameObject → FormulaCardData，供 CardClickHandler 查找
    private Dictionary<GameObject, FormulaCardData> goToData = new Dictionary<GameObject, FormulaCardData>();

    // 脏标记：与 PlayerCardInventory.InventoryVersion 比较，版本一致则跳过重建
    private int lastKnownInventoryVersion = -1;

    // 统一卡牌点击处理器
    private CardClickHandler clickHandler;

    private void Awake()
    {
        // 初始化 CardClickHandler（如果尚未挂载）
        if (contentRoot != null)
        {
            clickHandler = contentRoot.GetComponent<CardClickHandler>();
            if (clickHandler == null)
                clickHandler = contentRoot.gameObject.AddComponent<CardClickHandler>();
            clickHandler.Initialize(this);
        }
    }

    private void OnEnable()
    {
        InitializeScrollRect();
        // 脏检查：库存版本未变化则跳过重建
        if (PlayerCardInventory.Instance != null &&
            PlayerCardInventory.Instance.InventoryVersion != lastKnownInventoryVersion)
        {
            RefreshAllCards();
        }
        else
        {
            // 仅更新删卡费用 UI（不重建卡片）
            if (ShopManager.Instance != null)
            {
                UpdateDeletionUI(ShopManager.Instance.CalculateDeletionCost());
            }
        }
        // 记录当前库存版本
        if (PlayerCardInventory.Instance != null)
            lastKnownInventoryVersion = PlayerCardInventory.Instance.InventoryVersion;

        // 订阅选择模式变更事件
        if (CardSelectionManager.Instance != null)
            CardSelectionManager.Instance.OnSelectionModeChanged += OnSelectionModeChanged;
    }

    private void OnDisable()
    {
        goToData.Clear();
        lastKnownInventoryVersion = -1;
        // 取消事件订阅
        if (CardSelectionManager.Instance != null)
            CardSelectionManager.Instance.OnSelectionModeChanged -= OnSelectionModeChanged;
    }

    /// <summary>
    /// ISelectablePanel 接口：选择模式变更时回调
    /// </summary>
    public void OnSelectionModeChanged(CardSelectionManager.SelectionMode newMode)
    {
        // 删卡模式进入时更新费用UI
        if (newMode == CardSelectionManager.SelectionMode.RemoveCard)
        {
            if (ShopManager.Instance != null)
            {
                deletionCost = ShopManager.Instance.CalculateDeletionCost();
                UpdateDeletionUI(deletionCost);
            }
        }
        // MoreMoreBetter 模式：已由 ShowCardsForMoreMoreBetter() 单独处理，此处无需额外逻辑
    }

    /// <summary>
    /// ISelectablePanel 接口：处理公式卡点击，由 CardClickHandler 统一分发
    /// </summary>
    public void HandleCardClick(GameObject clickedCardRoot, CardSelectionManager.SelectionMode mode)
    {
        FormulaCardUI cardUI = clickedCardRoot.GetComponent<FormulaCardUI>();
        if (cardUI == null) return;
        FormulaCardData data = cardUI.GetFormulaCardData();
        if (data == null) return;

        switch (mode)
        {
            case CardSelectionManager.SelectionMode.RemoveCard:
                OnFormulaCardDeleteSelected(data);
                break;
            case CardSelectionManager.SelectionMode.MoreMoreBetter:
                CardSelectionManager.Instance.OnCardSelected(data);
                break;
        }
    }

    /// <summary>
    /// 处理公式卡删除选择
    /// </summary>
    private void OnFormulaCardDeleteSelected(FormulaCardData selectedFormula)
    {
        // 把数量判定放在这里
        if (!PlayerCardInventory.Instance.CanRemoveFormulaCard())
        {
            Debug.LogWarning($"[ShowMyFormula] 无法删除公式卡：最少需要保留 {PlayerCardInventory.Instance.minFormulaCardCount} 张");
            return;
        }

        if (selectedFormula == null)
        {
            Debug.LogError("[ShowMyFormula] 选择的公式卡为空");
            return;
        }

        // 触发CardSelectionManager的回调
        CardSelectionManager.Instance.OnCardSelected(selectedFormula);
        deletionCost = ShopManager.Instance.CalculateDeletionCost();

        // 执行删卡逻辑
        ExecuteFormulaCardDeletion(selectedFormula);
    }

    /// <summary>
    /// ִ�й�ʽ��ɾ���߼�
    /// </summary>
    private void ExecuteFormulaCardDeletion(FormulaCardData cardToDelete)
    {
        // �����ж��Ƶ����ʱ�ж�
        if (!PlayerCardInventory.Instance.CanRemoveFormulaCard())
        {
            Debug.LogWarning($"[ShowMyFormula] �޷�ɾ����ʽ����������Ҫ���� {PlayerCardInventory.Instance.minFormulaCardCount} ��");
            return;
        }

        // --- �����̵��ܲ���ɾ ---
        if (ShopManager.Instance != null)
        {
            if (!ShopManager.Instance.OnFormulaCardDeleted(cardToDelete))
            {
                deletionCost = ShopManager.Instance.CalculateDeletionCost();
                return;
            }
        }
        // ʹ��Լ�������ɾ������
        bool deleted = PlayerCardInventory.Instance.RemoveFormulaCard(cardToDelete);

        if (deleted)
        {
            CardManager.Instance.SyncDeckFromInventory();
            // ˢ����ʾ
            RefreshAllCards();
        }
        else
        {
            Debug.LogWarning("[ShowMyFormula] ɾ����ʽ��ʧ��");
        }
    }
    /// <summary>
    /// ��ʼ������֧��
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
            // �����ڸ�����ĸ�������
            Transform parent = contentRoot.parent;
            if (parent != null)
            {
                scrollRect = parent.GetComponent<ScrollRect>();
            }
        }

        // ȷ�� contentRoot �� LayoutElement
        LayoutElement le = contentRoot.GetComponent<LayoutElement>();
        if (le == null)
        {
            le = contentRoot.gameObject.AddComponent<LayoutElement>();
        }
        le.preferredWidth = -1;      // �����ÿ��Լ��
        le.preferredHeight = -1;     // �����ø߶�Լ���������ݾ�����
        le.flexibleHeight = 1;       // �������߶�

        // �ҵ��򴴽� ScrollRect
        if (scrollRect == null)
        {
            // �����ڵ�ǰ������
            scrollRect = GetComponent<ScrollRect>();
        }

        // ���û�ҵ����Զ�����
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
                    scrollRect.horizontal = false;      // ����ˮƽ����
                    scrollRect.vertical = true;         // ���ô�ֱ����
                    scrollRect.movementType = ScrollRect.MovementType.Elastic;
                    scrollRect.elasticity = 0.1f;
                    scrollRect.scrollSensitivity = 15;   // �������������

                    Image image = scrollParent.GetComponent<Image>();
                    if (image == null)
                    {
                        image = scrollParent.gameObject.AddComponent<Image>();
                        image.color = new Color(1, 1, 1, 0.01f);
                    }

                    Debug.Log("[ShowMyCard] �Զ������� ScrollRect ���");
                }
            }
        }

        //ȷ�� contentRoot ������� RectTransform ������ȷ
        RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        if (scrollRectTransform != null)
        {
            // ���� ScrollRect �Ĵ�СԼ��
            LayoutElement scrollLE = scrollRect.GetComponent<LayoutElement>();
            if (scrollLE == null)
            {
                scrollLE = scrollRect.gameObject.AddComponent<LayoutElement>();
            }
            scrollLE.preferredHeight = 600;  //���ù�������ĸ߶ȣ��ɸ�����Ҫ������
        }

    }
    public void RefreshAllCards()
    {
        // 1. ����ɿ���
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        GenerateFormulaCards();
        
        // 3. ǿ���ؽ�����
        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
        }

        if (scrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
        }
        // 刷新后记录库存版本
        if (PlayerCardInventory.Instance != null)
            lastKnownInventoryVersion = PlayerCardInventory.Instance.InventoryVersion;

        if (ShopManager.Instance != null)
        {
            deletionCost = ShopManager.Instance.CalculateDeletionCost();
        }

        UpdateDeletionUI(deletionCost);
    }
    private void UpdateDeletionUI(BigInteger cost)
    {
        if(ShopManager.Instance.isDeletionMode == false)
        {
            deletionCostPanel.gameObject.SetActive(false);
        }
        else
        {
            deletionCostPanel.gameObject.SetActive(true);   
            deletionCostPanel.transform.SetAsLastSibling(); // ȷ������ǰ����ʾ

            deletionCostText.text = "һ          " + FormatBigNumber(cost).ToString();

            Debug.Log($"[ShowMyFormula] ����UI");
        }

    }
    public string FormatBigNumber(BigInteger number)
    {
        return NumberDisplayFormatter.Format(number);
    }
    // 颜色配置（公式卡展示面板用）
    public Color incrementalColor = Color.green;
    public Color diceColor = Color.red;
    public Color normalColor = Color.black;

    /// <summary>
    /// 通用方法：设置文本内容和颜色
    /// </summary>
    void SetPartDisplay(Text textComp, NumberComponent component, int currentValue)
    {
        if (textComp == null || component == null) return;

        if (component.isDice)
        {
            textComp.text = $"~{component.diceSides}~";
            textComp.color = diceColor;
        }
        else if (component.isIncremental)
        {
            textComp.text = $"{{{currentValue}}}";
            textComp.color = incrementalColor;
        }
        else
        {
            textComp.text = currentValue.ToString();
            textComp.color = normalColor;
        }
    }

    void GenerateFormulaCards()
    {
        // ��PlayerCardInventory��ȡʵʱ���ݣ������ֿ�һ�£�
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

    // 多多益善：显示公式卡供选择，点击由 CardClickHandler 统一处理
    public void ShowCardsForMoreMoreBetter()
    {
        // 清空旧卡片
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        // 清空反向映射
        goToData.Clear();

        // 生成卡片
        GenerateFormulaCards();

        // 刷新UI
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
    }
}
