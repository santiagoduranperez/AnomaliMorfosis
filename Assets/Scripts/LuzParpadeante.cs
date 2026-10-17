using System.Collections;
using UnityEngine;

// Luz que titila como un tubo fluorescente en mal estado (ambientacion de terror, Parcial 2).
// Usa una corrutina: cada cierto tiempo aleatorio dispara una rafaga de parpadeos y,
// de vez en cuando, un apagon corto. Se pone en cualquier GameObject que tenga un Light.
[RequireComponent(typeof(Light))]
public class LuzParpadeante : MonoBehaviour
{
    [Header("Intensidad")]
    [Tooltip("Intensidad normal de la luz (si queda en 0 se toma la que tenga el Light al arrancar).")]
    [SerializeField] private float intensidadBase = 0f;
    [Tooltip("Cuanto puede bajar la intensidad en cada parpadeo (0 = nada, 1 = se apaga del todo).")]
    [Range(0f, 1f)] [SerializeField] private float profundidadParpadeo = 0.8f;

    [Header("Tiempos (segundos)")]
    [Tooltip("Espera minima y maxima entre una rafaga de parpadeos y la siguiente.")]
    [SerializeField] private Vector2 esperaEntreRafagas = new Vector2(1.5f, 6f);
    [Tooltip("Cantidad minima y maxima de parpadeos por rafaga.")]
    [SerializeField] private Vector2Int parpadeosPorRafaga = new Vector2Int(2, 6);
    [Tooltip("Duracion de cada parpadeo individual.")]
    [SerializeField] private Vector2 duracionParpadeo = new Vector2(0.03f, 0.12f);

    [Header("Apagon")]
    [Tooltip("Probabilidad (0 a 1) de que una rafaga termine en un apagon corto.")]
    [Range(0f, 1f)] [SerializeField] private float probabilidadApagon = 0.15f;
    [SerializeField] private Vector2 duracionApagon = new Vector2(0.4f, 1.5f);

    [Header("Sonido (opcional)")]
    [Tooltip("Si hay un AudioSource en el mismo objeto, se reproduce en cada rafaga (zumbido/chispazo). Lo carga Uxia.")]
    [SerializeField] private AudioSource sonidoParpadeo;

    private Light luz;
    private Coroutine rutina;

    private void Awake()
    {
        luz = GetComponent<Light>();
        if (intensidadBase <= 0f) intensidadBase = luz.intensity;
        if (sonidoParpadeo == null) sonidoParpadeo = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        rutina = StartCoroutine(CicloParpadeo());
    }

    private void OnDisable()
    {
        if (rutina != null) StopCoroutine(rutina);
        if (luz != null) luz.intensity = intensidadBase;
    }

    private IEnumerator CicloParpadeo()
    {
        while (true)
        {
            // Luz estable un rato
            luz.intensity = intensidadBase;
            yield return new WaitForSeconds(Random.Range(esperaEntreRafagas.x, esperaEntreRafagas.y));

            if (sonidoParpadeo != null) sonidoParpadeo.Play();

            // Rafaga de parpadeos rapidos
            int cantidad = Random.Range(parpadeosPorRafaga.x, parpadeosPorRafaga.y + 1);
            for (int i = 0; i < cantidad; i++)
            {
                luz.intensity = intensidadBase * (1f - Random.Range(profundidadParpadeo * 0.5f, profundidadParpadeo));
                yield return new WaitForSeconds(Random.Range(duracionParpadeo.x, duracionParpadeo.y));
                luz.intensity = intensidadBase;
                yield return new WaitForSeconds(Random.Range(duracionParpadeo.x, duracionParpadeo.y));
            }

            // A veces se corta del todo
            if (Random.value < probabilidadApagon)
            {
                luz.intensity = 0f;
                yield return new WaitForSeconds(Random.Range(duracionApagon.x, duracionApagon.y));
            }
        }
    }
}
