using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraFollow : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 offset = new Vector3(0f, 2f, -5f);
    public float sensibilidadMouse = 2f;

    private float rotacionX = 0f;
    private float rotacionY = 0f;

    void Start()
    {
        // Bloquea el puntero al centro de la pantalla y lo oculta
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Lee el movimiento del mouse con el nuevo Input System
        if (Mouse.current != null)
        {
            Vector2 deltaMouse = Mouse.current.delta.ReadValue();
            rotacionY += deltaMouse.x * sensibilidadMouse * 0.1f;
            rotacionX -= deltaMouse.y * sensibilidadMouse * 0.1f;

            // Limita el ángulo vertical para no dar la vuelta completa
            rotacionX = Mathf.Clamp(rotacionX, -30f, 60f);
        }

        // Calcula la rotación y la nueva posición en órbita alrededor del cubo
        Quaternion rotacion = Quaternion.Euler(rotacionX, rotacionY, 0f);
        transform.position = objetivo.position + rotacion * offset;

        // La cámara siempre apunta hacia el objetivo
        transform.LookAt(objetivo.position + Vector3.up * 1f);
    }
}