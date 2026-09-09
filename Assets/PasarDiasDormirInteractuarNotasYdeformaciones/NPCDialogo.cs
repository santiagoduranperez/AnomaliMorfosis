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

    [Header("Persecucion (opcional, solo si 'esPeligroso' esta tildado)")]
    [Tooltip("Agregado el 09/09 a pedido de Uxia en el QA: antes el paciente peligroso se quedaba quieto todo el tiempo. Distancia a la que empieza a perseguir al jugador.")]
    public float rangoDeteccion = 6f;
    [Tooltip("Mas lenta que la velocidad base del jugador (5) para que siempre se pueda escapar corriendo.")]
    public float velocidadPersecucion = 2.5f;

    private int indiceFrase = 0;
    private bool jugadorCerca = false;
    private bool yaHizoDano = false;
    private Transform jugadorTransform;

    private void Start()
    {
        var jugadorGO = GameObject.FindGameObjectWithTag("Player");
        if (jugadorGO != null) jugadorTransform = jugadorGO.transform;
    }

    private void Update()
    {
        if (esPeligroso)
        {
            PerseguirSiElJugadorEstaCerca();
            return; // los peligrosos no dialogan, solo persiguen y hacen dano al contacto
        }
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

    // Agregado el 09/09: movimiento simple hacia el jugador dentro del rango de deteccion,
    // sin NavMesh ni raycasts (IA basica, tal como pidieron los profes para el Parcial 1).
    // Se mueve mas lento que el jugador, asi que siempre se puede escapar corriendo.
    private void PerseguirSiElJugadorEstaCerca()
    {
        if (jugadorTransform == null) return;

        float distancia = Vector3.Distance(transform.position, jugadorTransform.position);
        if (distancia > rangoDeteccion) return;

        Vector3 destino = new Vector3(jugadorTransform.position.x, transform.position.y, jugadorTransform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidadPersecucion * Time.deltaTime);
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
