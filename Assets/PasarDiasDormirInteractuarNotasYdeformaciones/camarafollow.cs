using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    public Transform objetivo;

    [Header("Posición y Distancia")]
    public Vector3 offset = new Vector3(0f, 1.5f, -3f);
    public float sensibilidadMouse = 2f;

    [Header("Colisión con Paredes")]
    public LayerMask capasColision; 
    public float radioColision = 0.25f;

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

        Quaternion rotacion = Quaternion.Euler(rotacionX, rotacionY, 0f);
        Vector3 puntoObjetivo = objetivo.position + Vector3.up * offset.y;
        Vector3 direccionAtras = rotacion * new Vector3(0, 0, offset.z);
        Vector3 posicionDeseada = puntoObjetivo + direccionAtras;

        // Sistema de colision: una esfera desde el personaje hacia la camara
        Vector3 direccionRaycast = (posicionDeseada - puntoObjetivo).normalized;
        float distanciaMax = Mathf.Abs(offset.z);

        if (Physics.SphereCast(puntoObjetivo, radioColision, direccionRaycast, out RaycastHit hit, distanciaMax, capasColision))
        { 
            transform.position = puntoObjetivo + direccionRaycast * hit.distance;
        }
        else
        {
            transform.position = posicionDeseada;
        }

        transform.LookAt(puntoObjetivo);
    }
}