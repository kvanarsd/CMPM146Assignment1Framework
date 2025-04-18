using UnityEngine;
using System.Collections.Generic;

public class PathFinder : MonoBehaviour
{
    // Assignment 2: Implement AStar
    //
    // DO NOT CHANGE THIS SIGNATURE (parameter types + return type)
    // AStar will be given the start node, destination node and the target position, and should return 
    // a path as a list of positions the agent has to traverse to reach its destination, as well as the
    // number of nodes that were expanded to find this path
    // The last entry of the path will be the target position, and you can also use it to calculate the heuristic
    // value of nodes you add to your search frontier; the number of expanded nodes tells us if your search was
    // efficient
    //
    // Take a look at StandaloneTests.cs for some test cases
    public class AStarEntry
    {
        public GraphNode node;
        public float dist;
        public float heuristic;
        public float totalDist;
        public AStarEntry parent;
        public GraphNeighbor neighbor;

        public AStarEntry(GraphNode node, float dist, float heuristic, AStarEntry parent, GraphNeighbor neighbor)
        {
            this.node = node;
            this.dist = dist;
            this.heuristic = heuristic;
            this.totalDist = dist + heuristic;
            this.parent = parent;
            this.neighbor = neighbor;
        }
    }
    public static (List<Vector3>, int) AStar(GraphNode start, GraphNode destination, Vector3 target)
    {
        Debug.Log("Called " + target);
        // Implement A* here
        List<Vector3> path = new List<Vector3>() { target };

        // create data struction that holds both distances and parent
        List<AStarEntry> frontier = new() { new AStarEntry(start, 0, GetHeuristic(start, destination), null, null) };
        List<GraphNode> expanded = new();

        AStarEntry best = null;
        while (frontier.Count > 0)
        {
            best = frontier[0];
            expanded.Add(best.node);
            frontier.RemoveAt(0);

            if (best.node == destination) break;

            foreach (GraphNeighbor neighbor in best.node.GetNeighbors())
            {   
                if(expanded.Contains(neighbor.GetNode())) continue;

                AStarEntry child = new AStarEntry(neighbor.GetNode(), best.dist + GetHeuristic(best.node, neighbor.GetNode()), GetHeuristic(neighbor.GetNode(), destination), best, neighbor);
                InsertSorted(frontier, child);
                
                // if already read skip
            }
        }

        while (best != null && best.parent != null)
        {
            path.Insert(0, GetCenterOfWall(best.neighbor.GetWall()));
            best = best.parent;
        }

        // return path and number of nodes expanded
        return (path, expanded.Count);
    }

    public static Vector3 GetCenterOfWall(Wall wall) {
        return (wall.start + wall.end) / 2f;
    }

    public static float GetHeuristic(GraphNode start, GraphNode end)
    {
        return (start.GetCenter() - end.GetCenter()).magnitude;
    }

    public static void InsertSorted(List<AStarEntry> AStarList, AStarEntry entry)
    {
        for (int i = 0; i < AStarList.Count; i++)
        {
            if(AStarList[i].totalDist > entry.totalDist)
            {
                AStarList.Insert(i, entry); 
                return;
            }
        }
        AStarList.Add(entry);
        return;
    }

    public Graph graph;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventBus.OnTarget += PathFind;
        EventBus.OnSetGraph += SetGraph;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetGraph(Graph g)
    {
        graph = g;
    }

    // entry point
    public void PathFind(Vector3 target)
    {
        if (graph == null) return;

        // find start and destination nodes in graph
        GraphNode start = null;
        GraphNode destination = null;
        foreach (var n in graph.all_nodes)
        {
            if (Util.PointInPolygon(transform.position, n.GetPolygon()))
            {
                start = n;
            }
            if (Util.PointInPolygon(target, n.GetPolygon()))
            {
                destination = n;
            }
        }
        if (destination != null)
        {
            // only find path if destination is inside graph
            EventBus.ShowTarget(target);
            (List<Vector3> path, int expanded) = PathFinder.AStar(start, destination, target);

            Debug.Log("found path of length " + path.Count + " expanded " + expanded + " nodes, out of: " + graph.all_nodes.Count);
            EventBus.SetPath(path);
        }
        

    }

    

 
}
