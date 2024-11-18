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

    private const float Tolerance = 1.05f;

    [Header ("Const Values")]
    private const float StepLength = 0.1f;

    public void AddPattern(PatternData newPattern)
    {
        if (newPattern && !patterns.Contains(newPattern))
        {
            patterns.Add(newPattern);
        }
    }
    
    public PatternData GetClosestPattern(List<Vector3> drawnPattern, (Vector3 min, Vector3 max) boundingCoordinates)
    {
        var scale =  6f / (boundingCoordinates.max - boundingCoordinates.min).x;
        drawnPattern = drawnPattern.Select(v => (v - boundingCoordinates.min) * scale).ToList();
        
        var drawnPatternLength = drawnPattern.Count * StepLength * scale;
        
        var possiblePatternList = (from pattern in patterns
            let likelihood = EstimateLikelihood_PatternLength(pattern.totalDistance, drawnPatternLength)
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

    private static float EstimateLikelihood_PatternLength(float patternLength, float drawnPatternLength)
    {
        return drawnPatternLength > patternLength * Tolerance ? 0f : Mathf.Clamp01(drawnPatternLength / patternLength);
    }
    
    private static float EstimateLikelihood_StartAndEndPoints((Vector3 start, Vector3 end) pattern, (Vector3 start, Vector3 end) drawnPattern)
    {
        var startDistance = Vector3.Distance(pattern.start, drawnPattern.start);
        var endDistance = Vector3.Distance(pattern.end, drawnPattern.end);
        
        const float maxDistance = 8.5f;
        
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
        var count = drawnPattern.Count;
        
        foreach (var keyPoint in pattern.selectedPoints)
        {

            if (count < 8) 
            {
                return false;
            }

            var index = GetClosestPointIndex(AsVector3(keyPoint), patternMap);
            indexes.Add(index);
            
            if (index == -1)
            {
                return false;
            }
            
            count -= RemoveSurroundingValues(patternMap, index);
        }
        
        return indexes.Zip(indexes.Skip(1), (a, b) => a < b).All(x => x);
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

                if (repeat >= 10)
                {
                    return index;
                }
            }
        }
        
        return index;
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

    private static int RemoveSurroundingValues(List<(bool blocked, Vector3 position)> pointList, int index)
    {
        var start = Math.Max(index - 5, 0);
        var end = Math.Min(index + 5, pointList.Count - 1);
        var count = 0;
        
        for (var i = start; i <= end; i++)
        {
            count++;
            var item = pointList[i];  
            item.blocked = true;      
            pointList[i] = item;  
        }

        return count;
    }
}