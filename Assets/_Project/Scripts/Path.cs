using UnityEngine;

public class Path : MonoBehaviour
{ 
    [SerializeField] private Transform[] waypoints;
    
    public int WaypointCount
    {
        get { return waypoints.Length; }
    }

    public Transform[] GetWaypoint(int index)
    {
        return int[index];
    }
}