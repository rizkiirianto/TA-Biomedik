using UnityEngine;
using UnityEngine.UI;

public class TearPart : MonoBehaviour
{
    private bool isTorn = false;
    private Image myImage;

    void Awake()
    {
        myImage = GetComponent<Image>();
    }
    // This function is called when the mouse cursor enters the collider
    void OnMouseEnter()
    {
        // Check the manager to see if we are currently holding the mouse button
        if (TearManager.isSwiping && !isTorn)
        {
            isTorn = true;
            myImage.enabled = true;
            Debug.Log("1 bagian sobek");
            
            // Tell the manager that one more part has been torn
            FindFirstObjectByType<TearManager>().PartTorn();
        }
    }
}
