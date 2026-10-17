using UnityEngine;
using UnityEngine.Audio;

// ZonaMusica.cs — Bloque B (Música adaptativa) · AnomaliMorfosis
// Va en un objeto con Collider marcado "Is Trigger" (ej.: zona del enemigo).
// Al ENTRAR el jugador pide el snapshot "Peligro"; al SALIR vuelve a "Normal".
public class ZonaMusica : MonoBehaviour
{
    [SerializeField] private AudioMixerSnapshot normal;   // arrastrar Normal
    [SerializeField] private AudioMixerSnapshot peligro;  // arrastrar Peligro
    [SerializeField, Min(0f)] private float segundosAlEntrar = 0.5f; // entra rápido: susto
    [SerializeField, Min(0f)] private float segundosAlSalir = 3f;    // sale lento: alivio

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || peligro == null) return;
        peligro.TransitionTo(segundosAlEntrar);
        Debug.Log("ZonaMusica: entra -> Peligro");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || normal == null) return;
        normal.TransitionTo(segundosAlSalir);
        Debug.Log("ZonaMusica: sale -> Normal");
    }
}
