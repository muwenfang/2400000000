using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectionManager : MonoBehaviour
{
    public static CardSelectionManager Instance;

    public enum SelectionMode
    {
        None,
        CardCheat,
        RemoveCard,
        WishCoinSelect,
        MoreMoreBetter,
        DarkBoxSelect
    }

    private SelectionMode currentMode;
    private Action<object> selectionCallback;

    [Header("冷却配置")]
    [SerializeField] private float selectionCooldown = 0.1f;

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

    public void StartCardSelection(SelectionMode mode, Action<object> callback)
    {
        currentMode = mode;
        selectionCallback = callback;
        Debug.Log($"[CardSelectionManager] 开启卡牌选择模式：{mode}");
        if (mode == SelectionMode.RemoveCard)
        {
            UIManager.Instance.myNumberCardPanel.SetActive(true);
            UIManager.Instance.myFormulaCardPanel.SetActive(true);
            UIManager.Instance.myBlessPanel.SetActive(true);
        }
    }

    public void OnCardSelected(object selectedObject)
    {
        if (selectedObject == null)
        {
            Debug.LogWarning("[CardSelectionManager] 选择了空对象");
            return;
        }
        Debug.Log($"[CardSelectionManager] 选择了：{selectedObject}");
        selectionCallback?.Invoke(selectedObject);
        if (currentMode != SelectionMode.RemoveCard)
        {
            EndCardSelection();
        }
    }

    public void EndCardSelection()
    {
        Debug.Log("[CardSelectionManager] 关闭卡牌选择模式");
        currentMode = SelectionMode.None;
        selectionCallback = null;
    }

    public SelectionMode GetCurrentMode()
    {
        return currentMode;
    }

    public bool IsSelecting()
    {
        return currentMode != SelectionMode.None;
    }
}
