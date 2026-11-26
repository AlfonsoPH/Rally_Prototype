using UnityEngine;

public class BallPickup : MonoBehaviour
{
    [HideInInspector]
    public TableBallSpawner spawner;

    private bool hasBeenGrabbed = false;

    public void OnGrabbed()
    {
        if (hasBeenGrabbed)
            return;

        hasBeenGrabbed = true;

        if (spawner != null)
            spawner.ReplaceBall();
    }
}