using UnityEngine;
using UnityEngine.SceneManagement;

public class PantallaFinal : MonoBehaviour
{
    public string escenaJuego = "SampleScene";
    public string escenaMenu = "MainMenu";

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Reintentar()
    {
        SceneManager.LoadScene(escenaJuego);
    }

    public void VolverAlMenu()
    {
        GameManagerHistoria.reintentarDesdeSubsuelo = false; // desde el menu se empieza una partida nueva
        SceneManager.LoadScene(escenaMenu);
    }
}