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
//  - Cuerpo propio armado con piezas: alto (2,4 m), flaco, encorvado, brazos larguisimos,
//    cabeza torcida que da sacudones, boca abierta y ojos rojos con luz que parpadea.
//  - Te persigue amenazando "te matare... jajaja"; si le das la espalda, parpadea y reaparece mas cerca.
//    (opcional: congelarSiLoMiras = se queda quieto mientras lo miras)
public class PacienteMaligno : MonoBehaviour
{
    [Header("Sonidos (se cargan solos de Resources/Maligno)")]
    public AudioClip grito, ataque, susurros, teMatare;
    public AudioClip[] huesos;

    [Header("Comportamiento")]
    public float silencioAntesDelGrito = 1.2f;
    public float esperaEntreGritos = 12f;
    public bool congelarSiLoMiras = false; // apagado: asi te persigue siempre
    public float cadaCuantoAmenaza = 18f;   // repite "te matare" mientras te persigue
    public float cadaCuantoParpadea = 5f;
    public float distanciaParpadeo = 1.8f;

    NPCDialogo npc; Transform jugador; Camera cam; Renderer cuerpo;
    AudioSource fuenteSusurros, fuenteHuesos, fuenteGrito, fuenteAmenaza; MusicaAdaptativa musica;
    Transform ojos, figura, cabeza, hombroI, hombroD; Light luzOjos;
    float tSacudon; Quaternion rotCabezaObjetivo = Quaternion.Euler(0f, 0f, 35f);
    bool detectado, gritando;
    float tUltimoGrito = -99f, tPaso, tParpadeo, tAmenaza; bool amenazaPendiente;
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
        if (teMatare == null) teMatare = Resources.Load<AudioClip>("Maligno/Maligno_TeMatare");
        if (huesos == null || huesos.Length == 0) huesos = Resources.LoadAll<AudioClip>("Maligno/Huesos");

        AudioMixerGroup sfx = null;
        foreach (var s in FindObjectsByType<AudioSource>())
            if (s.outputAudioMixerGroup != null && s.outputAudioMixerGroup.name == "SFX") { sfx = s.outputAudioMixerGroup; break; }

        fuenteSusurros = Fuente(sfx, 1f, 1f, 14f, 0.9f); fuenteSusurros.loop = true; fuenteSusurros.clip = susurros;
        if (susurros != null) fuenteSusurros.Play();
        fuenteHuesos = Fuente(sfx, 1f, 1f, 16f, 0.8f);
        fuenteGrito = Fuente(sfx, 0.4f, 4f, 45f, 1f); fuenteGrito.ignoreListenerPause = true;
        fuenteAmenaza = Fuente(sfx, 0f, 1f, 50f, 1f); fuenteAmenaza.ignoreListenerPause = true; // "te matare" en 2D, como al oido
        musica = FindAnyObjectByType<MusicaAdaptativa>(); // suena aunque todo este en silencio

        if (cuerpo != null) cuerpo.enabled = false; // escondemos la capsula original
        CrearFigura();
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

    Material Mat(Color c, bool lit)
    {
        var sh = Shader.Find(lit ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh); m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
        return m;
    }

    Transform Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 escala, Vector3 rot, Material m, string nombre)
    {
        var g = GameObject.CreatePrimitive(tipo); g.name = nombre; Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(padre, false); g.transform.localPosition = pos;
        g.transform.localScale = escala; g.transform.localEulerAngles = rot;
        g.GetComponent<Renderer>().material = m;
        return g.transform;
    }

    Transform Pivote(string nombre, Transform padre, Vector3 pos)
    {
        var t = new GameObject(nombre).transform; t.SetParent(padre, false); t.localPosition = pos; return t;
    }

    // Figura humanoide perturbadora: piel palida y sucia, muy flaca, encorvada, brazos hasta las rodillas
    void CrearFigura()
    {
        var piel = Mat(new Color(0.45f, 0.43f, 0.39f), true);
        var oscuro = Mat(new Color(0.02f, 0.01f, 0.01f), false);
        figura = Pivote("Figura_Maligna", transform, Vector3.zero);
        var torso = Pivote("Torso", figura, new Vector3(0f, -0.1f, 0f)); torso.localEulerAngles = new Vector3(22f, 0f, 0f); // encorvado hacia vos
        Pieza(PrimitiveType.Capsule, torso, new Vector3(0f, 0.55f, 0f), new Vector3(0.42f, 0.55f, 0.26f), Vector3.zero, piel, "Pecho");
        for (int i = 0; i < 4; i++) // costillas marcadas
            Pieza(PrimitiveType.Cube, torso, new Vector3(0f, 0.75f - i * 0.12f, 0.12f), new Vector3(0.36f - i * 0.02f, 0.02f, 0.04f), Vector3.zero, oscuro, "Costilla");
        Pieza(PrimitiveType.Capsule, torso, new Vector3(0f, 1.15f, 0.02f), new Vector3(0.09f, 0.16f, 0.09f), new Vector3(0f, 0f, 12f), piel, "Cuello");
        cabeza = Pivote("Cabeza", torso, new Vector3(0.03f, 1.3f, 0.04f));
        Pieza(PrimitiveType.Sphere, cabeza, new Vector3(0f, 0.17f, 0f), new Vector3(0.3f, 0.42f, 0.32f), Vector3.zero, piel, "Craneo");
        Pieza(PrimitiveType.Sphere, cabeza, new Vector3(-0.07f, 0.22f, 0.13f), new Vector3(0.1f, 0.08f, 0.06f), Vector3.zero, oscuro, "Cuenca_I");
        Pieza(PrimitiveType.Sphere, cabeza, new Vector3(0.07f, 0.22f, 0.13f), new Vector3(0.1f, 0.08f, 0.06f), Vector3.zero, oscuro, "Cuenca_D");
        Pieza(PrimitiveType.Sphere, cabeza, new Vector3(0f, 0.04f, 0.13f), new Vector3(0.13f, 0.2f, 0.06f), Vector3.zero, oscuro, "Boca_Abierta");
        hombroI = Pivote("Hombro_I", torso, new Vector3(-0.27f, 0.95f, 0f));
        hombroD = Pivote("Hombro_D", torso, new Vector3(0.27f, 0.95f, 0f));
        foreach (var h in new[] { hombroI, hombroD })
        {
            float lado = h == hombroI ? -1f : 1f;
            Pieza(PrimitiveType.Capsule, h, new Vector3(0.03f * lado, -0.38f, 0f), new Vector3(0.08f, 0.38f, 0.08f), new Vector3(0f, 0f, 6f * lado), piel, "Brazo");
            Pieza(PrimitiveType.Capsule, h, new Vector3(0.07f * lado, -1.1f, 0.06f), new Vector3(0.065f, 0.4f, 0.065f), new Vector3(-8f, 0f, 3f * lado), piel, "Antebrazo");
            for (int f = 0; f < 4; f++) // dedos largos y finos
                Pieza(PrimitiveType.Capsule, h, new Vector3(0.07f * lado + (f - 1.5f) * 0.035f, -1.62f, 0.09f), new Vector3(0.022f, 0.14f, 0.022f), new Vector3(-10f, 0f, (f - 1.5f) * 6f), piel, "Dedo");
        }
        foreach (float x in new[] { -0.11f, 0.11f }) // piernas flacas
        {
            Pieza(PrimitiveType.Capsule, figura, new Vector3(x, -0.55f, -0.08f), new Vector3(0.1f, 0.48f, 0.1f), new Vector3(-6f, 0f, 0f), piel, "Pierna");
        }
    }

    void CrearOjos()
    {
        ojos = new GameObject("Ojos_Malignos").transform; ojos.SetParent(cabeza != null ? cabeza : transform, false);
        ojos.localPosition = new Vector3(0f, 0.22f, 0.15f);
        var sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh == null) sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh); mat.color = new Color(1f, 0.02f, 0.02f);
        foreach (float x in new[] { -0.16f, 0.16f })
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(ojos, false); o.transform.localPosition = new Vector3(x * 0.45f, 0f, 0f);
            o.transform.localScale = new Vector3(0.045f, 0.035f, 0.03f);
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

        // Amenaza "te matare... jajaja" mientras te persigue
        if (amenazaPendiente || (detectado && !gritando && teMatare != null))
        {
            tAmenaza -= Time.deltaTime;
            if (tAmenaza <= 0f && teMatare != null)
            {
                StartCoroutine(Amenazar()); tAmenaza = cadaCuantoAmenaza; amenazaPendiente = false;
                Debug.Log("PacienteMaligno: TE MATARE (voz de Kiara)");
            }
        }

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
        ojos.localPosition = new Vector3(0f, 0.22f, 0.15f);
        AnimarFigura(dist);
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
        tAmenaza = 2.9f; amenazaPendiente = true; // despues del grito: "te matare" (suena aunque escapes)
        yield return new WaitForSeconds(0.6f);
        gritando = false;
    }

    // Movimientos a sacudones: la cabeza se tuerce de golpe, los brazos se balancean y tiemblan
    void AnimarFigura(float dist)
    {
        if (figura == null) return;
        tSacudon -= Time.deltaTime;
        if (tSacudon <= 0f)
        {
            rotCabezaObjetivo = Quaternion.Euler(Random.Range(-15f, 25f), Random.Range(-30f, 30f), Random.Range(-55f, 55f));
            tSacudon = detectado ? Random.Range(0.15f, 0.7f) : Random.Range(0.8f, 2.5f);
        }
        cabeza.localRotation = Quaternion.Slerp(cabeza.localRotation, rotCabezaObjetivo, Time.deltaTime * 22f); // giro brusco
        float t = Time.time;
        float temblor = detectado ? 6f : 1.5f;
        hombroI.localEulerAngles = new Vector3(Mathf.Sin(t * 1.3f) * 12f + Random.Range(-temblor, temblor), 0f, -4f);
        hombroD.localEulerAngles = new Vector3(Mathf.Sin(t * 1.3f + 2f) * 12f + Random.Range(-temblor, temblor), 0f, 4f);
        figura.localPosition = detectado ? Random.insideUnitSphere * 0.02f : Vector3.zero;
    }

    // "te matare" de Kiara: suena al frente y la musica baja mientras habla (ducking)
    IEnumerator Amenazar()
    {
        fuenteAmenaza.PlayOneShot(teMatare);
        if (musica != null)
        {
            float original = musica.volumenGeneral;
            musica.volumenGeneral = original * 0.3f;
            yield return new WaitForSeconds(teMatare.length * 0.8f);
            musica.volumenGeneral = original;
        }
    }

    IEnumerator Parpadear()
    {
        figura.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.12f);
        Vector3 hacia = jugador.position - transform.position; hacia.y = 0f;
        transform.position += hacia.normalized * distanciaParpadeo;
        posAnterior = transform.position;
        figura.gameObject.SetActive(true);
    }

    void OnDisable() { AudioListener.pause = false; }
}
