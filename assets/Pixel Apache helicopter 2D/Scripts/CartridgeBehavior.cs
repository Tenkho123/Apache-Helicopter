using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CartridgeBehavior : MonoBehaviour
{
    [Tooltip ("Dump cartridge fast or not")]
    [SerializeField] float dumpSpeed = 40f;
    [Tooltip ("Incase your cartridge wont hit any collider and fly straight to the void")]
    [SerializeField] float destroyCartridgeTime = 3f;
    [Tooltip ("Only trigger destroy time when hit a collider not destroy instantly (Not recommend since it can cause some performance)")]
    [SerializeField] bool onlyDestroyWhenOnGround;

    private Coroutine _returnToPoolTimerCoroutine;
    private Rigidbody2D rigidbody2D;
    private BoxCollider2D boxCollider2D;
    
    private void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        boxCollider2D = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        if (!onlyDestroyWhenOnGround) _returnToPoolTimerCoroutine = StartCoroutine(ReturnToPoolAfterTime());
        rigidbody2D.AddRelativeForce(new Vector2 (dumpSpeed, 0));
        boxCollider2D.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {   
        // You can apply filter here, like if it collide with a human which contain a collider, it will skip it
        if (collider.gameObject.CompareTag("HelicopterPart")) return;

        boxCollider2D.isTrigger = false;

        if (onlyDestroyWhenOnGround) _returnToPoolTimerCoroutine = StartCoroutine(ReturnToPoolAfterTime());
        else ObjectPoolManager.ReturnObjectPool(gameObject);
    }

    private IEnumerator ReturnToPoolAfterTime()
    {   
        float elapsedTime = 0f;
        while (elapsedTime < destroyCartridgeTime) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        ObjectPoolManager.ReturnObjectPool(gameObject);
    }
}
