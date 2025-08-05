using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    public float noiseVolume = 1f; // 0 = silencioso, 1 = normal, >1 = barulhento

    public void MakeNoise()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, noiseVolume * 20f);
        foreach (var col in cols)
        {
            if (col.TryGetComponent(out AIEnemyController ai))
            {
                ai.HeardNoise(transform.position, noiseVolume);
            }
        }
    }
}
