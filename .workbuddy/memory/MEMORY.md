# 24亿 项目长期备忘

## 数据体系（关键）
- 运行时祝福数据来自 Assets/prefabs/Blessing/*.asset（每祝福一个 SO）+ prefabs/Library/BlessingLibrary.asset 引用列表。BlessingLibrary.InitializeDefaultBlessings 是死代码（仅 ContextMenu），改代码里的祝福名/描述/类型不影响运行时。
- 资产 ID ≠ 代码库 ID：资产 49=暗箱操作 50=绝对正义 51=幸运星 52=福星 53=流星 54=财星 55=祸星 56=慈爱星 57=启明星 58=大七星 59=金融专家 61=赌场专员；类型枚举一致且唯一，跨 ID 逻辑一律按 blessingType 匹配。
- 数字卡/商店/背包共享 NumberCardLibrary 的 ScriptableObject 资产——**玩家卡牌的权威数值是实例的 `currentA/currentB`**（未被序列化，`BindInstance` 就是读它显示；手牌/牌堆/结算/黄金倍率全用它），库资产的 `partA.value` 只作初始值与**商店显示**。因此"让某张卡的数值增加"（福星 +1 黄金数、金融专家递增、慈爱星 +1…）一律改 `currentA/currentB`，绝不写 `cardData`——写 cardData 会连商店同款卡一起变。
- 递增判定统一走 `NumberCardInstance.CanIncrement(component)`：绿色数字恒递增；拥有金融专家时黄金数字也能递增（祝福资产描述原文是"黄金数字也能递增"）。节节高/势如破竹/能量扩散/慈爱星的文案都写"绿色数字"，故只对 `isIncremental` 生效，不含黄金。

## 用户约定
- Unity UI 优先用户在 Inspector 手工配置拖引用，避免代码生成 UI。
- 价格公式以用户提供的四步规范为准（期望→倍率修正→黄金数 100y(y+1)→舍入 5 的倍数+3 位有效数字+取绝对值）。
- 第一步期望必须**带符号**求和，绝对值只对"第一步的计算结果"整体取一次（`X = Math.Abs(expectation)`，在 `GetNumberCardPrice`）：`2^{0}` → x=(2+4+…+2^9)/9；`(-2)^{0}` → x=|-2+4-8+…+(-2)^9|/9。**禁止**对底数/指数/每一项提前 `Math.Abs`（那会让负底数卡与正底数卡算出同一个价格）。资产 089–099 号就是负底数卡（`(-2)^~6~`、`(-3)^~20~`…）。
- 幂运算用 `NumberCardInstance.SignedPow(底数, 指数)`（整数指数走连乘），不要直接用 `Math.Pow`——负底数 + 整数指数在部分运行时可能返回 NaN。

## 商店
- 刷新费公式：i² × 2^(n-1)，n = **本次商店（本回合）的刷新次数**，用 `shopRefreshCount`（每次 OpenShop 清零）；`refreshCount` 是全局累计，只给"走马观花"用，不得用于刷新费。丰盈宝库=永久免费（费用显示"免费"）。
- 删卡费公式（用户口径）：数字卡 `10 × 5^(已删数字卡数)`（10/50/250…），填空卡 `200 × 5^(已删填空卡数)`（200/1000/5000…）；两类计数独立。函数：`GetNextNumberCardDeletionCost()` / `GetNextFormulaCardDeletionCost()`。再乘难度系数 CardDeletionPrice（默认 1.0x）。

## 祝福：一次性 / "下一回合生效" 类
- 统一模式：`xxxPendingCharges`（购买时累加）→ 在下一回合的结算入口取走并清零（财星 `ConsumeWealthStarCharges`、流星 `ConsumeMeteorCharges`、慈爱星用 `compassionStarCount - lastAppliedCompassionStarCount`）。无论当回合是否满足触发条件都要消费（严格"仅下一回合"，不残留到以后）。
- 移除祝福层时必须在 `DecreaseStarCountVariable`（按 blessingType）里同步扣减待生效次数，否则会出现"祝福没了还生效"。
- 流星（可叠加）：`CardManager.PrepareCardsForCalculation` 在结算前给含骰子的卡打 `forceMaxDiceOnce`，由 `NumberCardInstance.PrepareForCalculation` 的骰子分支把该次判定直接置为当前面数（`currentDiceSidesX`）。副作用（赌为赢/大成功/赌场专员/唯心主义）因此自然联动，无需重复实现。
- 实用主义（不可叠加，仅保留价值最高的填空卡）：强制入口放在 `PlayerCardInventory.AddFormulaCard`（商店购买/多多益善/空想主义全部汇聚于此，防止再漏路径）；`ApplyPragmatismEffect` 直接改 `formulaCards` 列表后必须 `NotifyCardValueChanged()`，否则 `ShowMyFormula` 的 `InventoryVersion` 脏检查会跳过重建、界面残留已删的卡。

## 无尽模式
- 每回合扣点 = `GameManager.GetEndlessRoundCost(round)` = 24亿 × 10^(round-61)（round<61 时按 61 计，避免 `BigInteger.Pow` 负指数抛异常）。第61回合 24亿，之后每回合 ×10。
- `UIManager.GetCurrentStageRequirement()` 无尽时必须改调该函数：旧实现只查阶段表，round>60 恒返回 targetPoints=24亿，HUD 看不出递增。
- `EnterEndlessMode()`（场景按钮绑定）只设 `isEndlessMode` 与 `currentRound=61` 后直接 StartPlayerTurn，**不做点数/卡组/祝福初始化**——从主菜单进入会是 0 点 + 空卡组。

## 工程校验（离线编译 + 回归）
- 校验工程在 `D:\unity\hc_check`：`dotnet build` 编译 `Assets/scripts/**/*.cs`（引用 Unity 2022.3.62f3c1 的 UnityEngine.*.dll + Library/ScriptAssemblies 的 UnityEngine.UI/Unity.Burst/Unity.VisualScripting.*），`Program.cs` 用反射直接调用工程里的真实方法跑回归。改完脚本务必先跑一遍——编译错误（如 BigInteger×float 的 CS0019）会让整套脚本失效。
- 造 MonoBehaviour 假实例：`RuntimeHelpers.GetUninitializedObject` + 反射把 `UnityEngine.Object` 的 IntPtr 字段设非 0，否则 Unity 的 `operator==` 视其为 null；`Debug.Log*` 与 `UnityEngine.Random.Range` 离线都会抛 SecurityException（ECall），随机判定路径不可测。
- 含 Debug 的方法仍可测：把调用包进 try/catch（副作用都在日志之前完成），事后再断言列表/计数/`InventoryVersion`。
- 数字卡假资产同理：`(NumberCardData)GetUninitializedObject(typeof(NumberCardData))` + FakeMakeAlive，否则 NumberCardInstance 构造函数的 `cardData == null` 保护分支会走 Debug.LogError 而抛异常。
- 同一条消息里对**同一文件**发多个 Edit，只有最后一个会落盘（其余照样报 success）——必须串行改，改完 grep 确认声明行存在。
