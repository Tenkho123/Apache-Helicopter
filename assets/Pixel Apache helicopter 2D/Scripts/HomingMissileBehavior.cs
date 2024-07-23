using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HomingMissileBehavior : MonoBehaviour
{   
    [Tooltip("Missile's target, if there's none it will fly straight")]
    [SerializeField] public Transform target;
    [Tooltip("Access to ApacheController to get current helicopter direction")]
    // Since rotating using scale.x *= -1 won't flip its Vector3.right, so if I refuse to check the direction
    // when the helicopter turn left and you fire missile, it will fly backward instead of forward.
    [SerializeField] ApacheController apacheController;
    [SerializeField] GameObject explosionEffect;
    [SerializeField] GameObject trailEffect;
    [SerializeField] float moveSpeed = 10f;
    [Tooltip("When target move, the missile need to change its direction. This is the speed of rotation to the new direction")]
    [SerializeField] float rotateSpeed = 200f;
    [Tooltip("Time for a missile to respawn once it blow up (set to -1 if you dont want it to respawn anymore)")]
    [SerializeField] float respawnTime = 2f;
    [Tooltip("Time for a missile to activate self-destruction (set to -1 if  you want to disable this)")]
    // You can modify it to 
    [SerializeField] float destructionTime = 10f;

    [HideInInspector] public bool isIgnite;
    [HideInInspector] public bool isReadyToIgnite;

    private Coroutine _destructionTime;
    private Coroutine _respawnTime;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private Transform parent;

    private bool isRightDirection;

    // Start is called before the first frame update
    private void Start()
    {   
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        parent = transform.parent;

        isReadyToIgnite = true;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {   
        if (!isIgnite) return;
        transform.SetParent(null);
        
        Vector3 headDirection;
        if (isRightDirection) headDirection = transform.right;
        else headDirection = transform.right * -1;

        if (target != null) {
            Vector2 direction = (Vector2)target.position - rb.position;
            direction.Normalize();

            float rotateAmount;
            if (isRightDirection) rotateAmount = Vector3.Cross(direction, headDirection).z;
            else rotateAmount = Vector3.Cross(direction, headDirection).z;

            rb.angularVelocity = -rotateAmount * rotateSpeed;
        }

        rb.velocity = headDirection * moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {   
        if (!isIgnite || collider.gameObject.tag == "HelicopterPart") return;
        OnDestruction();
    }

    public void OnIgnite()
    {   
        isRightDirection = apacheController.isRightDirection;
        _destructionTime = StartCoroutine(DestructionTime());
        trailEffect.SetActive(true);

        isIgnite = true;
        isReadyToIgnite = false;
    }

    private void OnDestruction()
    {
        Instantiate(explosionEffect, transform.position, transform.rotation);
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;

        StopCoroutine(_destructionTime);
        _respawnTime = StartCoroutine(RespawnTime());

        trailEffect.SetActive(false);
        spriteRenderer.enabled = false;

        isIgnite = false;
        target = null;
    }

    private void OnRespawn()
    {
        transform.SetParent(parent);
        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;

        isReadyToIgnite = true;
        spriteRenderer.enabled = true;
    }

    private IEnumerator RespawnTime()
    {   
        float elapsedTime = 0f;
        while (elapsedTime < respawnTime) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        OnRespawn();
    }

    private IEnumerator DestructionTime()
    {   
        float elapsedTime = 0f;
        while (elapsedTime < destructionTime) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        OnDestruction();
    }
}
