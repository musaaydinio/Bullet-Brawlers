using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hedef ve Mesafe")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -7);

    [Header("Hýz Ayarý")]
    public float smoothSpeed = 10f;

    private void LateUpdate()
    {
        if (target == null) return;
       
        Quaternion rotation = Quaternion.Euler(30f, target.eulerAngles.y, 0f);
        Vector3 desiredPosition = target.position + (rotation * offset);
       
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
     
        transform.rotation = rotation;
    }
}