// Herramienta de Editor — Parcial 2 (Santiago).
// Menu: AnomaliMorfosis > Guia de recorrido > Agregar a la escena / Quitar
// Crea el objeto "Guia_Recorrido" con la luz guia (GuiaRecorrido.cs). Ctrl+Z lo deshace.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GuiaRecorridoBuilder
{
    private const string Nombre = "Guia_Recorrido";

    [MenuItem("AnomaliMorfosis/Guia de recorrido/Agregar a la escena")]
    public static void Agregar()
    {
        if (GameObject.Find(Nombre) != null)
        {
            EditorUtility.DisplayDialog("Guia de recorrido", "Ya hay un '" + Nombre + "' en la escena.", "OK");
            return;
        }
        var go = new GameObject(Nombre);
        Undo.RegisterCreatedObjectUndo(go, "Guia de recorrido");
        go.AddComponent<Light>();
        go.AddComponent<GuiaRecorrido>();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;
        Debug.Log("[Guia de recorrido] Agregada. Dale Play: la luz va al papel del dia, despues al paciente amistoso y despues a la cama. Guarda con Ctrl+S.");
    }

    [MenuItem("AnomaliMorfosis/Guia de recorrido/Quitar")]
    public static void Quitar()
    {
        var go = GameObject.Find(Nombre);
        if (go != null) Undo.DestroyObjectImmediate(go);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
