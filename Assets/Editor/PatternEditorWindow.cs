using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PatternEditorWindow : EditorWindow
{
    private readonly List<Vector2Int> _selectedPoints = new List<Vector2Int>();
    private readonly Stack<Vector2Int> _undoStack = new Stack<Vector2Int>();
    private Vector2Int? _lastPressedButton = null;

    private const int GridSize = 7;

    private string _patternName = "NewPattern"; 

    [MenuItem("Window/Pattern Editor")]
    public static void ShowWindow()
    {
        GetWindow<PatternEditorWindow>("Pattern Editor");
    }

    private void OnEnable()
    {
        ResetPattern();
    }

    private void OnGUI()
    {
        var windowWidth = position.width;
        var windowHeight = position.height;

        var visualizerSize = Mathf.Min(windowWidth * 0.6f, windowHeight * 0.4f);
        
        GUILayout.BeginVertical();

        GUILayout.Label("Pattern Visualizer", EditorStyles.boldLabel);
        GUILayout.Space(5);

        var visualizerOffsetX = (windowWidth - visualizerSize) / 2;
        var visualizerRect = new Rect(visualizerOffsetX, GUILayoutUtility.GetRect(0, visualizerSize).y, visualizerSize, visualizerSize);

        DrawPatternVisualizer(visualizerRect);

        GUILayout.Space(10);

        GUILayout.Label("Pattern Name", EditorStyles.boldLabel);
        _patternName = EditorGUILayout.TextField("Name", _patternName); // Input field for pattern name

        GUILayout.Label("Pattern Grid", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        for (var y = 0; y < GridSize; y++)
        {
            GUILayout.BeginHorizontal();
            for (var x = 0; x < GridSize; x++)
            {
                var defaultColor = GUI.backgroundColor;

                if (_lastPressedButton.HasValue && _lastPressedButton.Value == new Vector2Int(x, y))
                {
                    GUI.backgroundColor = Color.cyan;
                }

                if (GUILayout.Button("", GUILayout.Width(50), GUILayout.Height(50)))
                {
                    SelectPoint(new Vector2Int(x, y));
                }

                GUI.backgroundColor = defaultColor;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Confirm", GUILayout.Width(100)))
        {
            CreatePatternData();
        }

        if (GUILayout.Button("Undo", GUILayout.Width(100)))
        {
            UndoLastStep();
        }

        if (GUILayout.Button("Reset", GUILayout.Width(100)))
        {
            ResetPattern();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void CreatePatternData()
    {
        const string path = "Assets/ScriptableObjects/Patterns";
        
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
        
        var patternData = ScriptableObject.CreateInstance<PatternData>();

        var tmpPattern = _selectedPoints.Select(point => new Vector2Int(point.x, Math.Abs(point.y - 6))).ToList();
        patternData.selectedPoints = new List<Vector2Int>(tmpPattern);
        patternData.CalculateDistances();

        var assetPath = $"{path}/{_patternName}.asset";
        
        AssetDatabase.CreateAsset(patternData, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var manager = FindObjectOfType<PatternRecognition>();
        if (manager)
        {
            manager.AddPattern(patternData);
        }
    }

    private void SelectPoint(Vector2Int point)
    {
        var pointCount = _selectedPoints.Count;
        if (pointCount > 0 && (_selectedPoints[pointCount - 1] == point)) return;
        if (pointCount > 1 &&
            Vector2IntExtensions.IsSameLine(_selectedPoints[pointCount - 2], point, _selectedPoints[pointCount - 1]))
        {
            if (Vector2IntExtensions.IsCloser(_selectedPoints[pointCount - 2], point, _selectedPoints[pointCount - 1]))
            {
                return;
            }
            UndoLastStep();
        }

        _selectedPoints.Add(point);
        _undoStack.Push(point);
        _lastPressedButton = point;
        Repaint();
    }

    private void UndoLastStep()
    {
        if (_undoStack.Count <= 0) return;
        var lastPoint = _undoStack.Pop();
        _selectedPoints.Remove(lastPoint);
        _lastPressedButton = _undoStack.Count > 0 ? _undoStack.Peek() : (Vector2Int?)null;
        Repaint();
    }

    private void ResetPattern()
    {
        _selectedPoints.Clear();
        _undoStack.Clear();
        _lastPressedButton = null;
        Repaint();
    }

    private void DrawPatternVisualizer(Rect visualizerRect)
    {
        GUI.backgroundColor = Color.gray;
        GUI.Box(visualizerRect, GUIContent.none);
        GUI.backgroundColor = Color.white;

        DrawGrid(visualizerRect);

        if (_selectedPoints.Count <= 1) return;
        Handles.BeginGUI();

        var minBounds = new Vector2(visualizerRect.xMin, visualizerRect.yMin);
        var scaleFactor = new Vector2(visualizerRect.width / (GridSize - 1), visualizerRect.height / (GridSize - 1));

        for (var i = 0; i < _selectedPoints.Count - 1; i++)
        {
            Vector2 startPoint = _selectedPoints[i];
            Vector2 endPoint = _selectedPoints[i + 1];

            startPoint = Vector2.Scale(startPoint, scaleFactor) + minBounds;
            endPoint = Vector2.Scale(endPoint, scaleFactor) + minBounds;

            var t = (float)i / (_selectedPoints.Count - 1);
            var lineColor = Color.Lerp(Color.red, Color.blue, t);

            Handles.color = lineColor;
            Handles.DrawAAPolyLine(12, startPoint, endPoint); 
        }

        Handles.EndGUI();
    }

    private void DrawGrid(Rect visualizerRect)
    {
        Handles.color = Color.black;

        var minBounds = new Vector2(visualizerRect.xMin, visualizerRect.yMin);
        var scaleFactor = new Vector2(visualizerRect.width / (GridSize - 1), visualizerRect.height / (GridSize - 1));
        
        for (var i = 0; i < GridSize; i++)
        {
            var xPos = minBounds.x + (i * scaleFactor.x);
            Handles.DrawLine(new Vector3(xPos, minBounds.y), new Vector3(xPos, minBounds.y + (GridSize - 1) * scaleFactor.y));
            var yPos = minBounds.y + (i * scaleFactor.y);
            Handles.DrawLine(new Vector3(minBounds.x, yPos), new Vector3(minBounds.x + (GridSize - 1) * scaleFactor.x, yPos));
        }
        
        for (var i = 0; i < GridSize; i++)
        {
            GUI.Label(new Rect(minBounds.x + i * scaleFactor.x - 10, minBounds.y + visualizerRect.height, 50, 20), $"{i}"); 
            GUI.Label(new Rect(minBounds.x - 30, minBounds.y + i * scaleFactor.y - 10, 50, 20), $"{Math.Abs(i - GridSize + 1)}"); 
        }
    }
}
