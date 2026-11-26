
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlatformBounceSound : MonoBehaviour
{
    private AudioSource audioSource;
    private float lastBounceTime = -100f;

    [Header("Sound Settings")]
    public float bounceCooldown = 0.1f;
    public float minimumImpactSpeed = 0.5f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball"))
            return;

        if (Time.time - lastBounceTime < bounceCooldown)
            return;

        if (collision.relativeVelocity.magnitude < minimumImpactSpeed)
            return;

        if (audioSource.clip == null)
            return;

        audioSource.PlayOneShot(audioSource.clip);
        lastBounceTime = Time.time;
    }
}
