# 24亿 项目长期备忘

## 数据体系（关键）
- 运行时祝福数据来自 Assets/prefabs/Blessing/*.asset（每祝福一个 SO）+ prefabs/Library/BlessingLibrary.asset 引用列表。BlessingLibrary.InitializeDefaultBlessings 是死代码（仅 ContextMenu），改代码里的祝福名/描述/类型不影响运行时。
- 资产 ID ≠ 代码库 ID：资产 49=暗箱操作 50=绝对正义 51=幸运星 52=福星 53=流星(代码无实现) 54=财星 55=祸星 56=慈爱星 57=启明星 58=大七星；类型枚举一致且唯一，跨 ID 逻辑一律按 blessingType 匹配。
- 数字卡/商店/背包共享 NumberCardLibrary 的 ScriptableObject 资产——**玩家卡牌的权威数值是实例的 `currentA/currentB`**（未被序列化，`BindInstance` 就是读它显示；手牌/牌堆/结算/黄金倍率全用它），库资产的 `partA.value` 只作初始值与**商店显示**。因此"让某张卡的数值增加"（福星 +1 黄金数、金融专家递增、慈爱星 +1…）一律改 `currentA/currentB`，绝不写 `cardData`——写 cardData 会连商店同款卡一起变。
- 递增判定统一走 `NumberCardInstance.CanIncrement(component)`：绿色数字恒递增；拥有金融专家时黄金数字也能递增（祝福资产描述原文是"黄金数字也能递增"）。节节高/势如破竹/能量扩散/慈爱星的文案都写"绿色数字"，故只对 `isIncremental` 生效，不含黄金。

## 用户约定
- Unity UI 优先用户在 Inspector 手工配置拖引用，避免代码生成 UI。
- 价格公式以用户提供的四步规范为准（期望→倍率修正→黄金数 100y(y+1)→舍入 5 的倍数+3 位有效数字+取绝对值）。

## 商店
- 刷新费公式：i² × 2^(n-1)，n = **本次商店（本回合）的刷新次数**，用 `shopRefreshCount`（每次 OpenShop 清零）；`refreshCount` 是全局累计，只给"走马观花"用，不得用于刷新费。丰盈宝库=永久免费（费用显示"免费"）。
- 删卡费公式（用户口径）：数字卡 `10 × 5^(已删数字卡数)`（10/50/250…），填空卡 `200 × 5^(已删填空卡数)`（200/1000/5000…）；两类计数独立。函数：`GetNextNumberCardDeletionCost()` / `GetNextFormulaCardDeletionCost()`。再乘难度系数 CardDeletionPrice（默认 1.0x）。

## 工程校验（离线编译 + 回归）
- 校验工程在 `D:\unity\hc_check`：`dotnet build` 编译 `Assets/scripts/**/*.cs`（引用 Unity 2022.3.62f3c1 的 UnityEngine.*.dll + Library/ScriptAssemblies 的 UnityEngine.UI/Unity.Burst/Unity.VisualScripting.*），`Program.cs` 用反射直接调用工程里的真实方法跑回归。改完脚本务必先跑一遍——编译错误（如 BigInteger×float 的 CS0019）会让整套脚本失效。
- 造 MonoBehaviour 假实例：`RuntimeHelpers.GetUninitializedObject` + 反射把 `UnityEngine.Object` 的 IntPtr 字段设非 0，否则 Unity 的 `operator==` 视其为 null；`Debug.Log*` 离线会抛 SecurityException，含 Debug 的路径不能测。
- 数字卡假资产同理：`(NumberCardData)GetUninitializedObject(typeof(NumberCardData))` + FakeMakeAlive，否则 NumberCardInstance 构造函数的 `cardData == null` 保护分支会走 Debug.LogError 而抛异常。

