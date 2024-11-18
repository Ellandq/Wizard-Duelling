using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DrawMousePattern : MonoBehaviour
{
    [SerializeField] private PatternRecognitionManager patternRecognition;
    [SerializeField] private LineRenderer squareRenderer;
    private LineRenderer _lineRenderer;
    private List<Vector3> _pointsList;
    private Camera _mainCamera;
    
    
    private Vector3 _minBounds;
    private Vector3 _maxBounds;

    [Header("Settings")] 
    [SerializeField] private float startWidth = 0.02f;
    [SerializeField] private float endWidth = 0.2f;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _pointsList = new List<Vector3>();
        _mainCamera = Camera.main;

        _lineRenderer.positionCount = 0;
        _lineRenderer.startWidth = startWidth;
        _lineRenderer.endWidth = endWidth;

        CreateSquareRenderer();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _pointsList.Clear();
            _lineRenderer.positionCount = 0;
            
            var mousePosition = GetMouseWorldPosition();
            _minBounds = mousePosition;
            _maxBounds = mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            var mousePosition = GetMouseWorldPosition();

            if (_pointsList.Count == 0 || Vector3.Distance(mousePosition, _pointsList[_pointsList.Count - 1]) > 0.1f)
            {
                _pointsList.Add(mousePosition);

                _lineRenderer.positionCount = _pointsList.Count;
                _lineRenderer.SetPosition(_pointsList.Count - 1, mousePosition);
                
                UpdateBounds(mousePosition);
                UpdateSquare();
            }
        }

        if (!Input.GetMouseButtonUp(0)) return;
        var recognizedPattern = patternRecognition.GetClosestPattern(_pointsList, (_minBounds, _maxBounds));
        if (recognizedPattern) Debug.Log(recognizedPattern.name);
        squareRenderer.positionCount = 0;
    }

    private Vector3 GetMouseWorldPosition()
    {
        var mousePosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        return mousePosition;
    }

    private void CreateSquareRenderer()
    {
        squareRenderer.startWidth = 0.05f;
        squareRenderer.endWidth = 0.05f;
        squareRenderer.loop = true;
        squareRenderer.useWorldSpace = true;
        squareRenderer.positionCount = 0;
    }

    private void UpdateBounds(Vector3 point)
    {
        _minBounds = Vector3.Min(_minBounds, point);
        _maxBounds = Vector3.Max(_maxBounds, point);
    }

    private void UpdateSquare()
    {
        if (squareRenderer.positionCount == 0)
        {
            squareRenderer.positionCount = 5;
        }
        
        var center = (_minBounds + _maxBounds) / 2;
        var size = Mathf.Max(_maxBounds.x - _minBounds.x, _maxBounds.y - _minBounds.y) / 2; 

        var squareCorners = new Vector3[5];
        squareCorners[0] = new Vector3(center.x - size, center.y - size, 0);
        squareCorners[1] = new Vector3(center.x + size, center.y - size, 0);
        squareCorners[2] = new Vector3(center.x + size, center.y + size, 0);
        squareCorners[3] = new Vector3(center.x - size, center.y + size, 0);
        squareCorners[4] = squareCorners[0]; 

        squareRenderer.SetPositions(squareCorners);
    }
}
