using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Herramienta de Editor para generar de una sola vez el greybox del Parcial 1
// (instalacion principal con puzles de altura ligados al salto degradable) mas
// el GameManagerHistoria y la UI minima que necesita para funcionar.
//
// Todo se crea con la API de Unity (GameObject.CreatePrimitive, AddComponent, etc.),
// no se toca el archivo .unity a mano, asi que es 100% deshacible con Ctrl+Z y
// revisable en el Editor antes de commitear.
//
// Layout (coordenadas de mundo, todo alineado para que no haya huecos ni paredes
// que bloqueen el paso):
//   Celda_Inicial: piso x[-2,2] z[-2,2], puerta norte (hueco x[-1.5,1.5]) en z=2.
//   Corredor:      piso x[-1.5,1.5] z[2,10].
//   Sala_Principal:piso x[-10,10] z[10,26], puerta sur (hueco x[-1.5,1.5]) en z=10.
//
// Uso: en Unity, menu "AnomaliMorfosis > Generar Nivel Greybox (Parcial 1)".
public static class GreyboxLevelBuilder
{
    private const string RootName = "Greybox_InstalacionPrincipal";

    [MenuItem("AnomaliMorfosis/Generar Nivel Greybox (Parcial 1)")]
    public static void GenerarNivel()
    {
        // Greybox_InstalacionPrincipal es padre de Canvas_UI, asi que se limpia solo.
        // GameManagerHistoria y EventSystem quedan sueltos en la raiz de la escena
        // (no son hijos de nada), asi que hay que buscarlos y borrarlos aparte o quedan
        // duplicados cada vez que se regenera.
        var existenteRoot = GameObject.Find(RootName);
        var existenteGM = GameObject.Find("GameManagerHistoria");
        if (existenteRoot != null || existenteGM != null)
        {
            bool regenerar = EditorUtility.DisplayDialog(
                "Greybox ya existe",
                "Ya hay un nivel generado en la escena (Greybox y/o GameManagerHistoria). ¿Borrar todo y regenerarlo desde cero?",
                "Regenerar", "Cancelar");
            if (!regenerar) return;
            if (existenteRoot != null) Undo.DestroyObjectImmediate(existenteRoot);
            if (existenteGM != null) Undo.DestroyObjectImmediate(existenteGM);
        }

        Undo.SetCurrentGroupName("Generar Nivel Greybox");
        int grupoUndo = Undo.GetCurrentGroup();

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generar Nivel Greybox");

        // Materiales simples por color (greybox = sin arte final, solo para diferenciar bloques)
        var matPiso = CrearMaterial(new Color(0.55f, 0.55f, 0.55f));
        var matPared = CrearMaterial(new Color(0.75f, 0.75f, 0.78f));
        var matCelda = CrearMaterial(new Color(0.35f, 0.30f, 0.30f));
        var matPlataforma = CrearMaterial(new Color(0.55f, 0.35f, 0.20f));
        var matCama = CrearMaterial(new Color(0.25f, 0.35f, 0.55f));
        var matPapel = CrearMaterial(new Color(0.9f, 0.85f, 0.5f));

        // ---------- 1) Celda inicial (piso mundo x[-2,2] z[-2,2]) ----------
        var celda = CrearParent("Celda_Inicial", root.transform, new Vector3(0, 0, 0));
        CrearCubo("Piso_Celda", celda, new Vector3(0, -0.25f, 0), new Vector3(4, 0.5f, 4), matCelda);
        CrearCubo("Pared_Celda_Oeste", celda, new Vector3(-2f, 1.5f, 0), new Vector3(0.3f, 3, 4), matPared);
        CrearCubo("Pared_Celda_Este", celda, new Vector3(2f, 1.5f, 0), new Vector3(0.3f, 3, 4), matPared);
        CrearCubo("Pared_Celda_Sur", celda, new Vector3(0, 1.5f, -2f), new Vector3(4, 3, 0.3f), matPared);
        // Pared norte partida en 2, deja una puerta de x[-1.5,1.5] hacia el corredor
        CrearCubo("Pared_Celda_Norte_Izq", celda, new Vector3(-1.75f, 1.5f, 2f), new Vector3(0.5f, 3, 0.3f), matPared);
        CrearCubo("Pared_Celda_Norte_Der", celda, new Vector3(1.75f, 1.5f, 2f), new Vector3(0.5f, 3, 0.3f), matPared);

        // Movido el 08/09 de (-1.2, 1.2) a (-0.8, 1): la posicion original pisaba el
        // marco de la puerta norte (Pared_Celda_Norte_Izq), un bug visual/de colision
        // que reporto el QA de Uxia ("la cama choca contra una columna").
        var camaGO = CrearCubo("Cama", celda, new Vector3(-0.8f, 0.25f, 1f), new Vector3(1, 0.5f, 2), matCama);
        ConfigurarTrigger(camaGO);
        camaGO.AddComponent<Cama>();

        // ---------- 2) Corredor (piso mundo x[-1.5,1.5] z[2,10]) ----------
        var corredor = CrearParent("Corredor", root.transform, new Vector3(0, 0, 6f));
        CrearCubo("Piso_Corredor", corredor, new Vector3(0, -0.25f, 0), new Vector3(3, 0.5f, 8), matPiso);
        CrearCubo("Pared_Corredor_Izq", corredor, new Vector3(-1.5f, 1.5f, 0), new Vector3(0.3f, 3, 8), matPared);
        CrearCubo("Pared_Corredor_Der", corredor, new Vector3(1.5f, 1.5f, 0), new Vector3(0.3f, 3, 8), matPared);

        // ---------- 3) Sala principal (piso mundo x[-10,10] z[10,26]): 6 plataformas ----------
        // Alturas calculadas a partir de PlayerMovement.cs (baseJumpForce=8, jumpDecayPerDay=1.5,
        // gravity=-9.81): altura maxima teorica = fuerza^2 / (2*9.81), con margen para el desplazamiento.
        // Dia1: ~3.26m posible -> plataforma 2.0m | Dia6: ~0.05m posible -> plataforma casi al piso.
        var sala = CrearParent("Sala_Principal", root.transform, new Vector3(0, 0, 18f));
        CrearCubo("Piso_Sala", sala, new Vector3(0, -0.25f, 0), new Vector3(20, 0.5f, 16), matPiso);
        CrearCubo("Pared_Sala_Norte", sala, new Vector3(0, 2, 8f), new Vector3(20, 4, 0.3f), matPared);
        CrearCubo("Pared_Sala_Este", sala, new Vector3(10f, 2, 0), new Vector3(0.3f, 4, 16), matPared);
        CrearCubo("Pared_Sala_Oeste", sala, new Vector3(-10f, 2, 0), new Vector3(0.3f, 4, 16), matPared);
        // Pared sur partida en 2, deja la puerta de x[-1.5,1.5] que da al corredor
        CrearCubo("Pared_Sala_Sur_Izq", sala, new Vector3(-5.75f, 2, -8f), new Vector3(8.5f, 4, 0.3f), matPared);
        CrearCubo("Pared_Sala_Sur_Der", sala, new Vector3(5.75f, 2, -8f), new Vector3(8.5f, 4, 0.3f), matPared);

        // Dias 1 a 6: alturas calculadas del salto real (mismo criterio de siempre).
        // Dias 7 a 15: agregados el 08/09 (noche) para llegar a un playthrough de mas de
        // 10 minutos, como pide la catedra. Desde el dia 7 el salto ya esta en 0 (ver
        // CurrentJumpForce en PlayerMovement.cs), asi que estos 9 expedientes van al ras
        // del piso (sin puzzle de altura): encaja con el High Concept, despues de perder
        // el salto el desafio pasa a ser explorar/sobrevivir, no saltar.
        float[] alturas =
        {
            2.0f, 1.4f, 0.9f, 0.5f, 0.2f, 0.05f,
            0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f,
        };
        float[] posX =
        {
            -7f, -4.2f, -1.4f, 1.4f, 4.2f, 7f,
            -8.8f, -6.6f, -4.4f, -2.2f, 0f, 2.2f, 4.4f, 6.6f, 8.8f,
        };
        float[] posZ =
        {
            3f, 3f, 3f, 3f, 3f, 3f,
            6f, 6f, 6f, 6f, 6f, 6f, 6f, 6f, 6f,
        };
        var papeles = new GameObject[alturas.Length];

        for (int i = 0; i < alturas.Length; i++)
        {
            int dia = i + 1;
            CrearCubo($"Plataforma_Dia{dia}", sala,
                new Vector3(posX[i], alturas[i] / 2f, posZ[i]),
                new Vector3(1.6f, alturas[i], 1.6f), matPlataforma);

            var papelGO = CrearCubo($"Papel_Dia{dia}", sala,
                new Vector3(posX[i], alturas[i] + 0.3f, posZ[i]),
                new Vector3(0.4f, 0.05f, 0.5f), matPapel);
            ConfigurarTrigger(papelGO);
            var expediente = papelGO.AddComponent<Expediente>();
            expediente.contenidoTexto = $"Expediente del Dia {dia} - (texto pendiente: rol de Diseno de Juego / Narrativa).";
            papeles[i] = papelGO;
        }

        // ---------- 4) Puntos de respawn ----------
        var respawnOriginal = CrearParent("Respawn_Original", root.transform, new Vector3(0, 1, 0));
        var respawnDia7 = CrearParent("Respawn_Dia7", root.transform, new Vector3(0, 1, 15f));

        // ---------- 4.5) NPCs (pacientes): uno amistoso con dialogo, uno peligroso que resta vida ----------
        var matNpcAmistoso = CrearMaterial(new Color(0.3f, 0.55f, 0.35f));
        var matNpcPeligroso = CrearMaterial(new Color(0.6f, 0.15f, 0.15f));

        var npcAmistoso = CrearCapsula("Paciente_Amistoso", sala, new Vector3(-6f, 1f, -5f), matNpcAmistoso);
        ConfigurarTrigger(npcAmistoso);
        var dialogoAmistoso = npcAmistoso.AddComponent<NPCDialogo>();
        dialogoAmistoso.frases = new[]
        {
            "Otro dia mas aca adentro... (dialogo pendiente: rol de Diseno de Juego / Narrativa)",
            "Yo tambien vi cosas raras en el subsuelo. No bajes solo.",
        };

        var npcPeligroso = CrearCapsula("Paciente_Peligroso", sala, new Vector3(6f, 1f, -5f), matNpcPeligroso);
        ConfigurarTrigger(npcPeligroso);
        var dialogoPeligroso = npcPeligroso.AddComponent<NPCDialogo>();
        dialogoPeligroso.esPeligroso = true;
        dialogoPeligroso.danoAlTocar = 2;

        // ---------- 5) UI minima (Canvas + EventSystem) ----------
        var canvasGO = CrearCanvas(root.transform);
        var panelPrompt = CrearPanelPrompt(canvasGO.transform, out TextMeshProUGUI textoPrompt);
        var canvasLectura = CrearCanvasLectura(canvasGO.transform, out Image imagenDoc, out TextMeshProUGUI textoDoc, out Button botonAtras);
        var fadeGroup = CrearFadeScreen(canvasGO.transform, out CanvasGroup canvasGroup);
        CrearEventSystemSiNoExiste();

        // ---------- 6) GameManagerHistoria: crear y cablear todas las referencias ----------
        var gmGO = new GameObject("GameManagerHistoria");
        Undo.RegisterCreatedObjectUndo(gmGO, "Generar Nivel Greybox");
        var gm = gmGO.AddComponent<GameManagerHistoria>();

        gm.papelesGameObjects = papeles;
        gm.canvasLectura = canvasLectura;
        gm.imagenDocumentoUI = imagenDoc;
        gm.textoDocumentoUI = textoDoc;
        gm.botonAtrasUI = botonAtras;
        gm.panelPromptInteractuar = panelPrompt;
        gm.textoPromptInteractuar = textoPrompt;
        gm.fadeScreenGroup = canvasGroup;

        var textoEstado = CrearHudEstado(canvasGO.transform);
        gm.textoEstadoUI = textoEstado;

        var player = GameObject.Find("Player");
        if (player != null)
        {
            gm.jugadorTransform = player.transform;
            if (!player.CompareTag("Player"))
            {
                Debug.LogWarning("[GreyboxLevelBuilder] El objeto 'Player' no tiene el tag 'Player'. " +
                    "Cama.cs y Expediente.cs lo necesitan para detectar al jugador. Revisar manualmente.");
            }
        }
        else
        {
            Debug.LogWarning("[GreyboxLevelBuilder] No se encontro ningun GameObject llamado 'Player' en la escena para asignar a jugadorTransform.");
        }

        gm.puntoRespawnOriginal = respawnOriginal.transform;
        gm.puntoRespawnDia7 = respawnDia7.transform;

        EditorUtility.SetDirty(gm);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(grupoUndo);

        Debug.Log("Greybox + GameManagerHistoria + UI generados. Pendiente manual:\n" +
            "1) Si Unity muestra el dialogo 'Import TMP Essentials', aceptarlo (hace falta para que se vea el texto).\n" +
            "2) El Player ya deberia quedar parado en Celda_Inicial (coincide con su posicion actual 0,1,0). Si no, moverlo ahi.\n" +
            "3) Probar con Play: caminar, saltar, agarrar Papel_Dia1 (tecla E), ir a la Cama y dormir (tecla E) una vez agarrado el papel. Ahora hay 15 dias/expedientes en vez de 6 (para llegar a mas de 10 minutos de juego).\n" +
            "4) El texto de cada expediente (Expediente.contenidoTexto) y el sprite (imagenNota) son placeholder: los completa Diseno de Juego / Narrativa (Christian).\n" +
            "5) deformidadesCuerpo del GameManagerHistoria quedo vacio a proposito: son visuales de Arte (Kiara/Kiku), se agregan despues.\n" +
            "6) Ahora hay un HUD arriba a la izquierda (dia/vida/papeles), y 2 NPCs en la Sala Principal: el verde (Paciente_Amistoso) da dialogo con E, el rojo (Paciente_Peligroso) resta 2 de vida al tocarlo.");

        Selection.activeGameObject = root;
    }

    private static GameObject CrearParent(string nombre, Transform padre, Vector3 posicionLocal)
    {
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, "Generar Nivel Greybox");
        go.transform.SetParent(padre);
        go.transform.localPosition = posicionLocal;
        return go;
    }

    private static GameObject CrearCubo(string nombre, GameObject padre, Vector3 posicionLocal, Vector3 escala, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        Undo.RegisterCreatedObjectUndo(go, "Generar Nivel Greybox");
        go.transform.SetParent(padre.transform);
        go.transform.localPosition = posicionLocal;
        go.transform.localScale = escala;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return go;
    }

    private static GameObject CrearCapsula(string nombre, GameObject padre, Vector3 posicionLocal, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = nombre;
        Undo.RegisterCreatedObjectUndo(go, "Generar Nivel Greybox");
        go.transform.SetParent(padre.transform);
        go.transform.localPosition = posicionLocal;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return go;
    }

    private static void ConfigurarTrigger(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null) collider.isTrigger = true;
    }

    private static Material CrearMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private static GameObject CrearCanvas(Transform padre)
    {
        var canvasGO = new GameObject("Canvas_UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Generar Nivel Greybox");
        canvasGO.transform.SetParent(padre);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        return canvasGO;
    }

    private static GameObject CrearPanelPrompt(Transform canvas, out TextMeshProUGUI texto)
    {
        var panel = new GameObject("PanelPromptInteractuar", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Generar Nivel Greybox");
        panel.transform.SetParent(canvas, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 80);
        rect.sizeDelta = new Vector2(320, 60);

        var img = panel.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);

        var textoGO = new GameObject("TextoPromptInteractuar", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(textoGO, "Generar Nivel Greybox");
        textoGO.transform.SetParent(panel.transform, false);
        var textoRect = textoGO.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = Vector2.zero;
        textoRect.offsetMax = Vector2.zero;

        texto = textoGO.AddComponent<TextMeshProUGUI>();
        texto.text = "[E] Interactuar";
        texto.alignment = TextAlignmentOptions.Center;
        texto.fontSize = 28;
        texto.color = Color.white;

        return panel;
    }

    private static GameObject CrearCanvasLectura(Transform canvas, out Image imagenDoc, out TextMeshProUGUI textoDoc, out Button botonAtras)
    {
        var panel = new GameObject("CanvasLectura", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Generar Nivel Greybox");
        panel.transform.SetParent(canvas, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0, 0, 0, 0.85f);

        // Imagen del documento (placeholder, arriba)
        var imagenGO = new GameObject("ImagenDocumentoUI", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(imagenGO, "Generar Nivel Greybox");
        imagenGO.transform.SetParent(panel.transform, false);
        var imagenRect = imagenGO.GetComponent<RectTransform>();
        imagenRect.anchorMin = new Vector2(0.5f, 0.55f);
        imagenRect.anchorMax = new Vector2(0.5f, 0.55f);
        imagenRect.pivot = new Vector2(0.5f, 0.5f);
        imagenRect.sizeDelta = new Vector2(400, 300);
        imagenDoc = imagenGO.GetComponent<Image>();
        imagenDoc.color = new Color(1, 1, 1, 0.15f); // placeholder visible aunque no tenga sprite

        // Texto del documento (debajo de la imagen)
        var textoGO = new GameObject("TextoDocumentoUI", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(textoGO, "Generar Nivel Greybox");
        textoGO.transform.SetParent(panel.transform, false);
        var textoRect = textoGO.GetComponent<RectTransform>();
        textoRect.anchorMin = new Vector2(0.2f, 0.15f);
        textoRect.anchorMax = new Vector2(0.8f, 0.4f);
        textoRect.offsetMin = Vector2.zero;
        textoRect.offsetMax = Vector2.zero;
        textoDoc = textoGO.AddComponent<TextMeshProUGUI>();
        textoDoc.text = "";
        textoDoc.alignment = TextAlignmentOptions.Center;
        textoDoc.fontSize = 24;
        textoDoc.color = Color.white;

        // Boton Atras (tambien se puede cerrar con la tecla K, ya cableado en GameManagerHistoria)
        var botonGO = new GameObject("BotonAtrasUI", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(botonGO, "Generar Nivel Greybox");
        botonGO.transform.SetParent(panel.transform, false);
        var botonRect = botonGO.GetComponent<RectTransform>();
        botonRect.anchorMin = new Vector2(0.5f, 0.08f);
        botonRect.anchorMax = new Vector2(0.5f, 0.08f);
        botonRect.pivot = new Vector2(0.5f, 0.5f);
        botonRect.sizeDelta = new Vector2(160, 50);
        botonGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        botonAtras = botonGO.GetComponent<Button>();

        var botonTextoGO = new GameObject("Texto", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(botonTextoGO, "Generar Nivel Greybox");
        botonTextoGO.transform.SetParent(botonGO.transform, false);
        var botonTextoRect = botonTextoGO.GetComponent<RectTransform>();
        botonTextoRect.anchorMin = Vector2.zero;
        botonTextoRect.anchorMax = Vector2.one;
        botonTextoRect.offsetMin = Vector2.zero;
        botonTextoRect.offsetMax = Vector2.zero;
        var botonTexto = botonTextoGO.AddComponent<TextMeshProUGUI>();
        botonTexto.text = "Volver (K)";
        botonTexto.alignment = TextAlignmentOptions.Center;
        botonTexto.fontSize = 22;
        botonTexto.color = Color.white;

        panel.SetActive(false); // GameManagerHistoria.Start() ya lo hace, esto es solo para que no se vea en el Editor
        return panel;
    }

    private static GameObject CrearFadeScreen(Transform canvas, out CanvasGroup canvasGroup)
    {
        var go = new GameObject("FadeScreenGroup", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        Undo.RegisterCreatedObjectUndo(go, "Generar Nivel Greybox");
        go.transform.SetParent(canvas, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        go.GetComponent<Image>().color = Color.black;
        canvasGroup = go.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        go.SetActive(false); // GameManagerHistoria.Start() ya lo hace, esto es solo para que no se vea en el Editor
        return go;
    }

    private static TextMeshProUGUI CrearHudEstado(Transform canvas)
    {
        var go = new GameObject("HUD_Estado", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Generar Nivel Greybox");
        go.transform.SetParent(canvas, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(600, 40);

        var texto = go.AddComponent<TextMeshProUGUI>();
        texto.text = "Dia 1   Vida 10/10   Papeles hoy 0/1";
        texto.alignment = TextAlignmentOptions.TopLeft;
        texto.fontSize = 26;
        texto.color = Color.white;
        return texto;
    }

    private static void CrearEventSystemSiNoExiste()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Generar Nivel Greybox");
        Debug.Log("[GreyboxLevelBuilder] Se creo un EventSystem con InputSystemUIInputModule (el proyecto usa el Input System nuevo). " +
            "Si el boton 'Volver' de la lectura de notas no responde al click, revisar que el InputSystemUIInputModule tenga asignado " +
            "un Input Actions Asset de UI (se puede cerrar la lectura igual con la tecla K).");
    }
}
