// Herramienta de Editor — Parcial 2 (Santiago, 25/10). Conecta las animaciones de Kiara al juego.
// Menu: AnomaliMorfosis > Animaciones (Kiara) > Conectar / Probar en el subsuelo (en Play) / Quitar
//  1) Player (SampleScene): agrega el modelo Ch30_nonPBR como hijo "Modelo_Dia16" (apagado), con el
//     Animator "AC_personaje final", y el script AnimacionPersonajeDia16 que lo prende en el subsuelo.
//  2) Prefab Jefe (los 3 jefes): agrega el modelo Parasite como hijo "Modelo_Parasite" con el Animator
//     "Animacion jefes", oculta el cubo y los ojos (las luces rojas quedan) y agrega AnimacionJefe.
// Ctrl+Z deshace lo de la escena. Despues: Ctrl+S.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AnimacionesKiaraBuilder
{
    private const string ModeloPersonaje = "Assets/Prefabs/Animaciones personaje/Ch30_nonPBR.fbx";
    private const string ControllerPersonaje = "Assets/Prefabs/Animaciones personaje/AC_personaje final.controller";
    private const string ModeloJefe = "Assets/Prefabs/Animaciones Jefes/Parasite L Starkie.fbx";
    private const string ControllerJefe = "Assets/Prefabs/Animaciones Jefes/Animacion jefes.controller";
    private const string PrefabJefe = "Assets/JefesInformacion/Jefe.prefab";
    private const string NombrePersonaje = "Modelo_Dia16";
    private const string NombreJefe = "Modelo_Parasite";
    private const float AlturaPersonaje = 1.85f;
    private const float AlturaJefe = 2.6f;

    [MenuItem("AnomaliMorfosis/Animaciones (Kiara)/Conectar")]
    public static void Conectar()
    {
        string informe = "";
        var ctrlP = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPersonaje);
        var ctrlJ = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerJefe);
        var fbxP = AssetDatabase.LoadAssetAtPath<GameObject>(ModeloPersonaje);
        var fbxJ = AssetDatabase.LoadAssetAtPath<GameObject>(ModeloJefe);
        if (ctrlP == null || ctrlJ == null || fbxP == null || fbxJ == null)
        {
            EditorUtility.DisplayDialog("Animaciones (Kiara)", "No encontre los modelos o Animator Controllers de Kiara en Assets/Prefabs.", "OK");
            return;
        }

        // ---------- 1) Protagonista del dia 16 ----------
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) informe += "- AVISO: no encontre el Player (abri SampleScene).\n";
        else
        {
            Undo.SetCurrentGroupName("Animaciones Kiara");
            var viejo = player.transform.Find(NombrePersonaje);
            if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbxP);
            Undo.RegisterCreatedObjectUndo(modelo, NombrePersonaje);
            modelo.name = NombrePersonaje;
            modelo.transform.SetParent(player.transform, false);
            float piso = PisoDelJugador(player);
            Ajustar(modelo.transform, AlturaPersonaje, piso);
            var anim = PrepararAnimator(modelo, ctrlP);
            modelo.SetActive(false);

            var ctrl = player.GetComponent<AnimacionPersonajeDia16>();
            if (ctrl == null) ctrl = Undo.AddComponent<AnimacionPersonajeDia16>(player);
            var so = new SerializedObject(ctrl);
            so.FindProperty("modelo").objectReferenceValue = modelo;
            so.FindProperty("animator").objectReferenceValue = anim;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(player.scene);
            informe += "- Player: modelo del dia 16 conectado (Idle / Start Walking / Jogging con Speed).\n";
        }

        // ---------- 2) Jefes (prefab: aplica a los 3) ----------
        var raiz = PrefabUtility.LoadPrefabContents(PrefabJefe);
        if (raiz == null) informe += "- AVISO: no encontre " + PrefabJefe + "\n";
        else
        {
            var viejo = raiz.transform.Find(NombreJefe);
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

            var cubo = raiz.GetComponent<MeshRenderer>();
            float piso = cubo != null ? cubo.bounds.min.y : raiz.transform.position.y - 1f;

            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbxJ);
            modelo.name = NombreJefe;
            modelo.transform.SetParent(raiz.transform, false);
            Ajustar(modelo.transform, AlturaJefe, piso);
            var anim = PrepararAnimator(modelo, ctrlJ);

            if (cubo != null) cubo.enabled = false;
            foreach (var r in raiz.GetComponentsInChildren<MeshRenderer>(true))
                if (r.gameObject.name.StartsWith("Ojo")) r.enabled = false;

            var ctrl = raiz.GetComponent<AnimacionJefe>();
            if (ctrl == null) ctrl = raiz.AddComponent<AnimacionJefe>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("animator").objectReferenceValue = anim;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabJefe);
            PrefabUtility.UnloadPrefabContents(raiz);
            informe += "- Jefes: Parasite conectado (Idle / Walking con Speed, Attack al taclear o disparar).\n";
        }

        EditorUtility.DisplayDialog("Animaciones (Kiara)", "Listo:\n" + informe + "\nGuarda con Ctrl+S y proba en Play (dia 16).", "OK");
    }

    // Atajo para probar (equipo y QA): en Play salta directo al comienzo del subsuelo (dia 16),
    // usando la misma logica que el boton Retry del Game Over.
    [MenuItem("AnomaliMorfosis/Animaciones (Kiara)/Probar en el subsuelo (en Play)")]
    public static void ProbarEnSubsuelo()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Probar en el subsuelo", "Primero dale Play y despues usa este menu.", "OK");
            return;
        }
        GameManagerHistoria.reintentarDesdeSubsuelo = true;
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    [MenuItem("AnomaliMorfosis/Animaciones (Kiara)/Quitar")]
    public static void Quitar()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var m = player.transform.Find(NombrePersonaje); if (m != null) Undo.DestroyObjectImmediate(m.gameObject);
            var c = player.GetComponent<AnimacionPersonajeDia16>(); if (c != null) Undo.DestroyObjectImmediate(c);
            EditorSceneManager.MarkSceneDirty(player.scene);
        }
        var raiz = PrefabUtility.LoadPrefabContents(PrefabJefe);
        if (raiz != null)
        {
            var m = raiz.transform.Find(NombreJefe); if (m != null) Object.DestroyImmediate(m.gameObject);
            var c = raiz.GetComponent<AnimacionJefe>(); if (c != null) Object.DestroyImmediate(c);
            var cubo = raiz.GetComponent<MeshRenderer>(); if (cubo != null) cubo.enabled = true;
            foreach (var r in raiz.GetComponentsInChildren<MeshRenderer>(true)) if (r.gameObject.name.StartsWith("Ojo")) r.enabled = true;
            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabJefe);
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static Animator PrepararAnimator(GameObject modelo, RuntimeAnimatorController ctrl)
    {
        var anim = modelo.GetComponent<Animator>();
        if (anim == null) anim = modelo.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;                          // el movimiento lo hacen los scripts del equipo
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return anim;
    }

    private static float PisoDelJugador(GameObject player)
    {
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
            return player.transform.position.y + (cc.center.y - cc.height * 0.5f) * player.transform.lossyScale.y;
        return player.transform.position.y - 1f;
    }

    // Escala el modelo a una altura en metros (sin heredar la escala no uniforme del padre) y apoya los pies en el piso
    private static void Ajustar(Transform t, float altura, float piso)
    {
        Vector3 p = t.parent.lossyScale;
        t.localRotation = Quaternion.identity;
        t.localPosition = Vector3.zero;
        t.localScale = new Vector3(1f / p.x, 1f / p.y, 1f / p.z);
        Bounds b = Medir(t);
        if (b.size.y > 0.01f) t.localScale *= altura / b.size.y;
        b = Medir(t);
        t.position += Vector3.up * (piso - b.min.y);
    }

    private static Bounds Medir(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
