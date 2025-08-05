using UnityEngine;
// Add the namespace if AmmoDefinitions is in a different namespace, e.g.:

public class GetAmmo : MonoBehaviour
{
    [Header("Ammo Settings")]
    public int ammoAmount = 15; // Default amount if not using definitions
    public AmmoDefinitions ammoDefinitions; // Reference to AmmoDefinitions ScriptableObject
    public int ammoTypeIndex = 0; // Index for the ammo type in definitions

    [Header("Pickup Settings")]
    public float pickupDistance = 3f; // Max distance to pick up
    public string playerTag = "Player"; // Tag for player object

    private bool isPicked = false;

    void Update()
    {
        if (isPicked) return;

        // Raycast from camera to this object
        var cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            if (hit.collider == GetComponent<Collider>())
            {
                // Check for input (E key)
                if (Input.GetKeyDown(KeyCode.E))
                {
                    TryPickup();
                }
            }
        }
    }

    void TryPickup()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        var shooter = player.GetComponentInChildren<AdvancedPistol>();
        if (shooter != null)
        {
            // Use AmmoDefinitions if available
            if (ammoDefinitions != null && ammoTypeIndex >= 0 && ammoTypeIndex < ammoDefinitions.ammoTypes.Length)
            {
                var ammoType = ammoDefinitions.ammoTypes[ammoTypeIndex];
                shooter.AddAmmo(ammoType.name, ammoAmount, ammoType);
            }
            isPicked = true;
            Destroy(gameObject);
        }
    }
}