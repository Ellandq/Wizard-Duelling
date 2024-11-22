using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;
using static Vector2IntExtensions;

public class PatternRecognitionManager : MonoBehaviour
{
    public List<PatternData> patterns = new List<PatternData>();
    
    [Header("Settings")] 
    [SerializeField] private float minLikelihood = 0.6f;
    
    [Header ("Additional Values")]
    private const float Tolerance = .4f;
    private static float _scale;
    private static float _drawnPatternLength;

    public void AddPattern(PatternData newPattern)
    {
        if (newPattern && !patterns.Contains(newPattern))
        {
            patterns.Add(newPattern);
        }
    }
    
    public PatternData GetClosestPattern(List<Vector3> drawnPattern, (Vector3 min, Vector3 max) boundingCoordinates)
    {
        _scale =  6f / (boundingCoordinates.max - boundingCoordinates.min).x;
        drawnPattern = drawnPattern.Select(v => (v - boundingCoordinates.min) * _scale).ToList();
        
        _drawnPatternLength = CalculateLineLength(drawnPattern);
        Debug.Log(_drawnPatternLength);
        
        var possiblePatternList = (from pattern in patterns
            let likelihood = EstimateLikelihood_PatternLength(pattern.totalDistance)
            where !(minLikelihood > likelihood)
            select (likelihood, pattern)).ToList();
        
        var tmpPossiblePatternList = (from pattern in possiblePatternList
            let likelihood =
                EstimateLikelihood_StartAndEndPoints(GetPatternStartAndEndPoints(pattern.pattern),
                    (drawnPattern[0], drawnPattern[drawnPattern.Count - 1]))
            where !(minLikelihood > likelihood)
            select (pattern.likelihood, pattern.pattern)).ToList();
        
        possiblePatternList = tmpPossiblePatternList;
        
        return (from pattern in possiblePatternList
            let likelihood = EstimateLikelihood_KeyPoints(pattern.pattern, drawnPattern)
            where likelihood
            select pattern.pattern).FirstOrDefault();
    }

    private static float EstimateLikelihood_PatternLength(float patternLength)
    {
        var maxDifference = patternLength * Tolerance;
        return Mathf.Abs(_drawnPatternLength - patternLength) <= maxDifference
            ? Mathf.Clamp01(1f - Mathf.Abs(_drawnPatternLength - patternLength) / maxDifference)
            : 0f;
    }
    
    private static float EstimateLikelihood_StartAndEndPoints((Vector3 start, Vector3 end) pattern, (Vector3 start, Vector3 end) drawnPattern)
    {
        var startDistance = Vector3.Distance(pattern.start, drawnPattern.start);
        var endDistance = Vector3.Distance(pattern.end, drawnPattern.end);
        
        const float maxDistance = 8.5f; // apr = 6√2
        
        var normalizedStart = Mathf.Clamp01(1f - startDistance / maxDistance);
        var normalizedEnd = Mathf.Clamp01(1f - endDistance / maxDistance);
        
        return (normalizedStart + normalizedEnd) / 2f;
    }
    
    private static bool EstimateLikelihood_KeyPoints(PatternData pattern, List<Vector3> drawnPattern)
    {
        var indexes = new List<int>();
        var patternMap = drawnPattern.Zip(
            Enumerable.Repeat(false, drawnPattern.Count), 
            (position, blocked) => (blocked, position) 
        ).ToList();
        
        foreach (var keyPoint in pattern.selectedPoints)
        {
            var index = GetClosestPointIndex(AsVector3(keyPoint), patternMap);
            var previousIndex = indexes.Count == 0 ? 0 : indexes.Last();
            indexes.Add(index);
            
            if (index == -1)
            {
                return false;
            }
            
            MarkAsBlocked(patternMap, index, previousIndex);
        }

        var diff = 0f;
        for (var i = 1; i < indexes.Count; i++)
        {
            var lineLength = _drawnPatternLength * (indexes[i] - indexes[i - 1]) / drawnPattern.Count;
            var patternLineLength = Vector2Int.Distance(pattern.selectedPoints[i - 1], pattern.selectedPoints[i]);
            diff += Math.Abs(lineLength - patternLineLength);
        }
        
        return diff  <= pattern.totalDistance / 2f && indexes.Zip(indexes.Skip(1), (a, b) => a < b).All(x => x);
    }

    private static int GetClosestPointIndex(Vector3 target, List<(bool blocked, Vector3 position)> pointList)
    {
        var maxDistance = IsOnEdge(target) ? 1.5f : 1f;

        var index = -1;
        var lastDistance = 100f;
        var repeat = 0;

        for (var i = 0; i < pointList.Count; i++)
        {
            if (pointList[i].blocked) continue;
            var distance = Vector3.Distance(target, pointList[i].position);

            if (distance < lastDistance)
            {
                lastDistance = distance;  
                index = i;                
                repeat = 0;                
            }
            else if (distance > lastDistance && lastDistance < maxDistance)
            {
                repeat++;

                if (repeat >= 25)
                {
                    return index;
                }
            }
        }
        
        return index;
    }
    
    private static float CalculateLineLength(List<Vector3> points)
    {
        var length = 0f;
        var positionCount = points.Count;

        if (positionCount < 2)
            return 0f; 

        for (var i = 0; i < positionCount - 1; i++)
        {
            var start = points[i];
            var end = points[i + 1];
            length += Vector3.Distance(start, end);
        }

        return length;
    }
    
    private static bool IsOnEdge(Vector3 point)
    {
        return point.x == 0 || point.y == 0;
    }
    
    private static (Vector3, Vector3) GetPatternStartAndEndPoints(PatternData pattern)
    {
        var start = pattern.selectedPoints[0];
        var end = pattern.selectedPoints[pattern.selectedPoints.Count - 1];
        return (AsVector3(start), AsVector3(end));
    }

    private static int MarkAsBlocked(List<(bool blocked, Vector3 position)> pointList, int index, int lastIndex)
    {
        var count = 0;
        for (var i = lastIndex + Math.Min(lastIndex, 1); i <= index; i++)
        {
            count++;
            var item = pointList[i];  
            item.blocked = true;      
            pointList[i] = item;  
        }

        return count;
    }
}