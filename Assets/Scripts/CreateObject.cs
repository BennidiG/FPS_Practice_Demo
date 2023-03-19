using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateObject : MonoBehaviour
{
    public GameObject ShootObj;
    private GameObject ShootObject;
    private float Object_x = 0;
    private float Object_y = 4;

    // Start is called before the first frame update
    void Start()
    {
        CreatNewObj();
        CreatNewObj();
        CreatNewObj();
        CreatNewObj();
        CreatNewObj();
        CreatNewObj();
    }

    // Update is called once per frame
    void Update()
    {
        RayCmaera();
    }

    private void CreatNewObj()
    {
        Object_x = Object_x + Random.Range(-2f, 2f);
        Object_y = Object_y + Random.Range(-2f, 2f);
        Object_x = Mathf.Clamp(Object_x, -8, 8);
        Object_y = Mathf.Clamp(Object_y, 2, 6);
        GameObject ShootObject = Instantiate(ShootObj, new Vector3(Object_x, Object_y, -1), Quaternion.identity);
    }

    public void RayCmaera()
    {
        //µã»÷Êó±ê×ó¼ü
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray(transform.position, transform.forward * 100);       
            //Debug.DrawLine(transform.position, transform.position + transform.forward * 100, Color.red); 
            RaycastHit hitInfo;                                
            if (Physics.Raycast(ray, out hitInfo, 100))         
            {
                //Debug.Log(hitInfo.collider.gameObject.name);
                Destroy(hitInfo.collider.gameObject);
                CreatNewObj();
            }
        }
    }
}
