using UnityEngine;
using UnityEngine.UI;

public class PlayerFlashEffect : MonoBehaviour {
    public Image whiteOverlay;
    public float maxFlashDuration = 5f;
    public AnimationCurve intensityCurve;

    private float flashTimer = 0f;
    private bool isFlashed = false;

    void Start()
    {
        isFlashed = false;
        whiteOverlay.color = Color.clear;
    }

    public void TriggerFlash(Vector3 explosionPos)
    {
        Vector3 dir = (explosionPos - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, dir);
        float intensity = Mathf.Clamp01(dot); // Facing the blast = full effect
        flashTimer = maxFlashDuration * intensity;
        isFlashed = true;
    }

    void Update() {
        if (isFlashed) {
            flashTimer -= Time.deltaTime;
            float alpha = intensityCurve.Evaluate(1 - (flashTimer / maxFlashDuration));
            whiteOverlay.color = new Color(1, 1, 1, alpha);

            if (flashTimer <= 0f) {
                isFlashed = false;
                whiteOverlay.color = Color.clear;
            }
        }
    }
}
