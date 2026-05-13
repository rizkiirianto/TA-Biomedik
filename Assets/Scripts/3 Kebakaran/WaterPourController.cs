using UnityEngine;

public class WaterPourController : MonoBehaviour
{
    [Header("References")]
    public Transform pourOrigin; 
    public SpriteRenderer waterStreamSprite; 
    public ParticleSystem splashParticles; 

    [Header("Settings")]
    public LayerMask armLayer; 
    public float maxPourDistance = 5f;
    public bool isPouring = false;
    
    // NEW: A lock to prevent pouring when the game is over
    public bool canPour = true; 

    void Update()
    {
        // 1. We always want the bottle to follow the mouse, even if empty
        FollowMouse();

        // NEW: If the game is over, stop reading the mouse clicks!
        if (!canPour) return; 

        if (Input.GetMouseButtonDown(0)) StartPouring();
        if (Input.GetMouseButtonUp(0)) StopPouring();

        if (isPouring)
        {
            UpdateWaterStream();
        }
    }

    // NEW: A public function that other scripts can trigger to lock the bottle
    public void DisablePouring()
    {
        canPour = false;
        
        // If the player is holding down the click exactly when they win, 
        // force the water to stop immediately.
        if (isPouring)
        {
            StopPouring();
        }
    }

    void FollowMouse()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;
        transform.position = worldPosition;
    }

    void StartPouring()
    {
        isPouring = true;
        waterStreamSprite.enabled = true;
        if (splashParticles != null) splashParticles.Play();
    }

    void StopPouring()
    {
        isPouring = false;
        waterStreamSprite.enabled = false;
        if (splashParticles != null) splashParticles.Stop();
    }

    void UpdateWaterStream()
    {
        Vector2 origin = pourOrigin.position;
        waterStreamSprite.transform.position = origin;
        float spriteHeight = waterStreamSprite.sprite.bounds.size.y;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, maxPourDistance, armLayer);

        if (hits.Length > 0)
        {
            float distance = hits[0].distance;
            float requiredScale = distance / spriteHeight;
            waterStreamSprite.transform.localScale = new Vector3(waterStreamSprite.transform.localScale.x, requiredScale, 1f);

            if (splashParticles != null) 
            {
                splashParticles.transform.position = hits[0].point;
                splashParticles.transform.up = hits[0].normal;
            }

            foreach (RaycastHit2D singleHit in hits)
            {
                TreatmentManager manager = singleHit.collider.GetComponentInParent<TreatmentManager>();
                if (manager != null)
                {
                    manager.CheckHit(singleHit.collider);
                }
            }
        }
        else
        {
            float requiredScale = maxPourDistance / spriteHeight;
            waterStreamSprite.transform.localScale = new Vector3(waterStreamSprite.transform.localScale.x, requiredScale, 1f);
            
            if (splashParticles != null)
            {
                splashParticles.transform.position = origin + (Vector2.down * maxPourDistance);
            }
        }
    }
}