using UnityEngine;

public class DragTest : MonoBehaviour
{
    private void OnMouseDrag()
    {
        // This will only work if the object has a collider
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // Distance from camera
        transform.position = Camera.main.ScreenToWorldPoint(mousePos);
    }
}