//3D Movement Test *7/28/25*
using UnityEngine;

public class IgnoreColliision : MonoBehaviour
{
    [SerializeField]
    Collider thisCollider;

    [SerializeField]
    Collider[] colliderToIgnore;
    void Start()
    {
        foreach (Collider otherCollider in colliderToIgnore)
        {
            Physics.IgnoreCollision(thisCollider, otherCollider, true);
        }
    }
}