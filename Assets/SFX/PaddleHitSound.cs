
using UnityEngine;

public class PaddleHitSound : MonoBehaviour
{
    [SerializeField] private AudioSource hitAudioSource;
    private float lastHitTime = -100f;

    public float hitCooldown = 0.1f;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            if (Time.time - lastHitTime >= hitCooldown)
            {
                if (hitAudioSource != null && hitAudioSource.clip != null)
                {
                    hitAudioSource.PlayOneShot(hitAudioSource.clip);
                    lastHitTime = Time.time;
                }
            }
        }
    }
}
