using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

public class legs : MonoBehaviour
{

    [Header("balance stats")] // handles mass and support
    
    public float support_per_leg; // how much weight each leg offsets
    public float MASS;

    [Header("movement stats")] // all the movement controls

    
    public float speed;
    public float jump_hieght;
    public float sprint_mult_speed;
    public bool sprint;
    public float leg_speed = 6;

    [Header("weapon assist stats")] // weapon assistance

    public float recoil_assist_vertical;
    public float recoil_assist_retation;

    public leg_manager[] legser;

    float ST;
    
    public float step_interval; // need to impliment idle
    public string mode; // foreward,backward,idle
    
    Rigidbody2D rb;

    public Vector2 init_pos;

    public float integrity;
    public float tot_integrity;

    bool rg = false;

    public torso tors; // needs intefrity

    public BREAK br;

    public void INIT(torso t){
        br = GetComponent<BREAK>();
        if(t != null){tors = t;}
        rb = gameObject.GetComponent<Rigidbody2D>();
        legser = gameObject.GetComponentsInChildren<leg_manager>();
        for(int i = 0; i < legser.Length; i++){
            legser[i].INIT(); // initialises the legs
            legser[i].l = gameObject.GetComponent<legs>();
        }
        mode = "idle";
        transform.localPosition = Vector3.zero;
        init_pos = transform.localPosition;
    }

    void Update()
    {
        move();
        check_legs();

        // integrity analysis
        float integ = 0;
        foreach(leg_manager l in legser){
            integ += l.integrity;
        }
        tot_integrity = (integ+integrity) / (legser.Length+1);
        health_manager();
    }

    void health_manager(){
        bool break_check = false;
        float integ = 0;
        
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

    void check_legs(){
        // moves legs when far away
        for(int i = 0; i < legser.Length; i++){
            legser[i].mode = mode;
            if(legser[i].check()){ // if we have stepped and need to step again
                if(legser[i].paired.STEP || legser[i].paired.middair){
                    leg_manager l = legser[i];
                    if(l.paired.check() && !l.paired.middair){
                        // further away leg gets priority
                        float dist = Vector2.Distance(l.transform.position,transform.position);
                        float diste = Vector2.Distance(l.paired.transform.position,transform.position);
                        if(diste > dist){l = l.paired;}
                    }
                    l.mode = mode;
                    l.step(false);
                }
            }
        }
    }

    void move(){
        if(mode == "idle"){
            if(ST < Time.time){
                ST = Time.time + step_interval;
                for(int i = 0; i < legser.Length; i++){
                    if(legser[i].mode != "idle"){
                        legser[i].mode = "idle";
                        legser[i].step(false);
                        i = legser.Length;
                    }
                }
            }
        }
    }

    public Vector2 velocitiser(Vector2 inp){
        // manages velocity
        inp = inp*speed;
        if(sprint){inp = inp*sprint_mult_speed;}
        inp = inp * tot_integrity;
        if(float.IsNaN(inp.x)){
            inp = Vector2.zero;
        }
        return inp;
    }

    public void rgdoll(){ // arms break when ragdolling
        HingeJoint2D hinge = transform.GetComponent<HingeJoint2D>();
        foreach(leg_manager le in legser){le.rgdoll();le.breaker(br);}
        if(rg){
            // stops being ragdoll
            rg = false;
            mode = "idle";
            rb.isKinematic = true;
            rb.angularVelocity = 0;
            hinge.enabled = false;
        }
        else{
            // starts being ragdoll
            rg = true;
            rb.isKinematic = false;
            rb.velocity = rb.velocity;
            hinge.enabled = true;
        }
    }

    public void remove_limb(BREAK br){
        br.transform.parent = null;
        HingeJoint2D hinge = br.GetComponent<HingeJoint2D>();
        hinge.enabled = false;
    }
}