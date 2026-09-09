using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

public class torso : MonoBehaviour
{
    public Rigidbody2D rb;
    public legs leg;
    public head head;
    public shoulder shoul;

    [Header("balance stats")]

    public float MASS; // mass of the torso
    public float balance; 
    float max_balance; // max value balance can be
    public float balance_recovery; // how quickly we recover balance
    
    public float support;

    bool unbalanced;
    public bool recovered; // are we recoverd and stable

    [Header("health stats")]

    public float recovery_incr;
    public float recovery_perc;
    public float pulse_rate;
    float pulse_time;

    [Header("weapon assist stats")]

    public float cooling_assistance; // helps weapon cooldown

    public GameObject neck;

    float grav;

    float springStrength;
    float damping;

    public Vector2 cum_hieght;
    Vector2 max_hieght;
    public float add_hieght;

    public string groundedness; // ground, wall, middair

    Vector2 des_orientation;
    Vector3 des_angle;

    bool rg = false;

    BREAK hit_point;
    public float integrity;

    float ROT_force = 200;
    float counter_force = 5;

    public void INIT(legs l, head h, shoulder s){

        max_balance = balance;
        balance = 0;

        damping = 15;
        springStrength = 200;
        grav = 10;

        hit_point = gameObject.GetComponent<BREAK>();
        
        if(l != null){leg = l;}
        if(h != null){head = h;}
        if(s != null){shoul = s;}

        rb = GetComponent<Rigidbody2D>();
        cum_hieght = Vector2.zero;
        max_hieght = cum_hieght;
    }

    void FixedUpdate(){
        health_manager();
        grav_manager();
        manage_balance();
        healing_manager();
        if(!rg){
            if(leg != null){
                balancer();
                reset_hieght(); // needs rework
            }
            COMM_hieght();
            if(leg == null || leg.br.broken || leg.br.shattered){
                rgdoll();
                if(head != null){head.rgdoll();}
                if(leg != null){leg.rgdoll();}
                if(shoul != null){shoul.rgdoll();}
            }
        }
        
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

    void healing_manager(){
        if(pulse_time < Time.time){
            pulse_time = Time.time + pulse_rate;
            BREAK[] b = gameObject.GetComponentsInChildren<BREAK>();
            foreach(BREAK be in b){
                be.heal(recovery_incr,recovery_perc);
            }
        }
    }

    void grav_manager(){
        rb.AddForce(new Vector2(0,-1)*grav*rb.mass); // applies gravity
    }

    void balancer(){
        // maybe one day we will back flip
        //if(Vector2.Angle(transform.up,Vector2.up) > 1f){ // for some reason this is really expensive
            des_orientation = new Vector2(0,1);

            float diff = Vector2.Angle(transform.up,des_orientation);
            float meas_Tar_ang = 0; // physics based rather than physics cringe
            float meas_tra_ang = transform.eulerAngles.z;
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

            diff += (rb.angularVelocity*Time.deltaTime); // adds how far we are predicted to move this timestep

            float force = ROT_force*rb.mass;
                
            if(diff > 0){
                // this predicts we will be overshooting
                force = diff;
            }
            if(diff < force){
                // if rot force would cause an overshoot
                force = Mathf.Abs(diff-force);
            }
            force = Mathf.Clamp(force,-ROT_force,ROT_force); // prevents us exceeding the force limit
            rb.AddTorque(force*dir);

            if((dir > 0 && rb.angularVelocity+(force*dir) < 0) || dir < 0 && rb.angularVelocity+(force*dir) > 0){
                // we are travelling in the wrong direction
                float mag = Mathf.Abs(rb.angularVelocity);
                force = Mathf.Clamp(force,-mag,mag);
                // applies counterforce
                rb.AddTorque((force*dir)*counter_force);
            }

            if(1.0f / Time.unscaledDeltaTime < 5){
                // if we are lagging HARD
                transform.up = des_orientation;
            }

            if(leg != null && !leg.br.broken){
                leg.transform.localEulerAngles = Vector3.zero;
            }
            if(head != null && !head.hit_point.broken){
                head.transform.localEulerAngles = Vector3.zero;
            }
        //}
    }

    void manage_balance(){
        if(unbalanced){ // when unbalanced we decrease
            float ba = 0.5f;
            if(shoul != null){ba = shoul.balance_assist;}
            balance = Mathf.Lerp(balance,-1,Time.deltaTime*ba);
        }
        else{
            balance = Mathf.Lerp(balance,max_balance,Time.deltaTime*balance_recovery);
        }
        if(balance < 0){
            // ragdoll
            unbalanced = false;
            recovered = false;
        }
        else if(Mathf.Abs(balance-max_balance) < 0.1f){
            recovered = true;
        }
    }

    public void reset_hieght(){ // doesnt just reset hieght
        float point_hieght = 0;
        // hieght calc
        int div = 0;
        float CH = 0;

        float min = 0;
        float limb_length = 10;
        support = 0;
        max_hieght = Vector2.zero;

        cum_hieght = Vector2.zero;

        groundedness = "middair"; 

        for(int i = 0; i < leg.legser.Length; i++){
            if(leg.legser[i].integrity > 0){
                bool external_check = leg.legser[i].onwall;



                if(external_check){external_check = leg.legser[i].can_climb;} // if we are on the wall and can climb
                else{external_check = true;}
                
                if(!leg.legser[i].middair){ // are we middair?
                    if(leg.legser[i].onwall){ // are we on a wall?
                        groundedness = "wall";
                    }
                    groundedness = "grounded";
                }
                
                if(leg.legser[i].STEP && !leg.legser[i].middair && external_check){
                    div ++;
                    if(leg.legser[i].bal()){support += leg.support_per_leg;}
                    
                    //leg.legser[i].leg_speed = speed*2; // leg speed auto adjusts

                    if(limb_length > leg.legser[i].limb_length){limb_length = leg.legser[i].limb_length;} // shortest limb

                    if(max_hieght == Vector2.zero || max_hieght.x > leg.legser[i].max_length){max_hieght.x = leg.legser[i].max_length;}

                    CH += leg.legser[i].desired_hieght;
                    point_hieght += leg.legser[i].target.transform.position.y;

                    // figure out if we are unbalanced
                }
            }
        }
        des_orientation = des_orientation / div;
            //speed += (leg.speed)*leg.sprint_mult_speed;
            //support -= (leg.support_per_leg)*leg.sprint_mult_supp; // speed increase, support decrease

        unbalanced = false;
        if(rb.mass > support){
            unbalanced = true;
        }

        point_hieght = point_hieght / div;

        CH = CH / div;
        CH = CH + point_hieght;

        cum_hieght.x = (CH + Mathf.Abs(leg.transform.position.y-transform.position.y))+limb_length;
    
        //cum_hieght.y = (CH + Mathf.Abs(leg.transform.position.y-transform.position.y))-limb_length; // lower desired hieght
        cum_hieght.y = CH + Mathf.Abs(leg.transform.position.y-transform.position.y); // lowest is desired hieght
        
        max_hieght.y = point_hieght+limb_length;// lowest hieght
        max_hieght.x = point_hieght+max_hieght.x;
        if(cum_hieght.x > max_hieght.x){cum_hieght.x = max_hieght.x;}
        if(cum_hieght.y < max_hieght.y){cum_hieght.y = max_hieght.y;}

        if(float.IsNaN(cum_hieght.x)){
            cum_hieght = Vector2.zero;
            max_hieght = cum_hieght;
        }
    }

    void COMM_hieght(){ // need to improve clambering
        if(cum_hieght != Vector2.zero){ // stops us from getting abducted
            float MtS = support/rb.mass;

            if(MtS < 0){MtS = 0;} // min hieght
            if(MtS > 1){MtS = 1;} // max hieght
            
            float hieght_diff = Mathf.Abs(cum_hieght.x - cum_hieght.y);
            
            float des_hieght = cum_hieght.y+(hieght_diff*MtS); 

            float tar = max_hieght.x;
            if(add_hieght < 0){tar = max_hieght.y;}

            des_hieght = des_hieght + (tar-des_hieght)*Mathf.Abs(add_hieght); // needs to be fixed aswell
            
            des_hieght -= rb.position.y;
            
            if(des_hieght == 0){des_hieght = cum_hieght.x;}
            float vert = rb.velocity.y;

            float correction = (des_hieght * springStrength) - (vert * damping);
            if(correction+(grav*rb.mass) > 0){
                rb.AddForce(new Vector2(0,correction+(grav*rb.mass)), ForceMode2D.Force);
            }
        }
    }

    public void rgdoll(){ // arms break when ragdolling
        Rigidbody2D[] rbs = GetComponentsInChildren<Rigidbody2D>();
        HingeJoint2D[] hinge = transform.GetComponentsInChildren<HingeJoint2D>();
        Collider2D[] col = GetComponentsInChildren<Collider2D>();
        if(rg){
            // stops being ragdoll
            rg = false;
            for(int i = 0; i < rbs.Length; i++){
                if(rbs[i] != rb){
                    rbs[i].isKinematic = true;
                    rbs[i].angularVelocity = 0;
                }
            }
            for(int i = 0; i < hinge.Length; i++){
                hinge[i].enabled = false;
            }
            for(int i = 0; i < col.Length; i++){
                if(col[i].transform != transform){
                    col[i].isTrigger = true;
                }
            }
            rb.isKinematic = false; // just in case
        }
        else{
            balance = 0;
            // starts being ragdoll
            rg = true;
            for(int i = 0; i < rbs.Length; i++){
                if(rbs[i] != rb){
                    rbs[i].isKinematic = false;
                    rbs[i].velocity = rb.velocity;
                }
            }
            for(int i = 0; i < hinge.Length; i++){
                hinge[i].enabled = true;
            }
            for(int i = 0; i < col.Length; i++){
                if(col[i].transform.parent != transform){
                    col[i].isTrigger = false;
                }
            }
        }
    }
}
