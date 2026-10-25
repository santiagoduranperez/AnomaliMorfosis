using UnityEngine;

// Animaciones de los jefes del subsuelo — Parcial 2.
// Modelo y animaciones de Mixamo: Kiara Campos (Parasite, Humanoid + Animator Controller "Animacion jefes").
// IA de los jefes: Christian Marco (EnemigoJefe). Conexion: Santiago Duran Perez (25/10).
//  - Speed: velocidad real del jefe (NavMesh, tacleada o salto). Idle <-> Walking.
//  - Attack (trigger): se dispara cuando el jefe empieza la tacleada o dispara un proyectil.
// Mientras carga la tacleada (vibra) o queda frenado, Speed se fuerza a 0 para que no "camine en el lugar".
// Lo arma el menu: AnomaliMorfosis > Animaciones (Kiara) > Conectar
[RequireComponent(typeof(EnemigoJefe))]
public class AnimacionJefe : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float suavizado = 8f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private EnemigoJefe jefe;
    private Vector3 posAnterior;
    private float speed;
    private string estadoAnterior = "";

    private void Start()
    {
        jefe = GetComponent<EnemigoJefe>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        posAnterior = transform.position;
    }

    private void Update()
    {
        if (animator == null || jefe == null) return;
        string estado = jefe.Estado;

        Vector3 delta = transform.position - posAnterior; delta.y = 0f;
        posAnterior = transform.position;
        float v = Time.deltaTime > 0f ? Mathf.Min(delta.magnitude / Time.deltaTime, 15f) : 0f;
        if (estado == "CargandoTacleada" || estado == "FrenadoPostTacleada") v = 0f;
        speed = Mathf.Lerp(speed, v, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
        animator.SetFloat(SpeedHash, speed);

        if (estado != estadoAnterior)
        {
            if (estado == "Tacleando" || estado == "Disparando") animator.SetTrigger(AttackHash);
            estadoAnterior = estado;
        }
    }
}
