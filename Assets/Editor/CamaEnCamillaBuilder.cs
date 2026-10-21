// Herramienta de Editor — Parcial 2 (Santiago). Arregla el bug del QA de Uxia (20/10):
// "la camilla no tiene el script para dormir y se ve el cubo de la cama vieja al lado".
// Menu: AnomaliMorfosis > Usar la camilla como cama
//  - Oculta el cubo de la Cama (apaga su Mesh Renderer, NO la borra: el script Cama.cs sigue andando).
//  - Mueve el trigger de la Cama arriba de la camilla de Kiara y lo ajusta a su tamano,
//    asi el cartel "[E] Dormir" aparece al acercarse a la camilla.
// Ctrl+Z lo deshace. Despues: Ctrl+S.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CamaEnCamillaBuilder
{
    [MenuItem("AnomaliMorfosis/Usar la camilla como cama")]
    public static void Aplicar()
    {
        var cama = Object.FindAnyObjectByType<Cama>();
        var camilla = GameObject.Find("camilla");
        if (cama == null || camilla == null)
        {
            EditorUtility.DisplayDialog("Camilla", "No encontre " + (cama == null ? "la Cama (con Cama.cs)" : "el objeto 'camilla'") + " en la escena.", "OK");
            return;
        }

        var renderers = camilla.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            EditorUtility.DisplayDialog("Camilla", "La camilla no tiene modelos visibles para medir.", "OK");
            return;
        }
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        Undo.SetCurrentGroupName("Usar la camilla como cama");
        int grupo = Undo.GetCurrentGroup();

        var t = cama.transform;
        Undo.RecordObject(t, "Mover cama");
        t.rotation = Quaternion.identity;
        t.position = b.center;
        // la escala del padre puede no ser 1: se compensa para que el trigger mida lo mismo que la camilla
        Vector3 escalaPadre = t.parent != null ? t.parent.lossyScale : Vector3.one;
        Vector3 tam = b.size + new Vector3(0.6f, 0.6f, 0.6f); // margen para detectar al jugador al lado
        t.localScale = new Vector3(tam.x / escalaPadre.x, tam.y / escalaPadre.y, tam.z / escalaPadre.z);

        var mr = cama.GetComponent<MeshRenderer>();
        if (mr != null) { Undo.RecordObject(mr, "Ocultar cubo"); mr.enabled = false; }

        var col = cama.GetComponent<BoxCollider>();
        if (col != null) { Undo.RecordObject(col, "Trigger"); col.isTrigger = true; col.center = Vector3.zero; col.size = Vector3.one; }

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = cama.gameObject;
        EditorUtility.DisplayDialog("Camilla", "Listo: la camilla ahora es la cama (el cubo quedo invisible).\nProba en Play y guarda con Ctrl+S.", "OK");
    }
}
