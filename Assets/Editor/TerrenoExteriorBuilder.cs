// Herramienta de Editor — Parcial 2 (Santiago, rama escenario-p2).
// Menu: AnomaliMorfosis > Terreno exterior > Generar (Parcial 2) / Quitar
//
// Crea "Terreno_Exterior" alrededor de toda la instalacion (celda, corredor, sala y subsuelo):
//  - Un Terrain (asset en Assets/Terrenos/TerrenoExterior.asset) plano bajo los edificios y con
//    lomas que suben de a poco hacia los bordes, con ruido para que no quede perfecto.
//  - Dos capas de textura generadas por codigo: tierra/barro cerca de la instalacion y pasto seco
//    lejos (despues se pueden reemplazar por las texturas de Kiara en Paint Texture).
//  - Un cerco perimetral de postes y alambre, y arboles secos dispersos entre la niebla.
//
// Todo queda en la escena como objetos normales: se retoca a mano con las herramientas del Terrain
// (Raise/Lower, Smooth, Paint Texture...) como en la clase 10. Ctrl+Z deshace la generacion.
// Despues de generar: Ctrl+S para guardar la escena.

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TerrenoExteriorBuilder
{
    private const string RootName = "Terreno_Exterior";
    private const string Carpeta = "Assets/Terrenos";
    private const float AlturaMax = 40f;      // tope en Y del terreno (Terrain Height)
    private const float BaseY = -0.5f;        // el terreno arranca medio metro abajo del piso
    private const float AlturaBase = 0.2f;    // superficie a -0.3: queda escondida bajo los pisos
    private const float Margen = 70f;         // metros de terreno alrededor de la instalacion
    private const int Resolucion = 257;

    [MenuItem("AnomaliMorfosis/Terreno exterior/Generar (Parcial 2)")]
    public static void Generar()
    {
        var existente = GameObject.Find(RootName);
        if (existente != null)
        {
            if (!EditorUtility.DisplayDialog("Terreno ya generado",
                "Ya existe '" + RootName + "' en la escena. ¿Borrarlo y generarlo de nuevo?\n(Se pierden los retoques a mano.)",
                "Regenerar", "Cancelar")) return;
            Undo.DestroyObjectImmediate(existente);
        }

        Undo.SetCurrentGroupName("Generar terreno exterior");
        int grupo = Undo.GetCurrentGroup();

        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets", "Terrenos");

        // Limites de la instalacion (todo lo que tenga Renderer dentro del greybox y del subsuelo)
        Rect complejo = LimitesInstalacion();
        Rect rTerreno = new Rect(complejo.xMin - Margen, complejo.yMin - Margen,
                                 complejo.width + 2 * Margen, complejo.height + 2 * Margen);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Terreno exterior");

        // ---------- Terrain ----------
        var td = new TerrainData();
        td.heightmapResolution = Resolucion;
        td.size = new Vector3(rTerreno.width, AlturaMax, rTerreno.height);
        td.SetHeights(0, 0, CalcularAlturas(rTerreno, complejo));

        td.terrainLayers = new[]
        {
            CrearCapa("Tierra_Barro", new Color(0.20f, 0.16f, 0.12f), new Color(0.11f, 0.09f, 0.07f), 4f),
            CrearCapa("Pasto_Seco", new Color(0.27f, 0.26f, 0.15f), new Color(0.15f, 0.15f, 0.09f), 6f),
        };
        td.alphamapResolution = 256;
        td.SetAlphamaps(0, 0, CalcularTexturas(td.alphamapResolution, rTerreno, complejo));

        string rutaTerreno = AssetDatabase.GenerateUniqueAssetPath(Carpeta + "/TerrenoExterior.asset");
        AssetDatabase.CreateAsset(td, rutaTerreno);

        var terrenoGO = Terrain.CreateTerrainGameObject(td);
        Undo.RegisterCreatedObjectUndo(terrenoGO, "Terrain");
        terrenoGO.name = "Terreno";
        terrenoGO.transform.SetParent(root.transform, false);
        terrenoGO.transform.position = new Vector3(rTerreno.xMin, BaseY, rTerreno.yMin);
        var terreno = terrenoGO.GetComponent<Terrain>();

        // ---------- Cerco perimetral ----------
        var matMadera = CrearMaterial("Madera_Podrida", new Color(0.16f, 0.12f, 0.09f));
        var matAlambre = CrearMaterial("Alambre_Oxidado", new Color(0.22f, 0.17f, 0.13f));
        var cerco = Hijo("Cerco", root.transform);
        Rect rCerco = new Rect(complejo.xMin - 8f, complejo.yMin - 8f, complejo.width + 16f, complejo.height + 16f);
        CrearCerco(cerco, rCerco, terreno, matMadera, matAlambre);

        // ---------- Arboles secos ----------
        var arboles = Hijo("Arboles_Secos", root.transform);
        var rnd = new System.Random(16); // semilla fija: siempre sale el mismo bosque
        int creados = 0, intentos = 0;
        while (creados < 55 && intentos < 2000)
        {
            intentos++;
            float x = Mathf.Lerp(rTerreno.xMin + 5, rTerreno.xMax - 5, (float)rnd.NextDouble());
            float z = Mathf.Lerp(rTerreno.yMin + 5, rTerreno.yMax - 5, (float)rnd.NextDouble());
            float d = DistanciaARect(x, z, complejo);
            if (d < 12f || d > 55f) continue;
            float y = terreno.SampleHeight(new Vector3(x, 0, z)) + BaseY;
            CrearArbolSeco(arboles, new Vector3(x, y, z), rnd, matMadera);
            creados++;
        }

        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = terrenoGO;
        Debug.Log("[Terreno exterior] Generado (" + creados + " arboles). Retocalo con las herramientas del Terrain y guarda con Ctrl+S.");
    }

    [MenuItem("AnomaliMorfosis/Terreno exterior/Quitar")]
    public static void Quitar()
    {
        var root = GameObject.Find(RootName);
        if (root != null) Undo.DestroyObjectImmediate(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // ----------------------------------------------------------------------------------------

    private static Rect LimitesInstalacion()
    {
        bool hay = false;
        Bounds b = new Bounds();
        foreach (var nombre in new[] { "Greybox_InstalacionPrincipal", "Subsuelo" })
        {
            var go = GameObject.Find(nombre);
            if (go == null) continue;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds);
            }
        }
        if (!hay) return new Rect(-12f, -4f, 114f, 85f); // por si no encuentra nada
        return new Rect(b.min.x - 2f, b.min.z - 2f, b.size.x + 4f, b.size.z + 4f);
    }

    private static float DistanciaARect(float x, float z, Rect r)
    {
        float dx = Mathf.Max(r.xMin - x, 0f, x - r.xMax);
        float dz = Mathf.Max(r.yMin - z, 0f, z - r.yMax);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static float[,] CalcularAlturas(Rect rT, Rect complejo)
    {
        var h = new float[Resolucion, Resolucion];
        for (int zi = 0; zi < Resolucion; zi++)
        for (int xi = 0; xi < Resolucion; xi++)
        {
            float x = rT.xMin + rT.width * xi / (Resolucion - 1);
            float z = rT.yMin + rT.height * zi / (Resolucion - 1);
            float d = DistanciaARect(x, z, complejo);

            float altura = AlturaBase;
            float subida = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, Margen, d));
            float ruido = Mathf.PerlinNoise(x * 0.035f + 13.7f, z * 0.035f + 4.2f);
            float ruidoFino = Mathf.PerlinNoise(x * 0.15f + 2.1f, z * 0.15f + 9.3f);
            float cerca = Mathf.InverseLerp(2f, 14f, d); // sin relieve pegado a las paredes
            altura += subida * 16f + ruido * 4f * cerca * subida + ruidoFino * 0.35f * cerca;

            h[zi, xi] = Mathf.Clamp01(altura / AlturaMax);
        }
        return h;
    }

    private static float[,,] CalcularTexturas(int res, Rect rT, Rect complejo)
    {
        var a = new float[res, res, 2];
        for (int zi = 0; zi < res; zi++)
        for (int xi = 0; xi < res; xi++)
        {
            float x = rT.xMin + rT.width * xi / (res - 1);
            float z = rT.yMin + rT.height * zi / (res - 1);
            float d = DistanciaARect(x, z, complejo);
            float manchas = Mathf.PerlinNoise(x * 0.08f + 7.7f, z * 0.08f + 1.3f);
            float tierra = Mathf.Clamp01(1f - Mathf.InverseLerp(4f, 22f, d) + (manchas - 0.55f) * 1.6f);
            a[zi, xi, 0] = tierra;
            a[zi, xi, 1] = 1f - tierra;
        }
        return a;
    }

    private static TerrainLayer CrearCapa(string nombre, Color claro, Color oscuro, float tamanio)
    {
        string rutaPng = Carpeta + "/" + nombre + ".png";
        string rutaCapa = Carpeta + "/" + nombre + ".terrainlayer";
        var existente = AssetDatabase.LoadAssetAtPath<TerrainLayer>(rutaCapa);
        if (existente != null) return existente;

        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGB24, false);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            // ruido "tileable": se suman octavas de Perlin con coordenadas en anillo
            float u = x / (float)n, v = y / (float)n;
            float t = 0f, amp = 0.5f, freq = 4f;
            for (int o = 0; o < 4; o++)
            {
                float a1 = Mathf.PerlinNoise(Mathf.Cos(u * 2 * Mathf.PI) * freq + 50, Mathf.Sin(u * 2 * Mathf.PI) * freq + 50);
                float a2 = Mathf.PerlinNoise(Mathf.Cos(v * 2 * Mathf.PI) * freq + 90, Mathf.Sin(v * 2 * Mathf.PI) * freq + 90);
                t += (a1 + a2) * 0.5f * amp;
                amp *= 0.5f; freq *= 2f;
            }
            tex.SetPixel(x, y, Color.Lerp(oscuro, claro, Mathf.Clamp01(t * 1.1f)));
        }
        tex.Apply();
        File.WriteAllBytes(rutaPng, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(rutaPng);
        var importer = (TextureImporter)AssetImporter.GetAtPath(rutaPng);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.SaveAndReimport();

        var capa = new TerrainLayer
        {
            diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaPng),
            tileSize = new Vector2(tamanio, tamanio),
        };
        AssetDatabase.CreateAsset(capa, rutaCapa);
        return capa;
    }

    private static Material CrearMaterial(string nombre, Color color)
    {
        string ruta = Carpeta + "/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat != null) return mat;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader) { color = color };
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    private static Transform Hijo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.transform.SetParent(padre, false);
        return go.transform;
    }

    private static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        Object.DestroyImmediate(go.GetComponent<Collider>()); // decorativo: no molesta al jugador ni al NavMesh
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static void CrearCerco(Transform padre, Rect r, Terrain terreno, Material madera, Material alambre)
    {
        var esquinas = new[]
        {
            new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin),
            new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax),
        };
        int poste = 0;
        for (int lado = 0; lado < 4; lado++)
        {
            Vector2 a = esquinas[lado], b = esquinas[(lado + 1) % 4];
            float largo = Vector2.Distance(a, b);
            int cant = Mathf.Max(1, Mathf.RoundToInt(largo / 3f));
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= cant; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)cant);
                float y = terreno.SampleHeight(new Vector3(p.x, 0, p.y)) + BaseY;
                Vector3 pos = new Vector3(p.x, y, p.y);
                if (i < cant) // el ultimo poste de cada lado es el primero del lado siguiente
                {
                    var go = Pieza(PrimitiveType.Cube, "Poste_" + (poste++), padre, madera);
                    go.transform.position = pos + Vector3.up * 0.9f;
                    go.transform.localScale = new Vector3(0.14f, 1.8f, 0.14f);
                    // algunos postes torcidos
                    go.transform.rotation = Quaternion.Euler((poste * 37 % 11) - 5, 0, (poste * 53 % 9) - 4);
                }
                if (i > 0)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        float h = k == 0 ? 0.7f : 1.4f;
                        Vector3 p1 = prev + Vector3.up * h, p2 = pos + Vector3.up * h;
                        var al = Pieza(PrimitiveType.Cube, "Alambre", padre, alambre);
                        al.transform.position = (p1 + p2) * 0.5f;
                        al.transform.rotation = Quaternion.LookRotation(p2 - p1);
                        al.transform.localScale = new Vector3(0.03f, 0.03f, Vector3.Distance(p1, p2));
                    }
                }
                prev = pos;
            }
        }
    }

    private static void CrearArbolSeco(Transform padre, Vector3 pos, System.Random rnd, Material madera)
    {
        var arbol = new GameObject("ArbolSeco");
        Undo.RegisterCreatedObjectUndo(arbol, "ArbolSeco");
        arbol.transform.SetParent(padre, false);
        arbol.transform.position = pos;
        arbol.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 8 - 4, (float)rnd.NextDouble() * 360, (float)rnd.NextDouble() * 8 - 4);
        float alto = 4f + (float)rnd.NextDouble() * 4f;
        float grosor = 0.18f + (float)rnd.NextDouble() * 0.15f;

        var tronco = Pieza(PrimitiveType.Cylinder, "Tronco", arbol.transform, madera);
        tronco.transform.localPosition = new Vector3(0, alto / 2f, 0);
        tronco.transform.localScale = new Vector3(grosor, alto / 2f, grosor);

        int ramas = 2 + rnd.Next(3);
        for (int i = 0; i < ramas; i++)
        {
            float largo = 1.2f + (float)rnd.NextDouble() * 1.8f;
            float h = alto * (0.45f + (float)rnd.NextDouble() * 0.45f);
            var rama = Pieza(PrimitiveType.Cylinder, "Rama", arbol.transform, madera);
            rama.transform.localRotation = Quaternion.Euler(35f + (float)rnd.NextDouble() * 35f, (float)rnd.NextDouble() * 360f, 0f);
            rama.transform.localPosition = new Vector3(0, h, 0) + rama.transform.localRotation * new Vector3(0, largo / 2f, 0);
            rama.transform.localScale = new Vector3(grosor * 0.45f, largo / 2f, grosor * 0.45f);
        }
    }
}
