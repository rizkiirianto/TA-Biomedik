using UnityEngine;
using UnityEngine.EventSystems;

public class CallCardHoverArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CallCard owner;

    private void Awake()
    {
        owner = GetComponentInParent<CallCard>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner?.HandleHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.HandleHoverExit();
    }
}
