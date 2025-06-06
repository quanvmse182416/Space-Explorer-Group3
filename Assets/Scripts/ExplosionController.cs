using UnityEngine;

public class ExplosionController : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 1.0f; // Adjust based on animation length
    [SerializeField] private bool useAnimatorLength = true;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (useAnimatorLength && animator != null)
        {
            // Get the current animation clip length
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                destroyDelay = clipInfo[0].clip.length;
            }
        }

        // Destroy after animation finishes
        Destroy(gameObject, destroyDelay);
    }
}