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
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Salto degradable (mecanica central del High Concept)")]
    [SerializeField] private float baseJumpForce = 8f;
    [SerializeField] private float jumpDecayPerDay = 1.5f;
    [SerializeField] private int currentDay = 0; // Fallback cuando no hay GameManagerHistoria en la escena (ej. escena de prueba)

    [Header("Fisica manual (CharacterController no trae fisica propia)")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Inspeccion a distancia (Raycast, Clase 5)")]
    [Tooltip("Distancia maxima del Raycast de inspeccion, en metros.")]
    [SerializeField] private float distanciaInspeccion = 6f;

    private CharacterController controller;
    private Vector2 moveInput;
    private float verticalVelocity;

    // Dia 1 = sin degradacion todavia (diaActual arranca en 1 en el GameManagerHistoria).
    // Si existe el GameManagerHistoria de Christian en la escena, se usa su diaActual
    // real; si no (ej. escena de prueba sin el GameManager), se usa currentDay del Inspector.
    private int DayForDecay => GameManagerHistoria.Instance != null
        ? GameManagerHistoria.Instance.diaActual - 1
        : currentDay;

    private float CurrentJumpForce => Mathf.Max(0f, baseJumpForce - DayForDecay * jumpDecayPerDay);

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleGravityAndJump();
        HandleMovement();
        HandleInspeccionRaycast();
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
        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        move.Normalize();
        move *= moveSpeed;

        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }
}
