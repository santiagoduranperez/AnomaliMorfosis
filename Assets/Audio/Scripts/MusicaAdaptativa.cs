using UnityEngine;

// =====================================================================
// MusicaAdaptativa.cs — Bloque B (Música adaptativa, por CAPAS)
// AnomaliMorfosis · Parcial 2 Audio Interactivo · Santiago Duran Perez
//
// Técnica vertical (Clase 10): 3 capas del mismo largo (32 s, 120 bpm)
// suenan SIEMPRE en sincronía; el juego solo cambia cuánto se oye cada una.
// La música nunca se corta ni vuelve a empezar.
//
// ESTADOS Y EVENTOS QUE LOS DISPARAN:
//  CALMA       -> explorar la instalación sin enemigos cerca.
//  TENSIÓN     -> entrar al subsuelo, o un enemigo aparece a menos de
//                 2x su rango de detección.
//  PERSECUCIÓN -> un jefe o un paciente peligroso te detecta (estás dentro
//                 de su rango de detección).
// TIEMPOS (elegidos a propósito):
//  a Tensión: 2 s (sube de a poco, inquieta)
//  a Persecución: 0,3 s (casi un corte: susto)
//  de vuelta a Calma: 4 s (alivio lento, que no se olvide el peligro)
// =====================================================================
public class MusicaAdaptativa : MonoBehaviour
{
    public enum Estado { Calma, Tension, Persecucion }

    [SerializeField] private AudioSource capaCalma;
    [SerializeField] private AudioSource capaTension;
    [SerializeField] private AudioSource capaPersecucion;
    [SerializeField] private Transform jugador;

    [Header("Tiempos de transición (segundos)")]
    [SerializeField, Min(0.05f)] private float aTension = 2f;
    [SerializeField, Min(0.05f)] private float aPersecucion = 0.3f;
    [SerializeField, Min(0.05f)] private float aCalma = 4f;

    [Header("Volumen de cada capa por estado (Calma / Tensión / Persecución)")]
    [SerializeField] private Vector3 mezclaCalma       = new Vector3(0.6f, 0f,   0f);
    [SerializeField] private Vector3 mezclaTension     = new Vector3(0.35f, 0.7f, 0f);
    [SerializeField] private Vector3 mezclaPersecucion = new Vector3(0.15f, 0.5f, 0.8f);
    [Tooltip("Volumen general de la musica (0.8 = 20% mas baja, para que se escuchen los pacientes)")]
    public float volumenGeneral = 0.8f;

    public Estado EstadoActual { get; private set; } = Estado.Calma;

    private EnemigoJefe[] jefes;
    private NPCDialogo[] pacientes;
    private float tiempoFundido;

    void Start()
    {
        jefes = FindObjectsByType<EnemigoJefe>();
        pacientes = FindObjectsByType<NPCDialogo>();
        tiempoFundido = aCalma;
        AplicarInstantaneo(mezclaCalma);
    }

    void Update()
    {
        if (jugador == null) return;

        Estado nuevo = CalcularEstado();
        if (nuevo != EstadoActual)
        {
            // El tiempo del fundido depende de HACIA DÓNDE vamos.
            if (nuevo == Estado.Persecucion) tiempoFundido = aPersecucion;
            else if (nuevo == Estado.Tension) tiempoFundido = aTension;
            else tiempoFundido = aCalma;

            Debug.Log($"MusicaAdaptativa: {EstadoActual} -> {nuevo} ({tiempoFundido} s)");
            EstadoActual = nuevo;
        }

        Vector3 objetivo = EstadoActual == Estado.Persecucion ? mezclaPersecucion
                         : EstadoActual == Estado.Tension ? mezclaTension
                         : mezclaCalma;
        float paso = Time.deltaTime / tiempoFundido;
        capaCalma.volume       = Mathf.MoveTowards(capaCalma.volume,       objetivo.x * volumenGeneral, paso);
        capaTension.volume     = Mathf.MoveTowards(capaTension.volume,     objetivo.y * volumenGeneral, paso);
        capaPersecucion.volume = Mathf.MoveTowards(capaPersecucion.volume, objetivo.z * volumenGeneral, paso);
    }

    private Estado CalcularEstado()
    {
        bool cerca = false;
        Vector3 p = jugador.position;

        foreach (EnemigoJefe j in jefes)
        {
            if (j == null || !j.isActiveAndEnabled) continue;
            float d = Vector3.Distance(p, j.transform.position);
            if (d <= j.rangoDeteccion) return Estado.Persecucion;   // te detectó
            if (d <= j.rangoDeteccion * 2f) cerca = true;           // apareció cerca
        }
        foreach (NPCDialogo n in pacientes)
        {
            if (n == null || !n.esPeligroso || !n.isActiveAndEnabled) continue;
            float d = Vector3.Distance(p, n.transform.position);
            if (d <= n.rangoDeteccion) return Estado.Persecucion;
            if (d <= n.rangoDeteccion * 2f) cerca = true;
        }

        if (cerca || AmbienteZonas.JugadorEnSubsuelo) return Estado.Tension;
        return Estado.Calma;
    }

    private void AplicarInstantaneo(Vector3 m)
    {
        capaCalma.volume = m.x * volumenGeneral; capaTension.volume = m.y * volumenGeneral; capaPersecucion.volume = m.z * volumenGeneral;
    }
}
