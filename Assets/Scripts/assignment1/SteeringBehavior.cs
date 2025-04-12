using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;
using System.IO;
using Unity.Mathematics;

public class SteeringBehavior : MonoBehaviour
{
    
    public Vector3 target;
    public KinematicBehavior kinematic;
    public List<Vector3> path;
    public int pathIndex = 0;
    // you can use this label to show debug information,
    // like the distance to the (next) target
    public TextMeshProUGUI label;

    public float speed;
    public float velocity;
    public float slowdown_buffer;
    public float path_slowdown_buffer;
    public float path_dontSlowdown_buffer;
    public float switch_buffer;
    
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

        // Check if there is a target
        if (this.path == null)
        {
            TargetFollow(target);
        } else {
            PathFollow();
        }

        // kinematic.SetDesiredSpeed(kinematic.GetMaxSpeed());
        // kinematic.SetDesiredRotationalVelocity(kinematic.GetMaxRotationalVelocity());
    }

    public void TargetFollow(Vector3 target) {
        Vector3 pos = transform.position;

        //Class example
        float dist = (target - pos).magnitude;
        Vector3 dir = target - pos;
        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

        float speed = kinematic.GetMaxSpeed();
        
        float clampedDist = Mathf.Clamp(dist, 0, slowdown_buffer) / slowdown_buffer;
        speed *= clampedDist;
        if (dist < 1f) {
            speed = 0;
        }
        kinematic.SetDesiredSpeed(speed);
        kinematic.SetDesiredRotationalVelocity(angle * angle * Math.Sign(angle));
    }

    public void PathFollow() {
        Vector3 pos = transform.position;

        if (pathIndex == path.Count - 1) {
            TargetFollow(path[pathIndex]);
        } else if (path.Count != 0) {
            float dist = (path[pathIndex] - pos).magnitude;
            Vector3 dir = path[pathIndex] - pos;
            float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

            Vector3 targetToBefore = path[pathIndex] - pos;
            Vector3 targetToAfter = path[pathIndex] - path[pathIndex+1];
            float nextAngle = Vector3.SignedAngle(targetToBefore, targetToAfter, Vector3.up);

            float speed = kinematic.GetMaxSpeed();
            float clampedDist = Mathf.Clamp(dist, path_dontSlowdown_buffer, path_slowdown_buffer) / path_slowdown_buffer;
            kinematic.SetDesiredSpeed(speed);
            kinematic.SetDesiredRotationalVelocity(Mathf.Abs(Mathf.Pow(angle, 2)) * Math.Sign(angle));

            if (dist <= -switch_buffer/180*Math.Abs(nextAngle)+switch_buffer) {
                pathIndex++;
                Debug.Log("Switched to " + pathIndex + "!");
            }
        }
    }

    public void SetTarget(Vector3 target)
    {
        this.path = null;
        this.target = target;
        EventBus.ShowTarget(target);
    }

    public void SetPath(List<Vector3> path)
    {
        this.path = path;
        this.pathIndex = 0;
    }

    public void SetMap(List<Wall> outline)
    {
        this.path = null;
        this.target = transform.position;
    }
}
