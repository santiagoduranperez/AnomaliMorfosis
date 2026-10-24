using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private string escenaMenu = "MainMenu";
    private bool pausado;

    private void Start() { panelPausa.SetActive(false); }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (pausado) Reanudar(); else Pausar();
        }
    }

    public void Pausar()
    {
        pausado = true;
        panelPausa.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Reanudar()
    {
        pausado = false;
        panelPausa.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(escenaMenu);
    }

    public void Salir() { Application.Quit(); }
}