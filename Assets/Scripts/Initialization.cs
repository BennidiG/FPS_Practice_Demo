using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class Initialization : MonoBehaviour
{
    public string MSpeed = "5";
    public int MoSpeed = 5;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    
    private void OnGUI()
    {
        GUI.Label(new Rect(130, 25, 20, 25), "Mouse speed:");
        MSpeed = GUI.TextField(new Rect(150, 25, 100, 25), MSpeed, 100);
        if (GUI.Button(new Rect(540, 25, 100, 25), "Sure"))
        {
            MoSpeed = int.Parse(MSpeed);
            Debug.Log(MoSpeed);
        }
    }
}
