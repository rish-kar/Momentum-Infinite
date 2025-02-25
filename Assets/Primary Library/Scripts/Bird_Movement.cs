using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bird_Movement : MonoBehaviour
{

    private float speed;

    private Transform target;
    private int wavepointIndx = 0;

    private int _cacheVal;

    void Start()
    {
        target = Waypoints.waypoints[0];
        speed = Random.Range(100.0f, 150.0f);
    }

    private void Update()
    {
        _cacheVal++; //temporary cache variable

        if((int)Time.time * Time.deltaTime % 5 == 0) //within 5 second interval vary speed
        {
            float sp1 = Random.Range(4.0f, 15.0f);
            float sp2 = Random.Range(2500.0f, 5000.0f);

            int randVal = Random.Range(1, 100);

            if (randVal % 2 == 0)
            {
                speed = sp1; //varies speed 
                Debug.Log("Val 0");
                
            }
            else if (randVal % 2 != 0)
            {
               speed = sp2; //varies speed
                Debug.Log("Val 1");
            }
        }

        Vector3 dir = target.position - transform.position;
        transform.Translate(dir.normalized * Random.Range(10.0f, 15.0f) * Time.deltaTime, Space.World); //Move bird towards waypoint
        

        if (Vector3.Distance(transform.position, target.position) <= 0.2f)
        {
            GetNextWayPoint();
        }

        if(wavepointIndx == 1)
        { 
            DestroyObject(target.position.z);
        }

    }

    void GetNextWayPoint()
    {
        wavepointIndx++;
        target = Waypoints.waypoints[wavepointIndx];
    }

    void DestroyObject(float target)
    {
        if (this.gameObject.transform.position.z >= target-1)
        {
            DestroyImmediate(this.gameObject);
        }
    }

}
