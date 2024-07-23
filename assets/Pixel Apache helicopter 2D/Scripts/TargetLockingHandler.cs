using UnityEngine;

public class TargetLockingHandler : MonoBehaviour
{    
    [SerializeField] GameObject lockingIcon;
    [SerializeField] GameObject lockedIcon;
    [Tooltip ("Require time for missle to lock a specific target, if you move to another then it will be counting again")]
    [SerializeField] float timeToLockTarget = 1.5f;
    private float _lockingTime;
    [Tooltip ("Rate for locking icon appear and dissapear")]
    [SerializeField] float lockingRate = 0.2f;
    private float _lockingRate;

    private GameObject enemyInQueue;
    [HideInInspector] public GameObject currentEnemy; // when fire missile, it will track this var to see if theres enemy or not

    private void Update()
    {   
        if (!lockedIcon.activeSelf && enemyInQueue != null) {
            if (_lockingTime >= timeToLockTarget) {
                currentEnemy = enemyInQueue;
                ResetValue();
                lockedIcon.SetActive(true);
            }
            else {
                if (_lockingRate >= lockingRate) {
                    if (!lockingIcon.activeSelf) lockingIcon.SetActive(true);
                    else lockingIcon.SetActive(false);
                    _lockingRate = 0f;
                }
                else _lockingRate += Time.deltaTime;

                _lockingTime += Time.deltaTime;
            }
        }
    }

    private void ResetValue()
    {   
        enemyInQueue = null;
        lockedIcon.SetActive(false);
        lockingIcon.SetActive(false);
        _lockingTime = 0f;
        _lockingRate = 0f;
    }

    private void MoveIconToEnemy(Vector3 enemyPosition)
    {   
        Vector3 position = enemyPosition;
        position.z = lockingIcon.transform.position.z;
        lockingIcon.transform.position = position;
        lockedIcon.transform.position = position;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {   
        // If your enemy has many colliders attach to different objects in it, it will be hard for the locking missile to aim at the center
        // of your enemy. So I created an object with collider place at the center of enemy with special tag on it, and filter it here.
        if (collider.gameObject.tag != "LockablePoint") return;

        if (currentEnemy == null) {
            ResetValue();
            enemyInQueue = collider.gameObject;
            MoveIconToEnemy(enemyInQueue.transform.position);
        }
    }

    private void OnTriggerStay2D(Collider2D collider)
    {   
        if (collider.gameObject.tag != "LockablePoint") return;
        
        Transform lastTarget = null;
        if (currentEnemy != null) lastTarget = currentEnemy.transform;
        else if (enemyInQueue != null) lastTarget = enemyInQueue.transform;
        if (lastTarget == null || collider.transform == lastTarget) return;

        // Check if new enemy appear in the locking cursor nearer than the current enemy.
        if (Vector2.Distance(collider.transform.position, transform.position) < Vector2.Distance(lastTarget.position, transform.position)) {
            ResetValue();
            currentEnemy = null;
            enemyInQueue = collider.gameObject;
            MoveIconToEnemy(enemyInQueue.transform.position);
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject.tag != "LockablePoint") return;
        // If this is the last collider in homing zone, then reset all
        if (enemyInQueue == collider.gameObject || currentEnemy == collider.gameObject) {
            currentEnemy = null;
            ResetValue();
        }
    }
}
