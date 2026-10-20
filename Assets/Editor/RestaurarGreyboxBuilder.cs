// Herramienta de Editor — Parcial 2 (Santiago).
// Menu: AnomaliMorfosis > Restaurar Cama y plataformas
// Vuelve a crear, SOLO si faltan, la "Cama" (con Cama.cs, para dormir y pasar de dia)
// y las "Plataforma_Dia1" a "Plataforma_Dia6" (donde van los papeles de los dias 1 a 6),
// en las mismas posiciones que generaba GreyboxLevelBuilder. No toca nada mas de la escena.
// Ctrl+Z lo deshace. Despues: Ctrl+S.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RestaurarGreyboxBuilder
{
    [MenuItem("AnomaliMorfosis/Restaurar Cama y plataformas")]
    public static void Restaurar()
    {
        var celda = GameObject.Find("Celda_Inicial");
        var sala = GameObject.Find("Sala_Principal");
        if (celda == null || sala == null)
        {
            EditorUtility.DisplayDialog("Restaurar", "No encontre 'Celda_Inicial' o 'Sala_Principal' en la escena.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Restaurar Cama y plataformas");
        int grupo = Undo.GetCurrentGroup();
        int creados = 0;

        if (Object.FindAnyObjectByType<Cama>() == null)
        {
            var cama = Cubo("Cama", celda.transform, new Vector3(-0.8f, 0.25f, 1f), new Vector3(1f, 0.5f, 2f),
                            new Color(0.25f, 0.35f, 0.55f));
            cama.GetComponent<Collider>().isTrigger = true;
            cama.AddComponent<Cama>();
            creados++;
        }

        float[] alturas = { 2.0f, 1.4f, 0.9f, 0.5f, 0.2f, 0.05f };
        float[] posX = { -7f, -4.2f, -1.4f, 1.4f, 4.2f, 7f };
        for (int i = 0; i < alturas.Length; i++)
        {
            string nombre = "Plataforma_Dia" + (i + 1);
            if (sala.transform.Find(nombre) != null) continue;
            Cubo(nombre, sala.transform, new Vector3(posX[i], alturas[i] / 2f, 3f),
                 new Vector3(1.6f, alturas[i], 1.6f), new Color(0.55f, 0.35f, 0.20f));
            creados++;
        }

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Restaurar] Objetos creados: " + creados + " (0 = no faltaba nada). Guarda con Ctrl+S.");
        EditorUtility.DisplayDialog("Restaurar", "Listo: se crearon " + creados + " objetos.\nGuarda la escena con Ctrl+S.", "OK");
    }

    private static GameObject Cubo(string nombre, Transform padre, Vector3 posLocal, Vector3 escala, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posLocal;
        go.transform.localScale = escala;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        go.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = color };
        return go;
    }
}
