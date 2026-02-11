using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店槽位UI组件
/// 处理数字卡和公式卡的显示、价格、购买按钮和锁定状态
/// </summary>
public class ShopSlotUI : MonoBehaviour
{
    [Header("通用UI")]
    public Text priceText;     // 显示价格
    public Button buyButton;   // 购买按钮

    [Header("锁定遮罩（分别设置）")]
    public GameObject numberCardLockPanel; // 数字卡锁定遮罩
    public GameObject formulaCardLockPanel; // 公式卡锁定遮罩

    [Header("数字卡专用")]
    public Transform cardContentRoot; // 数字卡牌内容的父节点
    public CanvasGroup numberCardCanvasGroup; // 用于控制数字卡透明度

    [Header("公式卡专用")]
    public Text formulaNameText; // 公式卡名称文本
    public CanvasGroup formulaCardCanvasGroup; // 用于控制公式卡透明度

    [Header("购买后效果")]
    public float soldAlpha = 0.5f; // 售出后的透明度

    private ShopItem<NumberCardInstance> numberCardItem;
    private ShopItem<FormulaCardData> formulaCardItem;
    private int slotIndex;
    private Image buyButtonImage; // 购买按钮的Image组件
    private bool isNumberCard; // 标记是数字卡还是公式卡

    void Awake()
    {
        // 获取购买按钮的Image组件
        if (buyButton != null)
        {
            buyButtonImage = buyButton.GetComponent<Image>();
        }
    }

    /// <summary>
    /// 绑定数字卡数据
    /// </summary>
    public void BindNumberCard(ShopItem<NumberCardInstance> item, int index)
    {
        numberCardItem = item;
        formulaCardItem = null;
        slotIndex = index;
        isNumberCard = true;

        // 隐藏公式卡锁定面板
        if (formulaCardLockPanel != null)
            formulaCardLockPanel.SetActive(false);

        // 检查是否为锁定槽位
        if (item.cardData == null)
        {
            SetNumberCardLockedState(true);
            return;
        }

        SetNumberCardLockedState(false);

        // 显示价格
        priceText.text = item.sold ? "已售出" : $"{item.price}";

        // 设置按钮状态
        buyButton.interactable = !item.sold;
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyNumberCard);

        // 如果已售出，应用透明度效果
        if (item.sold)
        {
            ApplySoldEffect(true);
        }
        else
        {
            ResetAlpha(true);
        }

        // 生成数字卡牌视觉
        if (cardContentRoot != null)
        {
            // 清理旧内容
            foreach (Transform child in cardContentRoot)
            {
                Destroy(child.gameObject);
            }

            // 从工厂获取对应布局的Prefab
            GameObject cardPrefab = UIManager.Instance.numberCardLibrary.GetPrefab(item.cardData.cardData.layoutType);

            if (cardPrefab != null)
            {
                GameObject cardGo = Instantiate(cardPrefab, cardContentRoot);
                cardGo.transform.localScale = Vector3.one;
                cardGo.transform.localPosition = Vector3.zero;

                // 绑定卡牌数据到视图
                var view = cardGo.GetComponent<NumberCardLayoutView>();
                if (view != null)
                {
                    view.Bind(item.cardData.cardData);
                }

                // 禁用卡牌交互（商店中不需要拖拽）
                var controller = cardGo.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.enabled = false;
                }
            }
            else
            {
                Debug.LogError($"找不到布局类型 {item.cardData.cardData.layoutType} 的Prefab！");
            }
        }
    }

    /// <summary>
    /// 绑定公式卡数据
    /// </summary>
    public void BindFormulaCard(ShopItem<FormulaCardData> item, int index)
    {
        formulaCardItem = item;
        numberCardItem = null;
        slotIndex = index;
        isNumberCard = false;

        // 隐藏数字卡锁定面板
        if (numberCardLockPanel != null)
            numberCardLockPanel.SetActive(false);

        // 检查是否为锁定槽位
        if (item.cardData == null)
        {
            SetFormulaCardLockedState(true);
            return;
        }

        SetFormulaCardLockedState(false);

        // 显示公式卡名称
        if (formulaNameText != null)
        {
            formulaNameText.text = item.cardData.Name;
        }

        // 显示价格
        priceText.text = item.sold ? "已售出" : $"{item.price}";

        // 设置按钮状态
        buyButton.interactable = !item.sold;
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyFormulaCard);

        // 如果已售出，应用透明度效果
        if (item.sold)
        {
            ApplySoldEffect(false);
        }
        else
        {
            ResetAlpha(false);
        }
    }

    /// <summary>
    /// 设置数字卡锁定状态
    /// </summary>
    private void SetNumberCardLockedState(bool locked)
    {
        if (numberCardLockPanel != null)
        {
            numberCardLockPanel.SetActive(locked);
        }

        if (locked)
        {
            // 锁定状态：禁用按钮，清空价格
            buyButton.interactable = false;
            priceText.text = "未解锁";
        }
    }

    /// <summary>
    /// 设置公式卡锁定状态
    /// </summary>
    private void SetFormulaCardLockedState(bool locked)
    {
        if (formulaCardLockPanel != null)
        {
            formulaCardLockPanel.SetActive(locked);
        }

        if (locked)
        {
            // 锁定状态：禁用按钮，清空价格
            buyButton.interactable = false;
            priceText.text = "未解锁";

            if (formulaNameText != null)
            {
                formulaNameText.text = "???";
            }
        }
    }

    /// <summary>
    /// 应用售出效果（降低透明度）
    /// </summary>
    private void ApplySoldEffect(bool isNumberCardSlot)
    {
        // 降低卡牌内容透明度
        if (isNumberCardSlot && numberCardCanvasGroup != null)
        {
            numberCardCanvasGroup.alpha = soldAlpha;
        }
        else if (!isNumberCardSlot && formulaCardCanvasGroup != null)
        {
            formulaCardCanvasGroup.alpha = soldAlpha;
        }

        // 降低按钮透明度
        if (buyButtonImage != null)
        {
            Color buttonColor = buyButtonImage.color;
            buttonColor.a = soldAlpha;
            buyButtonImage.color = buttonColor;
        }

        // 降低价格文本透明度
        if (priceText != null)
        {
            Color textColor = priceText.color;
            textColor.a = soldAlpha;
            priceText.color = textColor;
        }
    }

    /// <summary>
    /// 重置透明度为正常值
    /// </summary>
    private void ResetAlpha(bool isNumberCardSlot)
    {
        // 重置卡牌内容透明度
        if (isNumberCardSlot && numberCardCanvasGroup != null)
        {
            numberCardCanvasGroup.alpha = 1f;
        }
        else if (!isNumberCardSlot && formulaCardCanvasGroup != null)
        {
            formulaCardCanvasGroup.alpha = 1f;
        }

        // 重置按钮透明度
        if (buyButtonImage != null)
        {
            Color buttonColor = buyButtonImage.color;
            buttonColor.a = 1f;
            buyButtonImage.color = buttonColor;
        }

        // 重置价格文本透明度
        if (priceText != null)
        {
            Color textColor = priceText.color;
            textColor.a = 1f;
            priceText.color = textColor;
        }
    }

    /// <summary>
    /// 购买数字卡
    /// </summary>
    private void OnBuyNumberCard()
    {
        if (numberCardItem == null) return;

        // 传递 ShopItem<NumberCardInstance>，需要 ShopManager 提供对应重载
        if (ShopManager.Instance.TryBuyNumberCard(numberCardItem))
        {
            // 购买成功，更新UI
            buyButton.interactable = false;
            priceText.text = "已售出";

            // 应用售出效果
            ApplySoldEffect(true);

            Debug.Log($"成功购买数字卡，槽位：{slotIndex}");
        }
    }

    /// <summary>
    /// 购买公式卡
    /// </summary>
    private void OnBuyFormulaCard()
    {
        if (formulaCardItem == null) return;

        if (ShopManager.Instance.TryBuyFormulaCard(formulaCardItem))
        {
            // 购买成功，更新UI
            buyButton.interactable = false;
            priceText.text = "已售出";

            // 应用售出效果
            ApplySoldEffect(false);

            Debug.Log($"成功购买公式卡：{formulaCardItem.cardData.Name}");
        }
    }
}
