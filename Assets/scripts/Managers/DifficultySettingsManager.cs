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

        RefreshAllDisplays();
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
