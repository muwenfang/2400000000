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
- `Data/RunSaveData.cs`（DTO）+ `Managers/RunSaveManager.cs`（静态类，无需挂场景）。与 SavingData（历史纪录）独立。存档 persistentDataPath/runsave.dat，单档。**清档只在普通/内卷开新游戏入口（用户约定 2026-10-31）：胜负（WinGame）与无尽模式入口都不清档**；SaveRun 对 MainMenu/GameOver/GameLose 是 default 不写档，所以对局结束后存档原样保留。
- 保存时机：ReturnStartMenu + OnApplicationQuit 自动存；sceneState 0=未结算保留手牌 1=商店保留本次商品 2=结算动画期间退出。
- 结算检查点：CalculateProcessSequence 在 PrepareCardsForCalculation（骰子定格）后 `CaptureSettlementCheckpoint`；继续游戏走 `GameManager.ResumePendingSettlement` → `SettlementScoreSequence(cardManager,false)` 无动画补完结算（与正常结算同一确定性计算，防重投骰子/逃避负分）。StartPlayerTurn 清检查点。结算协程已重构为 CalculateProcessSequence / SettlementScoreSequence / ShowScoreStepByStep(带 playSettleAnimation 参数)。
- 恢复：各管理器 CaptureToSave/RestoreFromSave；商店恢复直接填列表（hc_check 守卫禁止重新抽卡）；库存恢复实例 currentA/currentB/骰子面数；祝福逐字段恢复累积值（totalMultiplierBonus 等不可重推导）+ blessingTypeCount/ownedBlessingInstance 重建。
- 防作弊：XOR+Base64 混淆 + SHA256(json+密钥) 校验 + 点数异或掩码冗余 + 不变量验证（版本/回合范围/资产存在/祝福 id/槽位引用/点数副本）。离线无法绝对防作弊，目标是拦"直接改档"。
- 主菜单"继续游戏"按钮（用户 Inspector 绑定）：`GameManager.ContinueRun()` / `HasContinueSave()`。FormulaCardUI.Slots 暴露槽位供视觉恢复（RunSaveManager.RestorePlacedCardsVisual）。
- **资产解析必须三源合并**（2026-10-29 修复）：卡库只是商店抽卡池，起始牌组 6 张（`Assets/prefabs/Library/2,3,4,5,{0},~6~.asset`）与初始公式卡 `StarterCards/InitialFormula.asset` 均不在 NumberCardLibrary/FormulaCardLibrary 里。`RunSaveManager.GetNumberCardIndex/GetFormulaCardIndex` = 卡库 ∪ `CardManager.starterNumberCards/starterFormulaCards` ∪ `Resources.FindObjectsOfTypeAll<T>()`，按 SO.name 建缓存（保存时 InvalidateAssetIndex 重建）。**任何时候按名字查卡都不许只扫卡库**。
- 校验三个历史误杀点：① 槽位列表要求恰好等于 RequiredCount（实际未放牌时为空列表）→ 改为"≤RequiredCount"，仅 sceneState=2 才强制填满；② 商店价格用 `long.TryParse`（祝福价是 BigInteger）→ 改 BigInteger 解析 `IsNonNegativeIntegerString`；③ ApplyRun 失败直接 ClearSave → 改为保留文件（校验已拦篡改，删档会连带销毁玩家进度）。
- 恢复顺序：先切面板（ChangeState / ShowPanel）再 RestoreTurnUI 重建手牌，避免布局组在未激活面板下算错尺寸；sceneState=2 不走 ChangeState，需显式 ShowPanel(gameUIPanel)。
- **未结算恢复一律退回手牌**（2026-10-29 定案）：`CardManager.RestoreFromSave` 只有 `d.sceneState == 2` 才按 `selectedSlotHandIndices` 还原槽位；sceneState=0/1 槽位全空，本回合抽到的所有数字卡重新在手牌区生成。`GameManager.RestoreTurnUI(bool restoreSlotPlacements=false)` 只在 true 时调 `RestorePlacedCardsVisual()`。历史缺陷：早期无条件还原槽位，把已摆放的卡从手牌区移走，重进只剩没摆的那几张。

## 结算时序（2026-10-29 重构，务必保持）
- 结算拆两层：`ComputeSettlementScore`（确定性数值，含财星/反刍/反物质云等一次性效果）+ `AddSettlementPoints`（坠落/正义修正 + AddPoints）+ `FinishSettlementAfterPoints`（隐藏气泡/鬼神境/`EndTurn` → OpenShop → 写档）。三者分别由 `settlementComputed / settlementPointsApplied / settlementFinished` 保护，**必须幂等**：退出补算与动画协程共用同一套逻辑。
- 骰子定格统一入口 `SettleDiceAndLastRoundMax`（`settlementDiceSettled` 保护），内部抓结算检查点；重复调用会二次投骰/二次递增，禁止绕过。
- 结算协程句柄存 `activeSettlementCoroutine`；协程每个等待点后必须有 `if (settlementAborted) yield break;`（StopCoroutine 只停外层，嵌套协程靠这个标记停手）。
- 退出（`ReturnStartMenu` / `OnApplicationQuit`）若处于 Calculation → `FinishSettlementOnExit()`：置 abort + 停协程 + 当场补完结算 → 进商店 → `RunSaveManager.SaveSettledRun()`（sceneState=1）。继续游戏走商店恢复分支，商品/价格原样，**不重抽**。
- `FinishSettlementAfterPoints` 里结算完成即落盘（每个结算点=自动存档点），且仅在 `currentState == GameState.Shop` 时写；补算触发胜负时状态已切 GameOver/GameLose，不会写档，存档保持不动（WinGame 不再 ClearSave）。
- 历史缺陷：`ReturnStartMenu` 只切面板不停协程 → 动画跑完后 `OpenShop` + `ChangeState(Shop)` 把"新抽的商店"盖在主菜单上。

## 已知数据瑕疵（未改，待确认）
- 123/124 卡名都是 0^$50$（124 黄金实为 100）；118/120 都是 10$5$（120 底数实为 100）。
