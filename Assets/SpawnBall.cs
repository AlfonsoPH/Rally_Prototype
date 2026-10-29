
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [Header("Ball Settings")]
    public GameObject ballPrefab;
    public Transform spawnPoint;

    void Update()
    {
        // Detect a press of the left controller's index trigger
        if (OVRInput.GetDown(
            OVRInput.Button.PrimaryIndexTrigger,
            OVRInput.Controller.LTouch))
        {
            if (ballPrefab == null || spawnPoint == null)
            {
                Debug.LogWarning("Assign the ball prefab and spawn point!");
                return;
            }

            // Spawn the ball at the designated position
            Instantiate(
                ballPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            Debug.Log("Ball spawned!");
        }
    }
}
