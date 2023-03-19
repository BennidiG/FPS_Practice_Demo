using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class MoveCamera : MonoBehaviour
{
    public string MouseSpeed = "5";
    public string MoveSpeed = "8";
    private Transform mCamera;
    private Transform mRightHand;
    private AudioSource mAudio;
    public Rigidbody mRigidbody;
    //private float X = 0;
    //private float Y = 0;
    public float mMoveSpeed = 8;         // �����ƶ��ٶ�
    public float mMouseSensitivity = 5;     // �����ת�����ж�
    public float mMinimumX = -45;     
    public float mMaximumX = 45;      
    public float mMinimumY = -90;         
    public float mMaximumY = 90;    
    private Vector3 MoveRotation = new Vector3(0, 0, 0);

    //Initialization New_Initialization = new Initialization();



    private void OnGUI()
    {
        GUI.Label(new Rect(60, 25, 200, 25), "Mouse speed:");
        GUI.Label(new Rect(60, 50, 200, 25), "Move speed:");
        MouseSpeed = GUI.TextField(new Rect(150, 25, 100, 25), MouseSpeed, 100);
        MoveSpeed = GUI.TextField(new Rect(150, 50, 100, 25), MouseSpeed, 100);
        if (GUI.Button(new Rect(250, 25, 100, 25), "Sure"))
        {
            mMouseSensitivity = int.Parse(MouseSpeed);
        }
        if (GUI.Button(new Rect(250, 50, 100, 25), "Sure"))
        {
            mMoveSpeed = int.Parse(MoveSpeed);
        }
    }



    // Start is called before the first frame update
    void Start()
    {
        // ��ȡ����ͷ����
        mCamera = transform.Find("Main Camera");
        // ��ȡ���ֶ���
        mRigidbody = GameObject.Find("Main Camera").GetComponent<Rigidbody>();
        //mRigidbody = GameObject.Find("Main Camera");

    }

    // Update is called once per frame
    void Update()
    {
        UpdateMovement();
    }

    void LateUpdate()
    {
        UpdateLookAt();
    }

    private void UpdateMovement()
    {
        float distance = mMoveSpeed * Time.deltaTime;

        // ��
        if (Input.GetKey(KeyCode.A))
        {
            Debug.Log("A");
            mRigidbody.MovePosition(transform.position + new Vector3(-distance, 0, 0));
        }

        // ��
        if (Input.GetKey(KeyCode.D))
        {
            Debug.Log("D");
            mRigidbody.MovePosition(transform.position + new Vector3(distance, 0, 0));
        }
    }

    private void UpdateLookAt()
    {
        //X = Input.GetAxis("Mouse X") * mMouseSensitivity;
        //Y = Input.GetAxis("Mouse Y") * mMouseSensitivity;
        //mRigidbody.transform.localRotation = mRigidbody.transform.localRotation * Quaternion.Euler(-Y, 0, 0);
        //mRigidbody.transform.localRotation = mRigidbody.transform.localRotation * Quaternion.Euler(0, X, 0);


        // ������ת
        MoveRotation.y = MoveRotation.y + Input.GetAxis("Mouse X") * mMouseSensitivity;
        MoveRotation.y = Mathf.Clamp(MoveRotation.y, mMinimumY, mMaximumY);

        // ������ת
        MoveRotation.x = MoveRotation.x - Input.GetAxis("Mouse Y") * mMouseSensitivity;
        //MoveRotation.x = Mathf.Clamp(MoveRotation.x, mMinimumX, mMaximumX);

        //mRigidbody.transform.Rotate(new Vector3(MoveRotation.x, 45, 0), Space.Self);
        //mCamera.localEulerAngles = new Vector3(MoveRotation.x, MoveRotation.y, 0);
        mRigidbody.transform.localEulerAngles = new Vector3(MoveRotation.x,MoveRotation.y, 0);

    }
}
