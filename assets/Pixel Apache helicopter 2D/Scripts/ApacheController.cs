using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(RotorsHandler))]
public class ApacheController : MonoBehaviour
{   
    // Movement, turret rotating and firing functions lie here.
    // They are very simple, so if you want the controlling more realistic, feel free to edit them.

    // Using new input system is highly recommend, but I will just use old input system instead.

    // If you not sure about some function lines, just comment them and run the game and see what problem will happen.
    
    // The reason why I dont split them into multiple components such as movement handler script, turret handler script, ect
    // is in case you already have script with duplicate name, so I must gather them all in one script only, but still independent enough
    // for you to divide them into multiple components with your own script name if you want. 

    [Header ("Movement values")]
    [Tooltip ("Desired move speed")]
    [SerializeField] public float moveSpeed = 5f;
    [Tooltip ("Reach desired move speed fast or not when begin move the helikopter")]
    [SerializeField] public float acceleration = 2f;
    [Tooltip ("Reach zero move speed and begin hovering fast or not when release movement keys")]
    [SerializeField] public float deceleration = 2f;
    [Tooltip ("When you move left or right, the helicopter will lean on that way. This is its degree (recommend at default)")]
    [SerializeField] public float leanDegree = 7f;
    [Tooltip ("Determine lean fast or slow (recommend at default)")]
    [SerializeField] public float leanRate = 20f;

    [Header ("Main rotor (recommend default setting)")]
    [SerializeField] Transform mainRotor;
    [Tooltip ("The same as leanDegree above but this is for main rotor")]
    [SerializeField] public float mainRotorLeanDegree = 7f;
    [Tooltip ("The same as leanRate above but this is for main rotor")]
    [SerializeField] public float mainRotorleanRate = 40f;

    [Header ("Turret values")]
    [SerializeField] Transform turret;
    [SerializeField] Transform shotPoint;
    [SerializeField] Transform dumpCatridgePoint;
    [SerializeField] float turnSpeed = 400f;

    [Header ("Turret bullet")]
    [SerializeField] GameObject bullet;
    [SerializeField] GameObject bulletCatridge;
    [Tooltip ("Delay time between bullet shot (Not recommend set too low, else you gonna have some fried potatoes)")]
    [SerializeField] float bulletRateTime = 0.1f;
    float _bulletTime = 0f;

    [Header ("ASM (Air to surface) Carrier")]
    // This is a bit special since there are 4 holding point beneat an Apache helicopter's wings in real life, but due to my skill limitation
    // I only can try to simulate 2 missle carriers (4 missiles on each) on both left and right side of the Apache in "2D world".
    // Further upgrading will require more adjustment at the missles part in this script
    [SerializeField] Transform leftMissleCarrier;
    [SerializeField] Transform rightMissleCarrier;
    List<GameObject> missileList = new();

    [Header ("Swap weapons")]
    // The reason why I use "asm" instead of homing missile is because at the beginning I was about to add an "aam-stinger" (air to air missile)
    // too. But if I do that, I will have to build a ground and air detect system for the homing missiles. Which is out of this asset's purpose.
    [SerializeField] List<string> weaponsList = new() {"turret", "asm"};
    [Tooltip ("Current weapon when game first run is turret (work in run-time)")]
    [SerializeField] string currentWeapon;

    [Header ("Other")]
    [Tooltip ("Custom cursor for different weapons mode")]
    [SerializeField] Transform weaponCursor;
    [Tooltip ("When change moving direction, if this true then sprite wont flip in the opposite side, the heli will move backward instead")]
    [SerializeField] bool isAllowFlipHorizontal;
    [Tooltip ("Which direction is the heli facing")]
    [SerializeField] public bool isRightDirection = true;
    [Tooltip ("Gravity apply when engine stop while airborne")]
    [SerializeField] float gravity = 10f;
    [Tooltip ("If helicopter rotate too much (like got flip 180 vertically) this will be apply for quicker rotate back to normal")]
    [SerializeField] float quickBalanceLeanRate = 100f;

    private Rigidbody2D rigidbody2D;
    private RotorsHandler rotorsHandler;
    private WeaponCursor cursor;

    private Vector2 moveDirection;
    private Vector2 cursorPosition;
    private Vector2 rotatedVectorToTarget;

    private Vector2 mouseDirectionVector;
    private Quaternion targetRotation;

    private bool isEngineStart;
    private bool isAirborne;

    private float currentLeanRate; // For swap between leanRate and quickBalanceLeanRate
    float zAngle;

    private void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        rotorsHandler = GetComponent<RotorsHandler>();
        cursor = weaponCursor.GetComponent<WeaponCursor>();

        currentLeanRate = leanRate;
        if (transform.localScale.x < 0) isRightDirection = false;
        if (weaponsList.Count == 0) Debug.Log("No weapon assigned!");
        else currentWeapon = weaponsList[0];

        GetMissleData();
    }

    private void GetMissleData()
    {
        if (leftMissleCarrier != null) {
            foreach (Transform missleRail in leftMissleCarrier) if (missleRail.name == "MissleRail") missileList.Add(missleRail.GetChild(0).gameObject);
        }

        if (rightMissleCarrier != null) {
            foreach (Transform missleRail in rightMissleCarrier) if (missleRail.name == "MissleRail") missileList.Add(missleRail.GetChild(0).gameObject);
        }
    }

    private void Update()
    {   
        EngineHandler();
        MovementHandler();
        SwapWeaponHandler();
        if (currentWeapon == "turret") TurretHandler();
        if (currentWeapon == "asm") ASMHandler();

        AllowFlipHorizontalHandler();
    }

    private void EngineHandler()
    {   
        // Start engine
        if (Input.GetKeyDown(KeyCode.E) && !isEngineStart) {
            isEngineStart = true;
            rotorsHandler.speed = rotorsHandler.rotationSpeed;
        }

        // Disable engine
        else if (Input.GetKeyDown(KeyCode.E) && isEngineStart) {
            isEngineStart = false;
            rotorsHandler.speed = 0f;
        }

        // Set gravity to zero once main rotor reach desired rotation speed and opposite
        if (rotorsHandler.currentSpeed == rotorsHandler.rotationSpeed) {
            rigidbody2D.gravityScale = 0f;
            return;
        }
        else if (rotorsHandler.currentSpeed == 0f) {
            rigidbody2D.gravityScale = gravity;
            return;
        }

        // Instead of set the gravityScale instantly, I make it to increase or decrease with the rate of the main rotor's speed (when press E)
        rigidbody2D.gravityScale = Mathf.Abs(rotorsHandler.currentSpeed - rotorsHandler.rotationSpeed) / rotorsHandler.rotationSpeed * gravity;
    }

    private void MovementHandler()
    {   
        if (!isAirborne && mainRotor.rotation != Quaternion.Euler(0, 0, 0)) {
            mainRotor.rotation = Quaternion.RotateTowards(mainRotor.rotation, Quaternion.Euler(0, 0, 0), mainRotorleanRate * Time.deltaTime);
        }

        if (!isEngineStart) return;

        moveDirection = new Vector2 (Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // Decide the helicopter rotation slower or faster if the helicopter rotate too much
        // since eulerAngles do not return negative value, so I must minus 360f (if it over 180)

        if (transform.localRotation.eulerAngles.z > 180f) zAngle = transform.localRotation.eulerAngles.z - 360f;
        else zAngle = transform.localRotation.eulerAngles.z;

        if ((transform.localRotation.eulerAngles.z > 180f && zAngle < -leanDegree) || 
            (transform.localRotation.eulerAngles.z <= 180f && zAngle > leanDegree)) {
                currentLeanRate = quickBalanceLeanRate; 
            }
        else currentLeanRate = leanRate;

        if (!Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A) && isAirborne) {
            // Help main rotor still keep the local rotation if the helicopter rotation got flip vertically
            // you can try to rotate the whole helicopter in the inspector in the z axis with below lines being comment to see what gonna happen
            if (mainRotor.rotation != Quaternion.Euler(0, 0, 0) && zAngle >= -leanDegree && zAngle <= leanDegree) {
                mainRotor.rotation = Quaternion.RotateTowards(mainRotor.rotation, Quaternion.Euler(0, 0, 0), mainRotorleanRate * Time.deltaTime);
            }

            // Help the helicopter flip back if it got flip vertically (it's helicopter self balance)
            if (transform.rotation != Quaternion.Euler(0, 0, 0)) {
                // normal self balance if you release your A D movement keys
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, 0), currentLeanRate * Time.deltaTime);
            }
        }

        if (Input.GetKey(KeyCode.D)) {
            if (isAllowFlipHorizontal && !isRightDirection) {
                isRightDirection = true;
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                shotPoint.localRotation = Quaternion.Euler(0, 0, 0);
                dumpCatridgePoint.localRotation = Quaternion.Euler(0, 0, 180);
            }
            if (isRightDirection) mainRotor.localRotation = Quaternion.RotateTowards(mainRotor.localRotation, Quaternion.Euler(0, 0, -mainRotorLeanDegree), mainRotorleanRate * Time.deltaTime);
            else mainRotor.localRotation = Quaternion.RotateTowards(mainRotor.localRotation, Quaternion.Euler(0, 0, mainRotorLeanDegree), mainRotorleanRate * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, -leanDegree), currentLeanRate * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.A)) {
            if (isAllowFlipHorizontal && isRightDirection) {
                isRightDirection = false;
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                shotPoint.localRotation = Quaternion.Euler(0, 0, 180);
                dumpCatridgePoint.localRotation = Quaternion.Euler(0, 0, 0);
            }
            if (isRightDirection) mainRotor.localRotation = Quaternion.RotateTowards(mainRotor.localRotation, Quaternion.Euler(0, 0, mainRotorLeanDegree), mainRotorleanRate * Time.deltaTime);
            else mainRotor.localRotation = Quaternion.RotateTowards(mainRotor.localRotation, Quaternion.Euler(0, 0, -mainRotorLeanDegree), mainRotorleanRate * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, leanDegree), currentLeanRate * Time.deltaTime);
        }

        moveDirection = moveDirection.normalized;  

        if (moveDirection != Vector2.zero) SpeedUp();
        else if (rigidbody2D.velocity != Vector2.zero && moveDirection == Vector2.zero) SlowDown();
    }

    // Mimic the AddForce function, not move at "moveSpeed" constantly, but speeding up.
    private void SpeedUp()
    {
        rigidbody2D.velocity = Vector2.Lerp(rigidbody2D.velocity, moveDirection * moveSpeed, Time.deltaTime * acceleration);
    }
    
    // When you release your movement key, helicopter will slow down instead of stop immediately.
    private void SlowDown()
    {   
        rigidbody2D.velocity = Vector2.Lerp(rigidbody2D.velocity, Vector2.zero, Time.deltaTime * deceleration);
    }

    private void TurretHandler()
    {   
        TurretRotating();
        TurretShooting();
    }

    private void TurretRotating()
    {
        cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        mouseDirectionVector = (Vector2)turret.position - cursorPosition;
        if (isRightDirection) rotatedVectorToTarget = Quaternion.Euler(0, 0, 270) * mouseDirectionVector;
        else rotatedVectorToTarget = Quaternion.Euler(0, 0, 90) * mouseDirectionVector;
        
        targetRotation = Quaternion.LookRotation(forward: Vector3.forward, upwards: rotatedVectorToTarget);
        turret.rotation = Quaternion.RotateTowards(turret.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void TurretShooting()
    {   
        if (Mouse.current.leftButton.isPressed && _bulletTime >= bulletRateTime) {
            if (bullet != null) ObjectPoolManager.SpawnObject(objectToSpawn: bullet, shotPoint.position, shotPoint.rotation, ObjectPoolManager.PoolType.Gameobject);
            if (bulletCatridge != null) ObjectPoolManager.SpawnObject(objectToSpawn: bulletCatridge, dumpCatridgePoint.position, dumpCatridgePoint.rotation, ObjectPoolManager.PoolType.Gameobject); 
            _bulletTime = 0f;
        }
        else if (_bulletTime < bulletRateTime) _bulletTime += Time.deltaTime;
    }

    private void ASMHandler()
    {
        if (Input.GetMouseButtonDown(0)) {
            foreach (GameObject missle in missileList) {
                HomingMissileBehavior homingMissileBehavior = missle.GetComponent<HomingMissileBehavior>();

                if (homingMissileBehavior.isReadyToIgnite) {
                    TargetLockingHandler targetLockingHandler = cursor.homingMissileCursor.GetComponent<TargetLockingHandler>();
                    if (targetLockingHandler.currentEnemy) homingMissileBehavior.target = targetLockingHandler.currentEnemy.transform;
                    homingMissileBehavior.OnIgnite();
                    break;
                }
            }
        }
    }

    private void SwapWeaponHandler()
    {
        if (Input.GetKeyDown(KeyCode.Q)) {
            int index = weaponsList.IndexOf(currentWeapon) + 1;
            if (index == weaponsList.Count) index = 0;
            currentWeapon = weaponsList[index];
            SwapCursor(currentWeapon);
        }
    }

    private void SwapCursor(string name)
    {   
        switch (name) {
            case "turret":
                cursor.CurrentCursor = cursor.turretCursor;
                break;
            case "asm":
                cursor.CurrentCursor = cursor.homingMissileCursor;
                break;
        }
    }

    private void AllowFlipHorizontalHandler()
    {
        if (Input.GetKeyDown(KeyCode.R)) {
            if (isAllowFlipHorizontal) isAllowFlipHorizontal = false;
            else isAllowFlipHorizontal = true;
        }
    }

    // Detect when the helicopter is on air or on ground
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!collider.gameObject.CompareTag("HelicopterPart")) isAirborne = false;
    }

    private void OnTriggerExit2D(Collider2D collider)
    {   
        if (!collider.gameObject.CompareTag("HelicopterPart")) isAirborne = true;
    }
}
