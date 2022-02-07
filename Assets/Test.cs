using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
    //// 将字段m_MyValue改为属性myValue，并防止丢失其引用
    //[FormerlySerializedAs("myValue")]
    //string m_MyValue;
    //public string myValue
    //{
    //    get; set;
    //}
    public string a = "a";
    private void Update()
    {
        //a = "b" + a + "a";
        a = $"b{a}a";
        //a = string.Empty;
        if (a.Length > 1000)
        {
            enabled = false;
            return;
        }
        Debug.Log(a.Length.ToString());
    }
}
