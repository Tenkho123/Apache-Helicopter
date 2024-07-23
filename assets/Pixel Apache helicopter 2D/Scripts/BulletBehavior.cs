using System.Collections;
using UnityEngine;

public class BulletBehavior : MonoBehaviour
{   
    [Header ("Helikopter normal turret bullet")]
    [SerializeField] public float damage = 100f;
    [SerializeField] float moveSpeed = 30f;
    [Tooltip ("Incase your bullet wont hit any collider and fly straight to the void")]
    [SerializeField] float destroyBulletTime = 3f;

    private Coroutine _returnToPoolTimerCoroutine;
    private Rigidbody2D rigidbody2D;
    
    private void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _returnToPoolTimerCoroutine = StartCoroutine(ReturnToPoolAfterTime());
    }

    private void FixedUpdate()
    {
        rigidbody2D.velocity = transform.right * moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {   
        // In this case I use "HelicopterPart" tag to make the bullet not stop when you shoot at your helicopter 2d collider such as
        // wheel or body frame.
        // You can filter which object bullet can go through or hitable object by using "tags" or "layerMask".
        // I only use tag as an example to simplify the example. If tags fine for you, use it then.
        if (collider.gameObject.CompareTag("HelicopterPart")) return;

        // Set your own damage script here

        ObjectPoolManager.ReturnObjectPool(gameObject);
    }

    private IEnumerator ReturnToPoolAfterTime()
    {   
        float elapsedTime = 0f;
        while (elapsedTime < destroyBulletTime) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        ObjectPoolManager.ReturnObjectPool(gameObject);
    }
}
