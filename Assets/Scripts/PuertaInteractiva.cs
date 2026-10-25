using UnityEngine;

public class PuertaInteractiva : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private float anguloApertura = 90f; 
    [SerializeField] private float velocidadGiro = 2f;
    [SerializeField] private int diaMinimoParaAbrir = 1;

    [Header("Textos de UI")]
    [SerializeField] private GameObject cartelPresionarE; 
    [SerializeField] private GameObject cartelBloqueada;

    private bool estaAbierta = false;
    private Quaternion rotacionCerrada;
    private Quaternion rotacionAbierta;
    private bool jugadorCerca = false;

    private void Start()
    {
        rotacionCerrada = transform.localRotation;
        rotacionAbierta = rotacionCerrada * Quaternion.Euler(0, anguloApertura, 0);

        // Apagamos los carteles al iniciar
        if (cartelPresionarE != null) cartelPresionarE.SetActive(false);
        if (cartelBloqueada != null) cartelBloqueada.SetActive(false);
    }

    private void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            // Verificamos el día actual desde tu GameManagerHistoria
            int diaActualJuego = (GameManagerHistoria.Instance != null) ? GameManagerHistoria.Instance.diaActual : 1;

            if (diaActualJuego >= diaMinimoParaAbrir)
            {
                // Se puede abrir
                estaAbierta = !estaAbierta;
                if (cartelBloqueada != null) cartelBloqueada.SetActive(false);
            }
            else
            {
                // Está bloqueada por día
                Debug.Log("Aún no puedes abrir esta puerta, se requiere el día " + diaMinimoParaAbrir);
                if (cartelPresionarE != null) cartelPresionarE.SetActive(false);
                if (cartelBloqueada != null) cartelBloqueada.SetActive(true);
            }
        }

        // Suaviza el movimiento de apertura/cierre de la puerta
        Quaternion rotacionObjetivo = estaAbierta ? rotacionAbierta : rotacionCerrada;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, rotacionObjetivo, Time.deltaTime * velocidadGiro);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            int diaActualJuego = (GameManagerHistoria.Instance != null) ? GameManagerHistoria.Instance.diaActual : 1;

            // Dependiendo del día, mostramos un cartel u otro
            if (diaActualJuego >= diaMinimoParaAbrir)
            {
                if (cartelPresionarE != null) cartelPresionarE.SetActive(true);
            }
            else
            {
                if (cartelBloqueada != null) cartelBloqueada.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            // Apagamos ambos carteles al alejarse
            if (cartelPresionarE != null) cartelPresionarE.SetActive(false);
            if (cartelBloqueada != null) cartelBloqueada.SetActive(false);
        }
    }
}