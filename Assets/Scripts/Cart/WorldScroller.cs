using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    [SerializeField] private CartMotion cart;
    private Transform roadSurface;
    private void Awake() { roadSurface = transform.Find("RoadSurface"); }
    private void Update()
    {
        if (cart == null) return;
        float angle = cart.Heading * Mathf.Deg2Rad;
        transform.position += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (cart.Speed * 5f * Time.deltaTime);
        // The uniform road must cover the camera even after the scrolling tiles have looped.
        if (roadSurface != null && Camera.main != null)
        {
            Vector3 roadPosition = roadSurface.position;
            roadPosition.x = Camera.main.transform.position.x;
            roadSurface.position = roadPosition;
        }
    }
}
