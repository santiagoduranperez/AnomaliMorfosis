#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

// Herramienta de Editor: arma en la escena los bloques B (música) y D (ambiente)
// de Santiago. Se puede volver a ejecutar: borra lo anterior y lo rehace.
// Menú: AnomaliMorfosis > Audio > Construir ambientacion (Santiago)
public static class ConstruirAudioSantiago
{
    const string RAIZ = "AUDIO_Santiago (B Musica + D Ambiente)";
    const string MIXER = "Assets/Audio/Mixers/MezclaJuego.mixer";
    const string AMB = "Assets/Audio/Ambiente/";
    const string MUS = "Assets/Audio/Musica/";

    [MenuItem("AnomaliMorfosis/Audio/Construir ambientacion (Santiago)")]
    public static void Construir()
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MIXER);
        if (mixer == null) { Debug.LogError("No encuentro " + MIXER); return; }
        AudioMixerGroup gMusica = Grupo(mixer, "Musica"), gAmb = Grupo(mixer, "Ambiente");

        ConfigurarImportacion();

        var viejo = GameObject.Find(RAIZ);
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);
        var raiz = new GameObject(RAIZ);
        Undo.RegisterCreatedObjectUndo(raiz, "Construir audio");

        var jugador = GameObject.FindWithTag("Player");
        Transform tJug = jugador != null ? jugador.transform : null;

        // ---------- D · 2 ambientes 2D ----------
        var gA = Hijo(raiz, "D_Ambientes_2D");
        var aInst = Fuente(gA, "Ambiente_Instalacion", Vector3.zero, AMB + "Ambiente_Instalacion_Loop.wav", gAmb, 0f, 0.55f);
        var aSub  = Fuente(gA, "Ambiente_Subsuelo",   Vector3.zero, AMB + "Ambiente_Subsuelo_Loop.wav",   gAmb, 0f, 0f);
        var zonas = gA.AddComponent<AmbienteZonas>();
        var so = new SerializedObject(zonas);
        so.FindProperty("ambienteInstalacion").objectReferenceValue = aInst;
        so.FindProperty("ambienteSubsuelo").objectReferenceValue = aSub;
        so.FindProperty("jugador").objectReferenceValue = tJug;
        Bounds bSub = Limites("Subsuelo");
        bSub.Expand(new Vector3(0f, 8f, 0f));
        so.FindProperty("zonaSubsuelo").boundsValue = bSub;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---------- D · 5 fuentes 3D ----------
        var g3 = Hijo(raiz, "D_Fuentes_3D");
        Bounds celda = Limites("Celda_Inicial"), corr = Limites("Corredor"), sala = Limites("Sala_Principal");
        Fuente3D(g3, "3D_Fluorescente_Celda", new Vector3(celda.center.x, celda.min.y + 2.6f, celda.center.z),
                 AMB + "3D_Fluorescente_Loop.wav", gAmb, 0.55f, 1f, 6f);
        Fuente3D(g3, "3D_Goteo_Corredor", new Vector3(corr.center.x, corr.min.y + 0.3f, corr.center.z),
                 AMB + "3D_Goteo_Loop.wav", gAmb, 0.8f, 0.5f, 5f);
        Fuente3D(g3, "3D_Generador_Sala", new Vector3(sala.max.x - 1.5f, sala.min.y + 1f, sala.max.z - 1.5f),
                 AMB + "3D_Generador_Loop.wav", gAmb, 0.7f, 2f, 14f);
        Fuente3D(g3, "3D_Ventilacion_Subsuelo", new Vector3(bSub.center.x, Limites("Subsuelo").min.y + 3f, bSub.center.z),
                 AMB + "3D_Ventilacion_Loop.wav", gAmb, 0.55f, 2f, 12f);
        var amistoso = GameObject.Find("Paciente_Amistoso");
        var radio = Fuente3D(g3, "3D_Radio_Paciente", amistoso != null ? amistoso.transform.position + Vector3.up : sala.center,
                 AMB + "3D_Radio_Loop.wav", gAmb, 0.5f, 1f, 7f);
        if (amistoso != null) radio.transform.SetParent(amistoso.transform, true);

        // ---------- B · música por capas ----------
        var gM = Hijo(raiz, "B_Musica_Capas");
        var c1 = Fuente(gM, "Musica_Calma", Vector3.zero, MUS + "Musica_Calma_Loop.wav", gMusica, 0f, 0.6f);
        var c2 = Fuente(gM, "Musica_Tension", Vector3.zero, MUS + "Musica_Tension_Loop.wav", gMusica, 0f, 0f);
        var c3 = Fuente(gM, "Musica_Persecucion", Vector3.zero, MUS + "Musica_Persecucion_Loop.wav", gMusica, 0f, 0f);
        foreach (var s in new[] { c1, c2, c3 }) s.priority = 0;   // la música nunca se pierde
        var mus = gM.AddComponent<MusicaAdaptativa>();
        var sm = new SerializedObject(mus);
        sm.FindProperty("capaCalma").objectReferenceValue = c1;
        sm.FindProperty("capaTension").objectReferenceValue = c2;
        sm.FindProperty("capaPersecucion").objectReferenceValue = c3;
        sm.FindProperty("jugador").objectReferenceValue = tJug;
        sm.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log($"Audio construido: 2 ambientes, 5 fuentes 3D, 3 capas de musica. Jugador={(tJug != null)} Subsuelo={bSub}");
    }

    static AudioMixerGroup Grupo(AudioMixer m, string n) =>
        m.FindMatchingGroups(n).FirstOrDefault(g => g.name == n);

    static GameObject Hijo(GameObject p, string n) { var g = new GameObject(n); g.transform.SetParent(p.transform, false); return g; }

    static AudioSource Fuente(GameObject p, string n, Vector3 pos, string clip, AudioMixerGroup grupo, float blend, float vol)
    {
        var g = Hijo(p, n); g.transform.position = pos;
        var s = g.AddComponent<AudioSource>();
        s.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clip);
        if (s.clip == null) Debug.LogWarning("Falta el clip " + clip);
        s.outputAudioMixerGroup = grupo; s.loop = true; s.playOnAwake = true;
        s.spatialBlend = blend; s.volume = vol; s.dopplerLevel = 0f;
        return s;
    }

    static AudioSource Fuente3D(GameObject p, string n, Vector3 pos, string clip, AudioMixerGroup g, float vol, float min, float max)
    {
        var s = Fuente(p, n, pos, clip, g, 1f, vol);
        s.rolloffMode = AudioRolloffMode.Linear;   // predecible: llega a silencio en Max
        s.minDistance = min; s.maxDistance = max;
        return s;
    }

    static Bounds Limites(string nombre)
    {
        var go = GameObject.Find(nombre);
        if (go == null) { Debug.LogWarning("No encontre " + nombre); return new Bounds(Vector3.zero, Vector3.one * 10f); }
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 10f);
        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }

    static void ConfigurarImportacion()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Ambiente", "Assets/Audio/Musica" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) continue;
            var s = imp.defaultSampleSettings;
            bool largo = path.Contains("/Musica/") || path.Contains("Ambiente_");
            s.loadType = largo ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = 0.7f;
            imp.defaultSampleSettings = s;
            imp.forceToMono = path.Contains("/3D_");
            imp.SaveAndReimport();
        }
    }
}
#endif
