using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;

    public Vector3 offset = new Vector3(0, 4.5f, -6.5f);
    public float smoothSpeed = 10f;

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desirePosition = target.position + target.rotation * offset;
            transform.position = Vector3.Lerp(transform.position, desirePosition, smoothSpeed * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * 2f);
        }
    }
}