using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hedef ve Mesafe")]
    public Transform target;

    [Header("Mesafe Ayarlarý")]
    public Vector3 offset = new Vector3(0, 1, -10);

    [Header("Yukarý / Aþaðý Bakýþ Ayarlarý")]
    public float pitch = 30f;            
    public float pitchSensitivity = 40f; 
    public float minPitch = 15f;         
    public float maxPitch = 45f;        

    private void LateUpdate()
    {        
        float mouseY = Input.GetAxis("Mouse Y") * pitchSensitivity * Time.deltaTime;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch); 

        Quaternion localRotation = Quaternion.Euler(pitch, 0f, 0f);

        transform.localRotation = localRotation;
        transform.localPosition = localRotation * offset;
    }
}