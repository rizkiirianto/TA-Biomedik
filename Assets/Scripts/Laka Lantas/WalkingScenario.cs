using UnityEngine;

public class WalkingScenario : MonoBehaviour, IMiniGame
{
    [Header("References")]
    private GameManager gameManager; 
    private PlayerMovement playerMovement;
    
    [SerializeField] public Collider2D targetZone; 
    [SerializeField] public Collider2D playerCollider; 

    [Header("Audio References")]
    [SerializeField] private AudioSource ambianceAudioSource; // Suara ambien kota
    [SerializeField] private AudioSource walkingAudioSource;  // Suara jalan biasa
    [SerializeField] private AudioSource runningAudioSource;  // Suara lari (Shift)

    [System.Serializable]
    public struct UIButtonPrompts
    {
        public GameObject A;
        public GameObject D;
        public GameObject Shift;
    }

    [Header("UI Prompts")]
    [SerializeField] private UIButtonPrompts buttonPrompts;

    [Header("Settings")]
    [SerializeField] private float requiredStayTime = 3f;

    [Header("Debug Info")]
    public float stayTimer = 0f;
    private bool completed = false;
    private bool isGameActive = false; 

    private bool promptAConsumed;
    private bool promptDConsumed;
    private bool promptShiftConsumed;

    public void BeginGame(GameManager gm)
    {
        this.gameManager = gm;
        isGameActive = true; 
        Debug.Log("Minigame Walking Dimulai via Interface!");

        // Mainkan suara ambien kota saat minigame dimulai
        if (ambianceAudioSource != null && !ambianceAudioSource.isPlaying)
        {
            ambianceAudioSource.Play();
        }
    }

    void Start()
    {
        if (targetZone == null || playerCollider == null)
        {
            Debug.LogError("Target Zone (Square) atau Player belum dimasukkan ke inspector WalkingScenario!");
            enabled = false;
            return;
        }

        if (playerMovement == null)
        {
            playerMovement = playerCollider.GetComponentInParent<PlayerMovement>();
        }

        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement tidak ditemukan pada player collider WalkingScenario!");
            enabled = false;
            return;
        }

    }

    void Update()
    {
        if (completed || !isGameActive) return;

        bool isMoving = playerMovement.IsMoving;
        bool isRunning = playerMovement.IsSprinting;

        HandlePromptInput();

        if (targetZone.IsTouching(playerCollider))
        {
            HandleStay();
        }
        else
        {
            HandleExit();
        }

        UpdateMovementAudio(isMoving, isRunning);
    }

    private void HandleStay()
    {
        stayTimer += Time.deltaTime;

        if (stayTimer >= requiredStayTime)
        {
            CompleteScenario();
        }
    }

    private void HandleExit()
    {
        if (stayTimer > 0)
        {
            stayTimer = 0f;
            Debug.Log("Keluar zona, timer reset");
        }
    }

    private void UpdateMovementAudio(bool isMoving, bool isRunning)
    {
        if (isMoving)
        {
            if (isRunning)
            {
                if (!runningAudioSource.isPlaying) runningAudioSource.Play();
                if (walkingAudioSource.isPlaying) walkingAudioSource.Pause();
            }
            else
            {
                if (!walkingAudioSource.isPlaying) walkingAudioSource.Play();
                if (runningAudioSource.isPlaying) runningAudioSource.Pause();
            }
        }
        else
        {
            if (walkingAudioSource.isPlaying) walkingAudioSource.Pause();
            if (runningAudioSource.isPlaying) runningAudioSource.Pause();
        }
    }

    private void HandlePromptInput()
    {
        if (!promptAConsumed && Input.GetKeyDown(KeyCode.A))
        {
            promptAConsumed = true;
            if (buttonPrompts.A != null) buttonPrompts.A.SetActive(false);
        }

        if (!promptDConsumed && Input.GetKeyDown(KeyCode.D))
        {
            promptDConsumed = true;
            if (buttonPrompts.D != null) buttonPrompts.D.SetActive(false);
        }

        if (!promptShiftConsumed && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)))
        {
            promptShiftConsumed = true;
            if (buttonPrompts.Shift != null) buttonPrompts.Shift.SetActive(false);
        }
    }

    private void CompleteScenario()
    {
        completed = true;
        isGameActive = false;
        Debug.Log("Scenario Selesai!");
        
        // Matikan semua suara saat minigame selesai
        if (ambianceAudioSource != null) ambianceAudioSource.Stop();
        if (walkingAudioSource != null) walkingAudioSource.Stop();
        if (runningAudioSource != null) runningAudioSource.Stop();

        this.gameObject.SetActive(false);
        
        if (gameManager != null)
        {
            gameManager.OnMiniGameComplete("Player reach target");
        }
    }
}