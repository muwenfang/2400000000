using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowMyNumberCard : MonoBehaviour
{
    [Header("容器引用")]
    public Transform contentRoot;

    [Header("滚动配置")]
    public ScrollRect scrollRect; // 关键：需要绑定 ScrollRect 组件

    private Dictionary<NumberCardInstance, GameObject> cardGameObjects = new Dictionary<NumberCardInstance, GameObject>();
    [Header("显示设置")]
    public float cardScale = 1.0f;

    [Header("颜色配置")]
    public Color incrementalColor = Color.green;   // 递增数字：绿色
    public Color diceColor = Color.red; // 骰子数字：红色
    public Color normalColor = Color.black;        // 普通数字：黑色

    public bool isDeleteMode = false; // 是否处于删除模式
    private void OnEnable()
    {
        InitializeScrollRect();
        RefreshAllCards();

        // 是否处于删除模式
        isDeleteMode = ShopManager.Instance.isDeletionMode;
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
        cardGameObjects.Clear();

        GenerateNumberCards();
        
        // 3. 强制重建布局
        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
        }

        if (scrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
        }
    }
    void GenerateNumberCards()
    {
        // 获取玩家库存中的所有实例（包含 currentA, currentB 的实时数据）
        var instances = PlayerCardInventory.Instance.GetAllNumberCards();

        if (instances == null) return;

        foreach (var instance in instances)
        {
            if (instance == null || instance.cardData == null) continue;

            //  获取 Prefab
            GameObject prefab = UIManager.Instance.numberCardLibrary.GetPrefab(instance.cardData.layoutType);
            if (prefab == null) continue;

            // 实例化
            GameObject go = Instantiate(prefab, contentRoot);

            go.transform.localScale = UnityEngine.Vector3.one * cardScale;
            go.SetActive(true); // 确保显示

            //禁用 PlayerController 脚本
            PlayerController playerController = go.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;  // 禁用拖动功能
                Debug.Log("[ShowMyCard] 禁用了卡牌的拖动功能");
            }

            //禁用 IBeginDragHandler, IDragHandler, IEndDragHandler
            IBeginDragHandler beginDragHandler = go.GetComponent<IBeginDragHandler>();
            IDragHandler dragHandler = go.GetComponent<IDragHandler>();
            IEndDragHandler endDragHandler = go.GetComponent<IEndDragHandler>();

            // 移除这些事件监听（通过禁用脚本）
            foreach (var component in go.GetComponents<MonoBehaviour>())
            {
                if (component is PlayerController)
                {
                    component.enabled = false;
                }
            }

            // 判断是哪种视图组件，分别赋值
            // 尝试获取单数字视图
            var singleView = go.GetComponent<SingleNumberView>();
            if (singleView != null)
            {
                // 设置数值 (使用实例中的 currentA)
                SetPartDisplay(singleView.valueText, instance.cardData.partA, instance.currentA);
                // 可以在这里设置价格文字隐藏或显示
                if (singleView.priceText != null) singleView.priceText.gameObject.SetActive(false);
            }
            // 尝试获取组合视图 (加法/乘法/乘方)
            else
            {
                var compositeView = go.GetComponent<CompositeNumberView>();
                if (compositeView != null)
                {
                    // 设置 Part A (使用 currentA)
                    SetPartDisplay(compositeView.aText, instance.cardData.partA, instance.currentA);

                    // 设置 Part B (使用 currentB)
                    SetPartDisplay(compositeView.bText, instance.cardData.partB, instance.currentB);
                }
            }
            cardGameObjects[instance] = go;
        }
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
}
