// Herramienta de Editor — Parcial 2 (Santiago, rama escenario-p2).
// Menu: AnomaliMorfosis > Ambientacion de terror > Aplicar / Quitar
//
// Que hace "Aplicar":
//  1. Baja la luz direccional (luna fria y tenue).
//  2. Cielo negro en la Main Camera (el greybox no tiene techo, asi que el cielo celeste se veia).
//  3. Luz ambiente oscura + niebla (RenderSettings).
//  4. Crea "Iluminacion_Terror" con luces de tubo en la celda, el corredor y la sala principal,
//     la mayoria con LuzParpadeante (corrutina) y una luz roja de emergencia.
//  5. Crea un Volume global de post-procesado (vignette + grano + colores apagados) con su
//     propio perfil (Assets/Settings/TerrorVolumeProfile.asset), sin tocar el perfil original.
//
// El subsuelo no se toca: su atmosfera la maneja AtmospheraDia16 (Christian) desde el dia 16.
// Ctrl+Z deshace las luces, el volume, la camara y la luz direccional. La niebla y la luz
// ambiente se restauran con "Quitar". Despues de aplicar: Ctrl+S para guardar la escena.

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class AmbientacionTerrorBuilder
{
    private const string RootName = "Iluminacion_Terror";
    private const string ProfilePath = "Assets/Settings/TerrorVolumeProfile.asset";
    private const string PrefsKey = "AnomaliMorfosis_AmbientacionTerror_Previo";

    [Serializable]
    private class ValoresPrevios
    {
        public bool fog;
        public Color fogColor;
        public int fogMode;
        public float fogDensity;
        public int ambientMode;
        public Color ambientLight;
    }

    [MenuItem("AnomaliMorfosis/Ambientacion de terror/Aplicar (Parcial 2)")]
    public static void Aplicar()
    {
        if (GameObject.Find(RootName) != null)
        {
            bool rehacer = EditorUtility.DisplayDialog("Ambientacion ya aplicada",
                "Ya existe '" + RootName + "' en la escena. ¿Borrarlo y volver a generarlo?",
                "Regenerar", "Cancelar");
            if (!rehacer) return;
            Undo.DestroyObjectImmediate(GameObject.Find(RootName));
        }

        Undo.SetCurrentGroupName("Aplicar ambientacion de terror");
        int grupo = Undo.GetCurrentGroup();

        GuardarValoresPrevios();

        // 1) Luz direccional: luna fria y tenue
        foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            Undo.RecordObject(l, "Luz direccional");
            l.intensity = 0.15f;
            l.color = new Color(0.55f, 0.65f, 0.85f);
            l.shadows = LightShadows.Soft;
        }

        // 2) Cielo negro
        var cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Camara cielo negro");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.015f);
            var datos = cam.GetComponent<UniversalAdditionalCameraData>();
            if (datos != null)
            {
                Undo.RecordObject(datos, "Post procesado camara");
                datos.renderPostProcessing = true;
            }
        }

        // 3) Ambiente oscuro + niebla
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.055f, 0.07f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.04f);
        RenderSettings.fogDensity = 0.06f;

        // 4) Luces del nivel
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Iluminacion terror");

        Color tubo = new Color(0.75f, 0.92f, 0.85f);   // fluorescente verdoso
        Color calida = new Color(1f, 0.72f, 0.45f);    // lampara vieja
        Color emergencia = new Color(1f, 0.08f, 0.06f);

        Vector3 celda = Posicion("Celda_Inicial", new Vector3(0, 0, 0));
        Vector3 corredor = Posicion("Corredor", new Vector3(0, 0, 6f));
        Vector3 sala = Posicion("Sala_Principal", new Vector3(0, 0, 18f));

        CrearLuz("Luz_Celda", root.transform, celda + new Vector3(0, 2.6f, 0), calida, 1.6f, 5f, true, LightShadows.Soft);
        CrearLuz("Luz_Corredor_1", root.transform, corredor + new Vector3(0, 2.6f, -2f), tubo, 1.8f, 6f, true, LightShadows.None);
        CrearLuz("Luz_Corredor_2", root.transform, corredor + new Vector3(0, 2.6f, 2.5f), tubo, 1.4f, 6f, true, LightShadows.None);
        CrearLuz("Luz_Sala_Oeste", root.transform, sala + new Vector3(-5f, 3.6f, 0f), tubo, 2.2f, 11f, true, LightShadows.Soft);
        CrearLuz("Luz_Sala_Este", root.transform, sala + new Vector3(5f, 3.6f, 0f), tubo, 2.2f, 11f, false, LightShadows.Soft);
        CrearLuz("Luz_Emergencia", root.transform, sala + new Vector3(0f, 3.2f, -7.2f), emergencia, 2.5f, 7f, false, LightShadows.None);

        // 5) Post-procesado propio
        var volGO = new GameObject("Volume_Terror");
        Undo.RegisterCreatedObjectUndo(volGO, "Volume terror");
        volGO.transform.SetParent(root.transform, false);
        var vol = volGO.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;
        vol.sharedProfile = ObtenerPerfil();

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("[Ambientacion de terror] Aplicada. Guarda la escena con Ctrl+S. Las luces se ajustan en '" + RootName + "'.");
    }

    [MenuItem("AnomaliMorfosis/Ambientacion de terror/Quitar")]
    public static void Quitar()
    {
        var root = GameObject.Find(RootName);
        if (root != null) Undo.DestroyObjectImmediate(root);

        string json = EditorPrefs.GetString(PrefsKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            var p = JsonUtility.FromJson<ValoresPrevios>(json);
            RenderSettings.fog = p.fog;
            RenderSettings.fogColor = p.fogColor;
            RenderSettings.fogMode = (FogMode)p.fogMode;
            RenderSettings.fogDensity = p.fogDensity;
            RenderSettings.ambientMode = (AmbientMode)p.ambientMode;
            RenderSettings.ambientLight = p.ambientLight;
            EditorPrefs.DeleteKey(PrefsKey);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Ambientacion de terror] Quitada. La luz direccional y la camara se vuelven atras con Ctrl+Z o a mano en el Inspector.");
    }

    private static void GuardarValoresPrevios()
    {
        if (EditorPrefs.HasKey(PrefsKey)) return; // ya se guardaron los originales la primera vez
        var p = new ValoresPrevios
        {
            fog = RenderSettings.fog,
            fogColor = RenderSettings.fogColor,
            fogMode = (int)RenderSettings.fogMode,
            fogDensity = RenderSettings.fogDensity,
            ambientMode = (int)RenderSettings.ambientMode,
            ambientLight = RenderSettings.ambientLight,
        };
        EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(p));
    }

    private static Vector3 Posicion(string nombre, Vector3 porDefecto)
    {
        var go = GameObject.Find(nombre);
        return go != null ? go.transform.position : porDefecto;
    }

    private static void CrearLuz(string nombre, Transform padre, Vector3 pos, Color color,
        float intensidad, float rango, bool parpadea, LightShadows sombras)
    {
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.range = rango;
        l.shadows = sombras;
        if (parpadea) go.AddComponent<LuzParpadeante>();
    }

    private static VolumeProfile ObtenerPerfil()
    {
        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (perfil != null) return perfil;

        perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(perfil, ProfilePath);

        var vignette = perfil.Add<Vignette>(false);
        vignette.intensity.Override(0.45f);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(Color.black);
        Guardar(vignette, perfil);

        var grano = perfil.Add<FilmGrain>(false);
        grano.type.Override(FilmGrainLookup.Medium3);
        grano.intensity.Override(0.35f);
        Guardar(grano, perfil);

        var color = perfil.Add<ColorAdjustments>(false);
        color.postExposure.Override(-0.3f);
        color.saturation.Override(-40f);
        color.contrast.Override(15f);
        color.colorFilter.Override(new Color(0.88f, 0.97f, 0.92f));
        Guardar(color, perfil);

        EditorUtility.SetDirty(perfil);
        AssetDatabase.SaveAssets();
        return perfil;
    }

    private static void Guardar(VolumeComponent componente, VolumeProfile perfil)
    {
        componente.name = componente.GetType().Name;
        componente.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(componente, perfil);
    }
}
