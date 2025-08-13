//3D Movement Test *7/28/25*
using JetBrains.Annotations;
using Unity.Cinemachine;
using UnityEngine;

public class NetworkPlayer : MonoBehaviour
{
    [SerializeField]
    Rigidbody rigidbody3D;

    [SerializeField]
    ConfigurableJoint mainJoint;

    [SerializeField]
    Animator animator;

    HandGrabHandler[] handGrabHandlers;

    //Syncing of physics objects
    SyncPhysicsObject[] syncPhysicsObjects;

    //Cinemachine
    CinemachineCamera cinemachineCamera;
    CinemachineBrain cinemachineBrain;

    //Input
    Vector2 moveInputVector = Vector2.zero;

    // Raycasts
    RaycastHit[] raycastHits = new RaycastHit[10];

    //Controller settings
    public float Speed = 5;

    //States
    bool isGrounded = false;
    public bool isGrabbingActive = false;
    public bool isWall = false;
    bool isGrabPressed;
    public bool isJumpButtonPressed = false;

    public bool isActiveRagdoll = true;
    public bool IsGrabbingActive => isGrabbingActive;
    //public bool isRevivePressed;

    //Store original values
    float startSlerpPositionSpring = 0.0f;

    void Awake()
    {
        syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObject>();
        handGrabHandlers = GetComponentsInChildren<HandGrabHandler>();
    }

    void Start()
    {
        startSlerpPositionSpring = mainJoint.slerpDrive.positionSpring;
        //MakeActiveRagdoll();
    }

    // Update is called once per frame
    void Update()
    {
        //Move input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
            isJumpButtonPressed = true;

        isGrabPressed = Input.GetKey(KeyCode.G);
    }

    private void FixedUpdate()
    {
        //Assume the player is not grounded
        isGrounded = false;

        //Check to see if player is grounded (using sphere cast over ray cast to be thicker than just a ray)
        int numberOfHits = Physics.SphereCastNonAlloc(rigidbody3D.position, 0.1f, transform.up * -1, raycastHits, 0.5f);

        //Check for valid results
        for (int i = 0; i < numberOfHits; i++)
        {
            //Ignore self hits
            if (raycastHits[i].transform.root == transform)
                continue;

            isGrounded = true;

            break;
        }

        //Apply extra gravity to the character to make the game less floaty
        if (!isGrounded)
            rigidbody3D.AddForce(Vector3.down * 10);

        Vector3 localVelocifyVsForward = transform.forward * Vector3.Dot(transform.forward, rigidbody3D.linearVelocity);

        //Checks to see if wer are moving too fast
        float localForwardVelocity = localVelocifyVsForward.magnitude;
        
        float inputMagnitued = moveInputVector.magnitude;
        isGrabbingActive = isGrabPressed;

        if (isActiveRagdoll)
        {
            if (inputMagnitued != 0)
            {
                //Forces the character to look in the way we are walking towards
                Quaternion desiredDirection = Quaternion.LookRotation(new Vector3(moveInputVector.x, 0, moveInputVector.y * -1), transform.up);

                //Roate target towards direction
                mainJoint.targetRotation = Quaternion.RotateTowards(mainJoint.targetRotation, desiredDirection, Time.fixedDeltaTime * 300);

                if (localForwardVelocity < Speed)
                {
                    rigidbody3D.AddForce(transform.forward * inputMagnitued * 30);
                }
            }

            //Player animation
            animator.SetFloat("movementSpeed", localForwardVelocity * 0.4f);


            //Player jump
            if (isGrounded && isJumpButtonPressed)
            {
                rigidbody3D.AddForce(Vector3.up * 20, ForceMode.Impulse);

                isJumpButtonPressed = false;
            }
            
            //Player jump while grabbing onto a wall
            else if (isWall && isJumpButtonPressed && isGrabbingActive)
            {
                rigidbody3D.AddForce(Vector3.up * 20, ForceMode.Impulse);

                isJumpButtonPressed = false;              
            }

            Vector3 velocity = rigidbody3D.angularVelocity;
            rigidbody3D.angularVelocity = new Vector3(rigidbody3D.angularVelocity.x, Mathf.Clamp(rigidbody3D.angularVelocity.y, -Mathf.Infinity, 20f), rigidbody3D.angularVelocity.z);
        }

        //Update the joints rotation based on the animations
        for (int i = 0; i < syncPhysicsObjects.Length; i++)
        {
            if (isActiveRagdoll)
                syncPhysicsObjects[i].UpdateJointFromAnimation();          
        }

        foreach (HandGrabHandler handGrabHandler in handGrabHandlers)
        {
            handGrabHandler.UpdateState();
        }
    }
    
    
    /*  *** Code to add once we have fire damage; fire should set player into a small ragdoll animation before reviving the player ****
    
    void MakeRagdoll()
    {
        //update main joint
        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = 0;
        mainJoint.slerpDrive = jointDrive;

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
        {
            syncPhysicsObjects[i].MakeRagdoll();
        }

        isActiveRagdoll = false;
        isGrabbingActive = false;
    }

    void MakeActiveRagdoll()
    {
        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = startSlerpPositionSpring;
        mainJoint.slerpDrive = jointDrive;
        for (int i = 0; i < syncPhysicsObjects.Length; i++)
        {
            syncPhysicsObjects[i].MakeActiveRagdoll();
        }

        isActiveRagdoll = true;
        isGrabbingActive = false;
    }
    */

    private void OnCollisionEnter(Collision collision)
    {
        /* Used once we add fire damage
        if(collision.transform.CompareTag("CauseDamage"))
        {
            MakeRagdoll();
        }
        */
        
        //Checks to see if the player is colliding with object with the 'Wall' tag
        //if(collision.transform.CompareTag("Wall"))
        //{
        //    isWall = true;
        //}
    }
    /*
    private void OnCollisionExit(Collision collision)
    {
        if (!collision.transform.CompareTag("Wall"))
        {
            isWall = false;
        }
    }
    */

    //Camera tracking the player; still need to make it so the player can control the camera / it only tracks on the player's back
    public void Spawned()
    {
        cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        cinemachineBrain = FindFirstObjectByType<CinemachineBrain>();

        cinemachineCamera.Follow = transform;
        cinemachineCamera.LookAt = transform;
    }
}
