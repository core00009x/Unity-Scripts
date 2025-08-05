using UnityEngine;
using UnityEngine.InputSystem;

public class MoveDebug : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(Input.GetAxis("Horizontal") + "");
        Debug.Log(Input.GetAxis("Vertical") + "");
        
    }
}
