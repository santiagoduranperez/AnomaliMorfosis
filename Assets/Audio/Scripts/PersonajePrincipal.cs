using UnityEngine;
using UnityEngine.Audio;

// PersonajePrincipal (Santiago) - "El paciente 0-16"
// Se agrega solo al Player al iniciar la escena (no toca la escena ni los scripts del equipo).
// Reemplaza la capsula gris por un paciente humano que MUTA durante los 16 dias:
//  Dia 1: paciente normal, bata gris de hospital, cabeza rapada, descalzo, pulsera de internacion.
//  Dia 8+: se encorva, el brazo izquierdo se alarga, le salen bultos en la espalda, la piel se apaga.
//  Dia 12+: los ojos empiezan a brillar en rojo. Dia 16: casi una de las criaturas.
// Las deformidades del equipo (esferas de deformacionesfolder) siguen funcionando igual.
// Audio: respiracion 2D que pasa de normal a ronca segun la mutacion, y huesos que crujen al caminar.
public class PersonajePrincipal : MonoBehaviour
{
    public AudioClip respiracionNormal, respiracionMutada; public AudioClip[] huesos;

    Transform figura, torso, cabeza, hombroI, hombroD, piernaI, piernaD; Transform[] bultos;
    Material piel, matOjos; Light luzOjos;
    AudioSource respN, respM, fxHuesos;
    Transform carpetaDeformidades;
    float m, mObjetivo, fase, tHueso, tSacudon; Vector3 posAnterior; Quaternion sacudon = Quaternion.identity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Instalar()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null && p.GetComponent<PersonajePrincipal>() == null) p.AddComponent<PersonajePrincipal>();
    }

    void Start()
    {
        var r = GetComponent<MeshRenderer>(); if (r != null) r.enabled = false; // chau capsula
        if (respiracionNormal == null) respiracionNormal = Resources.Load<AudioClip>("Prota/Prota_Respiracion_Normal");
        if (respiracionMutada == null) respiracionMutada = Resources.Load<AudioClip>("Prota/Prota_Respiracion_Mutada");
        if (huesos == null || huesos.Length == 0) huesos = Resources.LoadAll<AudioClip>("Maligno/Huesos");
        AudioMixerGroup sfx = null;
        foreach (var s in FindObjectsByType<AudioSource>())
            if (s.outputAudioMixerGroup != null && s.outputAudioMixerGroup.name == "SFX") { sfx = s.outputAudioMixerGroup; break; }
        respN = Fuente(sfx, respiracionNormal, true); respM = Fuente(sfx, respiracionMutada, true); fxHuesos = Fuente(sfx, null, false);
        CrearFigura();
        posAnterior = transform.position;
        m = mObjetivo = CalcularMutacion(); AplicarMutacion();
    }

    AudioSource Fuente(AudioMixerGroup g, AudioClip c, bool loop)
    {
        var a = gameObject.AddComponent<AudioSource>(); a.outputAudioMixerGroup = g; a.clip = c; a.loop = loop;
        a.spatialBlend = 0f; a.playOnAwake = false; a.volume = 0f; if (loop && c != null) a.Play(); return a;
    }

    Material Mat(Color c) { var sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard"); var mt = new Material(sh); mt.color = c; if (mt.HasProperty("_Smoothness")) mt.SetFloat("_Smoothness", 0.2f); return mt; }
    Transform Pieza(PrimitiveType t, Transform p, Vector3 pos, Vector3 esc, Material mt)
    {
        var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>()); // sin colliders: no molesta al CharacterController
        g.transform.SetParent(p, false); g.transform.localPosition = pos; g.transform.localScale = esc; g.GetComponent<Renderer>().material = mt; return g.transform;
    }
    Transform Piv(string n, Transform p, Vector3 pos) { var t = new GameObject(n).transform; t.SetParent(p, false); t.localPosition = pos; return t; }

    void CrearFigura()
    {
        piel = Mat(new Color(0.8f, 0.72f, 0.66f));
        var bata = Mat(new Color(0.46f, 0.48f, 0.5f));
        var blanco = Mat(new Color(0.9f, 0.9f, 0.88f));
        matOjos = Mat(new Color(0.05f, 0.04f, 0.04f));
        figura = Piv("Figura_Paciente", transform, Vector3.zero);
        var cadera = Piv("Cadera", figura, new Vector3(0f, -0.1f, 0f));
        piernaI = Piv("Pierna_I", cadera, new Vector3(-0.1f, -0.1f, 0f)); piernaD = Piv("Pierna_D", cadera, new Vector3(0.1f, -0.1f, 0f));
        foreach (var p in new[] { piernaI, piernaD })
        {
            Pieza(PrimitiveType.Capsule, p, new Vector3(0f, -0.42f, 0f), new Vector3(0.11f, 0.42f, 0.11f), piel);
            Pieza(PrimitiveType.Cube, p, new Vector3(0f, -0.86f, 0.05f), new Vector3(0.1f, 0.05f, 0.2f), piel); // pie descalzo
        }
        Pieza(PrimitiveType.Cylinder, cadera, new Vector3(0f, -0.12f, 0f), new Vector3(0.38f, 0.22f, 0.28f), bata); // bata
        torso = Piv("Torso", figura, new Vector3(0f, -0.05f, 0f));
        Pieza(PrimitiveType.Capsule, torso, new Vector3(0f, 0.32f, 0f), new Vector3(0.38f, 0.3f, 0.24f), bata);
        Pieza(PrimitiveType.Capsule, torso, new Vector3(0f, 0.64f, 0f), new Vector3(0.08f, 0.08f, 0.08f), piel);
        cabeza = Piv("Cabeza", torso, new Vector3(0f, 0.7f, 0f));
        Pieza(PrimitiveType.Sphere, cabeza, new Vector3(0f, 0.13f, 0f), new Vector3(0.24f, 0.28f, 0.25f), piel); // cabeza rapada
        foreach (float x in new[] { -0.125f, 0.125f }) Pieza(PrimitiveType.Sphere, cabeza, new Vector3(x, 0.12f, 0f), new Vector3(0.04f, 0.07f, 0.05f), piel);
        foreach (float x in new[] { -0.05f, 0.05f }) Pieza(PrimitiveType.Sphere, cabeza, new Vector3(x, 0.15f, 0.115f), new Vector3(0.045f, 0.028f, 0.02f), matOjos);
        luzOjos = new GameObject("Luz_Ojos").AddComponent<Light>(); luzOjos.transform.SetParent(cabeza, false);
        luzOjos.transform.localPosition = new Vector3(0f, 0.15f, 0.25f); luzOjos.type = LightType.Point; luzOjos.color = new Color(1f, 0.05f, 0.03f);
        luzOjos.range = 2.5f; luzOjos.intensity = 0f; luzOjos.shadows = LightShadows.None;
        hombroI = Piv("Hombro_I", torso, new Vector3(-0.23f, 0.52f, 0f)); hombroD = Piv("Hombro_D", torso, new Vector3(0.23f, 0.52f, 0f));
        foreach (var h in new[] { hombroI, hombroD })
        {
            Pieza(PrimitiveType.Capsule, h, new Vector3(0f, -0.28f, 0f), new Vector3(0.08f, 0.28f, 0.08f), piel);
            Pieza(PrimitiveType.Capsule, h, new Vector3(0f, -0.78f, 0.02f), new Vector3(0.07f, 0.25f, 0.07f), piel);
            Pieza(PrimitiveType.Sphere, h, new Vector3(0f, -1.06f, 0.02f), new Vector3(0.08f, 0.1f, 0.05f), piel);
        }
        Pieza(PrimitiveType.Cylinder, hombroI, new Vector3(0f, -0.95f, 0.02f), new Vector3(0.085f, 0.015f, 0.085f), blanco); // pulsera de internacion
        bultos = new Transform[3];
        for (int i = 0; i < 3; i++)
            bultos[i] = Pieza(PrimitiveType.Sphere, torso, new Vector3(-0.07f + i * 0.08f, 0.5f - i * 0.12f, -0.12f), Vector3.zero, piel);
    }

    float CalcularMutacion()
    {
        var gm = GameManagerHistoria.Instance; if (gm == null) return 0f;
        return Mathf.Clamp01((gm.diaActual - 6f) / 10f); // empieza a notarse en el dia 7-8, completa en el 16
    }

    void AplicarMutacion()
    {
        figura.localScale = new Vector3(1f - 0.12f * m, 1f + 0.15f * m, 1f - 0.12f * m);
        hombroI.localScale = new Vector3(1f - 0.3f * m, 1f + 0.7f * m, 1f - 0.3f * m); // brazo izquierdo que se alarga
        for (int i = 0; i < bultos.Length; i++) { float b = Mathf.Clamp01(m * 1.6f - i * 0.25f) * (0.13f + i * 0.02f); bultos[i].localScale = Vector3.one * b; }
        piel.color = Color.Lerp(new Color(0.8f, 0.72f, 0.66f), new Color(0.45f, 0.48f, 0.42f), m);
        var gm = GameManagerHistoria.Instance; float e = gm != null ? Mathf.Clamp01((gm.diaActual - 11f) / 5f) : 0f;
        matOjos.color = Color.Lerp(new Color(0.05f, 0.04f, 0.04f), new Color(1f, 0.05f, 0.03f), e);
        luzOjos.intensity = e * 1.5f;
    }

    void Update()
    {
        if (figura == null) return;
        // La deformidad "Capsule" del dia 16 tapaba la figura nueva (que ya muestra la forma final): se oculta
        if (carpetaDeformidades == null) carpetaDeformidades = transform.Find("deformacionesfolder");
        if (carpetaDeformidades != null)
            foreach (var r in carpetaDeformidades.GetComponentsInChildren<MeshRenderer>())
                if (r.enabled && r.gameObject.name.StartsWith("Capsule")) r.enabled = false;
        mObjetivo = CalcularMutacion();
        if (!Mathf.Approximately(m, mObjetivo)) { m = Mathf.MoveTowards(m, mObjetivo, Time.deltaTime * 0.3f); AplicarMutacion(); }

        Vector3 delta = transform.position - posAnterior; delta.y = 0f; posAnterior = transform.position;
        float vel = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        bool camina = vel > 0.3f;
        fase += Time.deltaTime * Mathf.Clamp(vel, 0f, 8f) * 1.8f;
        float paso = camina ? Mathf.Sin(fase) : 0f;
        piernaI.localEulerAngles = new Vector3(paso * 30f, 0f, 0f);
        piernaD.localEulerAngles = new Vector3(-paso * 30f * (1f - 0.5f * m), 0f, 0f); // renguea al mutar
        hombroI.localEulerAngles = new Vector3(-paso * 18f, 0f, -3f - 6f * m);
        hombroD.localEulerAngles = new Vector3(paso * 20f, 0f, 3f);

        // encorvado y cabeza con sacudones cuando esta mutado
        torso.localEulerAngles = new Vector3(25f * m, 0f, 0f);
        tSacudon -= Time.deltaTime;
        if (m > 0.35f && tSacudon <= 0f)
        {
            sacudon = Quaternion.Euler(Random.Range(-10f, 10f) * m, Random.Range(-15f, 15f) * m, Random.Range(-25f, 25f) * m);
            tSacudon = Random.Range(0.6f, 3f) / (0.5f + m);
        }
        cabeza.localRotation = Quaternion.Slerp(cabeza.localRotation, Quaternion.Euler(10f * m, 0f, 18f * m) * sacudon, Time.deltaTime * 12f);

        // respiracion: normal -> ronca segun la mutacion, mas fuerte al moverse
        float esfuerzo = camina ? 1.3f : 1f;
        respN.volume = (1f - m) * 0.35f * esfuerzo;
        respM.volume = m * 0.55f * esfuerzo;

        // huesos que crujen al caminar cuando ya muto bastante
        tHueso -= Time.deltaTime;
        if (camina && m > 0.3f && tHueso <= 0f && huesos != null && huesos.Length > 0)
        {
            if (Random.value < m) { fxHuesos.pitch = Random.Range(0.9f, 1.2f); fxHuesos.PlayOneShot(huesos[Random.Range(0, huesos.Length)], 0.25f * m); }
            tHueso = Random.Range(0.7f, 1.3f);
        }
    }
}
