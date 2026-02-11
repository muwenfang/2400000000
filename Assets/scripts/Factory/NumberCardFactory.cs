using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class NumberCardFactory
{ 
    public static NumberCardInstance GenerateRandomCard()
    {
        NumberCardData data = ScriptableObject.CreateInstance<NumberCardData>();

        data.partA = new NumberComponent();
        data.partB = new NumberComponent();

        float rand = Random.value;//0~1之间的随机数

        if (rand < 0.25f)
        {
            data.logicalType = NumberCardData.LogicalType.Addition;
            data.layoutType = NumberCardLayoutType.Add_AB; // 对应 a + b
        }
        else if (rand < 0.5f)
        {
            data.logicalType = NumberCardData.LogicalType.Multiplication;
            data.layoutType = NumberCardLayoutType.Multiply_AB; // 对应 a × b
        }
        else if (rand < 0.75f)
        {
            data.logicalType = NumberCardData.LogicalType.Power;
            data.layoutType = NumberCardLayoutType.Composite_AB; // 对应 a ^ b (新增枚举)
        }
        else
        {
            data.logicalType = NumberCardData.LogicalType.Normal;
            data.layoutType = NumberCardLayoutType.Single; // 对应 a
        }
        SetupNumberComponent(data.partA);
        SetupNumberComponent(data.partB);

        return new NumberCardInstance(data);

    }

    private static void SetupNumberComponent(NumberComponent comp)
    {
        float typeRand = Random.value;

        if (typeRand < 0.2f) // 20% 骰子
        {
            comp.isDice = true;
            //面数
            comp.diceSides = DiceHelper.GetMaxSide();
        }
        else if (typeRand < 0.4f) // 20% 绿色数字
        {
            comp.isIncremental = true;
            comp.value = Random.Range(1, 5); // 初始 a 值
        }
        else // 60% 普通数字
        {
            comp.value = Random.Range(1, 10);
        }

    }

    public static class DiceHelper
    {
        private static readonly int[] DiceSides = { 4, 6, 8, 12, 20 };
        //获取最大面数
        public static int GetMaxSide()
        {
            int index = Random.Range(0, DiceSides.Length);//随机选择一个骰子面数
            return DiceSides[index];
        }

        public static int RollDice(int sides)
        {      
            return Random.Range(1, sides + 1);
            //掷骰子，返回1到max之间的随机数
            //这个是暂定的，之后会改
        }

        //ui
        public static string GetDiceName(int sides)
        {
            return "D" + sides.ToString();

        }

    }

}
