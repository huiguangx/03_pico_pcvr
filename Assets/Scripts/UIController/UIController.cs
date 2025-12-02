using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour {

    // Start is called before the first frame update
    public Text uiText; //待修改的文本
    public void OnAddButtonClick()
    {
        Debug.Log("✅ 点击成功");
        // string text = uiText.text;  //获取文本的值
        // int num=Int32.Parse(text);  //将文本转化为整数
        // uiText.text = num + 1 + ""; //让整数+1 ，然后在+""
    }
    public void ONDecreateButtonClick()
    {
        string text=uiText.text;
        int num=Int32.Parse(text);
        uiText.text = num - 1 + "";
    }
}
