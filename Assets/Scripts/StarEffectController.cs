using UnityEngine;

public class StarEffectController : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 1.0f; // Default fallback delay
    [SerializeField] private bool useAnimatorLength = true;
    
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        
        if (animator != null)
        {
            // Force the animation to not loop
            foreach (var animLayer in animator.parameters)
            {
                if (animLayer.type == AnimatorControllerParameterType.Bool && 
                    (animLayer.name.ToLower().Contains("loop") || animLayer.name.ToLower().Contains("repeat")))
                {
                    animator.SetBool(animLayer.name, false);
                }
            }
            
            if (useAnimatorLength)
            {
                // Get the current animation clip length
                AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    destroyDelay = clipInfo[0].clip.length;
                    Debug.Log($"Star effect will be destroyed after {destroyDelay} seconds");
                }
            }
        }
        
        // Destroy after animation finishes
        Destroy(gameObject, destroyDelay);
    }
    
    // Can be called by animation event at the end of the animation
    public void OnAnimationComplete()
    {
        Destroy(gameObject);
    }
}