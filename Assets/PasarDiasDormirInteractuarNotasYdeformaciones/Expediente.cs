using UnityEngine;
using UnityEngine.InputSystem;

public class Expediente : MonoBehaviour
{
    [Header("Contenido de esta Nota")]
    public Sprite imagenNota;
    [TextArea(3, 6)]
    public string contenidoTexto;

    private bool jugadorCerca = false;

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (jugadorCerca && Keyboard.current.eKey.wasPressedThisFrame)
        {
            GameManagerHistoria.Instance.AbrirLecturaDocumento(this);
        }
    }

    public void OnRecogido()
    {
        GameManagerHistoria.Instance.OcultarPrompt();
        gameObject.SetActive(false);
    }

    private bool EsJugador(Collider col)
    {
        if (col.CompareTag("Player")) return true;
        if (col.transform.root.CompareTag("Player")) return true;
        if (col.attachedRigidbody != null && col.attachedRigidbody.CompareTag("Player")) return true;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (EsJugador(other))
        {
            jugadorCerca = true;
            if (GameManagerHistoria.Instance != null)
            {
                GameManagerHistoria.Instance.MostrarPrompt("[E] Leer");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (EsJugador(other))
        {
            jugadorCerca = false;
            if (GameManagerHistoria.Instance != null)
            {
                GameManagerHistoria.Instance.OcultarPrompt();
            }
        }
    }
}