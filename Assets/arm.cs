using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

public class arm : MonoBehaviour
{
    public GameObject hand;
    public GameObject target;

    public float arm_length;

    public Rigidbody2D[] rbs;
    public shoulder shoul;

    public float integrity;

    public bool rg;

    public BREAK[] hit_points; 

    public void INIT()
    {
        hit_points = gameObject.GetComponentsInChildren<BREAK>();
        arm_length = 0;
        Transform[] gs = transform.GetChild(0).GetComponentsInChildren<Transform>();
        foreach(Transform g in gs){
            if(g.transform.childCount != 0){
                // if it has a son
                arm_length += Vector2.Distance(g.transform.position,g.transform.GetChild(0).position);
            }
        }
        rbs = gameObject.GetComponentsInChildren<Rigidbody2D>();
    }

    void Update(){
        health_manager();
    }

    void health_manager(){
        bool break_check = false;
        float integ = 0;
        
        for(int i = 0; i < hit_points.Length; i++){
            BREAK br = hit_points[i];
            if(br.broken){ // arm needs to go limp
                break_check = true;
            }
            if(br.shattered){ // limb needs to fall off
                // we assume at this point the limb is ragdolled
                foreach(BREAK b in hit_points){b.broken = true;}
                if(rg){
                    remove_limb(br);
                }
            }
            if(br.HP > br.func_threshold){
                integ ++; // at full functionality
            }
            else{
                integ += (br.HP - br.broken_threshold) / (br.func_threshold - br.broken_threshold);
            }
        }

        integrity = integ/hit_points.Length;

        if(break_check){
            if(!rg){
                rgdoll();
            }
        }
    }

    public void aim_rg(GameObject g,GameObject gun){
        if(!rg){
            foreach(Rigidbody2D r in rbs){
                float diff = Vector2.Angle(r.transform.right,g.transform.up);
                float meas_Tar_ang = g.transform.eulerAngles.z; 
                float meas_tra_ang = r.transform.eulerAngles.z;
                meas_Tar_ang += 90; // fixes it somehow
                if(Mathf.Abs(meas_tra_ang-meas_Tar_ang) > 180){ // if they are not on the same side
                    if(meas_tra_ang < 180 && meas_Tar_ang > 180){
                        meas_Tar_ang -= 360;
                    }
                    else{
                        meas_tra_ang -= 360;
                    }
                }
                int dir = 1;
                if(meas_Tar_ang < meas_tra_ang){dir = -1;}

                diff += (r.angularVelocity*Time.deltaTime); // adds how far we are predicted to move this timestep

                float force = 1; // replaced by arm strength later
                
                if(diff > 0){
                    // this predicts we will be overshooting
                    force = diff;
                }
                if(diff < force){
                    // if rot force would cause an overshoot
                    force = Mathf.Abs(diff-force);
                }
                force = Mathf.Clamp(force,-5,5); // prevents us exceeding the force limit
                r.AddTorque(force*dir);
                gun.transform.up = r.transform.right;
            }
        }
    }

    public void rgdoll(){ // arms break when ragdolling
        IKManager2D[] man = GetComponentsInChildren<IKManager2D>();
        HingeJoint2D[] hinge = GetComponentsInChildren<HingeJoint2D>();
        Collider2D[] col = GetComponentsInChildren<Collider2D>();
        if(hinge.Length > 0){hinge[0].connectedBody = transform.parent.GetComponent<Rigidbody2D>();}
        
        if(rg){
            // stops being ragdoll
            rg = false;
            for(int i = 0; i < rbs.Length; i++){
                rbs[i].bodyType = RigidbodyType2D.Kinematic;
                rbs[i].angularVelocity = 0;
            }
            for(int i = 0; i < man.Length; i++){
                man[i].enabled = true;
            }
            for(int i = 0; i < hinge.Length; i++){
                hinge[i].enabled = false;
            }
            for(int i = 0; i < col.Length; i++){
                col[i].isTrigger = true;
            }
        }
        else{
            // starts being ragdoll
            rg = true;
            for(int i = 0; i < rbs.Length; i++){
                rbs[i].bodyType = RigidbodyType2D.Dynamic;
            }
            for(int i = 0; i < man.Length; i++){
                man[i].enabled = false;
            }
            for(int i = 0; i < hinge.Length; i++){
                hinge[i].enabled = true;
            }
            for(int i = 0; i < col.Length; i++){
                col[i].isTrigger = false;
            }
        }
        reiniter.Instance.StartCoroutine(ReactivateObject());
        // so that the object can remember to follow us
    }

    public IEnumerator ReactivateObject() // this works now
    {
        Vector3[] v = new Vector3[rbs.Length];
        float[] r = new float[rbs.Length];
        for(int i = 0; i < rbs.Length; i++){
            v[i] = rbs[i].velocity;
            r[i] = rbs[i].angularVelocity;
            rbs[i].Sleep();
            rbs[i].velocity = Vector3.zero;
            rbs[i].angularVelocity = 0;
        }

        yield return null; // wait one frame
        
        for(int i = 0; i < rbs.Length; i++){

            rbs[i].velocity = v[i];
            rbs[i].angularVelocity = r[i];
            rbs[i].Sleep();
        } 
    }

    public void remove_limb(BREAK br){
        br.transform.parent = null;
        HingeJoint2D hinge = br.GetComponent<HingeJoint2D>();
        hinge.enabled = false;
    }
}
