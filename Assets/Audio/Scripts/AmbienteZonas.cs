using UnityEngine;

// =====================================================================
// AmbienteZonas.cs — Bloque D (Ambiente y espacialización)
// AnomaliMorfosis · Parcial 2 Audio Interactivo · Santiago Duran Perez
//
// Dos ambientes de fondo en loop (2D, grupo Ambiente):
//  - Instalación: zumbido eléctrico de hospital (celda, corredor, sala).
//  - Subsuelo: rumor grave y crujidos metálicos (zona de los jefes).
// EVENTO: entrar o salir de la zona del subsuelo. Hace un fundido cruzado
// (no un corte) para que el cambio de lugar se sienta como bajar una escalera.
// =====================================================================
public class AmbienteZonas : MonoBehaviour
{
    [SerializeField] private AudioSource ambienteInstalacion;
    [SerializeField] private AudioSource ambienteSubsuelo;
    [SerializeField] private Transform jugador;
    [SerializeField] private Bounds zonaSubsuelo;              // la arma el constructor
    [SerializeField] private float volumenInstalacion = 0.55f;
    [SerializeField] private float volumenSubsuelo = 0.7f;
    [Tooltip("Volumen general de ambientes y fuentes 3D (0.8 = 20% mas bajo)")]
    public float volumenGeneral = 0.8f;
    [SerializeField, Min(0.1f)] private float segundosFundido = 2.5f;

    // La música adaptativa lee esto para saber si el jugador bajó al subsuelo.
    public static bool JugadorEnSubsuelo { get; private set; }

    void Start()
    {
        JugadorEnSubsuelo = false;
        volumenInstalacion *= volumenGeneral; volumenSubsuelo *= volumenGeneral;
        foreach (var f in FindObjectsByType<AudioSource>())          // fuentes 3D del bloque D, 20% mas bajas
            if (f.gameObject.name.StartsWith("3D_")) f.volume *= volumenGeneral;
        ambienteInstalacion.volume = volumenInstalacion;
        ambienteSubsuelo.volume = 0f;
    }

    void Update()
    {
        if (jugador == null) return;

        bool dentro = zonaSubsuelo.Contains(jugador.position);
        if (dentro != JugadorEnSubsuelo)
        {
            JugadorEnSubsuelo = dentro;
            Debug.Log(dentro ? "AmbienteZonas: entra al SUBSUELO" : "AmbienteZonas: sale a la INSTALACION");
        }

        // Fundido cruzado: uno sube mientras el otro baja, en "segundosFundido".
        float paso = Time.deltaTime / segundosFundido;
        float objInst = dentro ? 0f : volumenInstalacion;
        float objSub  = dentro ? volumenSubsuelo : 0f;
        ambienteInstalacion.volume = Mathf.MoveTowards(ambienteInstalacion.volume, objInst, paso * volumenInstalacion);
        ambienteSubsuelo.volume    = Mathf.MoveTowards(ambienteSubsuelo.volume,    objSub,  paso * volumenSubsuelo);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.3f);
        Gizmos.DrawCube(zonaSubsuelo.center, zonaSubsuelo.size);
    }
}
