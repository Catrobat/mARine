using System.Collections;
using UnityEngine;

public class WaterPlaneMover : MonoBehaviour
{
    [SerializeField] private string waterPlaneName = "HighPolyPlane";
    [SerializeField] private float moveDistance = 2f;
    [SerializeField] private float moveDuration = 1f;

    private Transform _waterPlane;
    private bool _isMoving = false;

    void Awake()
    {
        _waterPlane = transform.Find(waterPlaneName);
    }

    public void MoveWaterUp()
    {
        if (_waterPlane != null && !_isMoving)
        {
            StartCoroutine(MoveWater(Vector3.up * moveDistance));
        }
    }

    public void MoveWaterDown()
    {
        if (_waterPlane != null && !_isMoving)
        {
            StartCoroutine(MoveWater(Vector3.down * moveDistance));
        }
    }

    public IEnumerator MoveWater(Vector3 direction)
    {
        _isMoving = true;
        Vector3 start = _waterPlane.localPosition;
        Vector3 end = start + direction;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            float t = elapsedTime / moveDuration;
            _waterPlane.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, t));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _waterPlane.localPosition = end;
        _isMoving = false;
    }
}