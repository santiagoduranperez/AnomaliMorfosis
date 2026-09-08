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
// currentDay es un valor hardcodeado por ahora (a proposito, para no bloquearse
// esperando el sistema de dias de Christian). Cuando el este listo, reemplazar
// esa variable por la referencia real al DayManager / GameState.

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Salto degradable (mecanica central del High Concept)")]
    [SerializeField] private float baseJumpForce = 8f;
    [SerializeField] private float jumpDecayPerDay = 1.5f;
    [SerializeField] private int currentDay = 0; // TODO: reemplazar por el DayManager cuando exista

    [Header("Fisica manual (CharacterController no trae fisica propia)")]
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private Vector2 moveInput;
    private float verticalVelocity;

    private float CurrentJumpForce => Mathf.Max(0f, baseJumpForce - currentDay * jumpDecayPerDay);

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleGravityAndJump();
        HandleMovement();
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
