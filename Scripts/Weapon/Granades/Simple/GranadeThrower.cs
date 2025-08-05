using UnityEngine;

public class GrenadeThrower : MonoBehaviour {
    public GameObject grenadePrefab;
    public Transform throwPoint;
    public float throwForce = 15f;
    public float upwardForce = 2f;

    void Update() {
        if (Input.GetButtonDown("Fire1")) {
            ThrowGrenade();
        }
    }

    void ThrowGrenade() {
        GameObject grenade = Instantiate(grenadePrefab, throwPoint.position, Quaternion.identity);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        Vector3 forceDirection = transform.forward * throwForce + transform.up * upwardForce;
        rb.AddForce(forceDirection, ForceMode.VelocityChange);
    }
}
