using UnityEngine;

public class RotorsHandler : MonoBehaviour
{   
    [SerializeField] Transform rotaryWing;
    [SerializeField] Transform tailRotor;
    public float rotationSpeed = 1000f;

    [Tooltip ("Time for the fans to rotate from 0 to desired speed so it wont rotate at maximum speed instantly when start engine and opposite")]
    [SerializeField] float fullyRotationTime = 5f;

    [HideInInspector] public float speed = 0f;
    [Tooltip ("Read-only, please do not change this in runtime")]
    public float currentSpeed = 0f;

    void Update()
    {   
        if (currentSpeed != speed) currentSpeed = Mathf.MoveTowards(currentSpeed, speed, rotationSpeed / fullyRotationTime * Time.deltaTime);

        rotaryWing.Rotate(Vector3.up * currentSpeed * Time.deltaTime);
        tailRotor.Rotate(Vector3.forward * currentSpeed * Time.deltaTime);
    }
}
