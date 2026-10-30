using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// 局内运行存档管理器（静态类，无需挂载到场景）。
///
/// 职责：
///  1. 在"返回主菜单 / 退出游戏"时，把当前局状态序列化为单一存档文件；
///  2. 结算动画期间退出 → 使用"结算检查点"（骰子/递增已定格的状态），
///     继续游戏时按"已结算"无动画补完结算并进入商店；
///  3. 主菜单"继续游戏"时校验并恢复整局状态；
///  4. 防作弊：载荷 XOR 混淆 + SHA-256 完整性校验 + 关键数值冗余比对 + 不变量验证。
///
/// 注意：单机离线游戏无法绝对防作弊（内存修改/反编译不可防），
/// 本方案的目标是阻止"直接改存档文件"这一最廉价的作弊途径。
/// </summary>
public static class RunSaveManager
{
    private const int SaveVersion = 1;

    /// <summary>存档文件名（保存在 Application.persistentDataPath）</summary>
    private const string SaveFileName = "runsave.dat";

    /// <summary>文件魔数（快速识别 + 版本隔离）</summary>
    private const string FileMagic = "RB24RUN1";

    /// <summary>
    /// 混淆/校验密钥。改动后所有旧存档立即失效（相当于版本重置）。
    /// </summary>
    private const string SecretKey = "24e8|RunSave|v1|do-not-edit|salt";

    /// <summary>点数冗余校验的异或掩码（防"只改一个字段"的简单篡改）</summary>
    private static readonly BigInteger PointsShadowMask = new BigInteger(0x5A17B00B5L);

    /// <summary>结算检查点：点击结算后（骰子/递增已定格）抓取的快照</summary>
    private static RunSaveData settlementCheckpoint;

    private static string SavePath
    {
        get { return Application.persistentDataPath + "/" + SaveFileName; }
    }

    #region 保存入口

    /// <summary>
    /// 保存当前局。由"返回主菜单"与"退出游戏"调用。
    /// 状态分派：
    ///  - 玩家回合（未结算）→ 保留本次抽到的手牌/公式卡；
    ///  - 商店 → 保留本次刷新出的商品；
    ///  - 结算动画期间 → 写入结算检查点（按"已结算"处理）；
    ///  - 主菜单/胜负界面 → 不保存。
    /// </summary>
    public static void SaveRun()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        switch (gm.currentState)
        {
            case GameManager.GameState.PlayerTurn:
                WriteFile(BuildSnapshot(0));
                break;

            case GameManager.GameState.Shop:
                WriteFile(BuildSnapshot(1));
                break;

            case GameManager.GameState.Calculation:
                // 优先使用结算检查点（骰子已定格）；检查点缺失（极早期的同帧退出）时
                // 退化为普通未结算存档，下一回合重新抽卡。
                if (settlementCheckpoint != null)
                {
                    WriteFile(settlementCheckpoint);
                }
                else
                {
                    WriteFile(BuildSnapshot(0));
                }
                break;

            default:
                // MainMenu / GameOver / GameLose：无进行中的局，不保存
                break;
        }
    }

    /// <summary>
    /// 结算进行中退出、且已当场静默补完结算（商店已生成）时调用：
    /// 直接写"商店界面"快照（sceneState=1）。继续游戏时也就是走商店恢复分支，
    /// 商品、价格、售出标记全部原样保留，不会重新抽一次。
    /// </summary>
    public static void SaveSettledRun()
    {
        if (GameManager.Instance == null) return;
        WriteFile(BuildSnapshot(1));
        settlementCheckpoint = null;
    }

    /// <summary>
    /// 结算检查点抓取入口：由 GameManager 在 PrepareCardsForCalculation 之后调用。
    /// 此刻骰子、递增、流星/慈爱星等"结算判定"已全部完成且不可更改，
    /// 此后任何时刻退出都按"结算完成"处理（防止退出重投骰子/逃避负分结算）。
    /// </summary>
    public static void CaptureSettlementCheckpoint()
    {
        if (GameManager.Instance == null) return;
        settlementCheckpoint = BuildSnapshot(2);
    }

    /// <summary>清除内存中的结算检查点（新回合开始时调用，防止跨回合误用）</summary>
    public static void ClearSettlementCheckpoint()
    {
        settlementCheckpoint = null;
    }

    /// <summary>开启新游戏 / 对局结束时调用：删除局内存档（新游戏自动覆盖旧档）</summary>
    public static void ClearSave()
    {
        settlementCheckpoint = null;
        try
        {
            if (System.IO.File.Exists(SavePath))
            {
                System.IO.File.Delete(SavePath);
                Debug.Log("[RunSave] 局内存档已清除");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RunSave] 清除存档失败：{e.Message}");
        }
    }

    /// <summary>主菜单查询：是否存在可继续的局内存档</summary>
    public static bool HasValidSave()
    {
        return TryLoadValidated() != null;
    }

    /// <summary>主菜单"继续游戏"按钮入口</summary>
    public static void ContinueRun()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.currentState != GameManager.GameState.MainMenu)
        {
            Debug.LogWarning("[RunSave] 只有在主菜单才能继续游戏");
            return;
        }

        RunSaveData data = TryLoadValidated();
        if (data == null)
        {
            // 区分"没有存档"与"存档存在但校验不通过"，避免只看到一句含糊提示
            if (!System.IO.File.Exists(SavePath))
            {
                Debug.Log("[RunSave] 没有局内存档文件：" + SavePath);
            }
            else
            {
                Debug.LogWarning("[RunSave] 存档存在但未通过校验，已取消继续游戏（详见上一条警告）；文件未被删除");
            }
            return;
        }

        if (ApplyRun(data))
        {
            Debug.Log($"[RunSave] 继续游戏成功：第{data.currentRound}回合，状态={data.sceneState}");
        }
    }

    #endregion

    #region 快照构建

    /// <summary>
    /// 抓取当前局快照。各管理器自带 CaptureToSave，本方法只做编排。
    /// </summary>
    private static RunSaveData BuildSnapshot(int sceneState)
    {
        // 资产索引可能因编辑器热重载/新加载资产而失效，保存前重建一次
        InvalidateAssetIndex();

        RunSaveData d = new RunSaveData();
        d.saveVersion = SaveVersion;
        d.sceneState = sceneState;

        // 先抓库存（手牌/祝福/ GameManager 都以库存索引做引用）
        PlayerCardInventory.Instance.CaptureToSave(d);
        CardManager.Instance.CaptureToSave(d);
        GameManager.Instance.CaptureToSave(d);
        BlessingManager.Instance.CaptureToSave(d);
        ShopManager.Instance.CaptureToSave(d);

        if (DifficultySettingsManager.Instance != null)
        {
            d.diffOneMoreCard = DifficultySettingsManager.oneMoreCard;
            d.diffHigherCost = DifficultySettingsManager.higherCost;
        }

        // 防作弊冗余：点数异或掩码副本
        d.pointsShadow = (SavingData.StringToBigInteger(d.currentPoints) ^ PointsShadowMask).ToString();

        return d;
    }

    #endregion

    #region 恢复

    /// <summary>
    /// 应用存档：恢复各管理器状态并按 sceneState 恢复界面。
    /// 返回 false 表示恢复失败（存档文件保留，便于修复后重试）。
    /// </summary>
    private static bool ApplyRun(RunSaveData d)
    {
        if (d == null) return false;

        // 1. 难度开关
        DifficultySettingsManager.oneMoreCard = d.diffOneMoreCard;
        DifficultySettingsManager.higherCost = d.diffHigherCost;

        // 2. 库存（数字卡/填空卡实例）
        if (!PlayerCardInventory.Instance.RestoreFromSave(d))
        {
            Invalidate("玩家卡牌库存恢复失败");
            return false;
        }

        // 3. 手牌 / 公式区 / 槽位（数据层）
        if (!CardManager.Instance.RestoreFromSave(d))
        {
            Invalidate("手牌恢复失败");
            return false;
        }

        // 4. 祝福系统
        if (!BlessingManager.Instance.RestoreFromSave(d))
        {
            Invalidate("祝福恢复失败");
            return false;
        }

        // 5. 全局进度
        GameManager.Instance.RestoreFromSave(d);

        // 6. 按场景状态恢复界面
        //    必须先切面板、再重建手牌/公式卡内容——UI 面板处于未激活状态时
        //    重建布局组会算错尺寸，卡牌视觉会挤在一起。
        switch (d.sceneState)
        {
            case 0:
                // 游戏界面（未结算）：本回合抽到的全部数字卡重新在手牌区生成，公式槽位留空
                GameManager.Instance.ChangeState(GameManager.GameState.PlayerTurn);
                GameManager.Instance.RestoreTurnUI(false);
                break;

            case 1:
                // 商店界面：保留本次刷新出的商品
                if (!ShopManager.Instance.RestoreFromSave(d))
                {
                    Invalidate("商店恢复失败");
                    return false;
                }
                GameManager.Instance.ChangeState(GameManager.GameState.Shop);
                UIManager.Instance.RefreshShopUI();
                break;

            case 2:
                // 已点击结算、动画期间退出：按"结算完成"处理 → 无动画补完结算，直接进商店
                // 该状态不走 ChangeState（结算状态不切面板），需显式显示游戏面板
                UIManager.Instance.ShowPanel(UIManager.Instance.gameUIPanel);
                GameManager.Instance.RestoreTurnUI(true);
                GameManager.Instance.ResumePendingSettlement();
                break;

            default:
                Invalidate($"未知场景状态 {d.sceneState}");
                return false;
        }

        settlementCheckpoint = null;
        return true;
    }

    /// <summary>
    /// 恢复失败：保留存档文件，仅记录原因并留在主菜单。
    /// 不再删档——校验阶段（ValidateRunSave）已经拦下篡改/损坏的存档，
    /// 这里的失败通常是"资产缺失/管理器未就绪"这类可修复问题，
    /// 直接删掉会连带销毁玩家的整局进度。
    /// </summary>
    private static void Invalidate(string reason)
    {
        Debug.LogWarning($"[RunSave] 存档恢复失败（{reason}），已保留存档文件，请检查资产配置后重试");
    }

    /// <summary>
    /// 恢复"已拖入公式槽位"的卡牌视觉位置：
    /// ShowHandCards/ShowFormulaCard 重建 UI 后，把对应手牌视图移回槽位内。
    /// 只处理表现层，数据层已在 CardManager.RestoreFromSave 中恢复。
    /// </summary>
    public static void RestorePlacedCardsVisual()
    {
        CardManager cardMgr = CardManager.Instance;
        UIManager ui = UIManager.Instance;
        if (cardMgr == null || ui == null || ui.formulaArea == null || ui.handArea == null) return;

        FormulaCardUI formulaUI = ui.formulaArea.GetComponentInChildren<FormulaCardUI>(true);
        if (formulaUI == null || cardMgr.selectedNumberCards == null) return;

        var slots = formulaUI.Slots;
        for (int i = 0; i < slots.Count && i < cardMgr.selectedNumberCards.Count; i++)
        {
            NumberCardInstance card = cardMgr.selectedNumberCards[i];
            FormulaSlot slot = slots[i];
            if (card == null || slot == null) continue;

            // 在手牌区找到绑定该实例的卡牌视图
            PlayerController controller = FindHandCardController(ui.handArea, card);
            if (controller == null)
            {
                Debug.LogWarning($"[RunSave] 槽位 {i} 的卡牌 {card.cardData.cardName} 在手牌区找不到视图，跳过视觉恢复");
                continue;
            }

            controller.transform.SetParent(slot.transform);
            controller.transform.localPosition = UnityEngine.Vector3.zero;
            controller.transform.localScale = UnityEngine.Vector3.one;

            RectTransform rt = controller.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new UnityEngine.Vector2(0.5f, 0.5f);
                rt.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
                rt.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
                rt.anchoredPosition = UnityEngine.Vector2.zero;
            }

            controller.isPlacedInSlot = true;
            controller.currentSlot = slot;

            NumberCardLayoutView view = controller.GetComponent<NumberCardLayoutView>();
            if (view != null) view.BindInstance(card);

            slot.currentCard = card;
            slot.filledNumberCardCount++;
        }
    }

    private static PlayerController FindHandCardController(Transform handArea, NumberCardInstance card)
    {
        foreach (Transform child in handArea)
        {
            PlayerController pc = child.GetComponent<PlayerController>();
            if (pc != null && pc.BoundCard == card)
            {
                return pc;
            }
        }
        return null;
    }

    #endregion

    #region 资产查找

    // 存档以 ScriptableObject.name（资产文件名）作为持久化标识。
    // 关键约束：卡库（NumberCardLibrary / FormulaCardLibrary）只用于商店抽卡，
    // "起始牌组"里的初始卡牌并不在卡库中（例：Library/2.asset、~6~、InitialFormula），
    // 因此资产解析必须覆盖「卡库 ∪ 起始牌组 ∪ 当前已加载的全部同类资产」，
    // 否则初始卡牌会解析失败，导致整份存档被判定无效（历史缺陷）。
    private static Dictionary<string, NumberCardData> numberCardIndex;
    private static Dictionary<string, FormulaCardData> formulaCardIndex;

    /// <summary>清除资产索引缓存（保存/加载前调用，确保新建或新加载的资产能被解析）</summary>
    public static void InvalidateAssetIndex()
    {
        numberCardIndex = null;
        formulaCardIndex = null;
    }

    /// <summary>按资产名（SO.name）查找数字卡数据</summary>
    public static NumberCardData FindNumberCardData(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return null;

        Dictionary<string, NumberCardData> index = GetNumberCardIndex();
        if (index == null) return null;

        NumberCardData data;
        return index.TryGetValue(assetName, out data) ? data : null;
    }

    /// <summary>按资产名（SO.name）查找填空卡数据</summary>
    public static FormulaCardData FindFormulaCardData(string assetName)
    {
        if (string.IsNullOrEmpty(assetName)) return null;

        Dictionary<string, FormulaCardData> index = GetFormulaCardIndex();
        if (index == null) return null;

        FormulaCardData data;
        return index.TryGetValue(assetName, out data) ? data : null;
    }

    private static Dictionary<string, NumberCardData> GetNumberCardIndex()
    {
        if (numberCardIndex != null) return numberCardIndex;

        CardManager cm = CardManager.Instance;
        if (cm == null) return null; // 管理器尚未就绪：暂不建缓存，下次调用再建

        Dictionary<string, NumberCardData> index =
            new Dictionary<string, NumberCardData>(StringComparer.Ordinal);

        // 1. 商店卡库
        if (cm.numberCardLibrary != null)
        {
            AppendNumberCards(index, cm.numberCardLibrary.allCards);
        }

        // 2. 起始牌组（初始卡牌的唯一可靠来源）
        AppendNumberCards(index, cm.starterNumberCards);

        // 3. 库存自己引用的卡库（可能与 CardManager 上的是同一份，去重由 TryAdd 处理）
        PlayerCardInventory inv = PlayerCardInventory.Instance;
        if (inv != null && inv.numberCardLibrary != null)
        {
            AppendNumberCards(index, inv.numberCardLibrary.allCards);
        }

        // 4. 兜底：当前已加载的全部数字卡资产（覆盖未挂进任何列表的资产）
        AppendNumberCards(index, Resources.FindObjectsOfTypeAll<NumberCardData>());

        numberCardIndex = index;
        return index;
    }

    private static Dictionary<string, FormulaCardData> GetFormulaCardIndex()
    {
        if (formulaCardIndex != null) return formulaCardIndex;

        CardManager cm = CardManager.Instance;
        if (cm == null) return null;

        Dictionary<string, FormulaCardData> index =
            new Dictionary<string, FormulaCardData>(StringComparer.Ordinal);

        if (cm.formulaCardLibrary != null)
        {
            AppendFormulaCards(index, cm.formulaCardLibrary.allCards);
        }

        // 起始公式卡同样可能不在卡库中（例：StarterCards/InitialFormula.asset）
        AppendFormulaCards(index, cm.starterFormulaCards);

        AppendFormulaCards(index, Resources.FindObjectsOfTypeAll<FormulaCardData>());

        formulaCardIndex = index;
        return index;
    }

    private static void AppendNumberCards(Dictionary<string, NumberCardData> index,
                                          IList<NumberCardData> cards)
    {
        if (index == null || cards == null) return;

        for (int i = 0; i < cards.Count; i++)
        {
            NumberCardData card = cards[i];
            if (card == null) continue;

            string key = card.name;
            if (string.IsNullOrEmpty(key)) continue; // 运行时 CreateInstance 的临时资产没有资产名

            if (!index.ContainsKey(key))
            {
                index.Add(key, card);
            }
        }
    }

    private static void AppendFormulaCards(Dictionary<string, FormulaCardData> index,
                                           IList<FormulaCardData> cards)
    {
        if (index == null || cards == null) return;

        for (int i = 0; i < cards.Count; i++)
        {
            FormulaCardData card = cards[i];
            if (card == null) continue;

            string key = card.name;
            if (string.IsNullOrEmpty(key)) continue;

            if (!index.ContainsKey(key))
            {
                index.Add(key, card);
            }
        }
    }

    /// <summary>按 id 查找祝福数据</summary>
    public static BlessingData FindBlessingData(int blessingId)
    {
        if (BlessingManager.Instance == null || BlessingManager.Instance.blessingLibrary == null) return null;
        return BlessingManager.Instance.blessingLibrary.GetBlessingById(blessingId);
    }

    #endregion

    #region 文件读写 / 混淆 / 校验

    private static void WriteFile(RunSaveData data)
    {
        if (data == null) return;
        try
        {
            string json = JsonUtility.ToJson(data);
            string payload = Obfuscate(json);
            string checksum = ComputeChecksum(json);
            string content = FileMagic + "|" + payload + "|" + checksum;
            System.IO.File.WriteAllText(SavePath, content);
            Debug.Log($"[RunSave] 局内存档已保存：{SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RunSave] 存档保存失败：{e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>读取 + 校验存档；任何一步失败返回 null（不删除文件，但视为不可用）</summary>
    private static RunSaveData TryLoadValidated()
    {
        try
        {
            if (!System.IO.File.Exists(SavePath)) return null;

            // 说明：资产索引不在此处重建。HasValidSave 会被主菜单按钮反复查询，
            // 每次重建要扫描全部已加载资产；索引改动只在保存时（BuildSnapshot）重建一次即可，
            // 跨会话时静态字段本来就会随进程/域重载清空。
            string content = System.IO.File.ReadAllText(SavePath);
            string[] parts = content.Split('|');
            if (parts.Length != 3 || parts[0] != FileMagic)
            {
                Debug.LogWarning("[RunSave] 存档格式不识别，视为无效");
                return null;
            }

            string json = Deobfuscate(parts[1]);

            // 完整性校验：任何篡改都会导致哈希不一致
            if (ComputeChecksum(json) != parts[2])
            {
                Debug.LogWarning("[RunSave] 存档校验失败（可能被篡改或损坏），视为无效");
                return null;
            }

            RunSaveData data = JsonUtility.FromJson<RunSaveData>(json);
            if (data == null)
            {
                Debug.LogWarning("[RunSave] 存档反序列化失败");
                return null;
            }

            if (!ValidateRunSave(data))
            {
                return null;
            }

            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RunSave] 存档读取失败：{e.Message}");
            return null;
        }
    }

    /// <summary>XOR + Base64 混淆（阻止直接用文本编辑器改档）</summary>
    private static string Obfuscate(string json)
    {
        byte[] raw = Encoding.UTF8.GetBytes(json);
        byte[] key = Encoding.UTF8.GetBytes(SecretKey);
        for (int i = 0; i < raw.Length; i++)
        {
            raw[i] ^= key[i % key.Length];
        }
        return Convert.ToBase64String(raw);
    }

    private static string Deobfuscate(string payload)
    {
        byte[] raw = Convert.FromBase64String(payload);
        byte[] key = Encoding.UTF8.GetBytes(SecretKey);
        for (int i = 0; i < raw.Length; i++)
        {
            raw[i] ^= key[i % key.Length];
        }
        return Encoding.UTF8.GetString(raw);
    }

    /// <summary>SHA-256(payloadJson + SecretKey) 的十六进制串</summary>
    private static string ComputeChecksum(string json)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json + SecretKey);
            byte[] hash = sha.ComputeHash(bytes);

            StringBuilder sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }

    #endregion

    #region 不变量验证（防作弊第二道关卡）

    /// <summary>
    /// 对解析后的存档做业务不变量验证。
    /// 即使攻击者同时改写哈希（拿到密钥），逻辑上不可能的数值也会在这里被拒绝。
    /// </summary>
    private static bool ValidateRunSave(RunSaveData d)
    {
        if (d.saveVersion != SaveVersion)
        {
            Debug.LogWarning($"[RunSave] 存档版本不匹配：{d.saveVersion} ≠ {SaveVersion}");
            return false;
        }

        if (d.sceneState < 0 || d.sceneState > 2) return false;
        if (d.gameMode < 0 || d.gameMode > 2) return false;

        // 点数必须可解析，且与异或掩码副本一致
        BigInteger points = SavingData.StringToBigInteger(d.currentPoints);
        if (d.pointsShadow != (points ^ PointsShadowMask).ToString())
        {
            Debug.LogWarning("[RunSave] 点数冗余校验不一致，存档无效");
            return false;
        }

        // 回合范围（防御性放宽到十万回合，无尽模式理论上可无限）
        if (d.currentRound < 1 || d.currentRound > 100000) return false;

        // 库存
        if (d.numberCards == null || d.numberCards.Count < PlayerCardInventory.Instance.minNumberCardCount)
        {
            Debug.LogWarning("[RunSave] 数字卡数量低于下限");
            return false;
        }
        if (d.numberCards.Count > 10000) return false;
        for (int i = 0; i < d.numberCards.Count; i++)
        {
            SavedNumberCard saved = d.numberCards[i];
            if (saved == null || FindNumberCardData(saved.card) == null)
            {
                Debug.LogWarning($"[RunSave] 数字卡资产不存在：{(saved == null ? "<空记录>" : saved.card)}" +
                                 "（存档按 SO.name 记录；若为新加入的初始卡，请确认它挂在 CardManager.starterNumberCards 上）");
                return false;
            }
            // 骰子面数只能是 0（非骰子）或正常面数，越界值说明被改过
            if (saved.diceSidesA < 0 || saved.diceSidesA > 1000 ||
                saved.diceSidesB < 0 || saved.diceSidesB > 1000)
            {
                Debug.LogWarning("[RunSave] 骰子面数非法");
                return false;
            }
        }

        // 填空卡
        if (d.formulaCards == null || d.formulaCards.Count < PlayerCardInventory.Instance.minFormulaCardCount)
        {
            Debug.LogWarning("[RunSave] 填空卡数量低于下限");
            return false;
        }
        if (d.formulaCards.Count > 10000) return false;
        for (int i = 0; i < d.formulaCards.Count; i++)
        {
            if (FindFormulaCardData(d.formulaCards[i]) == null)
            {
                Debug.LogWarning($"[RunSave] 填空卡资产不存在：{d.formulaCards[i]}" +
                                 "（若为新加入的初始公式卡，请确认它挂在 CardManager.starterFormulaCards 上）");
                return false;
            }
        }

        // 手牌引用
        if (d.handInventoryIndices == null) return false;
        foreach (int idx in d.handInventoryIndices)
        {
            if (idx < 0 || idx >= d.numberCards.Count) return false;
        }

        // 公式槽位
        FormulaCardData formula = string.IsNullOrEmpty(d.currentFormulaCard)
            ? null : FindFormulaCardData(d.currentFormulaCard);
        if (!string.IsNullOrEmpty(d.currentFormulaCard) && formula == null)
        {
            Debug.LogWarning($"[RunSave] 公式卡资产不存在：{d.currentFormulaCard}");
            return false;
        }

        if (d.selectedSlotHandIndices == null) return false;
        if (formula == null && d.selectedSlotHandIndices.Count > 0) return false;

        // 未结算时允许"一张都还没放"（列表为空）或"放了一部分"，
        // 只要不超过槽位总数即为合法——旧实现要求数量必须恰好等于 RequiredCount，
        // 导致"抽完牌还没摆就退出"的存档被误判作废。
        if (formula != null && d.selectedSlotHandIndices.Count > formula.RequiredCount) return false;

        HashSet<int> usedHand = new HashSet<int>();
        foreach (int slotIdx in d.selectedSlotHandIndices)
        {
            if (slotIdx >= 0)
            {
                if (slotIdx >= d.handInventoryIndices.Count) return false;
                if (!usedHand.Add(slotIdx)) return false; // 同一张手牌不能占两个槽位
            }
        }

        // 结算待完成状态下必须已填满所有槽位（否则无法复算本次结算）
        if (d.sceneState == 2)
        {
            if (formula == null) return false;
            if (d.selectedSlotHandIndices.Count != formula.RequiredCount) return false;
            foreach (int slotIdx in d.selectedSlotHandIndices)
            {
                if (slotIdx < 0) return false;
            }
        }

        // 祝福
        if (d.ownedBlessings == null || d.blessingsEverPurchased == null) return false;
        HashSet<int> seenIds = new HashSet<int>();
        foreach (SavedIntPair pair in d.ownedBlessings)
        {
            if (pair.v < 1 || pair.v > 10000) return false;
            if (!seenIds.Add(pair.k)) return false; // 重复 id
            if (FindBlessingData(pair.k) == null)
            {
                Debug.LogWarning($"[RunSave] 祝福 id 不存在：{pair.k}");
                return false;
            }
        }
        foreach (int id in d.blessingsEverPurchased)
        {
            if (FindBlessingData(id) == null) return false;
        }

        // 商店商品
        if (d.shopNumberCards == null || d.shopFormulaCards == null || d.shopBlessings == null) return false;
        foreach (SavedShopNumberItem item in d.shopNumberCards)
        {
            if (item == null) return false;
            if (!string.IsNullOrEmpty(item.card) && FindNumberCardData(item.card) == null) return false;
            if (!IsNonNegativeIntegerString(item.price)) return false;
        }
        foreach (SavedShopFormulaItem item in d.shopFormulaCards)
        {
            if (item == null) return false;
            if (!string.IsNullOrEmpty(item.card) && FindFormulaCardData(item.card) == null) return false;
            if (!IsNonNegativeIntegerString(item.price)) return false;
        }
        foreach (SavedShopBlessingItem item in d.shopBlessings)
        {
            if (item == null) return false;
            if (item.blessingId != 0 && FindBlessingData(item.blessingId) == null) return false;
            // 祝福价格是 BigInteger（24 亿经济体系下很容易超过 long），必须按大整数解析
            if (!IsNonNegativeIntegerString(item.price)) return false;
        }

        // 槽位数与解锁次数的合理性
        if (d.numberCardCount < 0 || d.formulaCardCount < 0 || d.blessingCardCount < 0) return false;
        if (d.refreshCount < 0 || d.shopRefreshCount < 0) return false;
        if (d.totalRemovedNumberCards < 0 || d.totalRemovedFormulaCards < 0) return false;

        return true;
    }

    /// <summary>严格校验"十进制非负整数字符串"（用于价格等 BigInteger 字段）</summary>
    private static bool IsNonNegativeIntegerString(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        BigInteger parsed;
        if (!BigInteger.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            return false;
        }
        return parsed >= BigInteger.Zero;
    }

    #endregion
}
