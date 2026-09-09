using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

public class leg_manager : MonoBehaviour
{
    public legs l;
    public LayerMask mask;
    public GameObject target;
    public Vector3 foot_pos = Vector3.zero; // where we want the foot to go

    public float idle_pos_x;
    public float step_pos_x;
    public float backstep_pos_x;
    public float step_dist;

    public float step_pos_y;

    public string mode; // "foreward","backwar","idle"

    public float max_length;
    public float desired_hieght;
    
    public float limb_length;
    public float velocity_scale = 0.5f;

    public int dir = -1; // this is where we face
    public int orientation = 1; // this is what side the limb is on

    bool raise_check = false;
    float raise_dist;
    
    public bool can_climb;

    public bool middair;
    public bool STEP = false;
    public bool onwall;

    public leg_manager paired;

    public float norm_ange;

    bool rg;

    public float integrity;

    public BREAK[] hit_points; 

    public void INIT()
    {
        hit_points = gameObject.GetComponentsInChildren<BREAK>();
        //set_val();
        mode = "idle";
        target.transform.parent = null;
        step(false);
    }

    public void set_val(){
        desired_hieght = Vector2.Distance(new Vector2(transform.localPosition.y,0),new Vector2(target.transform.localPosition.y,0));
        HingeJoint2D[] joints = gameObject.GetComponentsInChildren<HingeJoint2D>();
        for(int i = 0; i < joints.Length; i++){
            max_length += Vector2.Distance(joints[i].transform.localPosition,joints[i].transform.GetChild(0).localPosition);
        }
        limb_length = Vector2.Distance(joints[0].transform.localPosition,joints[0].transform.GetChild(0).localPosition);
    }

    void Update()
    {
        // move target to target pos 
        if(raise_check){
            if(raise_dist+step_pos_y < target.transform.position.y){
                raise_check = false;
                foot_pos.y -= step_pos_y;
            }
        }
        else{
            if(Vector3.Distance(target.transform.position,foot_pos) < 0.05f){
                STEP = true;
            }
        }
        float v_mult = Mathf.Abs((l.tors.rb.velocity.x*velocity_scale))+1;
        float speed = (l.leg_speed*integrity);
        speed = speed * (l.tors.head.Fine_Control*l.tors.head.integrity);
        target.transform.position = Vector3.Lerp(target.transform.position,foot_pos,(Time.deltaTime*speed)*v_mult);
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
            integrity = 0;
            if(!rg){
                rgdoll();
            }
        }
    }

    // Returns the local-space X offset added on top of idle_pos_x for the current mode
    float get_step_offset()
    {
        switch (mode)
        {
            case "foreward": return step_pos_x;
            case "backward": return backstep_pos_x;
            default:         return 0f; // idle
        }
    }
 
    public void step(bool inst) // still fucked, FIX
    {
        onwall = false;

        STEP = inst;
        Rigidbody2D rb = l.tors.GetComponent<Rigidbody2D>();

        float world_target_x = transform.position.x + ((idle_pos_x + get_step_offset()) * -dir);

        float world_body_x = transform.position.x; // our position

        float decayFactor = 1f - Mathf.Exp(-rb.drag * velocity_scale);
        world_target_x += ((rb.velocity / rb.drag) * decayFactor).x; // supposedly this should predict us
        
        //float range_min = Mathf.Min(world_body_x, world_target_x);
        //float range_max = Mathf.Max(world_body_x, world_target_x);

        float range_min = world_body_x;
        float range_max = world_target_x;

        const int RAY_COUNT = 11;

        Vector3 fallback = Vector3.zero;
 
        for (int i = 0; i < RAY_COUNT; i++)
        {
            // Fan rays outward in dir, sweeping downward as i increases
            float ray_x = orientation - (orientation * 0.1f * i);
            float ray_y = -0.1f * i;
  
            RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(ray_x, ray_y), max_length, mask);

            if(hit.collider != null){
                bool in_range = false;
                //in_range = hit.point.x >= range_min && hit.point.x <= range_max;
                
                float dist_1 = Mathf.Abs(range_min-range_max); // distance between body and max range
                float dist_2 = Mathf.Abs(range_min-hit.point.x); // distance between body and hit point
                in_range = dist_2 <= dist_1;
                if (!in_range) // it is in the valid range
                    continue;

                float normal_angle = Vector2.Angle(hit.normal, Vector2.up);
                bool  is_floor     = normal_angle < 90f;
                norm_ange = normal_angle;
                if (is_floor)
                {
                    place_foot(hit.point, liftFoot: !inst);
                    middair = false;
                    return;
                }
        
                // Wall — only place foot here if climbing is allowed and it's closer to the body
                if (can_climb)
                {
                    place_foot(hit.point, liftFoot: false);
                    middair = false;
                    return;
                }
                else{
                    fallback = hit.point;
                }
            }
        }
        if(fallback != Vector3.zero){ // if no valid floor pos is found it will try to grab onto the fall before the floor
            STEP = true;
            onwall = true;
            middair = false;
            place_foot(fallback,liftFoot: false);
            norm_ange = 0;
        }
        else{
            norm_ange = 0;
            STEP = false;
            middair = true;
            place_foot(new Vector2(transform.position.x,transform.position.y-max_length), liftFoot:false);
        }
    }
 
    // Sets foot_pos to pos. If liftFoot is true, arcs the foot up by step_pos_y first.
    void place_foot(Vector2 pos, bool liftFoot)
    {
        foot_pos    = pos;
        raise_check = liftFoot;
        if (liftFoot)
        {
            // distance from body
            raise_dist = foot_pos.y;
            foot_pos.y  += step_pos_y * 1.1f;
        }
    }

    public bool check()
    {
        if(middair){return true;}
        // Has the foot drifted too far from its idle X position?
        float world_idle_x = (idle_pos_x*-dir) + transform.position.x;
        
        if (Mathf.Abs(world_idle_x - target.transform.position.x) > step_dist){
            return true;
        }

        // Is the foot further than the leg can reach?
        if (Vector2.Distance(transform.position, target.transform.position) > max_length){
            return true;
        }
 
        // Is the line to the foot obstructed (e.g. leg clipping through a wall)?
        RaycastHit2D hit = Physics2D.Raycast(transform.position, foot_pos - transform.position, max_length, mask);
        if (hit.collider == null){
            return true;
        }
 
        // need a check to see if leg is lower than us to help climbing
        //if(STEP && Mathf.Abs(target.transform.position.y-transform.position.y) > max_length/2)
            //return true;
        if(STEP){
            if(target.transform.position.y > transform.position.y){
                return true; // foot is above head, major social faux pas
            }
        }

        return false;
    }

    public bool bal(){
        if(target.transform.position.y > transform.position.y){
            //return false; // for now we allow
        }
        bool check = false;
        if(dir == -1){
            // we are facing right
            if(target.transform.position.x < transform.position.x){check = true;}
        }
        else{
            // we are facing left
            if(target.transform.position.x > transform.position.x){check = true;}
        }

        if(check){
            // foot is behind us
            if(Mathf.Abs(transform.position.x-target.transform.position.x) > limb_length){
                return false;
            }
        }

        return true;
    }

    public void flip(){
        dir = -dir;
        orientation = -orientation;
        step(true);
    }

    public void rgdoll(){ // arms break when ragdolling
        Rigidbody2D[] rbs = GetComponentsInChildren<Rigidbody2D>();
        IKManager2D[] man = transform.GetComponentsInChildren<IKManager2D>();
        HingeJoint2D[] hinge = transform.GetComponentsInChildren<HingeJoint2D>();
        Collider2D[] col = GetComponentsInChildren<Collider2D>();
        if(rg){
            // stops being ragdoll
            rg = false;
            mode = "idle";
            for(int i = 0; i < rbs.Length; i++){
                rbs[i].isKinematic = true;
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
            step(true);
        }
        else{
            // starts being ragdoll
            STEP = false;
            onwall = false;
            middair = true;
            rg = true;
            for(int i = 0; i < rbs.Length; i++){
                rbs[i].isKinematic = false;
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
    }

    public void remove_limb(BREAK br){
        br.transform.parent = null;
        HingeJoint2D hinge = br.GetComponent<HingeJoint2D>();
        hinge.enabled = false;
        STEP = false;
    }

    public void breaker(BREAK br){
        foreach(BREAK b in hit_points){
            if(br.broken){
                b.broken = true;
            }
        }
    }
}