using System.Collections.Generic;

/// <summary>
/// 局内运行存档（Run Save）数据结构。
/// 与 SavingData（历史最高纪录/难度设置等元数据）相互独立，只保存"当前这一局"的进行中状态。
/// 全部使用 JsonUtility 可序列化的扁平字段：
///  - BigInteger 一律转 string 存储（与 SavingData 同一口径）
///  - Dictionary 一律转 List&lt;SavedIntPair&gt; 存储
///  - 卡牌资产以 ScriptableObject.name（资产文件名，全局唯一）作为持久化标识
/// </summary>
[System.Serializable]
public class RunSaveData
{
    // ============ 头部 ============
    /// 存档格式版本（结构不兼容时递增，旧档校验失败作废）
    public int saveVersion;

    /// 保存时的场景状态：0=游戏界面(未结算) 1=商店界面 2=已点击结算、动画期间退出
    public int sceneState;

    // ============ 全局进度 ============
    public int gameMode;              // GameManager.GameMode
    public bool isInvolutionMode;     // 内卷模式（静态标记，必须保存）
    public bool isEndlessMode;        // 无尽模式（静态标记，必须保存）

    /// 当前点数（BigInteger 字符串）
    public string currentPoints;
    /// 防作弊冗余：currentPoints 的异或掩码副本，加载时交叉比对
    public string pointsShadow;

    public int currentRound;
    public string stageRequirement;

    /// 本局统计数据（用于通关时写入历史纪录）
    public string roundMaxNumberCardValue;
    public long roundMaxMultiplier;
    public string roundMaxCalculationValue;

    /// 经验主义祝福引用的"上一回合最大数字卡"（库存索引，-1 表示无）
    public int lastRoundMaxInventoryIndex = -1;

    // 难度开关（DifficultySettingsManager 的静态标记）
    public bool diffOneMoreCard;
    public bool diffHigherCost;

    // ============ 玩家库存 ============
    /// 拥有的数字卡实例（含实例级动态数值 currentA/currentB/骰子面数）
    public List<SavedNumberCard> numberCards = new List<SavedNumberCard>();
    /// 拥有的填空卡（资产名列表，顺序即购买顺序）
    public List<string> formulaCards = new List<string>();

    // ============ 手牌 / 公式区 ============
    /// 本回合手牌（库存索引列表；商店状态时为上一回合的手牌，照常保存）
    public List<int> handInventoryIndices = new List<int>();
    /// 当前公式卡资产名（空串表示无）
    public string currentFormulaCard = "";
    /// 公式槽位 → 手牌索引（-1 = 空）
    public List<int> selectedSlotHandIndices = new List<int>();
    /// 嗜赌如命/爱财如命的本回合抽卡标记
    public bool drewDiceCardThisRound;
    public bool drewGoldenCardThisRound;
    /// 慈爱星"已应用计数"（CardManager 私有镜像）
    public int lastAppliedCompassionStarCount;

    // ============ 祝福系统 ============
    /// 拥有的祝福：id → 层数（按获得顺序保存）
    public List<SavedIntPair> ownedBlessings = new List<SavedIntPair>();
    /// 永久购买记录（NeverRefresh 判定用）
    public List<int> blessingsEverPurchased = new List<int>();

    // —— 累积效果数值（购买时叠加，无法由祝福列表重新推导，必须原样保存）——
    public long totalMultiplierBonus;
    public List<SavedIntPair> idealismDiceResults = new List<SavedIntPair>();   // 唯心主义
    public List<SavedDelayRecord> delayRecords = new List<SavedDelayRecord>();  // 延迟满足
    public List<SavedRuminationRecord> ruminationRecords = new List<SavedRuminationRecord>(); // 反刍
    public List<int> morningStarTargets = new List<int>();                      // 启明星锁定卡（库存索引）

    public int totalDialecticalCount;
    public int allGodsCount;
    public int luckTurnsCount;
    public int cardMasterCount;
    public int hasRichTreasure;
    public int wishCoinPurchaseCount;
    public int nihilismCount;
    public int darkBoxPurchaseCount;
    public int pureVesselPendingCharges;
    public bool hasLeadingCharge;
    public bool hasGambleToWin;
    public int hasEnergySpread;
    public int hasRisingUp;
    public int hasTemperlance;
    public bool hasIdealism;
    public float dialecticalPricePercent;
    public int applyPragmatism;
    public bool hasGodOfGambler;
    public float globalPriceDiscountPercent;
    public float blessDiscountPerBlessing;
    public int shortSightCount;
    public int reverse;
    public bool hasYinYang;
    public bool hasFall;
    public int hasUnstoppable;
    public int hasLoanWallet;
    public string loanPoints = "0";          // 贷款金额（BigInteger）
    public int minimalismMultiplier;
    public int minimalismCount;
    public int pragmatismDeleteCount;
    public int dayAfterDayCount;
    public int hasHastyAppreciation;
    public int hastyAppreciationBonus;
    public int bigSuccessCount;
    public bool hasJustice;
    public int luckyStarCount;
    public int fortuneStarCount;
    public int meteorCount;
    public int meteorPendingCharges;
    public int wealthStarCount;
    public int wealthStarPendingCharges;
    public int disasterStarCount;
    public int morningsStarCount;
    public int compassionStarCount;
    public int bigSevenStarCount;
    public bool hasFinancialExpert;
    public bool hasCasinoCommissioner;
    public int luxuriant;
    public int sellOff;
    public bool hasAddictedtoGambling;
    public bool hasLovingWealth;
    public int spiritGodRealm;
    public bool hasKingOfTheBoard;
    public string kingBoardGain = "0";       // BigInteger
    public int antimatterNucleus;
    public int colorful;
    public int antimatterCloud;
    public bool hasRumination;
    public int risingUpStepbyStep;
    public long risingUpStepbyStepBonus;
    public int eightWaysToWealth;

    /// 许愿币目标祝福 id（0 = 无）
    public int wishCoinTargetBlessingId;
    /// 暗箱操作目标祝福 id（0 = 无）
    public int darkBoxTargetBlessingId;
    /// 好事成双"上一个可叠加祝福"id（0 = 无）
    public int lastObtainedStackableBlessingId;

    // ============ 商店 ============
    public int refreshCount;              // 全局累计刷新次数（走马观花）
    public int shopRefreshCount;          // 本商店刷新次数（刷新费公式）
    public int totalRemovedNumberCards;
    public int totalRemovedFormulaCards;
    public int numberCardCount;           // 当前可见/已解锁槽位数
    public int formulaCardCount;
    public int blessingCardCount;
    public int numberSlotUnlockTimes;
    public int formulaSlotUnlockTimes;
    public int blessingSlotUnlockTimes;

    /// 本次刷新出的商品（含 sold 标记与当时锁定的价格 → 满足"商店退出保留本次商品"）
    public List<SavedShopNumberItem> shopNumberCards = new List<SavedShopNumberItem>();
    public List<SavedShopFormulaItem> shopFormulaCards = new List<SavedShopFormulaItem>();
    public List<SavedShopBlessingItem> shopBlessings = new List<SavedShopBlessingItem>();

    /// AlwaysRefresh 保底权重统计（private 字典，随局保存）
    public List<SavedIntPair> alwaysRefreshMissStreak = new List<SavedIntPair>();
    public List<SavedIntPair> alwaysRefreshAppearanceCount = new List<SavedIntPair>();
    /// 本回合已购买祝福（CurrentRoundOnly 排除用）
    public List<int> currentRoundPurchasedBlessings = new List<int>();
    /// 本商店访问中被排除的祝福（祸星）
    public List<int> blessingsExcludedFromRefresh = new List<int>();
}

// ==================== 子结构 ====================

/// <summary>数字卡实例快照（库存与商店通用）</summary>
[System.Serializable]
public class SavedNumberCard
{
    public string card;       // NumberCardData 资产名（SO.name）
    public int valueA;        // 实例 currentA
    public int valueB;        // 实例 currentB
    public int diceSidesA;    // 实例 currentDiceSidesA（赌场专员升级后可能高于库值）
    public int diceSidesB;    // 实例 currentDiceSidesB
}

[System.Serializable]
public class SavedIntPair
{
    public int k;
    public int v;

    public SavedIntPair() { }
    public SavedIntPair(int key, int value) { k = key; v = value; }
}

[System.Serializable]
public class SavedShopNumberItem
{
    public string card;
    public int valueA;
    public int valueB;
    public int diceSidesA;
    public int diceSidesB;
    public string price = "0";   // BigInteger
    public bool sold;
}

[System.Serializable]
public class SavedShopFormulaItem
{
    public string card;          // FormulaCardData 资产名
    public string price = "0";
    public bool sold;
}

[System.Serializable]
public class SavedShopBlessingItem
{
    public int blessingId;
    public string price = "0";
    public bool sold;
}

[System.Serializable]
public class SavedDelayRecord
{
    public int remainTurn;
    public int rewardMul;
}

[System.Serializable]
public class SavedRuminationRecord
{
    public int remainTurn;
    public string recordedPoints = "0";  // BigInteger（负数）
}
