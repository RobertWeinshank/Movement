using System.Collections.Generic;
using UnityEngine;

public class HandGrabHandler : MonoBehaviour
{
    [SerializeField] Animator animator;

    //A fixed joint that is created on the fly
    FixedJoint fixedJoint;

    //Our own rigidbody
    Rigidbody rigidbody3D;

    //References 
    NetworkPlayer networkPlayer;

    //public bool isWall = true;

    //private HashSet<Collider> wallContacts = new HashSet<Collider>();
    int wallContactCount = 0;

    private void Awake()
    {
        //Get references
        networkPlayer = transform.root.GetComponent<NetworkPlayer>();
        rigidbody3D = GetComponent<Rigidbody>();

        //Change tension to see how the character can grab onto things
        rigidbody3D.solverIterations = 255;
    }

    public void UpdateState()
    {
        //check if grabbing is active
        if (networkPlayer.IsGrabbingActive)
        {
            animator.SetBool("isGrabbing", true);
        }
        else
        {
            //check to see if there is a joint to destroy
            if(fixedJoint != null)
            {
                //give connected object a bit of force to move it when released
                if (fixedJoint.connectedBody != null)
                {
                    float forceAmountMultiplayer = 0.1f;

                    //toss object away before destroying joint
                    fixedJoint.connectedBody.AddForce((networkPlayer.transform.forward + Vector3.up * 0.25f) * forceAmountMultiplayer, ForceMode.Impulse);
                }
                Destroy(fixedJoint);
            }
            //Change animation state
            animator.SetBool("isCarrying", false);
            animator.SetBool("isGrabbing", false);
        }
    }

    bool TryCarryObject(Collision collision)
    {
        //Check if we are already carrying something
        if (fixedJoint != null)
            return false;

        //Check if we are in active ragdoll
        if (!networkPlayer.isActiveRagdoll)
            return false;

        //Avoid trying to grab yourself
        if (collision.transform.root == networkPlayer.transform)
            return false;

        //Checks to see if player is grabbing another rigidbody
        if (!collision.collider.TryGetComponent(out Rigidbody otherObjectRigidbody))
            return false;

        //Add fixed joint
        fixedJoint = transform.gameObject.AddComponent<FixedJoint>();

        //Connect the joint to the other objects rigidbody
        fixedJoint.connectedBody = otherObjectRigidbody;

        //Take care of anchor point
        fixedJoint.autoConfigureConnectedAnchor = false;

        //Transform collision from world to local space
        fixedJoint.connectedAnchor = collision.transform.InverseTransformPoint(collision.GetContact(0).point);


        //set animator to carry
        animator.SetBool("isCarrying", true);

        return true;
    }

    void OnCollisionEnter(Collision collision)
    {
        //Attemt to carry the other object
        TryCarryObject(collision);

        if (collision.transform.CompareTag("Wall"))
        {
            wallContactCount++;
            networkPlayer.isWall = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.transform.CompareTag("Wall"))
        {
            wallContactCount = Mathf.Max(0, wallContactCount - 1);
            if (wallContactCount == 0)
            {
                networkPlayer.isWall = false;
            }
        }
    }
}
