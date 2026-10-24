using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TriggerVictoria : MonoBehaviour
{
    [Header("Ui de Vicotria")]
    [SerializeField] private GameObject canvasVictoria;
    [SerializeField] private TextMeshProUGUI textoMensajeVictoria;

    private bool victoriaProcesada = false;

    private void Start()
    {
        if (canvasVictoria != null)
            canvasVictoria.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo se activa al tocar al jugador 
        if (!victoriaProcesada && other.CompareTag("Player"))
        {
            victoriaProcesada = true;
            EjecutarVictoria();
        }
    }

    private void EjecutarVictoria()
    {
        Debug.Log("VICTORIA! El jugador llego a la salida.");

        if (canvasVictoria != null)
        {
            canvasVictoria.SetActive(true);
        }

        if (textoMensajeVictoria != null)
        {
            textoMensajeVictoria.text = "¡ESCAPE EXITOSO!\nSobreviviste";
        }

        // Libera el cursor y pasa a la pantalla de Victoria de Giuli (Santiago, 24/10)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameManagerHistoria.reintentarDesdeSubsuelo = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Victory");
    }

}