// Herramienta de Editor — Parcial 2 (Santiago). Arma la camara del jugador con Cinemachine.
// Menu: AnomaliMorfosis > Camara Cinemachine > Aplicar / Quitar (volver a CamaraFollow)
//  - Main Camera: agrega CinemachineBrain y desactiva CamaraFollow (no lo borra).
//  - CM_Jugador: CinemachineCamera que sigue al Player con Orbital Follow (5 m, mouse),
//    Rotation Composer, Deoccluder (no atraviesa paredes), Impulse Listener + Impulse Source
//    (sacudon al recibir dano) y Basic Multi Channel Perlin (temblor en el subsuelo).
// Ctrl+Z lo deshace. Despues: Ctrl+S.
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CinemachineCamaraBuilder
{
    private const string Nombre = "CM_Jugador";
    private const string RuidoPreset = "Packages/com.unity.cinemachine/Presets/Noise/Handheld_normal_mild.asset";
    private const string RuidoPropio = "Assets/Settings/Ruido_CamaraEnMano.asset";
    private static string avisos = "";

    [MenuItem("AnomaliMorfosis/Camara Cinemachine/Aplicar")]
    public static void Aplicar()
    {
        avisos = "";
        var camGO = GameObject.Find("Main Camera");
        var player = GameObject.FindGameObjectWithTag("Player");
        if (camGO == null || player == null)
        {
            EditorUtility.DisplayDialog("Cinemachine", "No encontre " + (camGO == null ? "la 'Main Camera'" : "el objeto con tag Player") + ".", "OK");
            return;
        }
        var camara = camGO.GetComponent<Camera>();

        Undo.SetCurrentGroupName("Camara Cinemachine");
        int grupo = Undo.GetCurrentGroup();

        var viejo = GameObject.Find(Nombre);
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);

        // 1) Main Camera: Brain + apagar la camara vieja
        if (camGO.GetComponent<CinemachineBrain>() == null) Undo.AddComponent<CinemachineBrain>(camGO);
        var follow = camGO.GetComponent<CamaraFollow>();
        if (follow != null) { Undo.RecordObject(follow, "Apagar CamaraFollow"); follow.enabled = false; }

        // 2) Camara virtual
        var go = new GameObject(Nombre);
        Undo.RegisterCreatedObjectUndo(go, Nombre);
        go.transform.SetPositionAndRotation(camGO.transform.position, camGO.transform.rotation);

        var cm = go.AddComponent<CinemachineCamera>();
        cm.Follow = player.transform;
        cm.LookAt = player.transform;
        if (camara != null)
        {
            var lente = cm.Lens;
            lente.FieldOfView = camara.fieldOfView;
            lente.NearClipPlane = camara.nearClipPlane;
            lente.FarClipPlane = camara.farClipPlane;
            cm.Lens = lente;
        }

        var orbita = go.AddComponent<CinemachineOrbitalFollow>();
        Config(orbita, so =>
        {
            SetInt(so, "OrbitStyle", 0);                        // Sphere
            SetFloat(so, "Radius", 5f);                         // igual que la camara anterior (offset z = -5)
            SetVec3(so, "TargetOffset", new Vector3(0f, 1.2f, 0f));
            SetVec3(so, "TrackerSettings.PositionDamping", new Vector3(0.3f, 0.3f, 0.3f));
            SetVec2(so, "VerticalAxis.Range", new Vector2(-10f, 60f));
            SetFloat(so, "VerticalAxis.Value", 20f);
            SetFloat(so, "VerticalAxis.Center", 20f);
        });

        var composer = go.AddComponent<CinemachineRotationComposer>();
        Config(composer, so =>
        {
            SetVec3(so, "TargetOffset", new Vector3(0f, 0.8f, 0f)); // a la altura de la cabeza: asi la deteccion de paredes ve el cuerpo
            SetVec2(so, "Damping", new Vector2(0.2f, 0.2f));
        });

        var deoc = go.AddComponent<CinemachineDeoccluder>();
        Config(deoc, so =>
        {
            var capas = so.FindProperty("CollideAgainst");
            if (capas != null) capas.intValue = LayerMask.GetMask("Default"); else avisos += "\n- CollideAgainst";
            SetString(so, "IgnoreTag", "Player");
            SetFloat(so, "MinimumDistanceFromTarget", 0.5f);
            SetBool(so, "AvoidObstacles.Enabled", true);
            SetFloat(so, "AvoidObstacles.CameraRadius", 0.25f);
            SetFloat(so, "AvoidObstacles.SmoothingTime", 0.2f);
        });

        go.AddComponent<CinemachineImpulseListener>();
        var sacudon = go.AddComponent<CinemachineImpulseSource>();

        var temblor = go.AddComponent<CinemachineBasicMultiChannelPerlin>();
        temblor.NoiseProfile = ObtenerRuido();
        temblor.AmplitudeGain = 0f;
        temblor.FrequencyGain = 1f;

        // 3) Script que mueve la orbita con el mouse, sacude y activa el temblor
        var ctrl = go.AddComponent<CamaraCinemachine>();
        var sc = new SerializedObject(ctrl);
        sc.FindProperty("orbita").objectReferenceValue = orbita;
        sc.FindProperty("sacudon").objectReferenceValue = sacudon;
        sc.FindProperty("temblor").objectReferenceValue = temblor;
        sc.ApplyModifiedPropertiesWithoutUndo();

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;
        if (avisos != "") Debug.LogWarning("[Cinemachine] No se pudieron ajustar estos valores (quedan por defecto):" + avisos);
        EditorUtility.DisplayDialog("Cinemachine",
            "Listo: la camara ahora es una CinemachineCamera (CM_Jugador).\nCamaraFollow quedo desactivado.\nProba en Play y guarda con Ctrl+S." +
            (avisos != "" ? "\n\n(Hay un aviso en la Console, no es grave.)" : ""), "OK");
    }

    [MenuItem("AnomaliMorfosis/Camara Cinemachine/Quitar (volver a CamaraFollow)")]
    public static void Quitar()
    {
        var go = GameObject.Find(Nombre);
        if (go != null) Undo.DestroyObjectImmediate(go);
        var camGO = GameObject.Find("Main Camera");
        if (camGO != null)
        {
            var brain = camGO.GetComponent<CinemachineBrain>();
            if (brain != null) Undo.DestroyObjectImmediate(brain);
            var follow = camGO.GetComponent<CamaraFollow>();
            if (follow != null) { Undo.RecordObject(follow, "Prender CamaraFollow"); follow.enabled = true; }
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static NoiseSettings ObtenerRuido()
    {
        var preset = AssetDatabase.LoadAssetAtPath<NoiseSettings>(RuidoPreset);
        if (preset != null) return preset;
        var propio = AssetDatabase.LoadAssetAtPath<NoiseSettings>(RuidoPropio);
        if (propio != null) return propio;

        propio = ScriptableObject.CreateInstance<NoiseSettings>();
        AssetDatabase.CreateAsset(propio, RuidoPropio);
        var so = new SerializedObject(propio);
        var ori = so.FindProperty("OrientationNoise");
        if (ori != null)
        {
            ori.arraySize = 1;
            var e = ori.GetArrayElementAtIndex(0);
            SetNoise(e, "X", 0.5f, 0.8f);
            SetNoise(e, "Y", 0.4f, 0.8f);
            SetNoise(e, "Z", 0.3f, 0.4f);
        }
        else avisos += "\n- Ruido de camara";
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return propio;
    }

    private static void SetNoise(SerializedProperty e, string eje, float frecuencia, float amplitud)
    {
        var f = e.FindPropertyRelative(eje + ".Frequency"); if (f != null) f.floatValue = frecuencia;
        var a = e.FindPropertyRelative(eje + ".Amplitude"); if (a != null) a.floatValue = amplitud;
    }

    // ---- helpers: si una propiedad no existe en esta version, se avisa y se sigue ----
    private static void Config(Object o, System.Action<SerializedObject> cambios)
    {
        var so = new SerializedObject(o);
        cambios(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static SerializedProperty P(SerializedObject so, string n) { var p = so.FindProperty(n); if (p == null) avisos += "\n- " + so.targetObject.GetType().Name + "." + n; return p; }
    private static void SetFloat(SerializedObject so, string n, float v) { var p = P(so, n); if (p != null) p.floatValue = v; }
    private static void SetInt(SerializedObject so, string n, int v) { var p = P(so, n); if (p != null) p.intValue = v; }
    private static void SetBool(SerializedObject so, string n, bool v) { var p = P(so, n); if (p != null) p.boolValue = v; }
    private static void SetString(SerializedObject so, string n, string v) { var p = P(so, n); if (p != null) p.stringValue = v; }
    private static void SetVec2(SerializedObject so, string n, Vector2 v) { var p = P(so, n); if (p != null) p.vector2Value = v; }
    private static void SetVec3(SerializedObject so, string n, Vector3 v) { var p = P(so, n); if (p != null) p.vector3Value = v; }
}
