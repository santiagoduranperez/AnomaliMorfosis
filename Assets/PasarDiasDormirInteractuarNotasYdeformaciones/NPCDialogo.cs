using UnityEngine;
using UnityEngine.InputSystem;

// NPC basico de dialogo (paciente de la instalacion, rol de Diseno de Juego / Narrativa).
// Reutiliza el mismo panel de lectura que los Expedientes (GameManagerHistoria.AbrirLecturaTexto),
// pero a diferencia de Expediente.cs, el NPC no se desactiva al cerrar: se puede hablar
// con el mismo personaje varias veces. Cada interaccion avanza a la siguiente frase de la lista
// (y vuelve a la primera al llegar al final).
public class NPCDialogo : MonoBehaviour
{
    [Header("Dialogo (rol de Diseno de Juego / Narrativa: completar frases reales)")]
    [TextArea(2, 4)]
    public string[] frases = { "..." };

    [Header("Peligroso (opcional)")]
    [Tooltip("Si esta tildado, en vez de dialogo el NPC resta vida al jugador al primer contacto (paciente peligroso).")]
    public bool esPeligroso = false;
    [Tooltip("Solo aplica si 'esPeligroso' esta tildado.")]
    public int danoAlTocar = 2;

    private int indiceFrase = 0;
    private bool jugadorCerca = false;
    private bool yaHizoDano = false;

    private void Update()
    {
        if (esPeligroso) return; // los peligrosos no dialogan, solo hacen dano al contacto
        if (Keyboard.current == null) return;

        if (jugadorCerca && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (frases.Length == 0 || GameManagerHistoria.Instance == null) return;
            GameManagerHistoria.Instance.AbrirLecturaTexto(frases[indiceFrase]);
            indiceFrase = (indiceFrase + 1) % frases.Length;

            // Agregado el 08/09: hablar con el NPC amistoso pasa a ser, junto con el
            // papel del dia, requisito para poder dormir (ver GameManagerHistoria.PuedeDormir).
            GameManagerHistoria.Instance.RegistrarDialogoNpc();
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
        if (!EsJugador(other)) return;

        if (esPeligroso)
        {
            if (!yaHizoDano && GameManagerHistoria.Instance != null)
            {
                yaHizoDano = true;
                GameManagerHistoria.Instance.RecibirDano(danoAlTocar);
            }
            return;
        }

        jugadorCerca = true;
        if (GameManagerHistoria.Instance != null)
            GameManagerHistoria.Instance.MostrarPrompt("[E] Hablar");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!EsJugador(other)) return;

        if (esPeligroso)
        {
            yaHizoDano = false; // puede volver a hacer dano si el jugador vuelve a tocarlo
            return;
        }

        jugadorCerca = false;
        if (GameManagerHistoria.Instance != null)
            GameManagerHistoria.Instance.OcultarPrompt();
    }
}
