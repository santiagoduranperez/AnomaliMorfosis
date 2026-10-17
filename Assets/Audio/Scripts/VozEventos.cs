using UnityEngine;
using UnityEngine.Audio;

// =====================================================================
// VozEventos.cs — Integración del Bloque E (voces) + ducking de música
// AnomaliMorfosis · Parcial 2 Audio Interactivo
// Integración: Santiago Duran Perez (titular A) · Voces: Kiara y Uxia (E)
//
// Dispara cada línea de voz con un EVENTO del juego (salida por el grupo Voz):
//  - Abrir el expediente del día 1 -> Narradora (Voz_01)
//  - Abrir el expediente del día 7 -> Narradora (Voz_05)
//  - Acercarse al paciente amistoso -> Paciente (Voz_02 / Voz_03, se alternan)
//  - Bajar al subsuelo -> Altavoz (Voz_04 / Voz_06, se alternan)
// Mientras alguien habla, la MÚSICA baja (ducking) para que la voz se entienda.
// =====================================================================
public class VozEventos : MonoBehaviour
{
    [Header("Fuentes (Output = Voz)")]
    [SerializeField] private AudioSource vozNarradorAltavoz;  // 2D
    [SerializeField] private AudioSource vozPaciente;         // 3D, pegada al paciente

    [Header("Clips (arrastrar los WAV de Kiara y Uxia)")]
    [SerializeField] private AudioClip expedienteDia1;       // Voz_Kiara_01
    [SerializeField] private AudioClip expedienteDia7;       // Voz_Kiara_05
    [SerializeField] private AudioClip[] frasesPaciente;     // Voz_Kiara_02, Voz_Uxia_03
    [SerializeField] private AudioClip[] frasesAltavoz;      // Voz_Uxia_04, Voz_Uxia_06

    [Header("Referencias")]
    [SerializeField] private Transform jugador;
    [SerializeField] private Transform paciente;
    [SerializeField, Min(0.5f)] private float distanciaParaHablar = 3f;
    [SerializeField, Min(0f)] private float esperaEntreFrases = 8f;

    [Header("Ducking de la música")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string parametroVolumenMusica = "VolumenMusica";
    [SerializeField] private float bajadaDb = -10f;          // cuánto baja la música
    [SerializeField, Min(1f)] private float velocidadDbPorSeg = 40f;

    private bool lecturaAbiertaAntes, cercaAntes, subsueloAntes;
    private bool dijoDia1, dijoDia7;
    private int iPaciente, iAltavoz;
    private float ultimoPaciente = -999f;
    private float volumenMusicaDb;

    void Update()
    {
        GameManagerHistoria gm = GameManagerHistoria.Instance;

        // 1) Expediente abierto (flanco: recién se abrió)
        bool lecturaAbierta = gm != null && gm.canvasLectura != null && gm.canvasLectura.activeInHierarchy;
        if (lecturaAbierta && !lecturaAbiertaAntes)
        {
            if (gm.diaActual == 1 && !dijoDia1) { dijoDia1 = true; Decir(vozNarradorAltavoz, expedienteDia1, "expediente dia 1"); }
            else if (gm.diaActual == 7 && !dijoDia7) { dijoDia7 = true; Decir(vozNarradorAltavoz, expedienteDia7, "expediente dia 7"); }
        }
        lecturaAbiertaAntes = lecturaAbierta;

        // 2) Acercarse al paciente amistoso (con espera para no repetir)
        bool cerca = jugador != null && paciente != null &&
                     Vector3.Distance(jugador.position, paciente.position) <= distanciaParaHablar;
        if (cerca && !cercaAntes && Time.time - ultimoPaciente >= esperaEntreFrases && frasesPaciente.Length > 0)
        {
            ultimoPaciente = Time.time;
            Decir(vozPaciente, frasesPaciente[iPaciente], "paciente amistoso");
            iPaciente = (iPaciente + 1) % frasesPaciente.Length;
        }
        cercaAntes = cerca;

        // 3) Bajar al subsuelo -> altavoz
        bool enSubsuelo = AmbienteZonas.JugadorEnSubsuelo;
        if (enSubsuelo && !subsueloAntes && frasesAltavoz.Length > 0)
        {
            Decir(vozNarradorAltavoz, frasesAltavoz[iAltavoz], "altavoz subsuelo");
            iAltavoz = (iAltavoz + 1) % frasesAltavoz.Length;
        }
        subsueloAntes = enSubsuelo;

        // 4) Ducking: si alguien habla, la música baja; si no, vuelve a 0 dB
        bool hablando = (vozNarradorAltavoz != null && vozNarradorAltavoz.isPlaying) ||
                        (vozPaciente != null && vozPaciente.isPlaying);
        float objetivo = hablando ? bajadaDb : 0f;
        volumenMusicaDb = Mathf.MoveTowards(volumenMusicaDb, objetivo, velocidadDbPorSeg * Time.deltaTime);
        if (mixer != null) mixer.SetFloat(parametroVolumenMusica, volumenMusicaDb);
    }

    private void Decir(AudioSource fuente, AudioClip clip, string evento)
    {
        if (fuente == null || clip == null)
        {
            Debug.Log($"VozEventos: evento '{evento}' (todavia sin clip asignado)");
            return;
        }
        fuente.Stop();
        fuente.clip = clip;
        fuente.Play();
        Debug.Log($"VozEventos: '{evento}' -> {clip.name}");
    }
}
