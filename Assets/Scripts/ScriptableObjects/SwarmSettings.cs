using UnityEngine;

/// <summary>
/// Object containing preset data regarding follower rat behavior.
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SwarmSettings", order = 1)]
public class SwarmSettings : ScriptableObject
{
    [Header("Prefabs:")]
    [Tooltip("Prefab object for default rats in the swarm")] public GameObject basicRatPrefab;
    [Header("Rule Weights:")]
    [Range(0, 1), Tooltip("Tendency for rats to move toward other nearby rats")]              public float cohesionWeight = 1;
    [Range(0, 1), Tooltip("Tendency for rats to maintain a small distance from nearby rats")] public float separationWeight = 1;
    [Range(0, 1), Tooltip("Tendency for rats to match speed with other nearby rats")]         public float conformityWeight = 1;
}
