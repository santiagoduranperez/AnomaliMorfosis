using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject panelOpciones;

    public void Jugar()
    {
        GameManagerHistoria.reintentarDesdeSubsuelo = false; // partida nueva desde el dia 1
        Time.timeScale = 1f;
        SceneManager.LoadScene("SampleScene");
    }

    public void AbrirOpciones()
    {
        if (panelOpciones != null)
            panelOpciones.SetActive(true);
    }

    public void CerrarOpciones()
    {
        if (panelOpciones != null)
            panelOpciones.SetActive(false);
    }

    public void Salir()
    {
        Application.Quit();
    }
}