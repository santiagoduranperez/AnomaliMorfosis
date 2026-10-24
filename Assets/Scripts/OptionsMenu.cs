using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    public Slider sliderVolumen;
    public Toggle togglePantallaCompleta;

    void Start()
    {
        float volumen = PlayerPrefs.GetFloat("volumen", 1f);
        sliderVolumen.value = volumen;
        AudioListener.volume = volumen;
        togglePantallaCompleta.isOn = Screen.fullScreen;
    }

    public void CambiarVolumen(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat("volumen", valor);
    }

    public void PantallaCompleta(bool activa)
    {
        Screen.fullScreen = activa;
    }
}