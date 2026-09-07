using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoObjeto : MonoBehaviour
{
    [Header("Estadísticas de Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 7f;
    public float velocidadRotacion = 10f;

    [Header("Configuración del Dash (Día 7)")]
    public float fuerzaDash = 18f;
    public float cooldownDash = 0.8f;
    private float tiempoSiguienteDash = 0f;

    private Rigidbody rb;
    private bool enElSuelo = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // 1. Sincronizar desde el GameManager
        if (GameManagerHistoria.Instance != null)
        {
            velocidad = GameManagerHistoria.Instance.velocidadActual;
            fuerzaSalto = GameManagerHistoria.Instance.fuerzaSaltoActual;
        }

        // 2. Movimiento direccional
        Vector3 direccion = Vector3.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            direccion.z += 1f;

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            direccion.z -= 1f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            direccion.x -= 1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            direccion.x += 1f;

        // Dirección respecto a la cámara
        Vector3 direccionCamara = Vector3.zero;
        if (Camera.main != null)
        {
            direccionCamara = Camera.main.transform.TransformDirection(direccion);
            direccionCamara.y = 0;
        }

        Vector3 movimiento = direccionCamara.normalized * velocidad;
        rb.linearVelocity = new Vector3(movimiento.x, rb.linearVelocity.y, movimiento.z);

        // 3. Rotación
        if (movimiento.x != 0 || movimiento.z != 0)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(new Vector3(movimiento.x, 0f, movimiento.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);
        }

        // 4. Espacio y dash
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            bool modoDashActivo = GameManagerHistoria.Instance != null && GameManagerHistoria.Instance.modoDashDesbloqueado;

            if (modoDashActivo)
            {
                EjecutarDash();
            }
            else if (enElSuelo)
            {
                EjecutarSalto();
            }
        }
    }

    private void EjecutarSalto()
    {
        rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.Impulse);
        enElSuelo = false;
    }

    private void EjecutarDash()
    {
        if (Time.time >= tiempoSiguienteDash)
        {
            // Configuración del dash
            Vector3 direccionDash = transform.forward;
            rb.AddForce(direccionDash * fuerzaDash, ForceMode.Impulse);

            tiempoSiguienteDash = Time.time + cooldownDash;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        enElSuelo = true;
    }
}