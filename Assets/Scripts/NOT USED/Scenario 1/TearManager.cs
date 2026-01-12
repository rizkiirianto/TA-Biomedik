using UnityEngine;
using System.Collections;

public class TearManager : MonoBehaviour
{
    // This 'static' variable can be accessed from any script easily
    public static bool isSwiping = false;

    // Assign these in the Inspector
    public GameObject wadahTertutup; // The parent of all the small colliders
    public GameObject wadahTerbuka;
    public GameObject bukaWadahTisuPanel;
    public AudioSource sfxTisuBuka;
    private int totalParts;
    private int tornParts = 0;

    public TargetedWipe targetedWipeScript;

    void Start()
    {
        // At the start, count how many tearable parts there are
        totalParts = wadahTertutup.transform.childCount;
        //Debug.Log(totalParts);
        // Ensure the open image is hidden at the start
        wadahTerbuka.SetActive(false);
    }

    void Update()
    {
        // Check if the left mouse button is being held down
        if (Input.GetMouseButtonDown(0))
        {
            isSwiping = true;
            Debug.Log("Mulai swiping");
        }

        // Check if the left mouse button was released
        if (Input.GetMouseButtonUp(0))
        {
            isSwiping = false;
        }
    }

    // The TearPart script calls this function
    public void PartTorn()
    {
        sfxTisuBuka.Play();
        tornParts++;
        Debug.Log(tornParts);

        // Check if all parts have been torn
        if (tornParts >= totalParts)
        {
            Debug.Log("All parts torn! Opening pack.");
            OpenPack();
        }
    }

    void OpenPack()
    {
        StartCoroutine(OpenAndThenHide());
    }

    IEnumerator OpenAndThenHide()
    {
        wadahTertutup.SetActive(false);
        wadahTerbuka.SetActive(true);
        yield return new WaitForSeconds(2f);
        bukaWadahTisuPanel.SetActive(false);
        targetedWipeScript.ActivateCustomCursor();
    }
    
}