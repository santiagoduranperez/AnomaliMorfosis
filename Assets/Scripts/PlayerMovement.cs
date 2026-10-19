using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Character controller basico en tercera persona para AnomaliMorfosis.
// Sigue el enfoque de la Clase 5 (Raycast, Trigger y Colisiones): CharacterController
// no tiene fisica propia, asi que la gravedad y el salto se calculan a mano.
//
// SETUP EN UNITY (hacerlo una sola vez sobre el GameObject del Player):
// 1) Agregar el componente CharacterController (Add Component > Character Controller).
// 2) Agregar este script; como tiene [RequireComponent(typeof(PlayerInput))],
//    Unity va a agregar el componente PlayerInput solo.
// 3) En el componente PlayerInput: dejar que Unity cree/asigne el Input Actions Asset
//    por defecto (ya trae las acciones "Move" y "Jump" en el Action Map "Player").
// 4) En PlayerInput, poner Behavior = "Send Messages" (asi Unity llama solo a
//    OnMove() y OnJump() de este script, sin cablear nada mas).
// 5) Probar en Play: moverse con WASD / stick, saltar con Espacio / boton Sur.
// 6) Ajustar moveSpeed, baseJumpForce y jumpDecayPerDay a gusto.
//
// El salto degradable ahora se sincroniza automaticamente con el GameManagerHistoria
// de Christian (su "diaActual") cuando ese script esta presente en la escena.
// Si se prueba este script solo, en una escena sin GameManagerHistoria, usa el
// valor "currentDay" hardcodeado del Inspector como respaldo.

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Salto degradable (mecanica central del High Concept)")]
    [SerializeField] private float baseJumpForce = 8f;
    [SerializeField] private float jumpDecayPerDay = 1.5f;
    [SerializeField] private int currentDay = 0; // Fallback cuando no hay GameManagerHistoria en la escena (ej. escena de prueba)

    [Header("Habilidad DASH (Desbloqueable en Dia 16)")]
    [SerializeField] private float velocidadDash = 18f;
    [SerializeField] private float duracionDash = 0.25f;
    [SerializeField] private float cooldownDash = 1.2f;
    [Tooltip("Permite probar el Dash en cualquier dia sin esperar al Dia 16.")]
    [SerializeField] private bool desbloquearDashParaPruebas = false;

    [Header("Fisica manual (CharacterController no trae fisica propia)")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Inspeccion a distancia (Raycast, Clase 5)")]
    [Tooltip("Distancia maxima del Raycast de inspeccion, en metros.")]
    [SerializeField] private float distanciaInspeccion = 6f;

    [Header("Audio SFX")]
    [SerializeField] private AudioSource fuenteSFX;
    [SerializeField] private AudioClip[] clipsPasos; // Variaciones de pasos
    [SerializeField] private AudioClip[] clipsPasosTransformados; //al dia 16 suenan otros pasos
    [SerializeField] private AudioClip clipSalto;
    [SerializeField] private AudioClip clipDash;

    // Timer interno para el intervalo de pasos
    private float timerPasos = 0f;
    [SerializeField] private float intervaloBasePasos = 0.45f; // Cada 0,45 s suenan los pasos

    private CharacterController controller;
    private Vector2 moveInput;
    private float verticalVelocity;

    // Estado de Dash e Invulnerabilidad
    public bool EsInvulnerable { get; private set; } = false;
    public bool estaHaciendoDash = false;
    private float timerCooldownDash = 0f;

    // Dia 1 = sin degradacion todavia (diaActual arranca en 1 en el GameManagerHistoria).
    // Si existe el GameManagerHistoria de Christian en la escena, se usa su diaActual
    // real; si no (ej. escena de prueba sin el GameManager), se usa currentDay del Inspector.
    private int DayForDecay => GameManagerHistoria.Instance != null
        ? GameManagerHistoria.Instance.diaActual - 1
        : currentDay;

    private float CurrentJumpForce => Mathf.Max(0f, baseJumpForce - DayForDecay * jumpDecayPerDay);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (timerCooldownDash > 0f)
        {
            timerCooldownDash -= Time.deltaTime;
        }

        // Deteccion de Shift para realizar el Dash
        if (Keyboard.current != null && (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame))
        {
            IntentarEfectuarDash();
        }

        if (!estaHaciendoDash)
        {
            HandleGravityAndJump();
            HandleMovement();
            HandleInspeccionRaycast();
        }
    }

    private bool PuedeUsarDash()
    {
        if (desbloquearDashParaPruebas) return true;

        if (GameManagerHistoria.Instance != null)
        {
            return GameManagerHistoria.Instance.modoDashDesbloqueado || GameManagerHistoria.Instance.diaActual >= 16;
        }

        return false;
    }

    private void IntentarEfectuarDash()
    {
        if (!PuedeUsarDash()) return;
        if (estaHaciendoDash || timerCooldownDash > 0f) return;

        StartCoroutine(RutinaDash());
    }

    private IEnumerator RutinaDash()
    {
        estaHaciendoDash = true;
        EsInvulnerable = true;
        timerCooldownDash = cooldownDash;

        SonarSFX(clipDash); // sonido del DASH

        Vector3 camForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
        Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;
        Vector3 direccionDash = moveDir.sqrMagnitude > 0.01f ? moveDir.normalized : transform.forward;

        transform.forward = direccionDash;

        float timer = 0f;
        while (timer < duracionDash)
        {
            timer += Time.deltaTime;
            Vector3 desplazamiento = direccionDash * velocidadDash;
            desplazamiento.y = -0.5f;

            controller.Move(desplazamiento * Time.deltaTime);
            yield return null;
        }

        estaHaciendoDash = false;
        EsInvulnerable = false;
    }

    // Agregado el 10/09: Raycast de inspeccion a distancia (Clase 5), con la tecla R.
    // Primera version usaba transform.forward, pero el jugador no gira visualmente hacia
    // donde camina (no hay ningun script que rote el modelo), asi que "apuntar" con la
    // mirada no funcionaba. Se cambio por un enfoque mas robusto y muy usado en juegos
    // reales: buscar el expediente o NPC mas cercano en CUALQUIER direccion, y tirar el
    // Raycast derecho hacia el para confirmar que no hay una pared tapando la vista
    // ("chequeo de linea de vista"). Sigue siendo un sistema aparte del de triggers que
    // ya tienen Cama/Expediente/NPCDialogo: ese detecta por colision, este por Raycast.
    private void HandleInspeccionRaycast()
    {
        if (Keyboard.current == null || !Keyboard.current.rKey.wasPressedThisFrame) return;
        if (GameManagerHistoria.Instance == null) return;

        Vector3 origen = transform.position + Vector3.up * 1f;
        Transform objetivo = EncontrarObjetivoMasCercano(origen);

        if (objetivo == null)
        {
            Debug.Log("🔵 [Raycast-R] No hay ningun expediente ni NPC dentro de " + distanciaInspeccion + "m.");
            return;
        }

        Vector3 direccion = (objetivo.position - origen).normalized;

        if (Physics.Raycast(origen, direccion, out RaycastHit hit, distanciaInspeccion))
        {
            Debug.Log($"🔵 [Raycast-R] Pego en: '{hit.collider.name}' a {hit.distance:F1}m");

            var expediente = hit.collider.GetComponent<Expediente>();
            var npc = hit.collider.GetComponent<NPCDialogo>();

            if (expediente != null)
                GameManagerHistoria.Instance.MostrarPromptTemporal("A lo lejos: parece un expediente medico.", 2f);
            else if (npc != null)
                GameManagerHistoria.Instance.MostrarPromptTemporal("A lo lejos: hay alguien ahi.", 2f);
        }
    }

    // Recorre los Expediente y NPCDialogo activos en la escena y devuelve el mas cercano
    // al jugador, siempre que este dentro de distanciaInspeccion. Se llama solo al
    // presionar R (no todos los frames), asi que el costo es insignificante.
    private Transform EncontrarObjetivoMasCercano(Vector3 origen)
    {
        Transform mejor = null;
        float mejorDistancia = distanciaInspeccion;

        foreach (var exp in Object.FindObjectsByType<Expediente>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(origen, exp.transform.position);
            if (d < mejorDistancia)
            {
                mejorDistancia = d;
                mejor = exp.transform;
            }
        }

        foreach (var npc in Object.FindObjectsByType<NPCDialogo>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(origen, npc.transform.position);
            if (d < mejorDistancia)
            {
                mejorDistancia = d;
                mejor = npc.transform;
            }
        }

        return mejor;
    }

    // Llamado automaticamente por PlayerInput (Behavior: Send Messages) con el valor de "Move"
    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // Llamado automaticamente por PlayerInput (Behavior: Send Messages) al presionar "Jump"
    private void OnJump(InputValue value)
    {
        if (value.isPressed && controller.isGrounded && CurrentJumpForce > 0f)
        {
            verticalVelocity = CurrentJumpForce;
            SonarSFX(clipSalto); //sonido del salto
        }
    }

    private void HandleGravityAndJump()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            // Lo mantiene pegado al piso; sin esto, isGrounded puede tildarse.
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleMovement()
    {
        Vector3 camForward = Vector3.forward;
        Vector3 camRight = Vector3.right;

        if (Camera.main != null)
        {
            camForward = Camera.main.transform.forward;
            camRight = Camera.main.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
        }

        Vector3 move = camForward * moveInput.y + camRight * moveInput.x;

        if (move.sqrMagnitude > 0.001f)
        {
            move.Normalize();
            transform.forward = Vector3.Slerp(transform.forward, move, Time.deltaTime * 10f);
        }

        move *= moveSpeed;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
        //Sonidos al caminar
        if (controller.isGrounded && moveInput.sqrMagnitude > 0.1f)
        {
            bool estaTransformado = GameManagerHistoria.Instance != null && GameManagerHistoria.Instance.diaActual >= 16; //si es el dia 16 suena distinto
            float intervaloDinamico = intervaloBasePasos;
            // Calcula la velocidad del jugador para quedar acorde con el audio
            if (!estaTransformado)
            {
                float velocidadRef = 5f;
                float factorVelocidad = Mathf.Max(0.2f, GameManagerHistoria.Instance != null ? GameManagerHistoria.Instance.velocidadActual / velocidadRef : 1f);
                intervaloDinamico = intervaloBasePasos / factorVelocidad;
            }

            timerPasos -= Time.deltaTime;
            if (timerPasos <= 0f)
            {
                if (estaTransformado && clipsPasosTransformados != null && clipsPasosTransformados.Length > 0)
                {
                    SonarSFX(clipsPasosTransformados); // Suenan los pasos pesados/transformados
                }
                else
                {
                    SonarSFX(clipsPasos); // Suenan los pasos normales
                }
                timerPasos = intervaloDinamico; // Se adapta a lo lento que camina
            }
        }
        else
        {
            timerPasos = 0f;
        }
    }
    // PARTE DE AUDIOS
    private void SonarSFX(AudioClip clip) 
    {
        if (fuenteSFX == null || clip == null) return;
        fuenteSFX.pitch = Random.Range(0.96f, 1.04f); // Variacion de tono
        fuenteSFX.PlayOneShot(clip, Random.Range(0.9f, 1f)); // Variacion de volumen
    }

    private void SonarSFX(AudioClip[] clips)
    {
        if (fuenteSFX == null || clips == null || clips.Length == 0) return;
        AudioClip clipElegido = clips[Random.Range(0, clips.Length)];
        SonarSFX(clipElegido);
    }
}