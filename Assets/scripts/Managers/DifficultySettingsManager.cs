using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DifficultySettingControlGroup
{
    public Text valueText;
    public Button decreaseButton;
    public Button increaseButton;
}

/// <summary>
/// Settings panel difficulty manager for five price multipliers.
/// Uses inspector bindings first and falls back to auto-binding.
/// </summary>
public class DifficultySettingsManager : MonoBehaviour
{
    public static DifficultySettingsManager Instance { get; private set; }

    [Header("难度开关")]
    public Toggle oneMoreCardToggle;
    public Toggle higherCostToggle;
    // 使用 static 确保多局游戏间持久化（退出游戏后自动恢复默认 false）
    public static bool oneMoreCard = false;
    public static bool higherCost = false;

    [Header("初始数字卡+1")]
    public NumberCardData extraStartCard;

    [Header("难度等级显示")]
    public Text difficultyLevelText;
    private int difficultyLevel = 0;

    private const float MinMultiplier = 1f;
    private const float MaxMultiplier = 10f;
    private const float Step = 0.5f;
    private const float RowTolerance = 25f;

    [Header("Optional manual bindings")]
    public DifficultySettingControlGroup numberCardPriceControls = new DifficultySettingControlGroup();
    public DifficultySettingControlGroup formulaCardPriceControls = new DifficultySettingControlGroup();
    public DifficultySettingControlGroup blessingPriceControls = new DifficultySettingControlGroup();
    public DifficultySettingControlGroup deletionPriceControls = new DifficultySettingControlGroup();
    public DifficultySettingControlGroup refreshPriceControls = new DifficultySettingControlGroup();

    private bool initialized;

    public void Initialize()
    {
        TryAutoBindIfNeeded();

        if (!initialized)
        {
            BindControlGroup(numberCardPriceControls, DifficultySettingType.NumberCardPrice);
            BindControlGroup(formulaCardPriceControls, DifficultySettingType.FormulaCardPrice);
            BindControlGroup(blessingPriceControls, DifficultySettingType.BlessingPrice);
            BindControlGroup(deletionPriceControls, DifficultySettingType.CardDeletionPrice);
            BindControlGroup(refreshPriceControls, DifficultySettingType.ShopRefreshPrice);
            initialized = true;
        }

        BindToggles();
        RefreshAllDisplays();
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnEnable()
    {
        if (initialized)
        {
            RefreshAllDisplays();
        }
    }

    private void TryAutoBindIfNeeded()
    {
        if (HasCompleteBindings())
        {
            return;
        }

        AutoBindValueTexts();
        AutoBindButtons();
    }

    private bool HasCompleteBindings()
    {
        return IsGroupComplete(numberCardPriceControls)
            && IsGroupComplete(formulaCardPriceControls)
            && IsGroupComplete(blessingPriceControls)
            && IsGroupComplete(deletionPriceControls)
            && IsGroupComplete(refreshPriceControls);
    }

    private static bool IsGroupComplete(DifficultySettingControlGroup group)
    {
        return group != null
            && group.valueText != null
            && group.decreaseButton != null
            && group.increaseButton != null;
    }
    #region 自动生成按钮
    private void AutoBindValueTexts()
    {
        List<Text> candidates = new List<Text>();
        Text[] allTexts = GetComponentsInChildren<Text>(true);

        foreach (Text text in allTexts)
        {
            if (text == null || text.GetComponentInParent<Button>() != null)
            {
                continue;
            }

            if (TryParseMultiplierText(text.text, out _))
            {
                candidates.Add(text);
            }
        }

        candidates.Sort((a, b) =>
        {
            float ay = GetLocalY(a.rectTransform);
            float by = GetLocalY(b.rectTransform);
            if (Mathf.Abs(ay - by) > 0.01f)
            {
                return by.CompareTo(ay);
            }

            float ax = GetLocalX(a.rectTransform);
            float bx = GetLocalX(b.rectTransform);
            return ax.CompareTo(bx);
        });

        if (numberCardPriceControls.valueText == null && candidates.Count > 0) numberCardPriceControls.valueText = candidates[0];
        if (formulaCardPriceControls.valueText == null && candidates.Count > 1) formulaCardPriceControls.valueText = candidates[1];
        if (blessingPriceControls.valueText == null && candidates.Count > 2) blessingPriceControls.valueText = candidates[2];
        if (deletionPriceControls.valueText == null && candidates.Count > 3) deletionPriceControls.valueText = candidates[3];
        if (refreshPriceControls.valueText == null && candidates.Count > 4) refreshPriceControls.valueText = candidates[4];
    }

    private void AutoBindButtons()
    {
        List<Button> candidates = new List<Button>();
        Button[] allButtons = GetComponentsInChildren<Button>(true);

        foreach (Button button in allButtons)
        {
            if (button == null)
            {
                continue;
            }

            string buttonName = button.gameObject.name;
            if (string.Equals(buttonName, "Return", StringComparison.OrdinalIgnoreCase)
                || string.Equals(buttonName, "initialize", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            candidates.Add(button);
        }

        candidates.Sort((a, b) =>
        {
            float ay = GetLocalY((RectTransform)a.transform);
            float by = GetLocalY((RectTransform)b.transform);
            if (Mathf.Abs(ay - by) > 0.01f)
            {
                return by.CompareTo(ay);
            }

            float ax = GetLocalX((RectTransform)a.transform);
            float bx = GetLocalX((RectTransform)b.transform);
            return ax.CompareTo(bx);
        });

        List<List<Button>> rows = BuildButtonRows(candidates);
        AssignButtonsFromRows(rows);
    }

    private List<List<Button>> BuildButtonRows(List<Button> buttons)
    {
        List<List<Button>> rows = new List<List<Button>>();

        foreach (Button button in buttons)
        {
            float y = GetLocalY((RectTransform)button.transform);
            List<Button> matchedRow = null;

            foreach (List<Button> row in rows)
            {
                float rowY = GetLocalY((RectTransform)row[0].transform);
                if (Mathf.Abs(rowY - y) <= RowTolerance)
                {
                    matchedRow = row;
                    break;
                }
            }

            if (matchedRow == null)
            {
                matchedRow = new List<Button>();
                rows.Add(matchedRow);
            }

            matchedRow.Add(button);
        }

        rows.Sort((a, b) => GetLocalY((RectTransform)b[0].transform).CompareTo(GetLocalY((RectTransform)a[0].transform)));

        foreach (List<Button> row in rows)
        {
            row.Sort((a, b) => GetLocalX((RectTransform)a.transform).CompareTo(GetLocalX((RectTransform)b.transform)));
        }

        return rows;
    }

    private void AssignButtonsFromRows(List<List<Button>> rows)
    {
        AssignButtonsToGroup(numberCardPriceControls, rows, 0);
        AssignButtonsToGroup(formulaCardPriceControls, rows, 1);
        AssignButtonsToGroup(blessingPriceControls, rows, 2);
        AssignButtonsToGroup(deletionPriceControls, rows, 3);
        AssignButtonsToGroup(refreshPriceControls, rows, 4);
    }

    private static void AssignButtonsToGroup(DifficultySettingControlGroup group, List<List<Button>> rows, int rowIndex)
    {
        if (group == null || rowIndex >= rows.Count)
        {
            return;
        }

        List<Button> row = rows[rowIndex];
        if (row.Count < 2)
        {
            return;
        }

        if (group.decreaseButton == null) group.decreaseButton = row[0];
        if (group.increaseButton == null) group.increaseButton = row[row.Count - 1];
    }
    #endregion
    private void BindControlGroup(DifficultySettingControlGroup group, DifficultySettingType settingType)
    {
        if (group == null)
        {
            return;
        }

        if (group.decreaseButton != null)
        {
            group.decreaseButton.onClick.RemoveAllListeners();
            group.decreaseButton.onClick.AddListener(() => ChangeDifficulty(settingType, -Step));
        }

        if (group.increaseButton != null)
        {
            group.increaseButton.onClick.RemoveAllListeners();
            group.increaseButton.onClick.AddListener(() => ChangeDifficulty(settingType, Step));
        }
    }

    private void ChangeDifficulty(DifficultySettingType settingType, float delta)
    {
        if (DataSavingManager.Instance == null)
        {
            Debug.LogWarning("DifficultySettingsManager: DataSavingManager is missing");
            return;
        }

        float current = DataSavingManager.Instance.GetDifficultyMultiplier(settingType);
        float nextValue = Mathf.Clamp(current + delta, MinMultiplier, MaxMultiplier);
        nextValue = Mathf.Round(nextValue * 2f) / 2f;

        DataSavingManager.Instance.SetDifficultyMultiplier(settingType, nextValue);

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.ApplyDifficultySettings();
        }

        RefreshAllDisplays();
    }

    private void RefreshAllDisplays()
    {
        RefreshControlGroup(numberCardPriceControls, DifficultySettingType.NumberCardPrice);
        RefreshControlGroup(formulaCardPriceControls, DifficultySettingType.FormulaCardPrice);
        RefreshControlGroup(blessingPriceControls, DifficultySettingType.BlessingPrice);
        RefreshControlGroup(deletionPriceControls, DifficultySettingType.CardDeletionPrice);
        RefreshControlGroup(refreshPriceControls, DifficultySettingType.ShopRefreshPrice);
        RefreshDifficultyLevelDisplay();
    }

    private void RefreshControlGroup(DifficultySettingControlGroup group, DifficultySettingType settingType)
    {
        if (group == null || DataSavingManager.Instance == null)
        {
            return;
        }

        float current = DataSavingManager.Instance.GetDifficultyMultiplier(settingType);

        if (group.valueText != null)
        {
            group.valueText.text = $"{current:F1}";
        }

        if (group.decreaseButton != null)
        {
            group.decreaseButton.interactable = current > MinMultiplier;
        }

        if (group.increaseButton != null)
        {
            group.increaseButton.interactable = current < MaxMultiplier;
        }
    }

    private void BindToggles()
    {
            TryAutoBindTogglesAndText();
        if (oneMoreCardToggle != null)
        {
            oneMoreCardToggle.onValueChanged.RemoveAllListeners();
            oneMoreCardToggle.isOn = oneMoreCard;
            oneMoreCardToggle.onValueChanged.AddListener((val) =>
            {
                oneMoreCard = val;
                RefreshDifficultyLevelDisplay();
                // 即时生效：如果正在游戏中，立即添加/移除额外卡牌
                if (val && GameManager.Instance != null)
                {
                    ApplyOneMoreCardNow();
                }
            });
        }

        if (higherCostToggle != null)
        {
            higherCostToggle.onValueChanged.RemoveAllListeners();
            higherCostToggle.isOn = higherCost;
            higherCostToggle.onValueChanged.AddListener((val) =>
            {
                higherCost = val;
                RefreshDifficultyLevelDisplay();
                // 即时生效：刷新商店解锁费用显示
                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.RefreshUnlockCostDisplay();
                }
            });
        }

        RefreshDifficultyLevelDisplay();
    }

    /// <summary>
    /// 当 Inspector 引用为 null 时（如 AddComponent 动态创建），自动从子物体中查找 Toggle 和 Text
    /// </summary>
    private void TryAutoBindTogglesAndText()
    {
        if (oneMoreCardToggle == null || higherCostToggle == null || difficultyLevelText == null)
        {
            Toggle[] allToggles = GetComponentsInChildren<Toggle>(true);
            Text[] allTexts = GetComponentsInChildren<Text>(true);

            foreach (Toggle toggle in allToggles)
            {
                string lowerName = toggle.gameObject.name.ToLower();
                if (oneMoreCardToggle == null && lowerName.Contains("onemore"))
                {
                    oneMoreCardToggle = toggle;
                }
                else if (higherCostToggle == null && lowerName.Contains("higher"))
                {
                    higherCostToggle = toggle;
                }
            }

            if (difficultyLevelText == null)
            {
                foreach (Text text in allTexts)
                {
                    if (text.gameObject.name.ToLower().Contains("difficulty"))
                    {
                        difficultyLevelText = text;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 即时应用 oneMoreCard 效果（Toggle 开启时立即添加卡牌）
    /// </summary>
    private void ApplyOneMoreCardNow()
    {
        NumberCardData card = extraStartCard;
        if (card == null)
        {
            // 兜底：从卡牌库中找到值为1的普通数字卡
            var cardManager = FindObjectOfType<CardManager>();
            if (cardManager != null && cardManager.numberCardLibrary != null)
            {
                foreach (var c in cardManager.numberCardLibrary.allCards)
                {
                    if (c.partA != null && c.partA.value == 1
                        && !c.partA.isDice && !c.partA.isIncremental
                        && c.logicalType == NumberCardData.LogicalType.Normal
                        && c.partB == null)
                    {
                        card = c;
                        break;
                    }
                }
            }
        }

        if (card != null && PlayerCardInventory.Instance != null)
        {
            PlayerCardInventory.Instance.AddNumberCard(card);
            var cardManager = FindObjectOfType<CardManager>();
            if (cardManager != null) cardManager.SyncDeckFromInventory();
        }
    }

    public int GetCurrentDifficultyLevel()
    {
        CalculateTotalDifficultyLevel();
        return difficultyLevel;
    }

    private void CalculateTotalDifficultyLevel()
    {
        difficultyLevel = 0;
        if (DataSavingManager.Instance == null) return;

        // 五个倍率调节器：每0.5幅度 = 1级难度
        difficultyLevel += MultiplierToDifficultyLevel(DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.NumberCardPrice));
        difficultyLevel += MultiplierToDifficultyLevel(DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.FormulaCardPrice));
        difficultyLevel += MultiplierToDifficultyLevel(DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.BlessingPrice));
        difficultyLevel += MultiplierToDifficultyLevel(DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.CardDeletionPrice));
        difficultyLevel += MultiplierToDifficultyLevel(DataSavingManager.Instance.GetDifficultyMultiplier(DifficultySettingType.ShopRefreshPrice));

        // 开关各固定贡献5等级
        if (oneMoreCard) difficultyLevel += 5;
        if (higherCost) difficultyLevel += 5;
    }

    private int MultiplierToDifficultyLevel(float multiplier)
    {
        return (int)((multiplier - 1f) / 0.5f);
    }

    private void RefreshDifficultyLevelDisplay()
    {
        CalculateTotalDifficultyLevel();
        if (difficultyLevelText != null)
        {
            difficultyLevelText.text = $"{difficultyLevel}";
        }
    }

    private static bool TryParseMultiplierText(string value, out float parsedValue)
    {
        string normalized = value?.Trim().Replace("x", string.Empty).Replace("X", string.Empty);
        return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
            || float.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out parsedValue);
    }

    private static float GetLocalY(RectTransform rectTransform)
    {
        return rectTransform != null ? rectTransform.anchoredPosition.y : 0f;
    }

    private static float GetLocalX(RectTransform rectTransform)
    {
        return rectTransform != null ? rectTransform.anchoredPosition.x : 0f;
    }
}
