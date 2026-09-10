using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class GameManagerHistoria : MonoBehaviour
{
    public static GameManagerHistoria Instance;

    [Header("Progreso de Historia")]
    public int diaActual = 1;
    public int papelesRecolectadosHoy = 0;

    [Header("Papeles (6 Espacios en Inspector)")]
    public GameObject[] papelesGameObjects;

    [Header("UI Lectura de Notas")]
    public GameObject canvasLectura;
    public Image imagenDocumentoUI;
    public TextMeshProUGUI textoDocumentoUI;
    public Button botonAtrasUI;

    [Header("UI Cartel de Interacción (Presionar E)")]
    public GameObject panelPromptInteractuar;
    public TextMeshProUGUI textoPromptInteractuar;

    [Header("Dormir")]
    public CanvasGroup fadeScreenGroup;
    public float duracionFade = 1.5f;

    [Header("Configuracion del Jugador")]
    public Transform jugadorTransform;
    public Transform puntoRespawnOriginal;
    public Transform puntoRespawnDia7;

    [Header("Transformacion")]
    public GameObject[] deformidadesCuerpo;

    [Header("Estadísticas del Jugador")]
    public float velocidadActual = 5f;
    public float fuerzaSaltoActual = 8f;
    public bool modoDashDesbloqueado = false;

    [Header("Vida (10 al inicio, no se recupera hasta dormir; baja a 3 tras la primera semana)")]
    public int vidaMaxima = 10;
    public int vidaActual = 10;

    [Header("UI de Estado (dia / vida / papeles)")]
    public TextMeshProUGUI textoEstadoUI;

    private Expediente documentoActivo;
    private bool leyendoNota = false;
    private bool procesandoGameOver = false;
    private bool hablasteConNpcHoy = false;
    private Coroutine promptTemporalCoroutine;

    [Header("Efecto de Anomalia (Instantiate en runtime, Clase 8)")]
    [SerializeField] private float duracionEfectoAnomalia = 2.5f;
    private GameObject efectoAnomaliaTemplate;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (botonAtrasUI != null)
            botonAtrasUI.onClick.AddListener(CerrarLecturaDocumento);

        if (canvasLectura != null)
            canvasLectura.SetActive(false);

        if (fadeScreenGroup != null)
        {
            fadeScreenGroup.alpha = 0f;
            fadeScreenGroup.gameObject.SetActive(false);
        }

        OcultarPrompt();
        ActualizarPapelesEnEscena();
        ActualizarEstadoUI();
        CrearTemplateEfectoAnomalia();
    }

    // Agregado el 10/09: crea (una sola vez, por codigo, sin depender de ningun Prefab
    // de Assets) el "molde" que despues se clona con Instantiate cada vez que aparece
    // una anomalia nueva (ver GenerarEfectoAnomalia). Queda desactivado: nunca se ve
    // el template en si, solo sus clones.
    private void CrearTemplateEfectoAnomalia()
    {
        efectoAnomaliaTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        efectoAnomaliaTemplate.name = "EfectoAnomalia_Template";
        efectoAnomaliaTemplate.transform.localScale = Vector3.one * 0.3f;

        var colliderTemplate = efectoAnomaliaTemplate.GetComponent<Collider>();
        if (colliderTemplate != null) Destroy(colliderTemplate); // es solo visual, no debe chocar con nada

        var renderer = efectoAnomaliaTemplate.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.6f, 0.1f, 0.7f); // violeta enfermizo, acorde a la tematica
            renderer.material = mat;
        }

        efectoAnomaliaTemplate.SetActive(false);
    }

    // Genera (Instantiate) un efecto visual pasajero cada vez que se aplica una
    // deformidad nueva, y lo destruye solo unos segundos despues (Destroy con delay:
    // "destruccion controlada de objetos", Clase 8). Se llama desde AplicarMutacionYStats.
    private void GenerarEfectoAnomalia()
    {
        if (efectoAnomaliaTemplate == null || jugadorTransform == null) return;

        Vector3 posicion = jugadorTransform.position + jugadorTransform.forward * 1f + Vector3.up * 1.5f;
        GameObject clon = Instantiate(efectoAnomaliaTemplate, posicion, Quaternion.identity);
        clon.SetActive(true);
        Destroy(clon, duracionEfectoAnomalia);
    }

    // Agregado el 10/09: version del prompt de interaccion que se muestra sola por un
    // tiempo fijo y se cierra sola, sin depender de que el jugador siga parado en un
    // trigger (a diferencia de Cama/Expediente). La usa el Raycast de inspeccion a
    // distancia de PlayerMovement.
    public void MostrarPromptTemporal(string mensaje, float duracion)
    {
        if (promptTemporalCoroutine != null) StopCoroutine(promptTemporalCoroutine);
        promptTemporalCoroutine = StartCoroutine(RutinaPromptTemporal(mensaje, duracion));
    }

    private IEnumerator RutinaPromptTemporal(string mensaje, float duracion)
    {
        MostrarPrompt(mensaje);
        yield return new WaitForSeconds(duracion);
        OcultarPrompt();
        promptTemporalCoroutine = null;
    }

    private void Update()
    {
        // Se agrego F el 08/09 como alternativa a K (quedaba lejos para volver de una
        // lectura), a pedido del QA de Uxia. K se deja funcionando igual para no romper
        // el cartel "Volver (K)" que ya esta armado en la escena.
        if (leyendoNota && Keyboard.current != null &&
            (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame))
        {
            CerrarLecturaDocumento();
        }
    }

    public void MostrarPrompt(string mensaje)
    {
        if (panelPromptInteractuar == null)
        {
            Debug.LogError("🔴 [ERROR] 'panelPromptInteractuar' NO está asignado en el Inspector del GameManager.");
            return;
        }

        if (textoPromptInteractuar != null)
            textoPromptInteractuar.text = mensaje;

        panelPromptInteractuar.SetActive(true);
        // TRUCO CLAVE: Obliga al cartel a ponerse por delante de todos los demás elementos del Canvas
        panelPromptInteractuar.transform.SetAsLastSibling();

        Debug.Log("🟢 Prompt activado en pantalla: " + mensaje);
    }

    public void OcultarPrompt()
    {
        if (panelPromptInteractuar != null)
            panelPromptInteractuar.SetActive(false);
    }

    public int ObtenerPapelesRequeridosHoy()
    {
        return 1;
    }

    public void AbrirLecturaDocumento(Expediente doc)
    {
        OcultarPrompt();
        documentoActivo = doc;
        if (imagenDocumentoUI != null) imagenDocumentoUI.sprite = doc.imagenNota;
        if (textoDocumentoUI != null) textoDocumentoUI.text = doc.contenidoTexto;


        canvasLectura.SetActive(true);
        leyendoNota = true;
        Time.timeScale = 0f;
    }

    // Igual que AbrirLecturaDocumento, pero para texto suelto (dialogo de NPC) que no
    // se "recolecta" ni se desactiva al cerrar: se puede volver a hablar con el mismo NPC.
    public void AbrirLecturaTexto(string texto)
    {
        OcultarPrompt();
        documentoActivo = null;
        if (imagenDocumentoUI != null) imagenDocumentoUI.sprite = null;
        if (textoDocumentoUI != null) textoDocumentoUI.text = texto;

        canvasLectura.SetActive(true);
        leyendoNota = true;
        Time.timeScale = 0f;
    }

    public void CerrarLecturaDocumento()
    {
        canvasLectura.SetActive(false);
        leyendoNota = false;
        Time.timeScale = 1f;

        if (documentoActivo != null)
        {
            documentoActivo.OnRecogido();
            documentoActivo = null;
            papelesRecolectadosHoy++;
            ActualizarEstadoUI();
        }
    }

    public bool PuedeDormir()
    {
        return papelesRecolectadosHoy >= ObtenerPapelesRequeridosHoy() && hablasteConNpcHoy;
    }

    // Llamado por NPCDialogo (solo el NPC amistoso, no el peligroso) cuando el jugador
    // habla con el. Agregado el 08/09: antes se podia dormir con solo agarrar el papel,
    // sin recorrer el resto de la sala, lo que dejaba partidas completas muy cortas
    // (el QA de Uxia midio 4:03 en vez de los ~10 minutos que piden los profes).
    public void RegistrarDialogoNpc()
    {
        hablasteConNpcHoy = true;
    }

    public void Dormir()
    {
        OcultarPrompt();

        // Agregado el 08/09: si ya es el ultimo dia con papel para recolectar, no hay
        // mas dias que generar (ActualizarPapelesEnEscena no tiene mas papeles), asi
        // que en vez de seguir el ciclo normal (que dejaba al jugador en un dia sin
        // papel, sin ningun aviso) mostramos una pantalla de cierre. Detectado por el
        // QA de Uxia: "al finalizar los 16 dias, el juego concluye pero no muestra
        // ninguna pantalla de victoria".
        if (diaActual >= papelesGameObjects.Length)
        {
            StartCoroutine(RutinaFinDelJuego());
        }
        else
        {
            StartCoroutine(RutinaTransicionNoche());
        }
    }

    private IEnumerator RutinaTransicionNoche()
    {
        Debug.Log("🟢 Iniciando transición de noche (Fade a negro)...");

        if (fadeScreenGroup == null)
        {
            Debug.LogError("🔴 [ERROR] 'fadeScreenGroup' NO está asignado en el Inspector del GameManager.");
        }
        else
        {
            fadeScreenGroup.gameObject.SetActive(true);
            fadeScreenGroup.transform.SetAsLastSibling(); // Obliga al panel negro a ir al frente de todo
        }

        // 1. Pantalla negra (Fade Out usando tiempo independiente de Time.timeScale)
        float timer = 0f;
        while (timer < duracionFade)
        {
            timer += Time.unscaledDeltaTime;
            if (fadeScreenGroup != null)
                fadeScreenGroup.alpha = Mathf.Lerp(0f, 1f, timer / duracionFade);
            yield return null;
        }

        if (fadeScreenGroup != null) fadeScreenGroup.alpha = 1f;

        // 2. Avanza el día
        diaActual++;
        papelesRecolectadosHoy = 0;
        hablasteConNpcHoy = false;
        vidaActual = vidaMaxima; // la vida no se recupera hasta dormir

        // 3. Aplica deformidad
        AplicarMutacionYStats();
        ActualizarEstadoUI();

        // 4. Respawn del jugador
        if (diaActual >= 7)
        {
            if (puntoRespawnDia7 != null)
                jugadorTransform.position = puntoRespawnDia7.position;

            modoDashDesbloqueado = true;
        }
        else
        {
            if (puntoRespawnOriginal != null)
                jugadorTransform.position = puntoRespawnOriginal.position;
        }

        ActualizarPapelesEnEscena();

        yield return new WaitForSecondsRealtime(1f);

        // 5. Vuelve la pantalla a la normalidad (Fade In)
        timer = 0f;
        while (timer < duracionFade)
        {
            timer += Time.unscaledDeltaTime;
            if (fadeScreenGroup != null)
                fadeScreenGroup.alpha = Mathf.Lerp(1f, 0f, timer / duracionFade);
            yield return null;
        }

        if (fadeScreenGroup != null)
        {
            fadeScreenGroup.alpha = 0f;
            fadeScreenGroup.gameObject.SetActive(false);
        }

        Debug.Log($"🟢 Transición terminada. Ahora estás en el Día {diaActual}");
    }

    private void AplicarMutacionYStats()
    {
        int indiceDeformidad = diaActual - 2;
        if (indiceDeformidad >= 0 && indiceDeformidad < deformidadesCuerpo.Length)
        {
            if (deformidadesCuerpo[indiceDeformidad] != null)
                deformidadesCuerpo[indiceDeformidad].SetActive(true);
        }

        GenerarEfectoAnomalia();

        velocidadActual -= 0.5f;
        fuerzaSaltoActual -= 0.8f;

        if (diaActual >= 7 && vidaMaxima > 3)
        {
            vidaMaxima = 3;
            vidaActual = Mathf.Min(vidaActual, vidaMaxima);
        }
    }

    // Resta vida al jugador (ej. al tocar a un paciente peligroso). La vida no se
    // recupera sola: solo vuelve al maximo al dormir (ver RutinaTransicionNoche), o al
    // llegar a 0 (ver RutinaGameOver).
    public void RecibirDano(int cantidad)
    {
        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        ActualizarEstadoUI();

        if (vidaActual <= 0 && !procesandoGameOver)
        {
            StartCoroutine(RutinaGameOver());
        }
    }

    // Agregado el 08/09: hasta ahora la vida se quedaba en 0 sin que pasara nada.
    // Cuando llega a 0: fade a negro, cartel de GAME OVER (reutiliza el mismo panel de
    // "prompt" que ya existe, asi no hace falta cablear nada nuevo en el Inspector),
    // y respawn con la vida llena en el punto de reaparicion del tramo actual (mismo
    // criterio que usa el respawn nocturno en RutinaTransicionNoche).
    private IEnumerator RutinaGameOver()
    {
        procesandoGameOver = true;
        Debug.Log("🔴 GAME OVER: la vida llego a 0. Reiniciando con vida llena...");

        if (fadeScreenGroup != null)
        {
            fadeScreenGroup.gameObject.SetActive(true);
            fadeScreenGroup.transform.SetAsLastSibling();
        }

        float timer = 0f;
        while (timer < duracionFade)
        {
            timer += Time.unscaledDeltaTime;
            if (fadeScreenGroup != null)
                fadeScreenGroup.alpha = Mathf.Lerp(0f, 1f, timer / duracionFade);
            yield return null;
        }
        if (fadeScreenGroup != null) fadeScreenGroup.alpha = 1f;

        MostrarPrompt("GAME OVER — recuperando fuerzas...");

        vidaActual = vidaMaxima;
        if (diaActual >= 7)
        {
            if (puntoRespawnDia7 != null) jugadorTransform.position = puntoRespawnDia7.position;
        }
        else
        {
            if (puntoRespawnOriginal != null) jugadorTransform.position = puntoRespawnOriginal.position;
        }
        ActualizarEstadoUI();

        yield return new WaitForSecondsRealtime(1.2f);

        OcultarPrompt();

        timer = 0f;
        while (timer < duracionFade)
        {
            timer += Time.unscaledDeltaTime;
            if (fadeScreenGroup != null)
                fadeScreenGroup.alpha = Mathf.Lerp(1f, 0f, timer / duracionFade);
            yield return null;
        }
        if (fadeScreenGroup != null)
        {
            fadeScreenGroup.alpha = 0f;
            fadeScreenGroup.gameObject.SetActive(false);
        }

        procesandoGameOver = false;
    }

    // Agregado el 08/09 junto con el cambio en Dormir(): pantalla de cierre simple
    // cuando se completan todos los dias del greybox. Reutiliza el mismo fadeScreenGroup
    // y panelPromptInteractuar que ya existen (mismo criterio que RutinaGameOver), asi
    // que no hace falta cablear nada nuevo en el Inspector ni tocar la escena.
    private IEnumerator RutinaFinDelJuego()
    {
        Debug.Log("Fin del juego: se completaron todos los dias del greybox.");

        if (fadeScreenGroup != null)
        {
            fadeScreenGroup.gameObject.SetActive(true);
            fadeScreenGroup.transform.SetAsLastSibling();
        }

        float timer = 0f;
        while (timer < duracionFade)
        {
            timer += Time.unscaledDeltaTime;
            if (fadeScreenGroup != null)
                fadeScreenGroup.alpha = Mathf.Lerp(0f, 1f, timer / duracionFade);
            yield return null;
        }
        if (fadeScreenGroup != null) fadeScreenGroup.alpha = 1f;

        MostrarPrompt($"FIN - sobreviviste {papelesGameObjects.Length} dias en la instalacion");
    }

    public void ActualizarEstadoUI()
    {
        if (textoEstadoUI == null) return;
        textoEstadoUI.text = $"Dia {diaActual}   Vida {vidaActual}/{vidaMaxima}   Papeles hoy {papelesRecolectadosHoy}/{ObtenerPapelesRequeridosHoy()}";
    }

    private void ActualizarPapelesEnEscena()
    {
        for (int i = 0; i < papelesGameObjects.Length; i++)
        {
            if (papelesGameObjects[i] != null)
                papelesGameObjects[i].SetActive(false);
        }

        int index = diaActual - 1;
        if (index >= 0 && index < papelesGameObjects.Length)
        {
            if (papelesGameObjects[index] != null)
                papelesGameObjects[index].SetActive(true);
        }
    }
}