﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class EliminateManager : MonoBehaviour
{
    Text[] tex_psbNum = new Text[9];
    Text tex_absNum;
    TileBase[] baseTiles;
    Tilemap tm;

    Dictionary<Vector3Int, bool[]> psb_Nums = new Dictionary<Vector3Int, bool[]>();//通过坐标列出格子所有可能的数字
    Dictionary<Vector3Int, int> abs_Num = new Dictionary<Vector3Int, int>(); //通过坐标列出格子的绝对数字
    Dictionary<Vector3Int, bool> isFront = new Dictionary<Vector3Int, bool>();//通过坐标判断格子是否是手动编辑的格子
    Vector3Int[] dires = new Vector3Int[9];

    private void Awake()
    {
        tm = GameObject.Find("Grid/Tilemap").GetComponent<Tilemap>();
        baseTiles = VarsManager.GetVars().baseTiles;
        dires = VarsManager.GetVars().dires;
        Transform psbNumList = GameObject.Find("Canvas/Tex_PSB_Num").transform;
        for (int i = 0; i < psbNumList.childCount; i++)
            tex_psbNum[i] = psbNumList.GetChild(i).gameObject.GetComponent<Text>();
        tex_absNum = GameObject.Find("Canvas/Tex_ABS_Num/Tex_Num").GetComponent<Text>();

        EventCenter.AddListener(EventDefine.Eliminate, Eliminate);
        InstanceGrid();
        Eliminate();//简化
    }

    private void Update()
    {
        for (int i = 0; i < 9; i++)
            tex_psbNum[i].gameObject.SetActive(false);
        tex_absNum.text = string.Empty;

        Vector3Int cellPos = GetCellPos();
        if (cellPos != Vector3Int.zero)//返回的不是假坐标
        {
            for (int i = 1; i < 10; i++)
            {
                if (psb_Nums[cellPos][i])//输出可能数
                {
                    //print(i.ToString());
                    tex_psbNum[i - 1].gameObject.SetActive(true);
                }
            }
            if (abs_Num[cellPos] != -1)
            {
                tex_absNum.text = abs_Num[cellPos].ToString();
            }
        }
    }
    /// <summary> 初始化所有格子信息 </summary>
    public void InstanceGrid()
    {
        psb_Nums.Clear();
        abs_Num.Clear();
        List<bool> allTrue = new List<bool>();
        for (int i = 0; i < 10; i++)//bool从1-9（1对应数字1,9对应数字9)
            allTrue.Add(true);
        //填满可能数
        for (int y = 1; y < 10; y++)
            for (int x = 1; x < 10; x++)
                psb_Nums.Add(new Vector3Int(x, y, 0), allTrue.ToArray());
        //填满绝对数（全部为假值）
        for (int y = 1; y < 10; y++)
            for (int x = 1; x < 10; x++)
                abs_Num[new Vector3Int(x, y, 0)] = -1;
        //判断格子是否为手动填写的数字
        for (int y = 1; y < 10; y++)
            for (int x = 1; x < 10; x++)
                if (GetCellNum(new Vector3Int(x, y, 0)) != -1)
                    isFront[new Vector3Int(x, y, 0)] = true;
                else
                    isFront[new Vector3Int(x, y, 0)] = false;
    }
    /// <summary> 根据绝对数来排除cellPos坐标对应格子的可能数字 </summary>
    private void EliminateNumWithAbs(Vector3Int cellPos)
    {
        //psb_Num[new Vector3Int(2, 1, 0)][5] = false;//排除坐标(2,1)格子为5的可能
        //从行判断
        for (int x = 1; x < 10; x++)//x:1--9
        {
            int absNum = GetCellNum(new Vector3Int(x, cellPos.y, 0));
            if (absNum != -1)//返回的不是假值
                psb_Nums[cellPos][absNum] = false;//将数字排除掉
        }
        //从列判断
        for (int y = 1; y < 10; y++)//y:1--9
        {
            int absNum = GetCellNum(new Vector3Int(cellPos.x, y, 0));
            if (absNum != -1)//返回的不是假值
                psb_Nums[cellPos][absNum] = false;//将数字排除掉
        }
        //从宫里判断
        Vector3Int centrolPos = GetCentrolPos(cellPos);
        foreach (Vector3Int dire in dires)
        {
            int roundNum = GetCellNum(dire + centrolPos);//周围的格子
            if (roundNum != -1)
                psb_Nums[cellPos][roundNum] = false;//在当前格排除可能数字
        }
        //从本身是否为数字进行判断
        int num = GetCellNum(cellPos);
        if (num != -1)//不为空格子
            for (int i = 1; i < 10; i++)
                psb_Nums[cellPos][i] = false;
    }
    /// <summary> 根据可能数来排除cellPos坐标对应格子的可能数字 </summary>
    private void EliminateNumWithPsb(Vector3Int cellPos)
    {
        //从行排除
        for (int i = 1; i < 10; i++)
        {
            if (psb_Nums[cellPos][i])//遍历当前格子的数字
            {
                int count = 0;//与当前数字重复的次数
                for (int x = 1; x < 10; x++)//坐标
                {
                    if (psb_Nums[new Vector3Int(x, cellPos.y, 0)][i] && x != cellPos.x)//与其他格子的可能数出现重复
                        count++;
                }
                if (count == 0)//没有重复
                {
                    SetGridAbs(cellPos, i);
                    return;//已经排除，无需再做其他判断
                }
            }
        }
        //从列排除
        for (int i = 1; i < 10; i++)
        {
            if (psb_Nums[cellPos][i])//遍历当前格子的数字
            {
                int count = 0;//与当前数字重复的次数
                for (int y = 1; y < 10; y++)//坐标
                {
                    if (psb_Nums[new Vector3Int(cellPos.x, y, 0)][i] && y != cellPos.y)//检查是否与其他格子的可能数出现重复
                        count++;
                }
                if (count == 0)//没有重复
                {
                    //print(ceilPos);
                    SetGridAbs(cellPos, i);
                    return;
                }
            }
        }
        //从宫排除
        Vector3Int centrolPos = GetCentrolPos(cellPos);
        for (int i = 1; i < 10; i++)
        {
            if (psb_Nums[cellPos][i])//遍历当前格子的数字
            {
                int count = 0;//与当前数字重复的次数
                //TODO:与其他格子的可能数出现重复
                //坐标
                foreach (Vector3Int dire in dires)
                    if (psb_Nums[centrolPos + dire][i] && centrolPos + dire == cellPos)
                        count++;
                if (count == 0)//没有重复
                {
                    //print(ceilPos);
                    SetGridAbs(cellPos, i);
                    return;
                }
            }
        }
    }
    /// <summary> 更新cellPos坐标对应的绝对数字 </summary>
    private void AccuralizeNum(Vector3Int cellPos)
    {
        int psbCount = 0, value = 0;//可能数的个数，可能数
        for (int i = 1; i < 10; i++)//累计可能数的总个数,并记录当前可能数
            if (psb_Nums[cellPos][i])
            {
                psbCount++;
                value = i;
            }
        if (psbCount == 0)//当前格子本身就是数字
            abs_Num[cellPos] = GetCellNum(cellPos);//将当前格子对应的数字填入绝对数
        else if (psbCount == 1)//当前格子排除并只剩下1个可能数
            abs_Num[cellPos] = value;
    }
    /// <summary> 根据cellPos对应格子数据画入Tilemap </summary>
    private void DrawCell(Vector3Int cellPos)
    {
        //将绝对数画入Tilemap
        if (abs_Num[cellPos] != -1 && !isFront[cellPos])//目前坐标有绝对数且格子不为手动编辑
            tm.SetTile(cellPos, baseTiles[abs_Num[cellPos] - 1]);//将对应数字的格子画入对应坐标
    }

    /// <summary> 获取鼠标对应格子的坐标 </summary>
    public Vector3Int GetCellPos()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);//获取鼠标的世界坐标
        if (Physics2D.OverlapPoint(mousePos) != null)//鼠标点击到碰撞体
            //print(tm.GetTile(tm.WorldToCell(mousePos)).name);
            return tm.WorldToCell(mousePos);
        return Vector3Int.zero;//返回假值
    }
    /// <summary> 通过cellPos坐标返回该格子对应数字 </summary>
    public int GetCellNum(Vector3Int cellPos)
    {
        string gridName = tm.GetTile(cellPos).name;//通过坐标获取格子的名称
        if (gridName != "empty")//当前格子不是空格子
            return int.Parse(gridName);//返回格子所带有的数字
        else
            return -1;//返回假值-1
    }
    /// <summary> 设置cellPos坐标对应格子的绝对数 </summary>
    private void SetGridAbs(Vector3Int cellPos, int value)
    {
        //只需更改可能数，绝对数的处理交给AccuralizedNum方法来解决
        for (int i = 1; i < 10; i++)
            if (i != value)
                psb_Nums[cellPos][i] = false;
        EventCenter.Broadcast(EventDefine.ShowLog, string.Format("{0}已排除，对应数字：{1}\n", (Vector2Int)cellPos, value));
    }
    private void Eliminate()
    {
        EventCenter.Broadcast(EventDefine.ShowLog, "-");//将log清空
        for (int y = 1; y < 10; y++)
            for (int x = 1; x < 10; x++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                DrawCell(cellPos);
                EliminateNumWithAbs(cellPos);
                EliminateNumWithPsb(cellPos);
                AccuralizeNum(cellPos);
            }
    }
    /// <summary> 获取当前格子对应宫的中心格的坐标 </summary>
    public static Vector3Int GetCentrolPos(Vector3Int cellPos)
    {
        Vector3Int pos = new Vector3Int(Mathf.CeilToInt(cellPos.x / 3f), Mathf.CeilToInt(cellPos.y / 3f), 0);//求出大格坐标
        pos.Set(pos.x * 3, pos.y * 3, 0);//求出大格子的右顶点坐标
        pos += Vector3Int.down + Vector3Int.left;//求出大格子的中心点坐标
        return pos;
    }
}