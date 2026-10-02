using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Mesafe ve Yükseklik Ayarlarý")]
    public Vector3 tpsOffset = new Vector3(0f, 2.2f, -4.5f); // 3. Þahýs: Arka yörünge mesafesi
    public Vector3 fpsOffset = new Vector3(0f, 1.9f, 0.55f); // 1. Þahýs: Sabit göz hizasý

    [Header("Yukarý / Aþaðý Bakýþ Hassasiyeti")]
    public float pitchSensitivity = 40f;

    [Header("3. Þahýs Bakýþ Sýnýrlarý (Derece)")]
    public float minPitchTPS = -15f;
    public float maxPitchTPS = 45f;

    [Header("1. Þahýs Bakýþ Sýnýrlarý (Karakter Ýçine Girmeyen Doðal Açý)")]
    public float minPitchFPS = -15f; // Aþaðý bakýþ sýnýrý (Gövdenin içine girmemesi için)        
    public float maxPitchFPS = 60f;  // Yukarý bakýþ sýnýrý

    private float pitch = 0f;
    private bool isFirstPerson = false;

    private void LateUpdate()
    {
        if (target == null) return;

        // V tuþuna basýldýðýnda modu deðiþtir
        if (Input.GetKeyDown(KeyCode.V))
        {
            isFirstPerson = !isFirstPerson;
        }

        // Fare dikey hareketini al
        float mouseY = Input.GetAxis("Mouse Y") * pitchSensitivity * Time.deltaTime;
        pitch -= mouseY;

        // Moda göre bakýþ sýnýrlarýný uygula
        float minP = isFirstPerson ? minPitchFPS : minPitchTPS;
        float maxP = isFirstPerson ? maxPitchFPS : maxPitchTPS;
        pitch = Mathf.Clamp(pitch, minP, maxP);

        Quaternion localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (isFirstPerson)
        {
            // 1. ÞAHIS: Kamera göz hizasýnda SABÝT durur, dairesel yay çizmez!
            transform.localPosition = fpsOffset;
            transform.localRotation = localRotation;
        }
        else
        {
            // 3. ÞAHIS: Kamera karakterin arkasýnda dairesel yörüngede (orbit) döner
            transform.localRotation = localRotation;
            transform.localPosition = localRotation * tpsOffset;
        }
    }
}