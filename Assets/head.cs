using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

public class head : MonoBehaviour
{

    [Header("mini-map stats")] // mini-map stuff

    public float placeholder_1;

    [Header("discount stats")] // discounts and such

    public float placeholder_2;

    [Header("extra stats")] // mass and fine motor control

    public float MASS;
    public float Fine_Control;
    public float cooldown_assistance;

    bool rg = false;
    Rigidbody2D rb;

    public float integrity;

    public BREAK hit_point;
    torso tors;

    public void INIT(torso t){
        if(t != null){tors = t;}
        hit_point = gameObject.GetComponent<BREAK>();
        rb = gameObject.GetComponent<Rigidbody2D>();
        transform.parent = tors.neck.transform;
        transform.position = transform.parent.position;
    }

    void Update(){
        health_manager();
    }

    void health_manager(){
        bool break_check = false;
        float integ = 0;
        
        BREAK br = hit_point;
        if(br.broken){ // arm needs to go limp
            break_check = true;
        }
        if(br.shattered){ // limb needs to fall off
            // we assume at this point the limb is ragdolled
            br.broken = true;
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

        integrity = integ;

        if(break_check){
            integrity = 0;
            if(!rg){
                rgdoll();
            }
        }
    }

    public void rgdoll(){ // arms break when ragdolling
        HingeJoint2D hinge = GetComponent<HingeJoint2D>();
        Collider2D col = GetComponent<Collider2D>();
        if(rg){
            // stops being ragdoll
            rg = false;
            rb.isKinematic = true;
            rb.angularVelocity = 0;
            hinge.enabled = false;
            col.isTrigger = true;
        }
        else{
            // starts being ragdoll
            rg = true;
            rb.isKinematic = false;
            hinge.enabled = true;
            col.isTrigger = false;
        }
    }

    public void remove_limb(BREAK br){
        br.transform.parent = null;
        HingeJoint2D hinge = br.GetComponent<HingeJoint2D>();
        hinge.enabled = false;
    }
}