using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

// Script general para los jefes
public class EnemigoJefe : MonoBehaviour
{
    private enum EstadoEnemigo // que hace y que no puede hacer, puede tener mas de 1
    {
        Persiguiendo,
        CargandoTacleada,
        Tacleando,
        FrenadoPostTacleada,
        Saltando,
        Disparando
    }

    [Header("PARAMETROS GENERALES")]
    public float velocidadBase = 2.5f;
    public int danoAlTocar = 2;
    public float rangoDeteccion = 10f;

    [Header("TACLEADA")]
    public bool puedeTaclear = true;
    public float rangoTacleada = 6f;
    public float tiempoCargaTacleada = 3f;      // Duracion de la vibracion antes de embestir
    public float velocidadTacleada = 12f;       // Velocidad rápida al embestir
    public float duracionTacleada = 0.6f;       // Tiempo que dura el desplazamiento de embestida
    public float tiempoFrenadoPostTacleada = 5f; // Tiempo que queda congelado tras embestir
    public float cooldownTacleada = 8f;
    public float intensidadVibracion = 0.1f;    // Que tan fuerte tiembla al cargar

    [Header("SALTO")]
    public bool puedeSaltar = true;
    public float rangoSalto = 8f;
    public float velocidadSalto = 6f;          // Aumento de velocidad durante el salto
    public float fuerzaImpulsoSalto = 5f;       // Altura o impulso hacia arriba
    public float duracionSalto = 1.2f;
    public float cooldownSalto = 6f;
    [SerializeField] private float alturaSalto = 3f;
    private float posicionInicialY;

    [Header("DISPARO")]
    public bool puedeDisparar = true;
    public float rangoDisparo = 12f;
    public GameObject prefabProyectil;
    public Transform puntoDisparo;
    public float tiempoFrenoDisparo = 1f;       // Tiempo que se frena para disparar
    public float cooldownDisparo = 4f;

    [Header("PATRULLAJE CUANDO NO VE AL JUGADOR")]
    public List<Transform> puntosPatrulla;
    public float tiempoEsperaPunto = 2f;
    private int indicePuntoActual = 0;
    private bool esperandoEnPunto = false;

    private Transform jugadorTransform;
    private EstadoEnemigo estadoActual = EstadoEnemigo.Persiguiendo;
    private NavMeshAgent agent;

    // Timers de cooldown
    private float timerCooldownTacleada = 0f;
    private float timerCooldownSalto = 0f;
    private float timerCooldownDisparo = 0f;

    // Control interno de estados
    private float timerEstado = 0f;
    private Vector3 posicionInicialCarga;
    private Vector3 direccionTacleada;
    private bool yaHizoDanoEnTacleada = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = velocidadBase;
        }

        var jugadorGO = GameObject.FindGameObjectWithTag("Player");
        if (jugadorGO != null)
        {
            jugadorTransform = jugadorGO.transform;
        }
        if (puntosPatrulla != null && puntosPatrulla.Count > 0 && puntosPatrulla[0] != null)
        {
            ActivarNavMesh();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(puntosPatrulla[0].position);
            }
        }
    }

    private void Update()
    {
        if (jugadorTransform == null) return;

        ActualizarCooldowns();

        switch (estadoActual)
        {
            case EstadoEnemigo.Persiguiendo:
                ManejarPersecucionYEleccionAtaque();
                break;

            case EstadoEnemigo.CargandoTacleada:
                EjecutarCargaTacleada();
                break;

            case EstadoEnemigo.Tacleando:
                EjecutarTacleada();
                break;

            case EstadoEnemigo.FrenadoPostTacleada:
                EjecutarFrenadoPostTacleada();
                break;

            case EstadoEnemigo.Saltando:
                EjecutarSalto();
                break;

            case EstadoEnemigo.Disparando:
                EjecutarDisparo();
                break;
        }
    }

    private void ActualizarCooldowns()
    {
        if (timerCooldownTacleada > 0) timerCooldownTacleada -= Time.deltaTime;
        if (timerCooldownSalto > 0) timerCooldownSalto -= Time.deltaTime;
        if (timerCooldownDisparo > 0) timerCooldownDisparo -= Time.deltaTime;
    }

    private void ManejarPersecucionYEleccionAtaque()
    {
        float distancia = Vector3.Distance(transform.position, jugadorTransform.position);
        if (distancia > rangoDeteccion)
        {
            PatrullarFueraDeRango();
            return;
        }

        // SI EL JUGADOR ENTRA EN RANGO: PERSEGUIR Y ATACAR
        esperandoEnPunto = false; // Cancela la espera si venía patrullando

        if (puedeTaclear && timerCooldownTacleada <= 0f && distancia <= rangoTacleada)
        {
            DesactivarNavMesh();
            IniciarCargaTacleada();
            return;
        }

        if (puedeDisparar && timerCooldownDisparo <= 0f && distancia <= rangoDisparo)
        {
            DesactivarNavMesh();
            IniciarDisparo();
            return;
        }

        if (puedeSaltar && timerCooldownSalto <= 0f && distancia <= rangoSalto)
        {
            DesactivarNavMesh();
            IniciarSalto();
            return;
        }

        // Movimiento de persecución con NavMesh
        ActivarNavMesh();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = velocidadBase;
            agent.isStopped = false;
            agent.SetDestination(jugadorTransform.position);
        }
    }

    private void PatrullarFueraDeRango()
    {
        if (puntosPatrulla == null || puntosPatrulla.Count == 0)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            return;
        }

        ActivarNavMesh();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // Comprueba si llegó al punto de patrulla actual
        if (!esperandoEnPunto && !agent.pathPending && agent.remainingDistance <= 0.8f)
        {
            StartCoroutine(RutinaEsperarPuntoJefe());
        }
    }

    private IEnumerator RutinaEsperarPuntoJefe()
    {
        esperandoEnPunto = true;
        if (agent != null && agent.enabled) agent.isStopped = true;

        yield return new WaitForSeconds(tiempoEsperaPunto); // Espera 2 segundos (Punto 4 del parcial)

        indicePuntoActual = (indicePuntoActual + 1) % puntosPatrulla.Count;

        if (puntosPatrulla[indicePuntoActual] != null && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(puntosPatrulla[indicePuntoActual].position);
        }

        esperandoEnPunto = false;
    }
    // MANEJO DE NAVMESH
    private void ActivarNavMesh()
    {
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
        }
    }

    private void DesactivarNavMesh()
    {
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }
    }

    // --- LOGICA DE TACLEADA ---
    private void IniciarCargaTacleada()
    {
        estadoActual = EstadoEnemigo.CargandoTacleada;
        timerEstado = tiempoCargaTacleada;
        posicionInicialCarga = transform.position;
    }

    private void EjecutarCargaTacleada()
    {
        timerEstado -= Time.deltaTime;

        // Efecto visual de vibracion durante la carga
        Vector3 desplazamientoAleatorio = Random.insideUnitSphere * intensidadVibracion;
        desplazamientoAleatorio.y = 0; // mantener en plano horizontal
        transform.position = posicionInicialCarga + desplazamientoAleatorio;

        // Apuntar hacia el jugador mientras carga
        Vector3 mirarA = new Vector3(jugadorTransform.position.x, transform.position.y, jugadorTransform.position.z);
        transform.LookAt(mirarA);

        if (timerEstado <= 0f)
        {
            transform.position = posicionInicialCarga; // Restaurar antes de salir despedido
            direccionTacleada = (mirarA - transform.position).normalized;
            estadoActual = EstadoEnemigo.Tacleando;
            timerEstado = duracionTacleada;
            yaHizoDanoEnTacleada = false;
        }
    }

    private void EjecutarTacleada()
    {
        timerEstado -= Time.deltaTime;

        float distanciaPaso = velocidadTacleada * Time.deltaTime;

        if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, direccionTacleada, out RaycastHit hit, distanciaPaso))
        {
            estadoActual = EstadoEnemigo.FrenadoPostTacleada;
            timerEstado = tiempoFrenadoPostTacleada;
            timerCooldownTacleada = cooldownTacleada;
            return;
        }

        transform.position += direccionTacleada * distanciaPaso;

        if (timerEstado <= 0f)
        {
            estadoActual = EstadoEnemigo.FrenadoPostTacleada;
            timerEstado = tiempoFrenadoPostTacleada;
            timerCooldownTacleada = cooldownTacleada;
        }
    }

    private void EjecutarFrenadoPostTacleada()
    {
        timerEstado -= Time.deltaTime;

        if (timerEstado <= 0f)
        {
            ActivarNavMesh();
            estadoActual = EstadoEnemigo.Persiguiendo;
        }
    }

    // LOGICA DE SALTO
    private void IniciarSalto()
    {
        estadoActual = EstadoEnemigo.Saltando;
        timerEstado = duracionSalto;
        timerCooldownSalto = cooldownSalto;
        posicionInicialY = transform.position.y;
    }

    private void EjecutarSalto()
    {
        timerEstado -= Time.deltaTime;

        float progreso = Mathf.Clamp01(1f - (timerEstado / duracionSalto));

        Vector3 destinoHorizontal = new Vector3(jugadorTransform.position.x, transform.position.y, jugadorTransform.position.z);
        Vector3 nuevaPosicion = Vector3.MoveTowards(transform.position, destinoHorizontal, velocidadSalto * Time.deltaTime);

        float elevacionVertical = Mathf.Sin(progreso * Mathf.PI) * alturaSalto;
        nuevaPosicion.y = posicionInicialY + elevacionVertical;

        transform.position = nuevaPosicion;
        transform.LookAt(destinoHorizontal);

        if (timerEstado <= 0f)
        {
            ActivarNavMesh();
            estadoActual = EstadoEnemigo.Persiguiendo;
        }
    }

    // LOGICA DE DISPARO 
    private void IniciarDisparo()
    {
        estadoActual = EstadoEnemigo.Disparando;
        timerEstado = tiempoFrenoDisparo;
        timerCooldownDisparo = cooldownDisparo;

        // Orientar horizontalmente al jefe hacia el jugador
        Vector3 mirarA = new Vector3(jugadorTransform.position.x, transform.position.y, jugadorTransform.position.z);
        transform.LookAt(mirarA);

        // Instanciar y orientar el proyectil en diagonal
        if (prefabProyectil != null)
        {
            Transform origenDisparo = puntoDisparo != null ? puntoDisparo : transform;

            // 1 Instanciar el proyectil
            GameObject proyectil = Instantiate<GameObject>(prefabProyectil, origenDisparo.position, Quaternion.identity);

            // 2 Apuntar 
            Vector3 puntoObjetivoPecho = jugadorTransform.position + Vector3.up * 1f;

            // 3 Calcular el vector direccion en diagonal hacia el jugador
            Vector3 direccionDiagonal = (puntoObjetivoPecho - origenDisparo.position).normalized;

            // 4 Asignar esa direccion diagonal al proyectil
            proyectil.transform.forward = direccionDiagonal;
        }
    }

    private void EjecutarDisparo()
    {
        timerEstado -= Time.deltaTime;

        if (timerEstado <= 0f)
        {
            ActivarNavMesh();
            estadoActual = EstadoEnemigo.Persiguiendo;
        }
    }

    // DETECCION DE COLISION Y DAÑO
    private bool EsJugador(Collider col)
    {
        if (col.CompareTag("Player")) return true;
        if (col.transform.root.CompareTag("Player")) return true;
        if (col.attachedRigidbody != null && col.attachedRigidbody.CompareTag("Player")) return true;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!EsJugador(other)) return;

        if (GameManagerHistoria.Instance != null)
        {
            GameManagerHistoria.Instance.RecibirDano(danoAlTocar);
        }
    }
    private void OnDrawGizmosSelected()
    {
        // Visualizar el rango de detección en la vista de Escena
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}