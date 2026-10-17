using UnityEngine;
using UnityEngine.Audio;

// =====================================================================
// AudioDegradacion.cs  —  Bloque F (Programación de audio)
// AnomaliMorfosis · Parcial 2 Audio Interactivo · Santiago Duran Perez
//
// OBJETIVO: que el sonido del mundo se "enferme" igual que el personaje.
//  1) Cada día que pasa, el AMBIENTE suena más apagado (filtro paso bajo
//     que se va cerrando), como el salto que rinde cada vez menos.
//  2) Cuando la VIDA está baja, la MÚSICA se apaga (como oído tapado),
//     para que el jugador sienta el peligro sin mirar el HUD.
//
// PARÁMETROS QUE MODIFICA (expuestos en el Audio Mixer "MezclaJuego"):
//  - "CorteAmbiente": cutoff del Lowpass del grupo Ambiente (Hz)
//  - "CorteMusica":   cutoff del Lowpass del grupo Música   (Hz)
// =====================================================================
public class AudioDegradacion : MonoBehaviour
{
    [Header("Mixer del proyecto")]
    [SerializeField] private AudioMixer mixer;   // arrastrar MezclaJuego

    [Header("Degradación por día (Ambiente)")]
    [SerializeField] private int diaFinal = 16;            // último día del juego (GameManagerHistoria termina en el 16)
    [SerializeField] private float corteDia1 = 22000f;     // día 1: sin filtro (Hz)
    [SerializeField] private float corteDiaFinal = 1500f;  // último día: muy apagado

    [Header("Vida baja (Música)")]
    // Se usa PORCENTAJE y no un número fijo: desde el día 8 la vida máxima baja
    // a 3, y con "vida <= 3" la música quedaría tapada siempre.
    [SerializeField, Range(0f, 1f)] private float porcentajeCritico = 0.35f;
    [SerializeField] private float corteVidaNormal = 22000f;
    [SerializeField] private float corteVidaCritica = 600f;
    [SerializeField, Min(0f)] private float velocidad = 3f; // suavizado del cambio

    private float corteMusicaActual;

    void Start()
    {
        corteMusicaActual = corteVidaNormal;
    }

    void Update()
    {
        // Si no hay GameManager en la escena (escena de prueba), no hace nada.
        GameManagerHistoria gm = GameManagerHistoria.Instance;
        if (gm == null || mixer == null) return;

        // ---- 1) AMBIENTE según el DÍA ---------------------------------
        // t va de 0 (día 1) a 1 (día final). Clamp01 evita pasarse.
        float t = Mathf.Clamp01((gm.diaActual - 1f) / (diaFinal - 1f));
        float corteAmbiente = Mathf.Lerp(corteDia1, corteDiaFinal, t);
        mixer.SetFloat("CorteAmbiente", corteAmbiente);

        // ---- 2) MÚSICA según la VIDA (lógica condicional) -------------
        float objetivo;
        float porcentajeVida = (float)gm.vidaActual / gm.vidaMaxima;
        if (porcentajeVida <= porcentajeCritico)
            objetivo = corteVidaCritica;   // peligro: música tapada
        else
            objetivo = corteVidaNormal;    // normal: música limpia

        // Se acerca de a poco al objetivo para que no haya un corte brusco.
        corteMusicaActual = Mathf.Lerp(corteMusicaActual, objetivo, Time.deltaTime * velocidad);
        mixer.SetFloat("CorteMusica", corteMusicaActual);
    }
}
