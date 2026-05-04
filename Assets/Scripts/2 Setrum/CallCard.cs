using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CallCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string cardId = "";
    [SerializeField] private string cardDialog = "Dialog kartu";
    [SerializeField] private Transform visualChild; // Child dengan Image (akan offset saat hover)
    [SerializeField] private Transform buttonAreaChild; // Child dengan Button (tetap static)
    [SerializeField] private float hoverYOffset = 150f;
    [SerializeField] private float hoverEnterDelay = 0.05f;
    [SerializeField] private float hoverTransitionDuration = 0.12f;
    
    private RectTransform visualTransform;
    private Vector3 originalVisualPosition;
    private System.Action onCardSelectedCallback;
    private Button cardButton;
    private bool isRemoved;
    private Coroutine hoverRoutine;

    private void Awake()
    {
        if (visualChild != null)
        {
            Image visualImage = visualChild.GetComponent<Image>();
            if (visualImage != null)
            {
                visualImage.raycastTarget = false;
                visualTransform = visualImage.rectTransform;
            }
            else
            {
                visualTransform = visualChild.GetComponent<RectTransform>();
            }

            if (visualTransform != null)
            {
                originalVisualPosition = visualTransform.localPosition;
            }
        }

        if (buttonAreaChild != null)
        {
            cardButton = buttonAreaChild.GetComponent<Button>();

            CallCardHoverArea hoverArea = buttonAreaChild.GetComponent<CallCardHoverArea>();
            if (hoverArea == null)
            {
                hoverArea = buttonAreaChild.gameObject.AddComponent<CallCardHoverArea>();
            }
        }
        else
        {
            cardButton = GetComponent<Button>();
        }

        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardClicked);
        }
    }

    public void SetupCard(System.Action onSelected)
    {
        onCardSelectedCallback = onSelected;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        HandleHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HandleHoverExit();
    }

    public void HandleHoverEnter()
    {
        if (isRemoved || visualTransform == null)
        {
            return;
        }

        if (hoverRoutine != null)
        {
            StopCoroutine(hoverRoutine);
        }

        hoverRoutine = StartCoroutine(HoverInRoutine());
    }

    public void HandleHoverExit()
    {
        if (isRemoved || visualTransform == null)
        {
            return;
        }

        if (hoverRoutine != null)
        {
            StopCoroutine(hoverRoutine);
        }

        hoverRoutine = StartCoroutine(HoverOutRoutine());
    }

    private System.Collections.IEnumerator HoverInRoutine()
    {
        if (hoverEnterDelay > 0f)
        {
            yield return new WaitForSeconds(hoverEnterDelay);
        }

        if (isRemoved || visualTransform == null)
        {
            hoverRoutine = null;
            yield break;
        }

        Vector3 targetPosition = originalVisualPosition - new Vector3(0f, hoverYOffset, 0f);
        yield return AnimateVisualTo(targetPosition);
        hoverRoutine = null;
    }

    private System.Collections.IEnumerator HoverOutRoutine()
    {
        yield return AnimateVisualTo(originalVisualPosition);
        hoverRoutine = null;
    }

    private System.Collections.IEnumerator AnimateVisualTo(Vector3 targetPosition)
    {
        if (visualTransform == null)
        {
            yield break;
        }

        Vector3 startPosition = visualTransform.localPosition;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, hoverTransitionDuration);

        while (elapsed < duration)
        {
            if (visualTransform == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            visualTransform.localPosition = Vector3.LerpUnclamped(startPosition, targetPosition, easedT);
            yield return null;
        }

        visualTransform.localPosition = targetPosition;
    }

    private void OnCardClicked()
    {
        if (isRemoved)
        {
            return;
        }

        onCardSelectedCallback?.Invoke();
    }

    public void RemoveCard()
    {
        isRemoved = true;

        if (hoverRoutine != null)
        {
            StopCoroutine(hoverRoutine);
            hoverRoutine = null;
        }

        gameObject.SetActive(false);
    }

    public string GetCardDialog()
    {
        return cardDialog;
    }

    public string GetCardId()
    {
        if (!string.IsNullOrWhiteSpace(cardId))
        {
            return cardId.Trim();
        }

        return gameObject.name;
    }

    private void OnDestroy()
    {
        if (hoverRoutine != null)
        {
            StopCoroutine(hoverRoutine);
        }

        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(OnCardClicked);
        }
    }
}
