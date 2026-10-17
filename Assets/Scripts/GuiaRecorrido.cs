using System.Collections;
using UnityEngine;

// Guia de recorrido (path route) — Parcial 2, Santiago.
// Una luz tenue y calida que "flota" sobre el proximo objetivo del dia, para que el jugador
// sepa a donde ir sin carteles (encaja con la ambientacion de terror):
//   1) el papel/expediente del dia  ->  2) el paciente amistoso (hay que hablarle)
//   ->  3) la cama (dormir)  ->  desde el dia 16, la salida del subsuelo (ZonaSalidaVictoria).
// Lee el estado de GameManagerHistoria (Christian) sin modificarlo.
// Usa dos corrutinas: una elige el objetivo cada 0.3 s y otra hace latir la luz.
[RequireComponent(typeof(Light))]
public class GuiaRecorrido : MonoBehaviour
{
    [Header("Luz guia")]
    [SerializeField] private Color color = new Color(1f, 0.62f, 0.28f);
    [SerializeField] private float intensidadBase = 2.2f;
    [SerializeField] private float amplitudLatido = 0.8f;
    [SerializeField] private float velocidadLatido = 2.2f;
    [SerializeField] private float alturaSobreObjetivo = 1.4f;
    [SerializeField] private float velocidadSeguimiento = 3f;

    [Header("Cerca del objetivo")]
    [Tooltip("Cuando el jugador esta a menos de esta distancia, la luz baja para no encandilar.")]
    [SerializeField] private float distanciaAtenuar = 2.5f;

    [Header("Desde que dia se guia a la salida del subsuelo")]
    [SerializeField] private int diaSubsuelo = 16;
    [SerializeField] private string nombreSalida = "ZonaSalidaVictoria";

    private Light luz;
    private Transform objetivo;
    private Transform salida;
    private Cama cama;
    private NPCDialogo npcAmistoso;
    private float factorDistancia = 1f;

    private void Awake()
    {
        luz = GetComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.range = 4.5f;
        luz.shadows = LightShadows.None;
    }

    private void Start()
    {
        cama = FindAnyObjectByType<Cama>();
        foreach (var npc in FindObjectsByType<NPCDialogo>(FindObjectsSortMode.None))
            if (!npc.esPeligroso) { npcAmistoso = npc; break; }
        var go = GameObject.Find(nombreSalida);
        if (go != null) salida = go.transform;

        StartCoroutine(ElegirObjetivo());
        StartCoroutine(Latir());
    }

    private IEnumerator ElegirObjetivo()
    {
        var espera = new WaitForSeconds(0.3f);
        while (true)
        {
            objetivo = CalcularObjetivo();
            yield return espera;
        }
    }

    private Transform CalcularObjetivo()
    {
        var gm = GameManagerHistoria.Instance;
        if (gm == null) return null;

        if (gm.diaActual >= diaSubsuelo)
            return salida;

        if (gm.papelesRecolectadosHoy < gm.ObtenerPapelesRequeridosHoy())
        {
            int i = gm.diaActual - 1;
            if (gm.papelesGameObjects != null && i >= 0 && i < gm.papelesGameObjects.Length)
            {
                var papel = gm.papelesGameObjects[i];
                if (papel != null && papel.activeInHierarchy) return papel.transform;
            }
        }

        if (!gm.PuedeDormir() && npcAmistoso != null && npcAmistoso.gameObject.activeInHierarchy)
            return npcAmistoso.transform;

        return cama != null ? cama.transform : null;
    }

    private IEnumerator Latir()
    {
        while (true)
        {
            float latido = Mathf.Sin(Time.time * velocidadLatido) * amplitudLatido;
            luz.intensity = objetivo == null ? 0f : Mathf.Max(0f, (intensidadBase + latido) * factorDistancia);
            yield return null;
        }
    }

    private void Update()
    {
        if (objetivo == null) return;

        Vector3 destino = objetivo.position + Vector3.up * alturaSobreObjetivo;
        if ((transform.position - destino).sqrMagnitude > 400f)
            transform.position = destino; // si el objetivo esta muy lejos (ej. teletransporte al subsuelo), salta directo
        else
            transform.position = Vector3.Lerp(transform.position, destino, Time.deltaTime * velocidadSeguimiento);

        var gm = GameManagerHistoria.Instance;
        if (gm != null && gm.jugadorTransform != null)
        {
            float d = Vector3.Distance(gm.jugadorTransform.position, objetivo.position);
            factorDistancia = Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(1f, distanciaAtenuar, d));
        }
    }
}
