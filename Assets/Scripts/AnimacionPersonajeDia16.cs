using UnityEngine;

// Animaciones del protagonista transformado (dia 16) — Parcial 2.
// Modelo y animaciones de Mixamo: Kiara Campos (Humanoid + Animator Controller "AC_personaje final").
// Conexion al juego: Santiago Duran Perez (25/10).
// Al llegar al subsuelo (dia 16) el paciente armado con primitivas se reemplaza por el modelo de Kiara.
// El parametro Speed del Animator se calcula con la velocidad real del jugador:
//   Speed < 0.1 -> Idle | 0.1 a 2 -> Start Walking | > 2 -> Jogging
// Lo arma el menu: AnomaliMorfosis > Animaciones (Kiara) > Conectar
public class AnimacionPersonajeDia16 : MonoBehaviour
{
    [SerializeField] private GameObject modelo;      // Ch30_nonPBR (hijo del Player, empieza apagado)
    [SerializeField] private Animator animator;
    [SerializeField] private int diaTransformacion = 16;
    [SerializeField] private float suavizado = 10f;  // evita saltos bruscos entre animaciones

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private Vector3 posAnterior;
    private float speed;
    private bool activo;
    private Transform figura, deformaciones;

    private void Start()
    {
        posAnterior = transform.position;
        if (modelo != null) modelo.SetActive(false);
    }

    private void Update()
    {
        var gm = GameManagerHistoria.Instance;
        bool debeEstar = modelo != null && gm != null && gm.diaActual >= diaTransformacion;
        if (debeEstar != activo)
        {
            activo = debeEstar;
            modelo.SetActive(activo);
        }

        // La figura de primitivas (PersonajePrincipal) se oculta cuando aparece el modelo animado
        if (figura == null) figura = transform.Find("Figura_Paciente");
        if (figura != null && figura.gameObject.activeSelf == activo) figura.gameObject.SetActive(!activo);

        // Las deformaciones de los dias anteriores ya forman parte del cuerpo final
        if (activo)
        {
            if (deformaciones == null) deformaciones = transform.Find("deformacionesfolder");
            if (deformaciones != null)
                foreach (var r in deformaciones.GetComponentsInChildren<Renderer>())
                    if (r.enabled) r.enabled = false;
        }

        // Velocidad horizontal real (sirve igual con el CharacterController, el dash o la pausa)
        Vector3 delta = transform.position - posAnterior; delta.y = 0f;
        posAnterior = transform.position;
        float v = Time.deltaTime > 0f ? Mathf.Min(delta.magnitude / Time.deltaTime, 15f) : 0f;
        speed = Mathf.Lerp(speed, v, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
        if (activo && animator != null) animator.SetFloat(SpeedHash, speed);
    }
}
