
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PaddleHitSound : MonoBehaviour
{
    private AudioSource audioSource;
    private float lastHitTime = -100f;

    public float hitCooldown = 0.1f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            if (Time.time - lastHitTime >= hitCooldown)
            {
                if (audioSource.clip != null)
                {
                    audioSource.PlayOneShot(audioSource.clip);
                    lastHitTime = Time.time;
                }
            }
        }
    }
}
