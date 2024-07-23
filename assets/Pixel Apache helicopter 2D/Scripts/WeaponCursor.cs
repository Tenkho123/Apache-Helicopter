using UnityEngine;

public class WeaponCursor : MonoBehaviour
{
    public Transform turretCursor;
    public Transform homingMissileCursor;

    private Transform currentCursor;
    public Transform CurrentCursor {
        get {return currentCursor;}
        set {
            if (currentCursor != null) currentCursor.gameObject.SetActive(false);
            currentCursor = value;
            currentCursor.gameObject.SetActive(true);
        }
    }

    void Start()
    {
        foreach (Transform cursor in transform) cursor.gameObject.SetActive(false);
        CurrentCursor = turretCursor;
        Cursor.visible = false;
    }

    void Update()
    {
        currentCursor.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}
