using UnityEngine;

public class MutacionParticulas : MonoBehaviour
{
    [Header("REFERENCIAS")]
    [SerializeField] private ParticleSystem particulasMutacion;

    [Header("CONFIGURACIÓN DE DIAS")]
    [SerializeField] private int diaInicioMutacion = 8;
    [SerializeField] private int diaMaximoMutacion = 16;

    [Header("EMISION DE PARTICULAS")]
    [Tooltip("Particulas por segundo empezando del dia 8")]
    [SerializeField] private float emisionDiaInicial = 0.7f;

    [Tooltip("Particulas de transformacion final")]
    [SerializeField] private float emisionDiaFinal = 10f;

    /// <summary>
    /// </summary>

    public void ActualizarParticulasPorDia(int diaActual)
    {
        if (particulasMutacion == null) return;

        var emission = particulasMutacion.emission;

        // Antes del dia 8 no hay nada
        if (diaActual < diaInicioMutacion)
        {
            emission.rateOverTime = 0f;
            particulasMutacion.Stop();
            return;
        }

        if (!particulasMutacion.isPlaying)
        {
            particulasMutacion.Play();
        }

        // Calcular la progresion
        float factorDia = (float)(diaActual - diaInicioMutacion) / (diaMaximoMutacion - diaInicioMutacion);
        factorDia = Mathf.Clamp01(factorDia); // Mantener entre 0 y 1

        float tasaEmisionActual = Mathf.Lerp(emisionDiaInicial, emisionDiaFinal, factorDia);
        emission.rateOverTime = tasaEmisionActual;
    }
}