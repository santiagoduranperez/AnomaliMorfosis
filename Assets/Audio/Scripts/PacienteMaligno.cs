using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// PacienteMaligno (Santiago, Parcial 2 Audio Interactivo)
// Se agrega SOLO al paciente peligroso (NPCDialogo con esPeligroso) al iniciar la escena,
// sin tocar NPCDialogo ni la escena. Lo vuelve algo maligno:
//  - Susurros invertidos 3D en loop: lo escuchas antes de verlo.
//  - Al detectarte: SILENCIO total (todo el audio se pausa) y despues un grito demoniaco.
//  - Huesos que crujen mientras se mueve.
//  - Chillido cuando te hace dano.
//  - Ojos rojos con luz que parpadea; tiembla cuando lo miras.
//  - Si lo miras se congela; si le das la espalda, parpadea y reaparece mas cerca.
public class PacienteMaligno : MonoBehaviour
{
    [Header("Sonidos (se cargan solos de Resources/Maligno)")]
    public AudioClip grito, ataque, susurros;
    public AudioClip[] huesos;

    [Header("Comportamiento")]
    public float silencioAntesDelGrito = 1.2f;
    public float esperaEntreGritos = 12f;
    public bool congelarSiLoMiras = true;
    public float cadaCuantoParpadea = 5f;
    public float distanciaParpadeo = 1.8f;

    NPCDialogo npc; Transform jugador; Camera cam; Renderer cuerpo;
    AudioSource fuenteSusurros, fuenteHuesos, fuenteGrito;
    Transform ojos; Light luzOjos;
    bool detectado, gritando;
    float tUltimoGrito = -99f, tPaso, tParpadeo;
    Vector3 posAnterior; int vidaAnterior = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Instalar()
    {
        foreach (var n in FindObjectsByType<NPCDialogo>())
            if (n.esPeligroso && n.GetComponent<PacienteMaligno>() == null)
                n.gameObject.AddComponent<PacienteMaligno>();
    }

    void Start()
    {
        npc = GetComponent<NPCDialogo>();
        var j = GameObject.FindGameObjectWithTag("Player"); if (j != null) jugador = j.transform;
        cam = Camera.main; cuerpo = GetComponent<Renderer>();
        if (grito == null) grito = Resources.Load<AudioClip>("Maligno/Maligno_Grito");
        if (ataque == null) ataque = Resources.Load<AudioClip>("Maligno/Maligno_Ataque");
        if (susurros == null) susurros = Resources.Load<AudioClip>("Maligno/Maligno_Susurros_Loop");
        if (huesos == null || huesos.Length == 0) huesos = Resources.LoadAll<AudioClip>("Maligno/Huesos");

        AudioMixerGroup sfx = null;
        foreach (var s in FindObjectsByType<AudioSource>())
            if (s.outputAudioMixerGroup != null && s.outputAudioMixerGroup.name == "SFX") { sfx = s.outputAudioMixerGroup; break; }

        fuenteSusurros = Fuente(sfx, 1f, 1f, 14f, 0.9f); fuenteSusurros.loop = true; fuenteSusurros.clip = susurros;
        if (susurros != null) fuenteSusurros.Play();
        fuenteHuesos = Fuente(sfx, 1f, 1f, 16f, 0.8f);
        fuenteGrito = Fuente(sfx, 0.6f, 3f, 40f, 1f); fuenteGrito.ignoreListenerPause = true; // suena aunque todo este en silencio

        if (cuerpo != null) cuerpo.material.color = new Color(0.05f, 0.04f, 0.04f);
        CrearOjos();
        posAnterior = transform.position;
        if (GameManagerHistoria.Instance != null) vidaAnterior = GameManagerHistoria.Instance.vidaActual;
    }

    AudioSource Fuente(AudioMixerGroup g, float blend, float min, float max, float vol)
    {
        var a = gameObject.AddComponent<AudioSource>();
        a.outputAudioMixerGroup = g; a.spatialBlend = blend; a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = min; a.maxDistance = max; a.volume = vol; a.dopplerLevel = 0f; a.playOnAwake = false;
        return a;
    }

    void CrearOjos()
    {
        ojos = new GameObject("Ojos_Malignos").transform; ojos.SetParent(transform, false);
        ojos.localPosition = new Vector3(0f, 0.6f, 0.42f);
        var sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh == null) sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh); mat.color = new Color(1f, 0.02f, 0.02f);
        foreach (float x in new[] { -0.16f, 0.16f })
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(ojos, false); o.transform.localPosition = new Vector3(x, 0f, 0f);
            o.transform.localScale = new Vector3(0.12f, 0.07f, 0.06f);
            o.GetComponent<Renderer>().material = mat;
        }
        luzOjos = new GameObject("Luz_Ojos").AddComponent<Light>(); luzOjos.transform.SetParent(ojos, false);
        luzOjos.transform.localPosition = new Vector3(0f, 0f, 0.3f);
        luzOjos.type = LightType.Point; luzOjos.color = new Color(1f, 0.05f, 0.03f); luzOjos.range = 4f; luzOjos.intensity = 1f;
        luzOjos.shadows = LightShadows.None;
    }

    void Update()
    {
        if (jugador == null || npc == null) return;
        float dist = Vector3.Distance(transform.position, jugador.position);

        Vector3 mira = jugador.position - transform.position; mira.y = 0f;
        if (mira.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(mira);

        // Deteccion: silencio + grito (solo al entrar en rango y con espera entre gritos)
        if (!detectado && dist <= npc.rangoDeteccion)
        {
            detectado = true;
            if (Time.time - tUltimoGrito > esperaEntreGritos) StartCoroutine(SilencioYGrito());
        }
        else if (detectado && dist > npc.rangoDeteccion * 1.6f) detectado = false;

        // Huesos crujiendo mientras se mueve
        float vel = (transform.position - posAnterior).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        tPaso -= Time.deltaTime;
        if (vel > 0.5f && tPaso <= 0f && huesos != null && huesos.Length > 0)
        {
            fuenteHuesos.pitch = Random.Range(0.75f, 1.1f);
            fuenteHuesos.PlayOneShot(huesos[Random.Range(0, huesos.Length)], Random.Range(0.7f, 1f));
            tPaso = Random.Range(0.45f, 0.8f);
        }

        // Chillido cuando te hace dano
        var gm = GameManagerHistoria.Instance;
        if (gm != null)
        {
            if (vidaAnterior >= 0 && gm.vidaActual < vidaAnterior && dist < 2.5f && ataque != null)
                fuenteGrito.PlayOneShot(ataque);
            vidaAnterior = gm.vidaActual;
        }

        // Luz de los ojos: parpadeo inquieto, mas violento si te detecto
        if (!gritando) luzOjos.intensity = detectado ? Random.Range(1.5f, 4f) : 0.6f + Mathf.PerlinNoise(Time.time * 3f, 0f) * 0.8f;

        // Si no lo miras, parpadea y reaparece mas cerca
        bool loVes = LoEstanMirando();
        ojos.localPosition = new Vector3(0f, 0.6f, 0.42f) + (loVes && detectado ? Random.insideUnitSphere * 0.03f : Vector3.zero);
        tParpadeo -= Time.deltaTime;
        if (detectado && !loVes && tParpadeo <= 0f && dist > distanciaParpadeo + 1.5f)
        {
            StartCoroutine(Parpadear());
            tParpadeo = cadaCuantoParpadea * Random.Range(0.6f, 1.4f);
        }
    }

    void LateUpdate()
    {
        // Si lo miras, se queda congelado (deshace el movimiento de NPCDialogo de este frame)
        if (congelarSiLoMiras && detectado && LoEstanMirando()) transform.position = posAnterior;
        posAnterior = transform.position;
    }

    bool LoEstanMirando()
    {
        if (cam == null) return false;
        Vector3 punto = transform.position + Vector3.up * 0.5f;
        Vector3 vp = cam.WorldToViewportPoint(punto);
        if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) return false;
        Vector3 d = punto - cam.transform.position;
        foreach (var h in Physics.RaycastAll(cam.transform.position, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            if (jugador != null && h.transform.root == jugador.root) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            return false; // hay una pared en el medio
        }
        return true;
    }

    IEnumerator SilencioYGrito()
    {
        gritando = true; tUltimoGrito = Time.time;
        AudioListener.pause = true;                 // silencio total: musica, ambiente, todo
        luzOjos.intensity = 0f;
        Debug.Log("PacienteMaligno: silencio...");
        yield return new WaitForSecondsRealtime(silencioAntesDelGrito);
        AudioListener.pause = false;
        luzOjos.intensity = 6f;
        if (grito != null) fuenteGrito.PlayOneShot(grito);
        Debug.Log("PacienteMaligno: GRITO");
        yield return new WaitForSeconds(0.6f);
        gritando = false;
    }

    IEnumerator Parpadear()
    {
        if (cuerpo != null) cuerpo.enabled = false; ojos.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.12f);
        Vector3 hacia = jugador.position - transform.position; hacia.y = 0f;
        transform.position += hacia.normalized * distanciaParpadeo;
        posAnterior = transform.position;
        if (cuerpo != null) cuerpo.enabled = true; ojos.gameObject.SetActive(true);
    }

    void OnDisable() { AudioListener.pause = false; }
}
