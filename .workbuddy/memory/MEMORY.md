# 24亿 项目长期备忘

## 工程校验（离线编译 + 回归）
- 校验工程 `D:\unity\hc_check`：`dotnet build` 编译 Assets/scripts/**/*.cs，Program.cs 反射调用真实方法跑回归（2026-10-25：225 项）。改完脚本必跑。
- 可行命令（本机 NuGet 还原挂掉，禁止 --restore）：
  `cd D:\unity\hc_check && PROGRAMDATA="C:/ProgramData" APPDATA="C:/Users/Muwen/AppData/Roaming" ALLUSERSPROFILE="C:/ProgramData" dotnet build --no-restore && dotnet bin/Debug/net10.0/hc_check.dll`
- 不要改 TargetFramework（只认 net10.0）；`dotnet msbuild`/`csc` 被拦，只能 `dotnet build`。
- 造 MonoBehaviour 假实例：`GetUninitializedObject` + 反射设 m_CachedPtr≠0；要互相比较的假对象 m_InstanceID 必须各非 0 且不同（离线 == 比 InstanceID）。假 SO 资产同理（否则构造函数 Debug.LogError 分支抛异常）。
- `Debug.Log*`/`UnityEngine.Random.Range` 离线抛 SecurityException；含 Debug 的方法包 try/catch 后断言副作用。
- 同一消息对同一文件发多个 Edit 只有最后一个落盘——必须串行改，改完 grep 确认。

## 数据体系（关键）
- 运行时祝福 = Assets/prefabs/Blessing/*.asset + BlessingLibrary.allBlessings 引用列表（漏引用=永不出现）。资产 ID≠代码库 ID，跨 ID 逻辑按 blessingType 匹配。
- 玩家卡牌权威数值是**实例 currentA/currentB**（未序列化），库资产 partA.value 只作初始值+商店显示；改数值一律改实例，绝不写 cardData。
- 递增判定 `NumberCardInstance.CanIncrement`：绿色恒递增；金融专家使黄金也能递增。
- 数字卡颜色唯一入口 `NumberCardColors` + 单一资产 `NumberCardColorPalette.asset`；优先级 黄金>骰子>递增>普通。SO 类必须与文件名同名才能序列化。
- 卡牌资产持久标识 = **SO.name（资产文件名，唯一）**；cardName 有重复（123/124 都是 0^$50$、118/120 都是 10$5$），勿用 cardName 当 key。

## 用户约定
- Unity UI 优先 Inspector 手工拖引用，避免代码生成 UI。
- 价格公式四步：带符号期望（X=|整体|，禁止逐项/提前 Abs）→ 倍率修正 → 黄金 100y(y+1) → 舍入 5 的倍数+3 位有效数字。指数卡 9 种组合（第 9 种无随机成分直接 SignedPow）。幂用 `SignedPow`（勿 Math.Pow）。价格链用 `ToLongSaturated`/`SaturatingAdd` 防溢出。

## 商店
- 刷新费 i²×2^(n-1)，n=`shopRefreshCount`（每次 OpenShop 清零）；`refreshCount` 全局累计只给走马观花。
- 删卡费：数字卡 10×5^n、填空卡 200×5^n（独立计数），再乘难度系数。
- 祝福池：blessingId 必须唯一；refreshBehavior 0=总是/1=永不/2=本回合；AlwaysRefresh 保底权重 `DrawWeightedBlessingFromPool`（streak≥8 线性加权，上限 30）；**"出现"只认可见槽位 index<blessingCardCount**；统计只在开新局重置（InitializeShop/EnterEndlessMode）。祸星在 OpenShop 首次生成后排除（MarkDisasterStarExcluded 放 GenerateBlessings 之后）。GenerateBlessings 的剔除逻辑必须在 Clear 前快照 previouslyDisplayed。

## 祝福
- 获得祝福唯一入口 `BlessingManager.AcquireBlessing`（维护计数/已购/好事成双记录，记录更新在触发效果之前）。
- "下一回合生效"统一模式：xxxPendingCharges 购买累加 → 下一回合结算入口消费（财星/流星/慈爱星 lastAppliedCompassionStarCount 差值）；移除层在 DecreaseStarCountVariable 同步扣减。
- 纯粹容器=容器顶替付款：`ResolvePurchasePayment` 扣费前决定；商店标价统一 `GetEffectiveBlessingPrice`。
- 实用主义强制入口 `PlayerCardInventory.AddFormulaCard`；直接改 formulaCards 后必须 `NotifyCardValueChanged()`。

## 无尽模式
- 每回合扣点 `GetEndlessRoundCost` = 24亿×10^(round-61)（<61 按 61）；UIManager.GetCurrentStageRequirement 无尽时必须走它。
- EnterEndlessMode 只设标记+round=61，不做初始化。

## 局内存档（2026-10-25 建）
- `Data/RunSaveData.cs`（DTO）+ `Managers/RunSaveManager.cs`（静态类，无需挂场景）。与 SavingData（历史纪录）独立。存档 persistentDataPath/runsave.dat，单档，新游戏自动覆盖（ClearSave 挂在三个开新局入口 + WinGame）。
- 保存时机：ReturnStartMenu + OnApplicationQuit 自动存；sceneState 0=未结算保留手牌 1=商店保留本次商品 2=结算动画期间退出。
- 结算检查点：CalculateProcessSequence 在 PrepareCardsForCalculation（骰子定格）后 `CaptureSettlementCheckpoint`；继续游戏走 `GameManager.ResumePendingSettlement` → `SettlementScoreSequence(cardManager,false)` 无动画补完结算（与正常结算同一确定性计算，防重投骰子/逃避负分）。StartPlayerTurn 清检查点。结算协程已重构为 CalculateProcessSequence / SettlementScoreSequence / ShowScoreStepByStep(带 playSettleAnimation 参数)。
- 恢复：各管理器 CaptureToSave/RestoreFromSave；商店恢复直接填列表（hc_check 守卫禁止重新抽卡）；库存恢复实例 currentA/currentB/骰子面数；祝福逐字段恢复累积值（totalMultiplierBonus 等不可重推导）+ blessingTypeCount/ownedBlessingInstance 重建。
- 防作弊：XOR+Base64 混淆 + SHA256(json+密钥) 校验 + 点数异或掩码冗余 + 不变量验证（版本/回合范围/资产存在/祝福 id/槽位引用/点数副本）。离线无法绝对防作弊，目标是拦"直接改档"。
- 主菜单"继续游戏"按钮（用户 Inspector 绑定）：`GameManager.ContinueRun()` / `HasContinueSave()`。FormulaCardUI.Slots 暴露槽位供视觉恢复（RunSaveManager.RestorePlacedCardsVisual）。

## 已知数据瑕疵（未改，待确认）
- 123/124 卡名都是 0^$50$（124 黄金实为 100）；118/120 都是 10$5$（120 底数实为 100）。
