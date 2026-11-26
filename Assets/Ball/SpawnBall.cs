using UnityEngine;
using Oculus.Interaction;
using System.Collections;

public class BallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform spawnPoint;
    public Transform leftHandSpawnPoint;
    public GrabInteractor leftGrabInteractor;

    private bool autoGrabbed = false;

    void Update()
    {
        bool leftGrip = OVRInput.Get(
            OVRInput.Button.PrimaryHandTrigger);

        if (OVRInput.GetDown(
            OVRInput.Button.PrimaryIndexTrigger))
        {
            if (!autoGrabbed)
            {
                StartCoroutine(SpawnBall(leftGrip));
            }
        }

        if (autoGrabbed && !leftGrip)
        {
            leftGrabInteractor.ForceRelease();
            autoGrabbed = false;
        }
    }

    IEnumerator SpawnBall(bool autoGrab)
    {
        Transform point = autoGrab
            ? leftHandSpawnPoint
            : spawnPoint;

        GameObject ball = Instantiate(
            ballPrefab,
            point.position,
            point.rotation
        );

        yield return null;

        if (autoGrab && OVRInput.Get(
            OVRInput.Button.PrimaryHandTrigger))
        {
            GrabInteractable interactable =
                ball.GetComponentInChildren<GrabInteractable>();

            if (interactable != null)
            {
                leftGrabInteractor.ForceSelect(interactable);
                autoGrabbed = true;
            }
        }
    }
}