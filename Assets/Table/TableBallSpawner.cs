using UnityEngine;
using System.Collections;

public class TableBallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;
    public float respawnDelay = 0.5f;
    public float clearance = 0.15f;

    private GameObject currentBall;
    private bool isRespawning;

    void Start()
    {
        SpawnBall();
    }

    void SpawnBall()
    {
        currentBall = Instantiate(
            ballPrefab,
            transform.position,
            transform.rotation
        );

        BallPickup pickup = currentBall.GetComponent<BallPickup>();
        pickup.spawner = this;

        isRespawning = false;
    }

    public void ReplaceBall()
    {
        if (!isRespawning)
        {
            isRespawning = true;
            StartCoroutine(RespawnBall());
        }
    }

    IEnumerator RespawnBall()
    {
        GameObject previousBall = currentBall;

        yield return new WaitForSeconds(respawnDelay);

        yield return new WaitUntil(() =>
            previousBall == null ||
            Vector3.Distance(previousBall.transform.position,
                transform.position) > clearance
        );

        SpawnBall();
    }
}