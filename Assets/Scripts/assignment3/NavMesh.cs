using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class NavMesh : MonoBehaviour
{
    // implement NavMesh generation here:
    //    the outline are Walls in counterclockwise order
    //    iterate over them, and if you find a reflex angle
    //    you have to split the polygon into two
    //    then perform the same operation on both parts
    //    until no more reflex angles are present
    //
    //    when you have a number of polygons, you will have
    //    to convert them into a graph: each polygon is a node
    //    you can find neighbors by finding shared edges between
    //    different polygons (or you can keep track of this while 
    //    you are splitting)
    public Graph MakeNavMesh(List<Wall> outline)
    {
        List<Polygon> polygons = new List<Polygon>();
        polygons.Add(new Polygon(outline));

        while (true)
        {
            int first = FindReflex(outline, 0);
            int iterate = first + 1;

            while (true) { 
                int next = FindReflex(outline, iterate);
                Wall wall = new Wall(outline[first].end, outline[next].end);
                CheckAllCrosses(polygons, wall);

                iterate = next + 1;
            }
            
        }   
        Graph g = new Graph();
        g.all_nodes = new List<GraphNode>();
        return g;
    }

    public bool CheckCreatedAngles(List<Wall> outline, int start, int end, Wall startToEnd, Wall endToStart)
    {

    }

    public float AngleBetweenWalls(Wall wall1, Wall wall2)
    {
        return 0;
    }

    public bool CheckAllCrosses(List<Polygon> polygons, Wall wall)
    {
        foreach (Polygon p in polygons) foreach (Wall w in p.walls) if (w.Crosses(wall)) return true;

        return false;
    }

    public int FindReflex (List<Wall> outline, int i)
    {
        for (; i < outline.Count; i++)
        {
            Wall first = outline[i];
            Wall second = outline[(i + 1) % outline.Count];
            if (Vector3.Dot(first.normal, second.direction) < 0)
            {
                return i;
            }
        }

        return -1;
    }

    public class Polygon
    {
        public List<Wall> walls;

        public Polygon(List<Wall> walls)
        {
            this.walls = walls;
        }

        public void AddWall(Wall wall)
        {
            walls.Add(wall);
        }

        public (Polygon first, Polygon second) Split(Polygon Parent, int start, int end, Wall newWall) 
        {
            Polygon first = new Polygon();
            Polygon second = new Polygon();
            first.AddWall(newWall);
            second.AddWall(newWall);

            return (first, second);
        }
    }

    List<Wall> outline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventBus.OnSetMap += SetMap;
    }

    // Update is called once per frame
    void Update()
    {
       

    }

    public void SetMap(List<Wall> outline)
    {
        Graph navmesh = MakeNavMesh(outline);
        if (navmesh != null)
        {
            Debug.Log("got navmesh: " + navmesh.all_nodes.Count);
            EventBus.SetGraph(navmesh);
        }
    }

    


    
}
