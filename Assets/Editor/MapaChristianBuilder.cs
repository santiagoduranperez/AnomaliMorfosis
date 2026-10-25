// Herramienta de Editor — Parcial 2 (Santiago, 25/10). Pasa el mapa nuevo de Christian (EscenaConMapa)
// a ser la escena principal del juego, sin perder nada de lo integrado.
// Menu: AnomaliMorfosis > Mapa de Christian > Usar como escena principal
//  1) La escena actual se guarda como respaldo: Assets/Scenes/Respaldo/SampleScene_MapaAnterior.unity
//  2) EscenaConMapa pasa a llamarse SampleScene (el menu, el Retry y la Victoria cargan ese nombre).
//  3) Le agrega lo que se integro despues de que Christian armo su escena:
//     camara Cinemachine, pausa de Giuli, animaciones de Kiara y la mesa medica que faltaba.
// Menu: AnomaliMorfosis > Mapa de Christian > Jefes: dano al tocar = 1
//     En el dia 16 la vida maxima es 3 y los jefes sacaban 3: morias de un golpe. Con 1 aguantas 3 golpes.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapaChristianBuilder
{
    private const string Actual = "Assets/Scenes/SampleScene.unity";
    private const string Nueva = "Assets/Scenes/EscenaConMapa.unity";
    private const string CarpetaRespaldo = "Assets/Scenes/Respaldo";
    private const string Respaldo = "Assets/Scenes/Respaldo/SampleScene_MapaAnterior.unity";
    private static readonly string[] ObjetosAConservar = { "medical_table_-_17mb" };

    [MenuItem("AnomaliMorfosis/Mapa de Christian/Usar como escena principal")]
    public static void UsarComoPrincipal()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Mapa de Christian", "Primero sali del modo Play.", "OK"); return; }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Nueva) == null)
        {
            EditorUtility.DisplayDialog("Mapa de Christian", "No encontre " + Nueva + " (o ya se uso este menu).", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Mapa de Christian",
            "La escena actual se guarda como respaldo en Assets/Scenes/Respaldo y el mapa de Christian pasa a ser SampleScene.\n\nSeguir?",
            "Si, usar el mapa nuevo", "Cancelar")) return;

        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // libera las escenas antes de moverlas

        if (!AssetDatabase.IsValidFolder(CarpetaRespaldo)) AssetDatabase.CreateFolder("Assets/Scenes", "Respaldo");
        string e1 = AssetDatabase.MoveAsset(Actual, Respaldo);
        string e2 = e1 == "" ? AssetDatabase.MoveAsset(Nueva, Actual) : "no se movio";
        if (e1 != "" || e2 != "")
        {
            EditorUtility.DisplayDialog("Mapa de Christian", "No pude mover las escenas:\n" + e1 + "\n" + e2, "OK");
            return;
        }

        var escena = EditorSceneManager.OpenScene(Actual, OpenSceneMode.Single);

        // Objetos del mapa anterior que no estaban en el de Christian (ej. una mesa de Kiara)
        string copiados = "";
        var vieja = EditorSceneManager.OpenScene(Respaldo, OpenSceneMode.Additive);
        foreach (var nombre in ObjetosAConservar)
        {
            if (BuscarRaiz(escena, nombre) != null) continue;
            var src = BuscarRaiz(vieja, nombre);
            if (src == null) continue;
            var asset = PrefabUtility.GetCorrespondingObjectFromSource(src);
            GameObject copia = asset != null ? (GameObject)PrefabUtility.InstantiatePrefab(asset, escena) : Object.Instantiate(src);
            SceneManager.MoveGameObjectToScene(copia, escena);
            copia.name = src.name;
            copia.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
            copia.transform.localScale = src.transform.localScale;
            copiados += "\n   - " + nombre;
        }
        EditorSceneManager.CloseScene(vieja, true);
        SceneManager.SetActiveScene(escena);

        // Lo que se integro despues (cada herramienta muestra su propio cartel)
        CinemachineCamaraBuilder.Aplicar();
        IntegrarUIBuilder.Integrar();          // pausa + orden de escenas del build
        AnimacionesKiaraBuilder.Conectar();    // protagonista del dia 16 (los jefes ya vienen por el prefab)
        BajarDanoJefes();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        EditorUtility.DisplayDialog("Mapa de Christian",
            "Listo: el mapa de Christian ahora es SampleScene (la escena del juego)." +
            (copiados != "" ? "\nTraje del mapa anterior:" + copiados : "") +
            "\nRespaldo del mapa anterior: " + Respaldo, "OK");
    }

    [MenuItem("AnomaliMorfosis/Mapa de Christian/Jefes: dano al tocar = 1")]
    public static void BajarDanoJefes()
    {
        // Prefab (los 3 jefes) y las copias de la escena por si tienen el valor cambiado
        var raiz = PrefabUtility.LoadPrefabContents("Assets/JefesInformacion/Jefe.prefab");
        if (raiz != null)
        {
            var j = raiz.GetComponent<EnemigoJefe>();
            if (j != null) j.danoAlTocar = 1;
            PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/JefesInformacion/Jefe.prefab");
            PrefabUtility.UnloadPrefabContents(raiz);
        }
        foreach (var j in Object.FindObjectsByType<EnemigoJefe>(FindObjectsInactive.Include))
        {
            Undo.RecordObject(j, "Dano jefes");
            j.danoAlTocar = 1;
            PrefabUtility.RecordPrefabInstancePropertyModifications(j);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Jefes] Dano al tocar = 1 (la vida en el dia 16 es 3: aguantas 3 golpes).");
    }

    private static GameObject BuscarRaiz(Scene s, string nombre)
    {
        foreach (var go in s.GetRootGameObjects()) if (go.name == nombre) return go;
        return null;
    }
}
