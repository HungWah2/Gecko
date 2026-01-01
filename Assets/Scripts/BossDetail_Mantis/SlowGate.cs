using UnityEngine;

public class SlowGate : MonoBehaviour
{
    public float slowMultiplier = 0.5f;
    public float slowDuration = 0.5f;


    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        NegativeEffect negativeEffect = other.GetComponent<NegativeEffect>();
        if (negativeEffect != null)
        {
            negativeEffect.ApplySlow(slowMultiplier, slowDuration);
        }
    }
}
