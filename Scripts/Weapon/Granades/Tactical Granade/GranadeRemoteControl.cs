using UnityEngine;

public class GrenadeRemoteControl : MonoBehaviour {
    void Update() {
        if (Input.GetKeyDown(KeyCode.G)) {
            TacticalGrenade[] grenades = FindObjectsOfType<TacticalGrenade>();
            foreach (TacticalGrenade g in grenades) {
                g.SendMessage("Detonate");
            }
        }
    }
}
