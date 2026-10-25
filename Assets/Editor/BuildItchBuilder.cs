// Herramienta de Editor — Parcial 2 (Santiago, 25/10). Genera el build de Windows para subir a itch.io.
// Menu: AnomaliMorfosis > Build para itch.io (Windows)
// Usa las escenas de Build Settings (MainMenu, SampleScene, GameOver, Victory) y deja el juego en
// C:\Users\<usuario>\OneDrive\Desktop\MOTORES DESARROLLO 1\Build Parcial 2\AnomaliMorfosis
// (fuera del repositorio, asi no se sube a GitHub). Para itch.io: comprimir esa carpeta en .zip.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildItchBuilder
{
    [MenuItem("AnomaliMorfosis/Build para itch.io (Windows)")]
    public static void Construir()
    {
        string escritorio = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
        string carpeta = Path.Combine(escritorio, "MOTORES DESARROLLO 1", "Build Parcial 2", "AnomaliMorfosis");
        Directory.CreateDirectory(carpeta);

        var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (escenas.Length == 0 || !escenas[0].EndsWith("MainMenu.unity"))
        {
            EditorUtility.DisplayDialog("Build", "La primera escena del build tiene que ser MainMenu. Usa antes AnomaliMorfosis > Integrar UI (Giuli).", "OK");
            return;
        }

        PlayerSettings.productName = "AnomaliMorfosis";
        PlayerSettings.companyName = "Grupo T20 - UTN";
        PlayerSettings.bundleVersion = "2.0 (Parcial 2)";

        var opciones = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = Path.Combine(carpeta, "AnomaliMorfosis.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport r = BuildPipeline.BuildPlayer(opciones);
        if (r.summary.result == BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Build", "Listo (" + (r.summary.totalSize / 1048576f).ToString("0") + " MB).\nCarpeta:\n" + carpeta +
                "\n\nPara itch.io: comprimir la carpeta AnomaliMorfosis en .zip.", "OK");
            EditorUtility.RevealInFinder(carpeta);
        }
        else
            EditorUtility.DisplayDialog("Build", "El build fallo: " + r.summary.result + ". Mira la Console.", "OK");
    }
}
