# 24亿 项目长期备忘

## 工程校验（改完脚本必跑）
- `cd D:\unity\hc_check && PROGRAMDATA="C:/ProgramData" APPDATA="C:/Users/Muwen/AppData/Roaming" ALLUSERSPROFILE="C:/ProgramData" dotnet build --no-restore && dotnet bin/Debug/net10.0/hc_check.dll`
- 只认 net10.0；禁止 --restore / msbuild / csc，只能 dotnet build。Program.cs 反射跑回归。
- 假 MonoBehaviour/SO：GetUninitializedObject + m_CachedPtr≠0；互相比较的假对象 m_InstanceID 各非 0 且不同（离线 == 比 InstanceID）。
- Debug.Log*/Random.Range 离线抛 SecurityException；含 Debug 的方法包 try/catch 后断言副作用。
- 同一消息对同一文件发多个 Edit 只有最后一个落盘——串行改，改完 grep 确认。

## 数据体系
- 祝福资产 Assets/prefabs/Blessing/*.asset + BlessingLibrary.allBlessings 引用列表（漏引用=永不出现）；资产 ID≠代码库 ID，跨 ID 逻辑按 blessingType 匹配。
- 卡牌权威数值=实例 currentA/currentB（未序列化）；库资产 partA.value 只作初始值+商店显示，绝不写 cardData。
- 卡牌持久标识=SO.name（资产文件名，唯一）；cardName 有重复（123/124、118/120）勿当 key。
- 颜色唯一入口 NumberCardColors + NumberCardColorPalette.asset；优先级 黄金>骰子>递增>普通。SO 类必须与文件名同名才能序列化。

## 用户约定
- Unity UI 优先 Inspector 手工拖引用，避免代码生成 UI。
- 价格公式四步：带符号期望（X=|整体|，禁止提前 Abs）→ 倍率修正 → 黄金 100y(y+1) → 舍入 5 倍数+3 有效位；幂用 SignedPow；价格链 ToLongSaturated/SaturatingAdd 防溢出。

## 商店
- 刷新费 i²×2^(n-1)，n=shopRefreshCount（OpenShop 清零）；refreshCount 全局累计。
- 祝福池 refreshBehavior 0=总是/1=永不/2=本回合；"出现"只认可见槽位 index<blessingCardCount；统计只在开新局重置；祸星在 OpenShop 首次生成后排除。

## 祝福
- 唯一入口 BlessingManager.AcquireBlessing（记录更新在触发效果之前）。
- "下一回合生效"：xxxPendingCharges 购买累加 → 下回合结算入口消费；移除层同步扣减。
- 纯粹容器=容器顶替付款（ResolvePurchasePayment 扣费前决定）；标价统一 GetEffectiveBlessingPrice。
- 实用主义入口 PlayerCardInventory.AddFormulaCard；直接改 formulaCards 后必须 NotifyCardValueChanged()。

## 无尽模式
- GetEndlessRoundCost = 24亿×10^(round-61)（<61 按 61）；EnterEndlessMode 只设标记+round=61 不做初始化。

## 局内存档
- RunSaveData + RunSaveManager（静态类），runsave.dat 单档。清档只在普通/内卷开新游戏入口；胜负/无尽入口不清档。
- sceneState 0=未结算留手牌 1=商店留商品 2=结算动画中退出；保存时机 ReturnStartMenu/OnApplicationQuit + 每个结算完成点。
- 继续游戏 GameManager.ContinueRun()；未结算走 ResumePendingSettlement 补完（同一确定性计算，防逃避负分）；sceneState≠2 一律退回手牌（RestoreTurnUI(false)）。
- 资产解析三源合并：卡库 ∪ starterNumberCards/starterFormulaCards ∪ Resources.FindObjectsOfTypeAll，按 SO.name；按名查卡不许只扫卡库。
- 校验已修误杀点：槽位≤RequiredCount；祝福价 BigInteger 解析；ApplyRun 失败保留文件不删档。

## 结算时序（务必保持）
- ComputeSettlementScore + AddSettlementPoints + FinishSettlementAfterPoints 三层，幂等保护 settlementComputed/Applied/Finished。
- 骰子定格统一入口 SettleDiceAndLastRoundMax（重复调用会二次投骰）；协程每个等待点后 if(settlementAborted) yield break。
- 退出处于 Calculation → FinishSettlementOnExit()：补完结算→进商店→SaveSettledRun(sceneState=1)；仅 currentState==Shop 时写档。

## 图鉴（获得即解锁 + cardcodex.json 永不重置）
- 唯一入口 PlayerCardInventory.AddNumberCard/AddFormulaCard 与 BlessingManager.AcquireBlessing；解锁调用放在功能性副作用之后，异常绝不中断主流程。
- 初始牌组豁免 unlockCodex:false；标识：卡用 SO.name、祝福用 blessingId；SavingData.unlocked* 仅作旧版迁移源。
- 面板 CardCodexPanelUI 三段独立 SectionRefs；未激活页签 LateUpdate 补建。
- 预制体根节点 0 缩放陷阱：Instantiate 后必须 SetActive(true)+localScale=Vector3.one（0 尺寸根另用挂点尺寸兜底），别当冗余删。

## 成就（2026-11-16 建，19 项 + achievements.json 永不重置）
- id =《成就.xlsx》表格行序（0 起）；解锁唯一出口 UnlockAchievement（幂等+立即落盘+级联"感谢陪伴"）。
- 普通模式通关检测（WinGame(true) 且 !isInvolution&&!isEndless，快照在切状态前采集）：0 感谢游玩/1 难度≥50/2 难度≥100/9 零祝福/10 虚无≥50/11 全填空卡各一张/14 暗箱/15 数字卡≥50/16 祝福≥100/17 全含~20~。
- 全程记录：3 普通失败≥3（无尽/内卷失败不计）/4 无尽(源自普通)≥75 回合（StartPlayerTurn 检测，续档可补判）/5 内卷开局≥5/6 连续两次纯粹容器（AcquireBlessing 6.5 步）/7 许愿币选中许愿币/8 获得 cardName '{114}*{514}'（资产 083）/12 一回合刷新商店≥50（shopRefreshCount）/13 理财大师≥50。
- 关键口径：暗箱价格用 GetEffectiveBlessingPrice（0 价纯粹容器顶替全部祝福才能凑 30 个非正价）；空想主义种类 = FormulaCardLibrary ∪ starterFormulaCards（含 InitialFormula）每种恰 1。
- InitializeGame 现已设置 currentGameMode（此前从未赋值）；InitializeGame_invol 计 RecordInvolutionPlay。
- 持久化镜像 CardCodexManager：AchievementProgressData.cs（纯 C#）+ achievements.json + ioUnavailable 兜底；hc_check TestAchievements 85 项（总 393）。
- 解锁提示（2026-11-23）：AchievementToastManager（toastPrefab+toastContainer 拖引用，队列 spawnInterval 逐条、maxVisible 背压、有 LayoutGroup 交布局组、无则 BuildStackOffsets 手工堆叠）+ AchievementToastUI（单条，仅填 nameText，弹入→停留→淡出走 unscaledTime，播完回调销毁）。
