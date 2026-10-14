using UnityEngine;

// Script para los proyectiles disparados por el Jefe

public class ProyectilEnemigo : MonoBehaviour
{
    public float velocidad = 10f;
    public int dano = 1;
    public float tiempoVida = 5f;

    private void Start()
    {
        Destroy(gameObject, tiempoVida);
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);
    }

    private bool EsJugador(Collider col)
    {
        if (col.CompareTag("Player")) return true;
        if (col.transform.root.CompareTag("Player")) return true;
        if (col.attachedRigidbody != null && col.attachedRigidbody.CompareTag("Player")) return true;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignorar colisiones con el propio enemigo o con otros proyectiles
        if (other.CompareTag("Enemy") || other.GetComponent<ProyectilEnemigo>() != null) return;

        if (EsJugador(other))
        {
            if (GameManagerHistoria.Instance != null)
            {
                GameManagerHistoria.Instance.RecibirDano(dano);
            }
            Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            // Se destruye al chocar contra paredes u obstaculos
            Destroy(gameObject);
        }
    }
}