using UnityEngine;
using UnityEngine.InputSystem;

public class Cama : MonoBehaviour
{
    private bool jugadorCerca = false;

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (jugadorCerca && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (GameManagerHistoria.Instance.PuedeDormir())
            {
                GameManagerHistoria.Instance.Dormir();
            }
        }
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
                if (GameManagerHistoria.Instance.PuedeDormir())
                {
                    GameManagerHistoria.Instance.MostrarPrompt("[E] Dormir");
                }
                else
                {
                    GameManagerHistoria.Instance.MostrarPrompt("Falta expediente");
                }
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