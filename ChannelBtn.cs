using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChannelBtn : MonoBehaviour
{
    public TMP_InputField text;
    public void Call(){
        Data.cn = text.text;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
