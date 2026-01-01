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

        if (rand < 0.33f)//加法卡
        {
            data.logicalType = NumberCardData.LogicalType.Addition;

        }
        else if (rand < 0.66f)//乘法卡
        {
            data.logicalType = NumberCardData.LogicalType.Multiplication;

        }
        else//指数卡
        {
            data.logicalType = NumberCardData.LogicalType.Power;
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
        //价格根据复杂度调整
        //[to do]
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
            sides = GetMaxSide();//获取骰子最大面数
            return Random.Range(1, sides + 1);
            //掷骰子，返回1到max之间的随机数
            //这个是暂定的，之后会改
        }

        //ui
        public static string GetDiceName(int currentVal)
        {
            int sides = GetMaxSide();
            return "D" + sides.ToString();

        }

    }

}
