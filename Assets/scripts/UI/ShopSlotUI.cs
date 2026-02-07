using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI 容器引用")]
    public Transform cardContainer; // 放置卡牌 UI 的地方
    public Text priceText;          // 显示价格
    public Button buyButton;        // 购买按钮

    private ShopItem<NumberCardData> currentNumberItem;
    private ShopItem<FormulaCardData> currentFormulaItem;

    // 初始化数字卡商品
    public void SetItem(ShopItem<NumberCardData> item)
    {
        currentNumberItem = item;
        currentFormulaItem = null;
        priceText.text = $"$ {item.price}";

        // 核心：从工厂获取现成的卡牌 UI 并塞入容器
        GameObject prefab = UIManager.Instance.numberCardLibrary.GetPrefab(item.cardData.layoutType);
        GameObject cardGo = Instantiate(prefab, cardContainer);

        // 绑定数据（重用你已有的 Bind 逻辑）
        cardGo.GetComponent<NumberCardLayoutView>().Bind(item.cardData);

        // 确保卡牌在容器内居中并填满
        RectTransform rt = cardGo.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClick);
    }

    // 初始化公式卡商品 (同理)
    public void SetItem(ShopItem<FormulaCardData> item)
    {
        currentNumberItem = null;
        currentFormulaItem = item;
        priceText.text = $"$ {item.price}";

        GameObject cardGo = Instantiate(UIManager.Instance.formulaCardPrefab, cardContainer);
        cardGo.GetComponent<FormulaCardUI>().Bind(item.cardData);

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClick);
    }

    public void OnBuyClick()
    {
        bool success = false;
        if (currentNumberItem != null)
            success = ShopManager.Instance.TryBuyNumberCard(currentNumberItem);
        else if (currentFormulaItem != null)
            success = ShopManager.Instance.TryBuyFormulaCard(currentFormulaItem);

        if (success)
        {
            buyButton.interactable = false;
            priceText.text = "已售出";
            // 也可以选择 Destroy(gameObject);
        }
    }
}
