using UnityEngine;

public class MuroDash : MonoBehaviour
{
    [Header("Configuracion del muro")]
    [Tooltip("boxcolliderdelmuroACA")]
    [SerializeField] private BoxCollider colliderSolido;

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            if (PlayerMovement.Instance != null)
            {

                if (PlayerMovement.Instance.estaHaciendoDash)
                {

                    if (colliderSolido != null)
                        colliderSolido.enabled = false;

                    Invoke("ReactivarMuro", 0.4f);
                }
            }
        }
    }

    private void ReactivarMuro()
    {
        if (colliderSolido != null)
        {
            colliderSolido.enabled = true;
        }
    }
}