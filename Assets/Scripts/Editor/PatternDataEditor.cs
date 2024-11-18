using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PatternData))]
public class PatternDataEditor : Editor
{
    private const int GridSize = 7;

    private SerializedProperty _selectedPointsProperty;
    private SerializedProperty _distanceListProperty;
    private SerializedProperty _totalDistanceProperty;

    private void OnEnable()
    {
        _selectedPointsProperty = serializedObject.FindProperty("selectedPoints");
        _distanceListProperty = serializedObject.FindProperty("distanceList");
        _totalDistanceProperty = serializedObject.FindProperty("totalDistance");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_selectedPointsProperty, true);
        if (EditorGUI.EndChangeCheck())
        {
            RecalculateDistances();
        }
        
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(_distanceListProperty, true);
        EditorGUILayout.FloatField("Total Distance", _totalDistanceProperty.floatValue);
        EditorGUI.EndDisabledGroup();
        
        if (GUILayout.Button("Recalculate Distances"))
        {
            RecalculateDistances();
        }

        serializedObject.ApplyModifiedProperties();
        
        var patternData = (PatternData)target;
        var visualizerRect = GUILayoutUtility.GetRect(0, 300);
        DrawPatternVisualizer(visualizerRect, patternData.selectedPoints);
    }

    private void RecalculateDistances()
    {
        var patternData = (PatternData)target;
        
        patternData.distanceList = new List<float>();
        var totalDistance = 0f;

        for (var i = 0; i < patternData.selectedPoints.Count - 1; i++)
        {
            var distance = Vector2Int.Distance(patternData.selectedPoints[i], patternData.selectedPoints[i + 1]);
            patternData.distanceList.Add(distance);
            totalDistance += distance;
        }
        
        patternData.totalDistance = totalDistance;
        
        EditorUtility.SetDirty(patternData);
    }

    private void DrawPatternVisualizer(Rect visualizerRect, List<Vector2Int> selectedPoints)
    {
        var aspectRatio = 1.0f;
        var adjustedWidth = visualizerRect.width;
        var adjustedHeight = adjustedWidth * aspectRatio;

        if (adjustedHeight > visualizerRect.height)
        {
            adjustedHeight = visualizerRect.height;
            adjustedWidth = adjustedHeight / aspectRatio;
        }

        var xOffset = visualizerRect.x + (visualizerRect.width - adjustedWidth) / 2;
        var yOffset = visualizerRect.y + (visualizerRect.height - adjustedHeight) / 2;
        var adjustedRect = new Rect(xOffset, yOffset, adjustedWidth, adjustedHeight);

        GUI.backgroundColor = Color.gray;
        GUI.Box(adjustedRect, GUIContent.none);
        GUI.backgroundColor = Color.white;

        DrawGrid(adjustedRect);

        if (selectedPoints.Count <= 1) return;
        Handles.BeginGUI();

        var minBounds = new Vector2(adjustedRect.xMin, adjustedRect.yMin);
        var scaleFactor = new Vector2(adjustedRect.width / (GridSize - 1), adjustedRect.height / (GridSize - 1));

        for (var i = 0; i < selectedPoints.Count - 1; i++)
        {
            Vector2 startPoint = selectedPoints[i];
            Vector2 endPoint = selectedPoints[i + 1];
            
            startPoint = Vector2.Scale(startPoint, scaleFactor) + minBounds;
            endPoint = Vector2.Scale(endPoint, scaleFactor) + minBounds;
            
            startPoint.y = adjustedRect.yMax - (startPoint.y - adjustedRect.yMin);
            endPoint.y = adjustedRect.yMax - (endPoint.y - adjustedRect.yMin);

            var t = (float)i / (selectedPoints.Count - 1);
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
        }
        
        for (var i = 0; i < GridSize; i++)
        {
            var yPos = minBounds.y + (i * scaleFactor.y);
            Handles.DrawLine(new Vector3(minBounds.x, yPos), new Vector3(minBounds.x + (GridSize - 1) * scaleFactor.x, yPos));
        }
        
        for (var i = 0; i < GridSize; i++)
        {
            GUI.Label(new Rect(minBounds.x + i * scaleFactor.x - 10, minBounds.y + visualizerRect.height, 50, 20), $"{i}");
            GUI.Label(new Rect(minBounds.x - 30, minBounds.y + (GridSize - 1 - i) * scaleFactor.y - 10, 50, 20), $"{i}");
        }
    }

}
