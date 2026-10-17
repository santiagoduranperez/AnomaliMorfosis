using System.Collections;
using UnityEngine;

public class AtmospheraDia16 : MonoBehaviour
{
    [Header("LUZ DIRECCIONAL")]
    [SerializeField] private Light luzDireccional;
    [SerializeField] private float intensidadOscura = 0.15f;
    [SerializeField] private Color colorLuzNoche = new Color(0.1f, 0.15f, 0.3f); // Azul frío/oscuro

    [Header("NIEBLA DEL ENTORNO")]
    [SerializeField] private bool activarNiebla = true;
    [SerializeField] private Color colorNiebla = new Color(0.05f, 0.05f, 0.1f);
    [SerializeField] private float densidadNiebla = 0.04f;

    [Header("EFECTO DE PARTICULAS")]
    [SerializeField] private ParticleSystem particulasNieblaOBrusas;

    [Header("TRANSICION SUAVE")]
    [SerializeField] private float duracionTransicion = 2f;

    /// <summary>
    /// </summary>
    public void ActivarAtmosceraJefe()
    {
        StartCoroutine(TransicionarAOscuridad());

        // Activar niebla ambiental en Unity
        RenderSettings.fog = activarNiebla;
        RenderSettings.fogColor = colorNiebla;
        RenderSettings.fogMode = FogMode.Exponential; 
        RenderSettings.fogDensity = densidadNiebla;

        // Reproducir partículas de humo/cenizas
        if (particulasNieblaOBrusas != null)
        {
            particulasNieblaOBrusas.Play();
        }
    }

    private IEnumerator TransicionarAOscuridad()
    {
        if (luzDireccional == null) yield break;

        float intensidadInicial = luzDireccional.intensity;
        Color colorInicial = luzDireccional.color;
        float tiempo = 0f;

        while (tiempo < duracionTransicion)
        {
            tiempo += Time.deltaTime;
            float progreso = tiempo / duracionTransicion;

            luzDireccional.intensity = Mathf.Lerp(intensidadInicial, intensidadOscura, progreso);
            luzDireccional.color = Color.Lerp(colorInicial, colorLuzNoche, progreso);

            yield return null;
        }

        luzDireccional.intensity = intensidadOscura;
        luzDireccional.color = colorLuzNoche;
    }
}