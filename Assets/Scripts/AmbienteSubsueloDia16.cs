using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Ambientacion del subsuelo — Parcial 2, Santiago. Aplica las Clases 11 y 12:
//  - Post-procesado (Clase 12): un Volume con prioridad 20 (por encima del Volume_Terror global)
//    que arranca con Weight 0. Al llegar al dia 16 (cuando el jugador despierta en el subsuelo),
//    una corrutina sube el Weight de 0 a 1 de forma progresiva, igual que el ejemplo de la clase.
//  - Sonido (Clase 11): una Audio Reverb Zone con preset de cueva que se enciende en el subsuelo
//    y acompana a la camara (donde esta el Audio Listener), asi todo lo que suena ahi tiene eco.
// Lee el dia de GameManagerHistoria (Christian) sin modificarlo.
public class AmbienteSubsueloDia16 : MonoBehaviour
{
    [SerializeField] private Volume volumen;
    [SerializeField] private AudioReverbZone eco;
    [SerializeField] private int diaSubsuelo = 16;
    [Tooltip("Segundos que tarda el efecto en aparecer por completo.")]
    [SerializeField] private float duracionTransicion = 4f;

    private bool activo;
    private Transform oyente;

    private void Start()
    {
        if (volumen != null) volumen.weight = 0f;
        if (eco != null) eco.enabled = false;
        StartCoroutine(EsperarSubsuelo());
    }

    private IEnumerator EsperarSubsuelo()
    {
        var espera = new WaitForSeconds(0.5f);
        while (!activo)
        {
            var gm = GameManagerHistoria.Instance;
            if (gm != null && gm.diaActual >= diaSubsuelo)
            {
                activo = true;
                if (eco != null) eco.enabled = true;
                yield return StartCoroutine(Transicion(0f, 1f));
            }
            yield return espera;
        }
    }

    private IEnumerator Transicion(float desde, float hasta)
    {
        float t = 0f;
        while (t < duracionTransicion)
        {
            t += Time.deltaTime;
            if (volumen != null) volumen.weight = Mathf.Lerp(desde, hasta, t / duracionTransicion);
            yield return null;
        }
        if (volumen != null) volumen.weight = hasta;
    }

    private void LateUpdate()
    {
        if (!activo || eco == null) return;
        if (oyente == null)
        {
            var listener = FindAnyObjectByType<AudioListener>();
            if (listener != null) oyente = listener.transform;
        }
        if (oyente != null) eco.transform.position = oyente.position;
    }
}
