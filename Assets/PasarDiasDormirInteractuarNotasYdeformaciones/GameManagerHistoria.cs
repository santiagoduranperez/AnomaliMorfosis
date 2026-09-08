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
    }

    private void Update()
    {
        if (leyendoNota && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
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
        return papelesRecolectadosHoy >= ObtenerPapelesRequeridosHoy();
    }

    public void Dormir()
    {
        OcultarPrompt();
        StartCoroutine(RutinaTransicionNoche());
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