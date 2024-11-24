using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using static Vector2IntExtensions;

public class DrawMousePattern : MonoBehaviour
{
    [SerializeField] private PatternRecognition patternRecognition;
    [SerializeField] private LineRenderer squareRenderer;
    [SerializeField] private LineRenderer recognizedPatternRenderer;
    [SerializeField] private LineRenderer tempLineRenderer;
    private LineRenderer _lineRenderer;
    private List<Vector3> _pointsList;
    private List<Vector3> _fullPatternList;
    private List<Vector3> _fadePoints;
    private Camera _mainCamera;

    private Vector3 _minBounds;
    private Vector3 _maxBounds;

    [Header("Settings")] 
    [SerializeField] private float startWidth = 0.02f;
    [SerializeField] private float endWidth = 0.2f;
    [SerializeField] private float fadeOutSpeed = 1.0f;
    [SerializeField] private float drawFadeOutSpeed = 1.0f;
    [SerializeField] private float flashDuration = 0.5f; 

    private Coroutine _fadeCoroutine;
    private bool _isDrawing;
    private bool _patternFound;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _pointsList = new List<Vector3>();
        _fullPatternList = new List<Vector3>();
        _fadePoints = new List<Vector3>();
        _mainCamera = Camera.main;

        _lineRenderer.positionCount = 0;
        _lineRenderer.startWidth = startWidth;
        _lineRenderer.endWidth = endWidth;

        CreateSquareRenderer();
        SetupRecognizedPatternRenderer();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _pointsList.Clear();
            _fadePoints.Clear();
            _lineRenderer.positionCount = 0;
            _isDrawing = true;

            var mousePosition = GetMouseWorldPosition();
            _minBounds = mousePosition;
            _maxBounds = mousePosition;

            StartCoroutine(FadeOutWhileDrawing());
        }

        if (Input.GetMouseButton(0))
        {
            var mousePosition = GetMouseWorldPosition();

            if (_pointsList.Count == 0 || Vector3.Distance(mousePosition, _pointsList[_pointsList.Count - 1]) > 0.01f)
            {
                _pointsList.Add(mousePosition);
                _fadePoints.Add(mousePosition);

                _lineRenderer.positionCount = _fadePoints.Count;
                _lineRenderer.SetPosition(_fadePoints.Count - 1, mousePosition);

                UpdateBounds(mousePosition);
                UpdateSquare();
            }
        }

        if (!Input.GetMouseButtonUp(0)) return;
        _isDrawing = false;
        _fullPatternList = new List<Vector3>(_pointsList);
        squareRenderer.positionCount = 0;
        _fadeCoroutine = StartCoroutine(FadeOutLine());
        var recognizedPattern = patternRecognition.GetClosestPattern(_pointsList, (_minBounds, _maxBounds));
        
        if (!recognizedPattern) return;
        tempLineRenderer.positionCount = _fullPatternList.Count;
        tempLineRenderer.SetPositions(_fullPatternList.ToArray());
        tempLineRenderer.Simplify(0.5f);
        
        if (recognizedPattern.selectedPoints.Count != tempLineRenderer.positionCount) return;
        Debug.Log(recognizedPattern.name);
        _patternFound = true;
        StartCoroutine(FlashSimplifiedPattern());
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

    private void SetupRecognizedPatternRenderer()
    {
        recognizedPatternRenderer.positionCount = 0;
        recognizedPatternRenderer.startWidth = startWidth;
        recognizedPatternRenderer.endWidth = startWidth;
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

    private IEnumerator FadeOutLine()
    {
        while (_fadePoints.Count > 0)
        {
            if (_patternFound)
            {
                _fadePoints.Clear();
                _lineRenderer.positionCount = 0;
                _patternFound = false;
                yield break; 
            }
            
            var timePerPoint = fadeOutSpeed / _fadePoints.Count;

            _fadePoints.RemoveAt(0);
            
            _lineRenderer.positionCount = _fadePoints.Count;
            if (_fadePoints.Count > 0)
            {
                _lineRenderer.SetPositions(_fadePoints.ToArray());
            }

            yield return new WaitForSeconds(timePerPoint);
        }

        _lineRenderer.positionCount = 0;
    }


    private IEnumerator FadeOutWhileDrawing()
    {
        while (_isDrawing)
        {
            if (_fadePoints.Count > 2)
            {
                _fadePoints.RemoveAt(0);

                _lineRenderer.positionCount = _fadePoints.Count;
                _lineRenderer.SetPositions(_fadePoints.ToArray());
            }

            yield return new WaitForSeconds(drawFadeOutSpeed / Mathf.Max(1, _fadePoints.Count));
        }
    }

    private IEnumerator FlashSimplifiedPattern()
    {
        var simplifiedCount = tempLineRenderer.positionCount;
        var simplifiedPoints = new Vector3[simplifiedCount];
        tempLineRenderer.GetPositions(simplifiedPoints);
        
        recognizedPatternRenderer.positionCount = simplifiedCount;
        recognizedPatternRenderer.SetPositions(simplifiedPoints);

        var halfFlashTime = flashDuration / 2f;
        
        for (float t = 0; t < halfFlashTime; t += Time.deltaTime)
        {
            var width = Mathf.Lerp(startWidth, endWidth, t / halfFlashTime);
            recognizedPatternRenderer.startWidth = width;
            recognizedPatternRenderer.endWidth = width;
            yield return null;
        }
        
        for (float t = 0; t < halfFlashTime; t += Time.deltaTime)
        {
            var width = Mathf.Lerp(endWidth, startWidth, t / halfFlashTime);
            recognizedPatternRenderer.startWidth = width;
            recognizedPatternRenderer.endWidth = width;
            yield return null;
        }

        recognizedPatternRenderer.positionCount = 0;
    }
}
