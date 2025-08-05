using UnityEngine;

public class MouseSystemDebug : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(Input.GetAxis("Mouse X") + "");
        Debug.Log(Input.GetAxis("Mouse Y") + "");
    }
}
