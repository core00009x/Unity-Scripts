using UnityEngine;
using System.Collections.Generic;

public class AIGroupManager : MonoBehaviour
{
    public static AIGroupManager Instance;

    private List<AIEnemyController> enemies = new();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterEnemy(AIEnemyController enemy)
    {
        if (!enemies.Contains(enemy)) enemies.Add(enemy);
    }

    public void UnregisterEnemy(AIEnemyController enemy)
    {
        enemies.Remove(enemy);
    }

    public void BroadcastPlayerSpotted(Vector3 position, AIEnemyController sender, float range = 30f)
    {
        foreach (var enemy in enemies)
        {
            if (enemy == sender) continue;
            if (Vector3.Distance(enemy.transform.position, sender.transform.position) <= range)
            {
                enemy.OnAllySpottedPlayer(position);
            }
        }
    }

    public void BroadcastNoiseHeard(Vector3 noisePos, AIEnemyController sender, float range = 20f)
    {
        foreach (var enemy in enemies)
        {
            if (enemy == sender) continue;
            if (Vector3.Distance(enemy.transform.position, sender.transform.position) <= range)
            {
                enemy.OnAllyHeardNoise(noisePos);
            }
        }
    }
}
