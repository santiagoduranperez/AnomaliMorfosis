// Herramienta de Editor — Parcial 2 (Santiago). Aplica lo visto en las Clases 11 y 12:
//  - Clase 12 (Post-Processing): un Volume con perfil propio (rojo, viñeta mas cerrada, grano y
//    aberracion cromatica) con prioridad 20, mas alta que el Volume_Terror global (10). Arranca con
//    Weight 0 y AmbienteSubsueloDia16 lo sube de a poco con una corrutina al llegar al dia 16.
//  - Clase 11 (Sonido): una Audio Reverb Zone con preset de cueva que se enciende en el subsuelo.
// Menu: AnomaliMorfosis > Ambientacion del subsuelo (P2) > Aplicar / Quitar
// No toca nada mas de la escena. Ctrl+Z lo deshace. Despues: Ctrl+S.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SubsueloAmbienteBuilder
{
    private const string RootName = "Ambiente_Subsuelo_P2";
    private const string ProfilePath = "Assets/Settings/PP_Subsuelo.asset";

    [MenuItem("AnomaliMorfosis/Ambientacion del subsuelo (P2)/Aplicar")]
    public static void Aplicar()
    {
        if (GameObject.FindAnyObjectByType<GameManagerHistoria>() == null)
        {
            EditorUtility.DisplayDialog("Subsuelo", "No encontre el GameManagerHistoria en la escena.", "OK");
            return;
        }

        var viejo = GameObject.Find(RootName);
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);

        Undo.SetCurrentGroupName("Ambientacion del subsuelo");
        int grupo = Undo.GetCurrentGroup();

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, RootName);

        // 1) Post-procesado del subsuelo: prioridad 20 (pisa al Volume_Terror global), arranca en 0
        var volGO = new GameObject("Volume_Subsuelo");
        volGO.transform.SetParent(root.transform, false);
        var vol = volGO.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 20f;
        vol.weight = 0f;
        vol.sharedProfile = ObtenerPerfil();

        // 2) Eco de cueva: se enciende en el subsuelo y sigue a la camara
        var revGO = new GameObject("ReverbZone_Subsuelo");
        revGO.transform.SetParent(root.transform, false);
        var rev = revGO.AddComponent<AudioReverbZone>();
        rev.reverbPreset = AudioReverbPreset.Cave;
        rev.minDistance = 15f;
        rev.maxDistance = 20f;
        rev.enabled = false;

        // 3) Controlador: al llegar al dia 16 sube el efecto de a poco con una corrutina
        var ctrl = root.AddComponent<AmbienteSubsueloDia16>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("volumen").objectReferenceValue = vol;
        so.FindProperty("eco").objectReferenceValue = rev;
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Subsuelo",
            "Listo: al llegar al subsuelo (dia 16) el post-procesado rojo aparece de a poco y se enciende el eco de cueva.\n" +
            "Proba en Play bajando al subsuelo (dia 16) y guarda con Ctrl+S.", "OK");
    }

    [MenuItem("AnomaliMorfosis/Ambientacion del subsuelo (P2)/Quitar")]
    public static void Quitar()
    {
        var go = GameObject.Find(RootName);
        if (go != null) Undo.DestroyObjectImmediate(go);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static VolumeProfile ObtenerPerfil()
    {
        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (perfil != null) return perfil;

        perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(perfil, ProfilePath);

        var color = perfil.Add<ColorAdjustments>(false);
        color.postExposure.Override(-0.5f);
        color.contrast.Override(25f);
        color.saturation.Override(-25f);
        color.colorFilter.Override(new Color(1f, 0.72f, 0.70f)); // tinte rojizo de peligro
        Guardar(color, perfil);

        var vignette = perfil.Add<Vignette>(false);
        vignette.intensity.Override(0.55f);
        vignette.smoothness.Override(0.5f);
        vignette.color.Override(new Color(0.18f, 0f, 0f));
        Guardar(vignette, perfil);

        var grano = perfil.Add<FilmGrain>(false);
        grano.type.Override(FilmGrainLookup.Medium4);
        grano.intensity.Override(0.5f);
        Guardar(grano, perfil);

        var aberracion = perfil.Add<ChromaticAberration>(false);
        aberracion.intensity.Override(0.3f);
        Guardar(aberracion, perfil);

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
