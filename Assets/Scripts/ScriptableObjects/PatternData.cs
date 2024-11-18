using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PatternData", menuName = "Custom/PatternData", order = 1)]
public class PatternData : ScriptableObject
{
    public List<Vector2Int> selectedPoints;
    public List<float> distanceList;
    public float totalDistance;
    
    public void CalculateDistances()
    {
        distanceList = new List<float>();

        for (var i = 0; i < selectedPoints.Count - 1; i++)
        {
            var distance = Vector2Int.Distance(selectedPoints[i], selectedPoints[i + 1]);
            distanceList.Add(distance);
        }
    }
}