﻿using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EventsRegister : MonoBehaviour
{
    Tilemap tm;
    private void Awake()
    {
        tm = GameObject.Find("Grid/Tilemap").GetComponent<Tilemap>();
        EventCenter.AddListener(EventDefine.ClearGrid, () =>
        {
            Tilemap tm = GameObject.Find("Grid/Tilemap").GetComponent<Tilemap>();
            //将所有格子改为empty格子
            for (int y = 1; y < 10; y++)
                for (int x = 1; x < 10; x++)
                    tm.SetTile(new Vector3Int(x, y, 0), VarsManager.GetVars().baseTiles[9]);
            GameObject[] bgs = GameObject.FindGameObjectsWithTag("BG");
            foreach (GameObject bg in bgs)
                bg.GetComponent<SpriteRenderer>().color = new Color(0, 0.7352409f, 1, 1);
            GameObject.Find("Canvas/Editing/Btn_EditFinish").SetActive(true);
        });
        EventCenter.AddListener(EventDefine.OpenSourceCode, () =>
        {
            Application.OpenURL(@"https://github.com/Sinkorty/Sudoku-Gongge");
        });
        EventCenter.AddListener(EventDefine.SwitchEditMode, () =>
        {
            Switching(true);
        });
        EventCenter.AddListener(EventDefine.SwitchEliminateMode, () =>
        {
            GameObject[] bgs = GameObject.FindGameObjectsWithTag("BG");
            foreach (GameObject bg in bgs)
                bg.GetComponent<SpriteRenderer>().color = new Color(0, 0.7352409f, 1, 1);
            Switching(false);
        });
        EventCenter.AddListener(EventDefine.ReadSudokuFile, () =>
        {
            List<int> nums = new List<int>();
            foreach (char c in ReadFile(@"C:\Users\admin\Desktop\SudokuData.sd").ToCharArray())   //将文件中的数据转化为全部转化成数字数组
                nums.Add(int.Parse(c.ToString()));
            int i = 0;
            for (int y = 1; y < 10; y++)
                for (int x = 1; x < 10; x++, i++)
                    tm.SetTile(new Vector3Int(x, y, 0), VarsManager.GetVars().frontTiles[nums[i]]);
        });
        EventCenter.AddListener(EventDefine.SaveSudokuFile, () =>
        {
            StringBuilder caption = new StringBuilder();
            for (int y = 1; y < 10; y++)
                for (int x = 1; x < 10; x++)
                    caption.Append(GetCellNum(new Vector3Int(x, y, 0)));
            WriteFile(@"C:\Users\admin\Desktop\SudokuData.sd", caption.ToString());
        });
    }
    private void Switching(bool isEdit)
    {
        //false --> isEdit , true --> !isEdit
        GameObject.Find("Grid").GetComponent<EditGridManager>().enabled = isEdit;
        GameObject.Find("Grid").GetComponent<EliminateManager>().enabled = !isEdit;
        transform.Find("Btn_Eliminate").gameObject.SetActive(!isEdit);
        transform.Find("Tex_PSB_Num").gameObject.SetActive(!isEdit);
        transform.Find("Tex_ABS_Num").gameObject.SetActive(!isEdit);
        transform.Find("Editing").gameObject.SetActive(isEdit);
        transform.Find("Btn_Edit").gameObject.SetActive(!isEdit);
    }
    private string ReadFile(string path)
    {
        try//读取文件，获取内容
        {
            FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);//为了测试，将文件在C盘桌面上进行读取
            byte[] buffer = new byte[1024];
            int len = file.Read(buffer, 0, buffer.Length);
            string caption = Encoding.ASCII.GetString(buffer, 0, len);
            file.Close();
            file.Dispose();
            return caption;
        }
        catch
        {
            throw new Exception("文件读取错误");
        }
    }
    private void WriteFile(string path, string caption)
    {
        try
        {
            FileStream file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            byte[] buffer = Encoding.ASCII.GetBytes(caption);
            file.Write(buffer, 0, buffer.Length);
            file.Close();
            file.Dispose();
        }
        catch
        {
            throw new Exception("已有文件名相同的文件");
        }
    }
    private int GetCellNum(Vector3Int cellPos)
    {
        string gridName = tm.GetTile(cellPos).name;
        if (gridName != "empty") return int.Parse(gridName);
        else return 0;
    }
}