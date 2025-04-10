using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SteeringBehavior : MonoBehaviour
{
    public Vector3 target;
    public KinematicBehavior kinematic;
    public List<Vector3> path;
    // you can use this label to show debug information,
    // like the distance to the (next) target
    public TextMeshProUGUI label;

    public float speed;
    public float velocity;
    public Vector3 slowdown_buffer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        kinematic = GetComponent<KinematicBehavior>();
        target = transform.position;
        path = null;
        EventBus.OnSetMap += SetMap;
    }

    // Update is called once per frame
    void Update()
    {
        // Assignment 1: If a single target was set, move to that target
        //                If a path was set, follow that path ("tightly")

        // you can use kinematic.SetDesiredSpeed(...) and kinematic.SetDesiredRotationalVelocity(...)
        //    to "request" acceleration/decceleration to a target speed/rotational velocity

        Vector3 pos = transform.position;

        if (target != null && pos != target)
        {
            //Debug.Log(pos.z + " " + pos.x + ", " + target.z + " " + target.x);
            if (pos.z < target.z && target.z - pos.z > slowdown_buffer.z)
            {
                kinematic.SetDesiredSpeed(speed);
                Debug.Log("Forward");
            }
            else if (pos.z > target.z && pos.z - target.z > slowdown_buffer.z)
            {
                kinematic.SetDesiredSpeed(-speed);
                Debug.Log("Backward");
            } else if (Mathf.Abs(pos.z - target.z) < slowdown_buffer.z)
            {
                kinematic.SetDesiredSpeed(0);
                Debug.Log("Stopping Straight");
            }

            Vector3 toTarget = (target - pos).normalized;
            Vector3 forward = transform.forward;
            if (kinematic.speed < 0) forward *= -1;
            if (Vector3.Dot(forward, toTarget) > 0.6)
            {
                kinematic.SetDesiredRotationalVelocity(0);
                Debug.Log("Stopping Rotation");
            } else if (pos.x < target.x)
            {
                kinematic.SetDesiredRotationalVelocity(velocity);
                Debug.Log("Rotating Right");
            }
            else if (pos.x > target.x)
            {
                kinematic.SetDesiredRotationalVelocity(-velocity);
                Debug.Log("Rotating Left");
            }
        }
    }

    public void SetTarget(Vector3 target)
    {
        this.target = target;
        EventBus.ShowTarget(target);
    }

    public void SetPath(List<Vector3> path)
    {
        this.path = path;
    }

    public void SetMap(List<Wall> outline)
    {
        this.path = null;
        this.target = transform.position;
    }
}
