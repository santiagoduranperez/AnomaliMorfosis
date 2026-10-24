using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Camara del jugador con Cinemachine — Parcial 2, Santiago.
// Reemplaza a CamaraFollow (que queda desactivado en la Main Camera, por si hay que volver atras).
// La CinemachineCamera "CM_Jugador" usa:
//  - Orbital Follow: orbita alrededor del jugador a 5 m, igual que la camara anterior.
//  - Rotation Composer: siempre mira a la cabeza del jugador.
//  - Deoccluder: si una pared tapa al jugador, la camara se acerca (no atraviesa paredes).
//  - Impulse Listener: recibe los sacudones de camara.
//  - Basic Multi Channel Perlin: temblor "camara en mano" que aparece en el subsuelo (dia 16).
// Este script mueve la orbita con el mouse (nuevo Input System), sacude la camara cuando el
// jugador pierde vida y activa de a poco el temblor al llegar al subsuelo.
public class CamaraCinemachine : MonoBehaviour
{
    [Header("Referencias (las asigna el menu AnomaliMorfosis > Camara Cinemachine)")]
    [SerializeField] private CinemachineOrbitalFollow orbita;
    [SerializeField] private CinemachineImpulseSource sacudon;
    [SerializeField] private CinemachineBasicMultiChannelPerlin temblor;

    [Header("Mouse")]
    [SerializeField] private float sensibilidadMouse = 2f;

    [Header("Sacudon al recibir dano")]
    [SerializeField] private float fuerzaSacudon = 0.6f;

    [Header("Temblor en el subsuelo")]
    [SerializeField] private int diaSubsuelo = 16;
    [SerializeField] private float temblorSubsuelo = 0.8f;
    [SerializeField] private float velocidadTemblor = 0.3f;

    private int vidaAnterior = -1;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (temblor != null) temblor.AmplitudeGain = 0f;
    }

    private void Update()
    {
        MoverConMouse();

        var gm = GameManagerHistoria.Instance;
        if (gm == null) return;

        // Sacudon cuando la vida baja (golpe del paciente peligroso o de un jefe)
        if (vidaAnterior >= 0 && gm.vidaActual < vidaAnterior && sacudon != null)
            sacudon.GenerateImpulseWithForce(fuerzaSacudon);
        vidaAnterior = gm.vidaActual;

        // Temblor de camara en mano que crece de a poco en el subsuelo
        if (temblor != null)
        {
            float objetivo = gm.diaActual >= diaSubsuelo ? temblorSubsuelo : 0f;
            temblor.AmplitudeGain = Mathf.MoveTowards(temblor.AmplitudeGain, objetivo, velocidadTemblor * Time.deltaTime);
        }
    }

    private void MoverConMouse()
    {
        if (orbita == null || Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * sensibilidadMouse * 0.1f;
        orbita.HorizontalAxis.Value = Mathf.Repeat(orbita.HorizontalAxis.Value + delta.x + 180f, 360f) - 180f;
        float v = orbita.VerticalAxis.Value - delta.y;
        orbita.VerticalAxis.Value = Mathf.Clamp(v, orbita.VerticalAxis.Range.x, orbita.VerticalAxis.Range.y);
    }
}
