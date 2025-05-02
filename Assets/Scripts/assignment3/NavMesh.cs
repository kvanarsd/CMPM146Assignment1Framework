using System.Collections.Generic;
//using System.Drawing;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class NavMesh : MonoBehaviour
{
    static int wallsHit = 0;

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
        foreach (Wall wall in outline) {
            Debug.Log(wall.start);
        }

        Vector3 point = new Vector3(-100, 0, 50);
        Debug.Log("Point inside: " + Util.PointInPolygon(point, outline));

        wallsHit = 0;

        List<Polygon> polygons = new List<Polygon>();

        Stack<Polygon> toHandle = new();
        toHandle.Push(new Polygon(outline));

        int saftey = 10000;

        while (toHandle.Count > 0 && saftey > 0) {
            saftey--;
            
            (Polygon first, Polygon second) = toHandle.Pop().FindSplit();

            if (second != null) {
                toHandle.Push(first);
                toHandle.Push(second);
            } else {
                polygons.Add(first);
            }
        }

        if (saftey == 0) {
            Debug.Log("Saftey was triggered!");
        }

        Graph g = new Graph();
        g.all_nodes = new List<GraphNode>();

        int i = 0;
        foreach (Polygon p in polygons) {
            p.graphNode = new GraphNode(i++, p.walls);
            g.all_nodes.Add(p.graphNode);
        }

        foreach (Polygon p in polygons) {
            foreach (Polygon other in polygons) {
                if (p == other) continue;
                int shared = p.SharesWall(other);
                if (shared != -1) {
                    p.graphNode.AddNeighbor(other.graphNode, shared);
                }
            }
        }
        
        return g;
    }

    public class Polygon
    {
        public List<Wall> walls;
        public GraphNode graphNode;

        public Polygon(List<Wall> walls)
        {
            this.walls = walls;
        }

        public void AddWall(Wall wall)
        {
            walls.Add(wall);
        }

        // If there are no reflex angles, second will == null and first == this. 
        public (Polygon first, Polygon second) FindSplit() {
            int first = FindReflex(walls, 0);

            if (first == -1) {
                // There are no reflex points!
                return (this, null);
            }

            int iterate = first + 1;
            (int index, int created, float dist) bestWall = (-1, 0, 0);

            while (true) { 
                int next = FindReflex(walls, iterate);
                if (next == -1 && bestWall.index == -1) {
                    // There are no more reflex points other than our current one.

                    CreateSphere(walls[first].end);
                    int i = 2;
                    int loop = 0;

                    Debug.Log("no next!");

                    while (loop < walls.Count - 3) {
                        if (!this.CollidesWithWall(walls[first].end, walls[(first+i)%walls.Count].end))
                        {
                            int created = CheckCreatedReflex(first, (first+i)%walls.Count);
                            float dist = Vector3.Distance(walls[first].end, walls[(first+i)%walls.Count].end);
                            if(bestWall.index == -1 || bestWall.created > created || (bestWall.created == created && bestWall.dist > dist)) {
                                bestWall = ((first+i)%walls.Count, created, dist);
                            }
                        } 
                        
                        i++;
                        loop++;
                    }

                    if (bestWall.index == -1) {
                        throw new AbandonedMutexException(loop + " " + i + " FindSplit couldn't find non-colliding split");
                    } else {
                        return Split((first+1)%walls.Count, (bestWall.index+1)%walls.Count, Color.blue);
                    }
                    
                } else if (next == -1) {
                    return Split((first+1)%walls.Count, (bestWall.index+1)%walls.Count, Color.white);
                }

                if (!this.CollidesWithWall(walls[first].end, walls[next].end)) {
                    int created = CheckCreatedReflex(first, next);
                    float dist = Vector3.Distance(walls[first].end, walls[next].end);
                    if(bestWall.index == -1 || bestWall.created > created || (bestWall.created == created && bestWall.dist > dist)) {
                        bestWall = (next, created, dist);
                    }
                }

                iterate = next + 1;
            }
        }

        public (Polygon first, Polygon second) Split(int start, int end, Color color) // start = inclusive, end = exclisive
        {
            Polygon first = new Polygon(GetRange(walls, start, end));
            Polygon second = new Polygon(GetRange(walls, end, start));

            Vector3 p1 = first.walls[0].start;
            Vector3 p2 = first.walls[^1].end;

            Debug.DrawLine(p1, p2, color, 10, false);

            // CreateSphere(first.walls[first.walls.Count-1].end);
            // CreateSphere(first.walls[0].start);

            first.AddWall(new Wall(p2, p1));
            second.AddWall(new Wall(p1, p2));

            return (first, second);
        }

        public bool CollidesWithWall(Vector3 start, Vector3 end) {
            if (!Util.PointInPolygon(GetCenterpoint(start, end), walls)) {
                return true;
            }
            foreach (Wall wall in walls) {
                if ((start == wall.start && end == wall.end) || (start == wall.end && end == wall.start)) {
                    return true;
                }
                Vector3 midpoint = GetCenterpoint(start, end);
                if (wall.Crosses(midpoint, midpoint)) {
                    return true;
                }
                if (start == wall.start || start == wall.end || end == wall.start || end == wall.end) {
                    continue;
                }
                if (wall.Crosses(start, end)) {
                    // Debug.Log(start + " " + end + " - " + wall.start + " " + wall.end);
                    return true;
                }
            }
            return false;
        }

        public int SharesWall(Polygon polygon) {
            foreach (Wall wall in walls) {
                foreach(Wall otherWall in polygon.walls) {
                    if (wall.Same(otherWall)) return walls.IndexOf(wall);
                }
            }
            return -1;
        }

        public int CheckCreatedReflex(int start, int end)
        {
            Vector3 p1 = walls[start].end;
            Vector3 p2 = walls[end].end;
            Wall firstWall = new Wall(p1, p2);
            Wall secondWall = new Wall(p2, p1);

            int created = 0;

            if ((Vector3.Dot(walls[start].normal, firstWall.direction) < 0) ||
                (Vector3.Dot(secondWall.normal, walls[(start + 1)%walls.Count].direction) < 0))
            {
                created++;
            } 

            if ((Vector3.Dot(walls[end].normal, secondWall.direction) < 0) ||
                (Vector3.Dot(firstWall.normal, walls[(end + 1) % walls.Count].direction) < 0))
            {
                created++;
            }

            return created;
        }
    }

    public static GameObject CreateSphere(Vector3 position) {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * 15;
        return sphere;
    }

    public static Vector3 GetCenterpoint(Vector3 p1, Vector3 p2) {
        return (p1 + p2) / 2f;
    }

    public static float AngleBetweenWalls(Wall wall1, Wall wall2)
    {
        return 0;
    }

    public static bool CheckAllCrosses(List<Polygon> polygons, Wall wall)
    {
        foreach (Polygon p in polygons) foreach (Wall w in p.walls) if (w.Crosses(wall)) return true;

        return false;
    }

    public static int FindReflex(List<Wall> outline, int i)
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

    public static List<T> GetRange<T>(List<T> list, int a, int b) { // a is inclusive, b is exclusive
        if (a < 0 || b < 0 || a >= list.Count || b >= list.Count) {
            throw new System.Exception($"Splice(): a ({a}) or b ({b}) was out of bounds for length ({list.Count}).");
        }
        List<T> cutout = new();
        int current = a;
        while (current != b)
        {
            cutout.Add(list[current]);
            current = (current + 1) % list.Count;
        }
        return cutout;
    }

    public static List<T> RemoveRange<T>(List<T> list, int a, int b) { // a is inclusive, b is exclusive
        if (a < 0 || b < 0 || a >= list.Count || b >= list.Count) {
            throw new System.Exception($"Splice(): a ({a}) or b ({b}) was out of bounds for length ({list.Count}).");
        }
        List<T> cutout = GetRange(list, a, b);
        if (a < b) {
            list.RemoveRange(a, b - a);
        } else {
            int aToEnd = list.Count - a;
            list.RemoveRange(a, aToEnd);
            list.RemoveRange(0, b);
        }
        
        return cutout;
    }

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
