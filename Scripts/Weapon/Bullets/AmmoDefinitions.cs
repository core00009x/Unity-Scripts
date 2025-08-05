using UnityEngine;

[System.Serializable]
public class AmmoType
{
    public string name;
    public int maxCarry;
    public float damage;
    public float penetration;
    public float velocity;
    public GameObject bulletPrefab;
    public Sprite icon;
}

[CreateAssetMenu(fileName = "AmmoDefinitions", menuName = "Guns/Ammo Definitions")]
public class AmmoDefinitions : ScriptableObject
{
    public AmmoType[] ammoTypes;
}