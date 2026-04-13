using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

[System.Serializable]
public class SavingData
{
    [Header("普通模式")]
    public BigInteger TotalPointsN = 0;     //普通模式最大点数
    public float RateN = 0;                 //最大倍率
    public BigInteger NumbercardPointN = 0; //最大数字卡点数
    public BigInteger CalculationPointN = 0;//最大结算点数
    [Header("内卷模式")]
    public BigInteger TotalPointsI = 0;     
    public float RateI = 0;
    public BigInteger NumbercardPointI = 0;
    public BigInteger CalculationPointI = 0;

    public int accomplishTimes = 0;         //通关次数
    public int MaxPoint = 0;                //最大点数


}
