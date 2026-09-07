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

    private Expediente documentoActivo;
    private bool leyendoNota = false;

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

        // 3. Aplica deformidad
        AplicarMutacionYStats();

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