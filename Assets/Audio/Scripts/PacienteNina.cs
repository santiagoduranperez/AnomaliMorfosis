using UnityEngine;
using UnityEngine.Audio;

// PacienteNina (Santiago, Parcial 2 Audio Interactivo)
// Se agrega solo al Paciente_Amistoso al iniciar la escena, sin tocar NPCDialogo ni la escena.
// Lo convierte en una paciente adolescente indefensa, aterrada:
//  - Figura propia: chica flaca con bata de hospital, pelo largo que le tapa la cara,
//    cabeza gacha y brazos abrazandose a si misma; tiembla con cada sollozo.
//  - Llanto 3D en loop (se la escucha llorar desde el pasillo).
//  - Audio interactivo: si te acercas se calma un poco y levanta la cabeza hacia vos;
//    si el paciente maligno anda cerca, llora mas fuerte y tiembla mas;
//    cuando habla (voces del grupo), el llanto baja para que se entienda.
public class PacienteNina : MonoBehaviour
{
    public AudioClip llanto;
    public float volumenLlanto = 0.8f;
    public float distanciaCalma = 2.5f;
    public float distanciaMiedo = 10f;

    Transform jugador, cuerpo, cabeza, hombroI, hombroD; Transform maligno;
    AudioSource fuenteLlanto; float miedo; Renderer capsula;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Instalar()
    {
        foreach (var n in FindObjectsByType<NPCDialogo>())
            if (!n.esPeligroso && n.name.Contains("Amistoso") && n.GetComponent<PacienteNina>() == null)
                n.gameObject.AddComponent<PacienteNina>();
    }

    void Start()
    {
        var j = GameObject.FindGameObjectWithTag("Player"); if (j != null) jugador = j.transform;
        var m = FindAnyObjectByType<PacienteMaligno>(); if (m != null) maligno = m.transform;
        if (llanto == null) llanto = Resources.Load<AudioClip>("Nina/Nina_Llanto_Loop");

        AudioMixerGroup sfx = null;
        foreach (var s in FindObjectsByType<AudioSource>())
            if (s.outputAudioMixerGroup != null && s.outputAudioMixerGroup.name == "SFX") { sfx = s.outputAudioMixerGroup; break; }
        fuenteLlanto = gameObject.AddComponent<AudioSource>();
        fuenteLlanto.outputAudioMixerGroup = sfx; fuenteLlanto.clip = llanto; fuenteLlanto.loop = true;
        fuenteLlanto.spatialBlend = 1f; fuenteLlanto.rolloffMode = AudioRolloffMode.Linear;
        fuenteLlanto.minDistance = 1f; fuenteLlanto.maxDistance = 14f; fuenteLlanto.dopplerLevel = 0f;
        fuenteLlanto.volume = volumenLlanto;
        if (llanto != null) { fuenteLlanto.time = Random.Range(0f, llanto.length * 0.9f); fuenteLlanto.Play(); }

        capsula = GetComponent<Renderer>(); if (capsula != null) capsula.enabled = false;
        CrearFigura();
    }

    Material Mat(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh); m.color = c; if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f); return m;
    }
    Transform Pieza(PrimitiveType t, Transform p, Vector3 pos, Vector3 esc, Vector3 rot, Material m)
    {
        var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(p, false); g.transform.localPosition = pos; g.transform.localScale = esc;
        g.transform.localEulerAngles = rot; g.GetComponent<Renderer>().material = m; return g.transform;
    }
    Transform Pivote(string n, Transform p, Vector3 pos) { var t = new GameObject(n).transform; t.SetParent(p, false); t.localPosition = pos; return t; }

    void CrearFigura()
    {
        var piel = Mat(new Color(0.78f, 0.7f, 0.64f));
        var bata = Mat(new Color(0.55f, 0.63f, 0.66f));
        var pelo = Mat(new Color(0.08f, 0.05f, 0.04f));
        var raiz = Pivote("Figura_Nina", transform, Vector3.zero);
        foreach (float x in new[] { -0.07f, 0.07f })
            Pieza(PrimitiveType.Capsule, raiz, new Vector3(x, -0.82f, 0f), new Vector3(0.075f, 0.2f, 0.075f), Vector3.zero, piel);
        Pieza(PrimitiveType.Cylinder, raiz, new Vector3(0f, -0.4f, 0f), new Vector3(0.34f, 0.4f, 0.24f), Vector3.zero, bata); // bata de hospital
        cuerpo = Pivote("Cuerpo", raiz, new Vector3(0f, -0.05f, 0f));
        cuerpo.localEulerAngles = new Vector3(12f, 0f, 0f); // un poco encogida
        Pieza(PrimitiveType.Capsule, cuerpo, new Vector3(0f, 0.12f, 0f), new Vector3(0.28f, 0.24f, 0.19f), Vector3.zero, bata);
        Pieza(PrimitiveType.Capsule, cuerpo, new Vector3(0f, 0.36f, 0f), new Vector3(0.06f, 0.07f, 0.06f), Vector3.zero, piel);
        cabeza = Pivote("Cabeza", cuerpo, new Vector3(0f, 0.42f, 0f));
        Pieza(PrimitiveType.Sphere, cabeza, new Vector3(0f, 0.1f, 0.01f), new Vector3(0.18f, 0.22f, 0.19f), Vector3.zero, piel);
        Pieza(PrimitiveType.Sphere, cabeza, new Vector3(0f, 0.13f, -0.015f), new Vector3(0.205f, 0.235f, 0.2f), Vector3.zero, pelo);
        Pieza(PrimitiveType.Capsule, cabeza, new Vector3(0f, -0.07f, -0.07f), new Vector3(0.19f, 0.2f, 0.06f), new Vector3(8f, 0f, 0f), pelo); // pelo largo atras
        foreach (float x in new[] { -0.06f, 0.06f }) // mechones que le tapan la cara
            Pieza(PrimitiveType.Capsule, cabeza, new Vector3(x, 0.0f, 0.09f), new Vector3(0.05f, 0.15f, 0.03f), new Vector3(-6f, 0f, x * 60f), pelo);
        hombroI = Pivote("Hombro_I", cuerpo, new Vector3(-0.15f, 0.27f, 0f));
        hombroD = Pivote("Hombro_D", cuerpo, new Vector3(0.15f, 0.27f, 0f));
        foreach (var h in new[] { hombroI, hombroD })
        {
            float l = h == hombroI ? -1f : 1f;
            Pieza(PrimitiveType.Capsule, h, new Vector3(0.01f * l, -0.12f, 0.03f), new Vector3(0.05f, 0.13f, 0.05f), new Vector3(-25f, 0f, 0f), piel);
            // antebrazo cruzado sobre el pecho: se abraza a si misma
            Pieza(PrimitiveType.Capsule, h, new Vector3(-0.1f * l, -0.2f, 0.13f), new Vector3(0.045f, 0.13f, 0.045f), new Vector3(0f, 0f, 75f * l), piel);
        }
        cabeza.localEulerAngles = new Vector3(32f, 0f, 0f); // cabeza gacha
    }

    void Update()
    {
        if (jugador == null || cuerpo == null) return;
        float dJug = Vector3.Distance(transform.position, jugador.position);
        float dMal = maligno != null ? Vector3.Distance(transform.position, maligno.position) : 999f;

        // miedo: 0 tranquila, 1 aterrada (el maligno cerca)
        float objetivoMiedo = dMal < distanciaMiedo ? 1f - dMal / distanciaMiedo : 0f;
        miedo = Mathf.MoveTowards(miedo, objetivoMiedo, Time.deltaTime * 0.8f);

        bool cerca = dJug < distanciaCalma;
        float vol = cerca ? volumenLlanto * 0.4f : volumenLlanto;
        vol = Mathf.Lerp(vol, 1f, miedo);
        foreach (var a in GetComponentsInChildren<AudioSource>())          // ducking: cuando ella habla, baja el llanto
            if (a != fuenteLlanto && !a.loop && a.isPlaying) { vol = 0.12f; break; }
        fuenteLlanto.volume = Mathf.MoveTowards(fuenteLlanto.volume, vol, Time.deltaTime * 1.5f);
        fuenteLlanto.pitch = 1f + miedo * 0.08f;

        // te mira solo cuando estas cerca, y levanta un poco la cabeza
        if (cerca)
        {
            Vector3 d = jugador.position - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 2f);
        }
        float gacha = cerca ? 14f : 32f;
        // temblor de sollozos: hombros que se sacuden, mas rapido con miedo
        float t = Time.time, frec = 5f + miedo * 5f, amp = 2.5f + miedo * 4f;
        float sollozo = Mathf.Max(0f, Mathf.Sin(t * frec)) * amp * (0.6f + 0.4f * Mathf.PerlinNoise(t * 0.7f, 3f));
        cabeza.localEulerAngles = new Vector3(Mathf.LerpAngle(cabeza.localEulerAngles.x, gacha, Time.deltaTime * 2f) + sollozo * 0.3f, 0f, Mathf.Sin(t * 0.5f) * 4f);
        cuerpo.localEulerAngles = new Vector3(12f + sollozo, 0f, 0f);
        hombroI.localEulerAngles = new Vector3(0f, 0f, -sollozo * 0.6f);
        hombroD.localEulerAngles = new Vector3(0f, 0f, sollozo * 0.6f);
    }
}
