using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    private enum EnemyState
    {
        Patrol,
        Flee
    }
    private EnemyState currentState = EnemyState.Patrol;

    [Header("Fall Settings")]
    public float pushForce = 1f;
    private Rigidbody rb;
    private bool hasFallen = false;

    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float waypointThreshold = 0.5f;

    private int currentWaypointIndex = 0;

    [Header("Vision Settings")]
    public Transform eyeTransform;
    public float visionRange = 10f;
    public float visionAngle = 60f;
    public LayerMask visionBlockers;
    private Transform playerTransform;

    [Header("Flee Settings")]
    public Transform fleeWaypoint;
    public float fleeSpeed = 30f;
    private int consecutiveFramesSeen = 0;
    public int framesToTriggerFlee = 5;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError("Enemy cannot find GameObject with 'Player' tag.", this);
        }

        if (eyeTransform != null)
        {
            Debug.LogWarning("Eye Transform not set, defaulting to enemy's main transform.", this);
            eyeTransform = this.transform;
        }

        if (fleeWaypoint != null)
        {
            Debug.LogError("Flee Waypoint not set, enemy has nowhere to flee.", this);
        }
    }

    void Update()
    {
        if (hasFallen)
        {
            return;
        }

        CheckForPlayer();

        switch (currentState)
        {
            case EnemyState.Patrol:
                if (waypoints.Length > 0)
                {
                    HandlePatrol();
                }
                break;

            case EnemyState.Flee:
                HandleFlee();
                break;
        }
    }

    private void CheckForPlayer()
    {
        if (playerTransform == null)
        {
            return;
        }

        bool isPlayerVisible = false;

        Vector3 directionToPlayer = playerTransform.position - eyeTransform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer <= visionRange)
        {
            float angle = Vector3.Angle(eyeTransform.forward, directionToPlayer);
            if (angle <= visionAngle * 0.5f)
            {
                if (!Physics.Linecast(eyeTransform.position, playerTransform.position, visionBlockers))
                {
                    isPlayerVisible = true;
                }
            }
        }

        if (isPlayerVisible)
        {
            Debug.Log("Player Spotted!");
            consecutiveFramesSeen++;

            if (consecutiveFramesSeen >= framesToTriggerFlee && currentState != EnemyState.Flee)
            {
                Debug.Log("Fleeing!");
                currentState = EnemyState.Flee;
            }
        }
        else
        {
            consecutiveFramesSeen = 0;
        }
    }

    private void HandlePatrol()
    {
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 targetPosition = new Vector3(targetWaypoint.position.x,
                                             transform.position.y,
                                             targetWaypoint.position.z);
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;
        rb.MovePosition(newPosition);

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector2 targetPos2D = new Vector2(targetWaypoint.position.x, targetWaypoint.position.z);
        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.z);
        if (Vector2.SqrMagnitude(targetPos2D - enemyPos2D) < waypointThreshold * waypointThreshold)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    private void HandleFlee()
    {
        if (fleeWaypoint == null)
        {
            return;
        }

        Vector3 targetPosition = new Vector3(fleeWaypoint.position.x,
                                             transform.position.y,
                                             fleeWaypoint.position.z);

        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        rb.MovePosition(transform.position + moveDirection * fleeSpeed * Time.deltaTime);

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector2 targetPos2D = new Vector2(fleeWaypoint.position.x, fleeWaypoint.position.z);
        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.z);

        if (Vector2.SqrMagnitude(targetPos2D - enemyPos2D) < waypointThreshold * waypointThreshold)
        {
            Debug.Log("Reached flee waypoint, resetting scene.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasFallen)
        {
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            hasFallen = true;
            rb.isKinematic = false;
            Vector3 pushDirection = transform.position - collision.transform.position;
            pushDirection = pushDirection.normalized;
            rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
        }
    }
}
