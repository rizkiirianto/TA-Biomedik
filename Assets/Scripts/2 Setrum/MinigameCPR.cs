using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MinigameCPR : MonoBehaviour, IMiniGame
{
    [Header("References")]
    [SerializeField] private RectTransform laneRoot;
    [SerializeField] private RectTransform clickTargetArea;
    [SerializeField] private RectTransform wadahTarget;
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform despawnPoint;
    [SerializeField] private RectTransform incomingPrefab;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Image depthBarFill;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private GameObject tebaringCompressed;

    [Header("Sprites")]
    [SerializeField] private Sprite incomingSprite;
    [SerializeField] private Sprite successSprite;

    [Header("Rhythm Settings")]
    [SerializeField] private float bpm = 100f;
    [SerializeField] private float spawnIntervalBeats = 1f;
    [SerializeField] private float travelBeatsToTarget = 2f;
    [SerializeField] private float holdDurationBeats = 0.5f;
    [SerializeField] private float incomingSpawnDelayBeats = 0.5f;

    [Header("Spawn Control")]
    [SerializeField] private float stopSpawningAfterSeconds = 50f;

    [Header("Hit Detection")]
    [SerializeField, Range(0f, 1f)] private float minOverlapToRegister = 0f;
    [SerializeField] private float latePassMargin = 0f;

    [Header("Hold Validation")]
    [SerializeField] private float shallowThresholdBeats = 0.42f;
    [SerializeField] private float perfectLowerBeats = 0.42f;
    [SerializeField] private float perfectUpperBeats = 0.58f;

    [Header("Visual Juice")]
    [SerializeField] private float successScaleMultiplier = 1.12f;
    [SerializeField] private float successPopDuration = 0.08f;
    [SerializeField] private float releaseFadeDuration = 0.18f;
    [SerializeField] private float lateFadeDuration = 0.22f;
    [SerializeField] private Color lateColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);

    [Header("Tutorial")]
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private Button tutorialAdvanceButton;
    [SerializeField] private GameObject[] tutorialSteps = new GameObject[3];

    [Header("Tutorial EN")]
    [SerializeField] private GameObject tutorialRootEN;
    [SerializeField] private Button tutorialAdvanceButtonEN;
    [SerializeField] private GameObject[] tutorialStepsEN = new GameObject[3];

    private GameObject ActiveTutorialRoot => PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? tutorialRootEN : tutorialRoot;
    private Button ActiveTutorialAdvanceButton => PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? tutorialAdvanceButtonEN : tutorialAdvanceButton;
    private GameObject[] ActiveTutorialSteps => PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? tutorialStepsEN : tutorialSteps;

    [Header("Audio")]
    [SerializeField] private AudioSource backingTrackSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip backingTrackClip;
    [SerializeField, Range(0f, 1f)] private float backingTrackVolume = 0.35f;
    [SerializeField] private bool playTrackOnStart = true;
    [SerializeField] private bool completeWhenTrackEnds = true;
    [SerializeField] private AudioClip rusukPatahSound;
    [SerializeField] private AudioClip soundAmbulance;
    [SerializeField, Range(0f, 1f)] private float rusukPatahPerfectChance = 0.05f;
    [SerializeField, Range(0f, 1f)] private float rusukPatahTooLongChance = 0.5f;
    [SerializeField, Min(1)] private int maxRusukPatahPlayCount = 6;

    [Header("Completion")]
    [SerializeField] private bool autoCompleteByPerfectCount = false;
    [SerializeField] private int requiredPerfectCompressions = 10;
    [SerializeField] private string minigameSuccessFeedback = "CPR selesai dengan ritme yang tepat.";
    [SerializeField] private UnityEvent onMinigameFinished;

    private class NoteRuntime
    {
        public RectTransform rect;
        public Image image;
        public Vector3 originalScale;
        public bool isHeld;
        public bool isResolved;
    }

    private readonly List<NoteRuntime> activeNotes = new List<NoteRuntime>();

    private GameManager gameManager;
    private Camera uiCamera;
    private float secondsPerBeat = 0.6f;
    private float spawnRateSeconds = 0.6f;
    private float noteSpeed = 600f;
    private float holdDurationSeconds = 0.3f;
    private float shallowThresholdSeconds = 0.25f;
    private float perfectLowerSeconds = 0.25f;
    private float perfectUpperSeconds = 0.35f;
    private float previousSpawnRateSeconds = 0.6f;
    private float appliedBpm = -1f;
    private float appliedSpawnIntervalBeats = -1f;
    private float appliedTravelBeatsToTarget = -1f;
    private float appliedHoldDurationBeats = -1f;
    private float appliedShallowThresholdBeats = -1f;
    private float appliedPerfectLowerBeats = -1f;
    private float appliedPerfectUpperBeats = -1f;
    private float spawnTimer;
    private float incomingSpawnDelaySeconds;
    private float elapsedMinigameSeconds;
    private bool minigameInitialized;
    private bool minigameFinished;
    private bool isHolding;
    private float holdTimer;
    private NoteRuntime heldNote;
    private int perfectCompressionCount;
    private bool hasTrackStarted;
    private bool gameplayStarted;
    private bool tutorialButtonBound;
    private int tutorialStepIndex = -1;
    private int shallowCompressionCount;
    private int tooLongCompressionCount;
    private int missInputCount;
    private int lateCount;
    private int rusukPatahPlayedCount;

    private void PlayAmbulanceLoopOnDetachedSource()
    {
        if (soundAmbulance == null)
        {
            return;
        }

        GameObject audioHost = new GameObject("CPR_AmbulanceLoopAudio");
        AudioSource loopSource = audioHost.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;
        loopSource.clip = soundAmbulance;
        loopSource.Play();
    }

    private void TryPlayRusukPatahSound(float playChance)
    {
        if (rusukPatahSound == null)
        {
            return;
        }

        if (rusukPatahPlayedCount >= Mathf.Max(1, maxRusukPatahPlayCount))
        {
            return;
        }

        float clampedChance = Mathf.Clamp01(playChance);
        if (clampedChance <= 0f || Random.value > clampedChance)
        {
            return;
        }

        AudioSource source = sfxSource != null ? sfxSource : backingTrackSource;
        if (source == null)
        {
            return;
        }

        source.PlayOneShot(rusukPatahSound);
        rusukPatahPlayedCount++;
    }

    private void Start()
    {
        InitializeMinigame();
    }

    public void BeginGame(GameManager gm)
    {
        gameManager = gm;
        InitializeMinigame();
    }

    private void InitializeMinigame()
    {
        if (minigameInitialized)
        {
            return;
        }

        minigameInitialized = true;
        elapsedMinigameSeconds = 0f;
        perfectCompressionCount = 0;
        shallowCompressionCount = 0;
        tooLongCompressionCount = 0;
        missInputCount = 0;
        lateCount = 0;
        rusukPatahPlayedCount = 0;
        RecalculateRhythmFromBpm(true);
        uiCamera = ResolveUICamera();
        EnsureAudioSources();

        gameplayStarted = false;
        hasTrackStarted = false;
        SetCompressedVisual(false);

        if (TryStartTutorial())
        {
            return;
        }

        StartGameplay();
    }

    private Camera ResolveUICamera()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
        }

        return null;
    }

    private void Update()
    {
        if (minigameFinished || !minigameInitialized || !gameplayStarted)
        {
            return;
        }

        RefreshRhythmIfNeeded();
        CheckTrackCompletion();

        if (minigameFinished)
        {
            return;
        }

        elapsedMinigameSeconds += Time.deltaTime;

        HandleSpawning();
        MoveNotes();
        HandleInput();
        UpdateDepthBar();
    }

    private void HandleSpawning()
    {
        if (stopSpawningAfterSeconds > 0f && elapsedMinigameSeconds >= stopSpawningAfterSeconds)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        while (spawnTimer <= 0f)
        {
            SpawnIncoming();
            spawnTimer += spawnRateSeconds;
        }
    }

    private void SpawnIncoming()
    {
        if (incomingPrefab == null || laneRoot == null || spawnPoint == null)
        {
            return;
        }

        RectTransform noteRect = Instantiate(incomingPrefab, laneRoot);
        noteRect.anchoredPosition = spawnPoint.anchoredPosition;

        Image noteImage = noteRect.GetComponent<Image>();
        if (noteImage != null && incomingSprite != null)
        {
            noteImage.sprite = incomingSprite;
        }

        activeNotes.Add(new NoteRuntime
        {
            rect = noteRect,
            image = noteImage,
            originalScale = noteRect.localScale,
            isHeld = false,
            isResolved = false
        });
    }

    private void MoveNotes()
    {
        if (wadahTarget == null)
        {
            return;
        }

        float targetX = wadahTarget.anchoredPosition.x;
        float leftBound = despawnPoint != null ? despawnPoint.anchoredPosition.x : targetX - 500f;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            NoteRuntime note = activeNotes[i];
            if (note == null || note.rect == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            if (!note.isHeld && !note.isResolved)
            {
                Vector2 pos = note.rect.anchoredPosition;
                pos.x -= noteSpeed * Time.deltaTime;
                note.rect.anchoredPosition = pos;

                if (HasPassedTarget(note.rect))
                {
                    MarkLate(note);
                }
            }

            if (note.rect.anchoredPosition.x < leftBound)
            {
                CleanupNote(i);
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryStartCompression();
        }

        if (isHolding)
        {
            holdTimer += Time.deltaTime;
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndCompression();
        }
    }

    private void TryStartCompression()
    {
        if (isHolding)
        {
            return;
        }

        if (!IsPointerInsideClickTarget())
        {
            SetFeedback("Klik tepat di area target.", new Color(1f, 0.85f, 0.2f));
            return;
        }

        NoteRuntime note = FindBestHittableNote();
        if (note == null)
        {
            missInputCount++;
            SetFeedback("Miss!", new Color(1f, 0.35f, 0.35f));
            return;
        }

        heldNote = note;
        heldNote.isHeld = true;

        if (heldNote.image != null && successSprite != null)
        {
            heldNote.image.sprite = successSprite;
            heldNote.image.color = Color.white;
        }

        holdTimer = 0f;
        isHolding = true;
        SetCompressedVisual(true);
        SetFeedback("Hold...", Color.white);

        StartCoroutine(PopScaleRoutine(heldNote));
    }

    private bool IsPointerInsideClickTarget()
    {
        if (clickTargetArea == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(clickTargetArea, Input.mousePosition, uiCamera);
    }

    private NoteRuntime FindBestHittableNote()
    {
        if (wadahTarget == null)
        {
            return null;
        }

        float bestOverlap = -1f;
        float bestCenterDelta = float.MaxValue;
        NoteRuntime bestNote = null;

        for (int i = 0; i < activeNotes.Count; i++)
        {
            NoteRuntime note = activeNotes[i];
            if (note == null || note.rect == null || note.isHeld || note.isResolved)
            {
                continue;
            }

            float overlap = GetNormalizedHorizontalOverlap(note.rect, wadahTarget);
            if (overlap < minOverlapToRegister)
            {
                continue;
            }

            float centerDelta = Mathf.Abs(note.rect.anchoredPosition.x - wadahTarget.anchoredPosition.x);
            if (overlap > bestOverlap || (Mathf.Approximately(overlap, bestOverlap) && centerDelta < bestCenterDelta))
            {
                bestOverlap = overlap;
                bestCenterDelta = centerDelta;
                bestNote = note;
            }
        }

        return bestNote;
    }

    private void EndCompression()
    {
        if (!isHolding)
        {
            SetCompressedVisual(false);
            return;
        }

        isHolding = false;
        SetCompressedVisual(false);

        if (heldNote == null)
        {
            holdTimer = 0f;
            return;
        }

        string resultText;
        Color resultColor;
        if (holdTimer >= perfectLowerSeconds && holdTimer <= perfectUpperSeconds)
        {
            resultText = "Perfect Compression!";
            resultColor = new Color(0.45f, 1f, 0.55f);
            perfectCompressionCount++;
            TryPlayRusukPatahSound(rusukPatahPerfectChance);
            TryFinishMinigame();
        }
        else if (holdTimer < shallowThresholdSeconds)
        {
            resultText = "Shallow Compression!";
            resultColor = new Color(1f, 0.7f, 0.2f);
            shallowCompressionCount++;
        }
        else
        {
            resultText = "Too Long!";
            resultColor = new Color(1f, 0.55f, 0.2f);
            tooLongCompressionCount++;
            TryPlayRusukPatahSound(rusukPatahTooLongChance);
        }

        SetFeedback(resultText, resultColor);
        heldNote.isHeld = false;
        heldNote.isResolved = true;
        StartCoroutine(FadeAndDestroyRoutine(heldNote, releaseFadeDuration));
        heldNote = null;
        holdTimer = 0f;
    }

    private void MarkLate(NoteRuntime note)
    {
        if (note == null || note.isResolved || note.isHeld)
        {
            return;
        }

        note.isResolved = true;
        lateCount++;

        if (note.image != null)
        {
            note.image.color = lateColor;
        }

        SetFeedback("Late!", new Color(1f, 0.45f, 0.45f));
        StartCoroutine(FadeAndDestroyRoutine(note, lateFadeDuration));
    }

    private IEnumerator PopScaleRoutine(NoteRuntime note)
    {
        if (note == null || note.rect == null)
        {
            yield break;
        }

        Vector3 from = note.originalScale;
        Vector3 to = note.originalScale * successScaleMultiplier;
        float elapsed = 0f;

        while (elapsed < successPopDuration)
        {
            if (note == null || note.rect == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / successPopDuration);
            note.rect.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        if (note != null && note.rect != null)
        {
            note.rect.localScale = to;
        }
    }

    private IEnumerator FadeAndDestroyRoutine(NoteRuntime note, float duration)
    {
        if (note == null || note.rect == null)
        {
            yield break;
        }

        if (note.image == null)
        {
            if (note.rect != null)
            {
                Destroy(note.rect.gameObject);
            }

            activeNotes.Remove(note);
            yield break;
        }

        Color startColor = note.image.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (note == null || note.image == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            note.image.color = c;
            yield return null;
        }

        if (note.rect != null)
        {
            Destroy(note.rect.gameObject);
        }

        activeNotes.Remove(note);
    }

    private void CleanupNote(int index)
    {
        NoteRuntime note = activeNotes[index];
        if (note != null && note.rect != null)
        {
            Destroy(note.rect.gameObject);
        }

        if (note == heldNote)
        {
            heldNote = null;
            isHolding = false;
            holdTimer = 0f;
            SetCompressedVisual(false);
        }

        activeNotes.RemoveAt(index);
    }

    private void RefreshRhythmIfNeeded()
    {
        bool unchanged = Mathf.Approximately(appliedBpm, bpm) &&
            Mathf.Approximately(appliedSpawnIntervalBeats, spawnIntervalBeats) &&
            Mathf.Approximately(appliedTravelBeatsToTarget, travelBeatsToTarget) &&
            Mathf.Approximately(appliedHoldDurationBeats, holdDurationBeats) &&
            Mathf.Approximately(appliedShallowThresholdBeats, shallowThresholdBeats) &&
            Mathf.Approximately(appliedPerfectLowerBeats, perfectLowerBeats) &&
            Mathf.Approximately(appliedPerfectUpperBeats, perfectUpperBeats);

        if (unchanged)
        {
            return;
        }

        RecalculateRhythmFromBpm(false);
    }

    private void RecalculateRhythmFromBpm(bool resetSpawnTimer)
    {
        float safeBpm = Mathf.Max(1f, bpm);
        secondsPerBeat = 60f / safeBpm;

        spawnRateSeconds = Mathf.Max(0.01f, spawnIntervalBeats * secondsPerBeat);
        incomingSpawnDelaySeconds = Mathf.Max(0f, incomingSpawnDelayBeats * secondsPerBeat);
        holdDurationSeconds = Mathf.Max(0.01f, holdDurationBeats * secondsPerBeat);
        shallowThresholdSeconds = Mathf.Max(0f, shallowThresholdBeats * secondsPerBeat);
        perfectLowerSeconds = Mathf.Max(0f, perfectLowerBeats * secondsPerBeat);
        perfectUpperSeconds = Mathf.Max(perfectLowerSeconds, perfectUpperBeats * secondsPerBeat);

        noteSpeed = CalculateNoteSpeed();

        if (resetSpawnTimer)
        {
            spawnTimer = spawnRateSeconds + incomingSpawnDelaySeconds;
        }
        else
        {
            float cycleProgress = previousSpawnRateSeconds > 0.0001f
                ? Mathf.Clamp01(1f - (spawnTimer / previousSpawnRateSeconds))
                : 0f;
            spawnTimer = Mathf.Max(0.01f, spawnRateSeconds * (1f - cycleProgress));
        }

        previousSpawnRateSeconds = spawnRateSeconds;
        appliedBpm = bpm;
        appliedSpawnIntervalBeats = spawnIntervalBeats;
        appliedTravelBeatsToTarget = travelBeatsToTarget;
        appliedHoldDurationBeats = holdDurationBeats;
        appliedShallowThresholdBeats = shallowThresholdBeats;
        appliedPerfectLowerBeats = perfectLowerBeats;
        appliedPerfectUpperBeats = perfectUpperBeats;
    }

    private float CalculateNoteSpeed()
    {
        float travelSeconds = Mathf.Max(0.01f, travelBeatsToTarget * secondsPerBeat);

        if (spawnPoint == null || wadahTarget == null)
        {
            return 600f;
        }

        float distanceToTarget = Mathf.Abs(spawnPoint.anchoredPosition.x - wadahTarget.anchoredPosition.x);
        if (distanceToTarget <= 0.01f)
        {
            return 600f;
        }

        // Keep the note arrival synchronized to beat timing regardless of BPM value.
        return distanceToTarget / travelSeconds;
    }

    private float GetNormalizedHorizontalOverlap(RectTransform a, RectTransform b)
    {
        if (!TryGetHorizontalSpan(a, out float aMin, out float aMax) ||
            !TryGetHorizontalSpan(b, out float bMin, out float bMax))
        {
            return 0f;
        }

        float overlapWidth = Mathf.Min(aMax, bMax) - Mathf.Max(aMin, bMin);
        if (overlapWidth <= 0f)
        {
            return 0f;
        }

        float aWidth = Mathf.Max(0.0001f, aMax - aMin);
        float bWidth = Mathf.Max(0.0001f, bMax - bMin);
        float baseWidth = Mathf.Min(aWidth, bWidth);
        return Mathf.Clamp01(overlapWidth / baseWidth);
    }

    private bool HasPassedTarget(RectTransform noteRect)
    {
        if (noteRect == null || wadahTarget == null)
        {
            return false;
        }

        if (!TryGetHorizontalSpan(noteRect, out float noteMin, out float noteMax) ||
            !TryGetHorizontalSpan(wadahTarget, out float targetMin, out float targetMax))
        {
            return false;
        }

        return noteMax < (targetMin - latePassMargin);
    }

    private bool TryGetHorizontalSpan(RectTransform rectTransform, out float minX, out float maxX)
    {
        minX = 0f;
        maxX = 0f;

        if (rectTransform == null)
        {
            return false;
        }

        Transform spaceRoot = laneRoot != null ? laneRoot : rectTransform.parent;
        if (spaceRoot == null)
        {
            return false;
        }

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector3 p0 = spaceRoot.InverseTransformPoint(corners[0]);
        Vector3 p1 = spaceRoot.InverseTransformPoint(corners[1]);
        Vector3 p2 = spaceRoot.InverseTransformPoint(corners[2]);
        Vector3 p3 = spaceRoot.InverseTransformPoint(corners[3]);

        minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x);
        maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x);
        return true;
    }

    private void UpdateDepthBar()
    {
        if (depthBarFill == null)
        {
            return;
        }

        float fill = isHolding && holdDurationSeconds > 0f
            ? Mathf.Clamp01(holdTimer / holdDurationSeconds)
            : 0f;

        depthBarFill.fillAmount = fill;
    }

    private void SetFeedback(string message, Color color)
    {
        if (feedbackText == null)
        {
            return;
        }

        feedbackText.text = message;
        feedbackText.color = color;
    }

    private bool TryStartTutorial()
    {
        GameObject resolvedTutorialRoot = ResolveTutorialRoot();
        if (resolvedTutorialRoot == null && !HasAnyTutorialStep())
        {
            return false;
        }

        if (resolvedTutorialRoot != null)
        {
            resolvedTutorialRoot.SetActive(true);
        }

        if (laneRoot != null)
        {
            laneRoot.gameObject.SetActive(false);
        }

        if (depthBarFill != null)
        {
            depthBarFill.fillAmount = 0f;
            depthBarFill.gameObject.SetActive(false);
        }

        BindTutorialButton();
        tutorialStepIndex = 0;
        ShowTutorialStep(tutorialStepIndex);
        SetFeedback(string.Empty, Color.white);
        return true;
    }

    private void BindTutorialButton()
    {
        if (tutorialButtonBound)
        {
            return;
        }

        Button activeBtn = ActiveTutorialAdvanceButton;

        if (activeBtn == null)
        {
            GameObject resolvedTutorialRoot = ResolveTutorialRoot();
            if (resolvedTutorialRoot != null)
            {
                activeBtn = resolvedTutorialRoot.GetComponentInChildren<Button>(true);
            }
        }

        if (activeBtn == null)
        {
            return;
        }

        activeBtn.onClick.AddListener(OnTutorialAdvanceClicked);
        tutorialButtonBound = true;
    }

    private void OnTutorialAdvanceClicked()
    {
        if (minigameFinished || gameplayStarted || !minigameInitialized)
        {
            return;
        }

        int stepCount = GetTutorialStepCount();
        if (stepCount <= 0)
        {
            CompleteTutorialAndStartGameplay();
            return;
        }

        tutorialStepIndex++;
        if (tutorialStepIndex >= stepCount)
        {
            CompleteTutorialAndStartGameplay();
            return;
        }

        ShowTutorialStep(tutorialStepIndex);
    }

    private void ShowTutorialStep(int stepIndex)
    {
        if (!HasAnyTutorialStep())
        {
            return;
        }

        for (int i = 0; i < ActiveTutorialSteps.Length; i++)
        {
            if (ActiveTutorialSteps[i] != null)
            {
                ActiveTutorialSteps[i].SetActive(i == stepIndex);
            }
        }
    }

    private void CompleteTutorialAndStartGameplay()
    {
        GameObject resolvedTutorialRoot = ResolveTutorialRoot();
        if (resolvedTutorialRoot != null)
        {
            resolvedTutorialRoot.SetActive(false);
        }

        StartGameplay();
    }

    private void StartGameplay()
    {
        if (gameplayStarted)
        {
            return;
        }

        gameplayStarted = true;

        if (laneRoot != null)
        {
            laneRoot.gameObject.SetActive(true);
        }

        if (depthBarFill != null)
        {
            depthBarFill.fillAmount = 0f;
            depthBarFill.gameObject.SetActive(true);
        }

        SetCompressedVisual(false);
        PrepareAndPlayTrack();
        SetFeedback(string.Empty, Color.white);
    }

    private GameObject ResolveTutorialRoot()
    {
        if (ActiveTutorialRoot != null)
        {
            return ActiveTutorialRoot;
        }

        for (int i = 0; i < ActiveTutorialSteps.Length; i++)
        {
            if (ActiveTutorialSteps[i] != null && ActiveTutorialSteps[i].transform.parent != null)
            {
                return ActiveTutorialSteps[i].transform.parent.gameObject;
            }
        }

        if (ActiveTutorialAdvanceButton != null)
        {
            return ActiveTutorialAdvanceButton.gameObject;
        }

        return null;
    }

    private bool HasAnyTutorialStep()
    {
        for (int i = 0; i < ActiveTutorialSteps.Length; i++)
        {
            if (ActiveTutorialSteps[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private int GetTutorialStepCount()
    {
        int count = 0;

        for (int i = 0; i < ActiveTutorialSteps.Length; i++)
        {
            if (ActiveTutorialSteps[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void TryFinishMinigame()
    {
        if (!autoCompleteByPerfectCount)
        {
            return;
        }

        if (completeWhenTrackEnds && HasValidTrack())
        {
            return;
        }

        if (perfectCompressionCount < Mathf.Max(1, requiredPerfectCompressions))
        {
            return;
        }

        CompleteMinigame();
    }

    private void PrepareAndPlayTrack()
    {
        hasTrackStarted = false;

        if (backingTrackSource == null || backingTrackClip == null || !playTrackOnStart)
        {
            return;
        }

        backingTrackSource.volume = Mathf.Clamp01(backingTrackVolume);
        backingTrackSource.clip = backingTrackClip;
        backingTrackSource.Play();
        hasTrackStarted = true;
    }

    private void EnsureAudioSources()
    {
        if (backingTrackSource == null)
        {
            backingTrackSource = GetComponent<AudioSource>();
        }

        if (sfxSource == null || sfxSource == backingTrackSource)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
        }
    }

    private void CheckTrackCompletion()
    {
        if (!completeWhenTrackEnds || !HasValidTrack())
        {
            return;
        }

        if (!hasTrackStarted)
        {
            if (backingTrackSource.isPlaying)
            {
                hasTrackStarted = true;
            }

            return;
        }

        if (!backingTrackSource.isPlaying)
        {
            CompleteMinigame();
        }
    }

    private bool HasValidTrack()
    {
        return backingTrackSource != null && backingTrackClip != null;
    }

    private void CompleteMinigame()
    {
        if (minigameFinished)
        {
            return;
        }

        minigameFinished = true;
        gameplayStarted = false;
        SetCompressedVisual(false);

        GameObject resolvedTutorialRoot = ResolveTutorialRoot();
        if (resolvedTutorialRoot != null)
        {
            resolvedTutorialRoot.SetActive(false);
        }

        if (laneRoot != null)
        {
            laneRoot.gameObject.SetActive(false);
        }

        if (backingTrackSource != null && backingTrackSource.isPlaying)
        {
            backingTrackSource.Stop();
        }

        PlayAmbulanceLoopOnDetachedSource();

        if (gameManager != null)
        {
            gameManager.RegisterMinigameCPRResult(
                perfectCompressionCount,
                shallowCompressionCount,
                tooLongCompressionCount,
                missInputCount,
                lateCount);
        }

        onMinigameFinished?.Invoke();

        if (gameManager != null)
        {
            gameManager.OnMiniGameComplete(PlayerPrefs.GetString("SelectedLanguage", "ID") == "EN" ? "CPR completed with correct rhythm." : minigameSuccessFeedback);
        }
    }

    private void OnDestroy()
    {
        if (tutorialAdvanceButton != null && tutorialButtonBound)
        {
            tutorialAdvanceButton.onClick.RemoveListener(OnTutorialAdvanceClicked);
        }

        for (int i = 0; i < activeNotes.Count; i++)
        {
            if (activeNotes[i] != null && activeNotes[i].rect != null)
            {
                Destroy(activeNotes[i].rect.gameObject);
            }
        }

        activeNotes.Clear();
    }

    private void SetCompressedVisual(bool isActive)
    {
        if (tebaringCompressed != null)
        {
            tebaringCompressed.SetActive(isActive);
        }
    }
}
