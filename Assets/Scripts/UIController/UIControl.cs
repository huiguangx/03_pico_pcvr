using System.Net.Mime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIControl : MonoBehaviour {

    // Start is called before the first frame update
    public Text uiText; //待修改的文本
    public TextMeshProUGUI uiTextPro;
   public void OnAddButtonClick()

    {
        Debug.Log("✅ 加按钮被点击");
        string text = uiTextPro.text;
        if (int.TryParse(text, out int num))
        {
            uiTextPro.text = (num + 1).ToString();
        }
        else
        {
            uiTextPro.text = "0"; // 默认值
        }
    }
    public void OnDecreaseButtonClick() // 修正拼写

    {
        Debug.Log("✅ 减按钮被点击");
        string text = uiTextPro.text;
        if (int.TryParse(text, out int num))
        {
            uiTextPro.text = (num - 1).ToString();
        }
        else
        {
            uiTextPro.text = "0";
        }
    }
}
