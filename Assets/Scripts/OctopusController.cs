using UnityEngine;
using UnityEngine.Splines;
using Vector2 = UnityEngine.Vector2;

[RequireComponent(typeof(Rigidbody))]
public class OctopusController : MonoBehaviour
{
    public FixedJoystick joystick;
    public float moveSpeed = 0.05f;
    public float laneSwitchCooldown = 0.5f;
    public float laneSwitchDuration = 0.5f; // duration of smooth lane transition
    public SplineContainer[] splineContainers;

    private int _currentSplineIndex = 2;
    private float _splinePosition = 0f;

    private float _lastLaneSwitchTime = -999f;
    private float _laneLerpTime = 0f;

    private SplineContainer _currentSpline;
    private SplineContainer _targetSpline;
    private bool _isSwitchingLanes = false;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _currentSpline = splineContainers[_currentSplineIndex];
        _targetSpline = _currentSpline;
    }

    private void FixedUpdate()
    {
        Vector2 input = joystick.Direction;

        if (input.magnitude > 0.1f)
        {
            // Forward/backward
            float forwardInput = input.y;
            _splinePosition += forwardInput * moveSpeed * Time.fixedDeltaTime;
            _splinePosition = Mathf.Clamp01(_splinePosition);

            // Handle lane switch input
            float horizontalInput = input.x;
            if (Mathf.Abs(horizontalInput) > 0.6f && Time.time - _lastLaneSwitchTime > laneSwitchCooldown)
            {
                int desiredIndex = _currentSplineIndex;

                if (horizontalInput > 0 && _currentSplineIndex < splineContainers.Length - 1)
                    desiredIndex++;
                else if (horizontalInput < 0 && _currentSplineIndex > 0)
                    desiredIndex--;

                if (desiredIndex != _currentSplineIndex)
                {
                    _targetSpline = splineContainers[desiredIndex];
                    _currentSplineIndex = desiredIndex;
                    _isSwitchingLanes = true;
                    _laneLerpTime = 0f;
                    _lastLaneSwitchTime = Time.time;
                }
            }

            // Evaluate positions on splines
            Vector3 posCurrent = _currentSpline.EvaluatePosition(_splinePosition);
            Vector3 posTarget = _targetSpline.EvaluatePosition(_splinePosition);
            Vector3 tangentTarget = _targetSpline.EvaluateTangent(_splinePosition);

            Vector3 finalPos;

            if (_isSwitchingLanes)
            {
                _laneLerpTime += Time.fixedDeltaTime / laneSwitchDuration;
                finalPos = Vector3.Lerp(posCurrent, posTarget, _laneLerpTime);

                if (_laneLerpTime >= 1f)
                {
                    _currentSpline = _targetSpline;
                    _isSwitchingLanes = false;
                }
            }
            else
            {
                finalPos = posTarget;
            }

            // Move and rotate
            _rb.MovePosition(finalPos);

            if (tangentTarget != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(tangentTarget, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, toRotation, 5f * Time.fixedDeltaTime));
            }
        }
    }
}
