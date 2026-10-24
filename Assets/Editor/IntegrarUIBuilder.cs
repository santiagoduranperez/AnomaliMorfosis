// Herramienta de Editor — Parcial 2 (Santiago, 24/10). Integra la UI de Giuli en el juego.
// Menu: AnomaliMorfosis > Integrar UI (Giuli)
//  1) Prefab MenuPausa: conecta el campo panelPausa con su hijo "Panel" y mete los botones adentro.
//  2) SampleScene: coloca el MenuPausa (si no esta) para que Esc pause el juego.
//  3) Build Settings: MainMenu primero, despues SampleScene, GameOver y Victory.
// Despues: Ctrl+S.
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class IntegrarUIBuilder
{
    private const string PrefabPausa = "Assets/Prefabs/MenuPausa.prefab";
    private static readonly string[] Escenas =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/GameOver.unity",
        "Assets/Scenes/Victory.unity",
    };

    [MenuItem("AnomaliMorfosis/Integrar UI (Giuli)")]
    public static void Integrar()
    {
        string informe = "";

        // 1) Conectar panelPausa dentro del prefab
        var raiz = PrefabUtility.LoadPrefabContents(PrefabPausa);
        if (raiz == null)
        {
            EditorUtility.DisplayDialog("Integrar UI", "No encontre " + PrefabPausa, "OK");
            return;
        }
        var menu = raiz.GetComponentInChildren<MenuPausa>(true);
        var panel = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Panel");
        if (menu != null && panel != null)
        {
            var so = new SerializedObject(menu);
            so.FindProperty("panelPausa").objectReferenceValue = panel.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            // Los botones estaban al lado del Panel: los paso adentro para que se oculten con el
            var botones = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Buttons"));
            if (botones != null && botones.parent != panel)
            {
                botones.SetParent(panel, true);
                botones.SetAsLastSibling();
                informe += "- Botones de la pausa movidos dentro del Panel.\n";
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabPausa);
            informe += "- MenuPausa: panelPausa conectado al Panel.\n";
        }
        else informe += "- AVISO: no encontre el script MenuPausa o el hijo 'Panel' en el prefab.\n";
        PrefabUtility.UnloadPrefabContents(raiz);

        // 2) Colocar el MenuPausa en la escena del juego
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene")
            informe += "- AVISO: la escena abierta es '" + escena.name + "'. Abri SampleScene y volve a correr el menu para poner la pausa.\n";
        else if (Object.FindAnyObjectByType<MenuPausa>(FindObjectsInactive.Include) != null)
            informe += "- La pausa ya estaba en la escena.\n";
        else
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPausa);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            Undo.RegisterCreatedObjectUndo(inst, "Poner MenuPausa");
            EditorSceneManager.MarkSceneDirty(escena);
            Selection.activeGameObject = inst;
            informe += "- MenuPausa agregado a SampleScene (Esc pausa).\n";
        }

        // 3) Orden de escenas del build
        EditorBuildSettings.scenes = Escenas
            .Where(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null)
            .Select(p => new EditorBuildSettingsScene(p, true))
            .ToArray();
        informe += "- Build: MainMenu, SampleScene, GameOver, Victory.\n";

        EditorUtility.DisplayDialog("Integrar UI (Giuli)", "Listo:\n" + informe + "\nGuarda con Ctrl+S.", "OK");
    }
}
