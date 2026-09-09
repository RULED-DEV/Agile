using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

public class CONTROL : MonoBehaviour
{

    // testing and then mapping
    // make min hieght just be average des_hieght
    // make hieght not change instantly

    public Rigidbody2D rb;
    Camera cam;

    bool rg = false;
    bool eq = false;

    public legs leg;
    public torso tors;
    public shoulder shoul;
    public head head;

    string weapon_status = "ready"; // ready, aiming, swapping

    float des_orientation;
    Vector3 des_angle;

    int sel = 1;

    public GameObject[] weps_1;
    public GameObject[] weps_2;

    public GameObject[] aimer = new GameObject[2];

    public void INIT(armory w1, armory w2){
        aimer[0] = transform.GetChild(0).GetChild(0).GetChild(0).gameObject; // returns both aim points
        aimer[1] = transform.GetChild(0).GetChild(0).GetChild(1).gameObject; // returns both aim points
        cam = FindObjectOfType<Camera>();
        leg = gameObject.GetComponentInChildren<legs>();
        tors = gameObject.GetComponentInChildren<torso>();
        shoul = GetComponentInChildren<shoulder>();
        head = GetComponentInChildren<head>();

        float ms = 0;

        if(w1 != null){
            if(shoul != null){
                weps_1 = w1.INIT_to_hand(shoul.gameObject);
            }
        }
        if(w2 != null){
            if(tors != null){
                weps_2 = w2.INIT_to_holster(tors.gameObject);
            }
        }

        foreach(GameObject g in weps_1){ms += g.GetComponent<Rigidbody2D>().mass;}
        foreach(GameObject g in weps_2){ms += g.GetComponent<Rigidbody2D>().mass;}

        if(leg != null){leg.INIT(tors); ms += leg.MASS;}
        if(tors != null){tors.INIT(leg,head,shoul); ms += tors.MASS;}
        if(shoul != null){shoul.INIT(tors); ms += shoul.MASS;}
        if(head != null){head.INIT(tors); ms += head.MASS;}

        rb.mass = ms;
        
        BREAK[] v = gameObject.GetComponentsInChildren<BREAK>();
        //VANITY[] b = gameObject.GetComponentsInChildren<VANITY>();
        for(int i = 0; i < v.Length; i++){
            //b[i].INIT();
            v[i].INIT();
        }
        
        rgdoll();
    }

    void Update(){
        ragdollser();
        COMM_camera();
        if(!rg){
            COMM_swap();

            if(leg != null){ // leggy stuff
                COMM_jump();
                COMM_VEL();
                COMM_elevate();
                COMM_flip();
            }
            
            if(eq && shoul != null){ // gunny stuff
                if(tors != null){COMM_AIM();}
                COMM_FIRE();
            }
        }
        else{
            if(shoul != null && tors != null){COMM_point_rg();}
            if(Input.anyKeyDown && !Input.GetKeyDown(KeyCode.K) && !Input.GetMouseButton(0) && !Input.GetMouseButton(1)){
                rgdoll(); // makes it more intuitive to deragdoll
            }
        }
    }

    void COMM_camera(){
        cam.transform.position = Vector3.Lerp(cam.transform.position,tors.transform.position,Time.deltaTime*2);
        cam.transform.position = new Vector3(cam.transform.position.x,cam.transform.position.y,-10);
    }

    void COMM_jump(){
        if(Input.GetKey(KeyCode.Space)){
            
            bool check = true;

            if(tors.groundedness == "middair"){check = false;}
            
            if(tors.recovered){check = false;} // support too low

            float jump_force = leg.jump_hieght*leg.tot_integrity;

            if(check){
                if(tors.groundedness == "grounded"){
                    rb.AddForce(Vector2.up*(jump_force*-(tors.add_hieght-1)),ForceMode2D.Impulse);
                }
                else{ // doesnt work, i dont particuarly care tho
                    rb.AddForce(Vector2.up*(jump_force*-(tors.add_hieght-1))/1.5f,ForceMode2D.Impulse); // less hieght
                    rb.AddForce(Vector2.right*tors.transform.localScale.x*(-rb.velocity.x*1.5f),ForceMode2D.Impulse); // less hieght
                }
            }
        }
    }

    void COMM_flip(){
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = cam.ScreenToWorldPoint(mousePosition);
        if(mousePosition.x > tors.transform.position.x && tors.transform.localScale.x != 1){
            tors.transform.localScale = new Vector2(1,1);
            leg_manager[] l = GetComponentsInChildren<leg_manager>();
            foreach(leg_manager le in l){
                le.flip();
            }

            for(int i = 0; i < 2; i++){
                if(i < weps_1.Length){weps_1[i].transform.localScale = new Vector3(-1,1,1);}
                if(i < weps_2.Length){weps_2[i].transform.localScale = new Vector3(-1,1,1);}
            }
        }
        if(mousePosition.x < tors.transform.position.x && tors.transform.localScale.x != -1){
            GameObject[] w = weps_1;
            if(sel != 1){w = weps_2;}
            tors.transform.localScale = new Vector2(-1,1);
            leg_manager[] l = GetComponentsInChildren<leg_manager>();
            foreach(leg_manager le in l){
                le.flip();
            }
            for(int i = 0; i < 2; i++){
                if(i < weps_1.Length){weps_1[i].transform.localScale = new Vector3(1,1,1);}
                if(i < weps_2.Length){weps_2[i].transform.localScale = new Vector3(1,1,1);}
            }
        }
    }

    void COMM_elevate(){
        if(Input.GetKey(KeyCode.W)){
            tors.add_hieght = Mathf.Lerp(tors.add_hieght,1,Time.deltaTime*2);
        }
        else if(Input.GetKey(KeyCode.S)){
            tors.add_hieght = Mathf.Lerp(tors.add_hieght,-1,Time.deltaTime*2);
        }
        else{
            tors.add_hieght = Mathf.Lerp(tors.add_hieght,0,Time.deltaTime*2f);
        }
    }

    void COMM_VEL(){ // potential issue with movephys, main body can rotate causing fuckiness
        Vector2 inp_vect = Vector2.zero;
        leg.sprint = false;
        if(Input.GetKey(KeyCode.LeftShift)){leg.sprint = true;}
        if(Input.GetKey(KeyCode.A)){
            inp_vect = -Vector2.right;
            if(tors.transform.localScale.x > 0){
                leg.mode = "backward";
            }
            else{
                leg.mode = "foreward";
            }
        }
        if(Input.GetKey(KeyCode.D)){
            inp_vect = Vector2.right;
            if(tors.transform.localScale.x > 0){
                leg.mode = "foreward";
            }
            else{
                leg.mode = "backward";
            }
        }
        if(inp_vect != Vector2.zero){
            rb.AddForce(leg.velocitiser(inp_vect));
        }
    }

    void COMM_AIM(){ 
        GameObject[] w = weps_1;
        if(sel != 1){w = weps_2;}

        if(Input.GetMouseButton(1)){ // this should be updated for mult points
            for(int i = 0; i < aimer.Length; i++){
                int ie = i;
                if(ie >= w.Length){ie = w.Length-1;}
                aimer[i].transform.localPosition = new Vector3(0,w[ie].GetComponent<GUN_CONT>().depression,0);

                float d = shoul.arms[i].arm_length;
                d = d - w[ie].GetComponent<GUN_CONT>().hold_dist;

                // d is now the max distance that we could hold the weapon at

                d = d * w[ie].GetComponent<GUN_CONT>().hold_perc;

                aimer[i].transform.GetChild(0).localPosition = new Vector3(0,d,0);

                Vector3 mousePosition = Input.mousePosition;
                mousePosition = cam.ScreenToWorldPoint(mousePosition);
                aimer[i].transform.up = mousePosition - aimer[i].transform.position;
                aimer[i].transform.eulerAngles = new Vector3(0,0,aimer[i].transform.eulerAngles.z);
            }
        }
        else{
            // assume a resting pos
            for(int i = 0; i < aimer.Length; i++){
                int ie = i;
                if(ie >= w.Length){ie = w.Length-1;}
                aimer[i].transform.localPosition = new Vector3(0,w[ie].GetComponent<GUN_CONT>().rest_depression,0);

                float d = shoul.arms[i].arm_length;
                d = d - w[ie].GetComponent<GUN_CONT>().hold_dist;

                // d is now the max distance that we could hold the weapon at

                d = d * w[ie].GetComponent<GUN_CONT>().rest_dist;

                aimer[i].transform.GetChild(0).localPosition = new Vector3(0,d,0);

                float dir = -transform.localScale.x;

                aimer[i].transform.eulerAngles = new Vector3(0,0,w[ie].GetComponent<GUN_CONT>().rest_ang * dir);
            }
        }
    }

    void COMM_FIRE(){ // effects look wack, you should kill yourself ?
        if(Input.GetMouseButton(0)){
            // wants to fire, for now we assume weapon is ready
            GameObject[] w = weps_1;
            if(sel != 1){w = weps_2;}

            foreach(GameObject g in w){
                g.GetComponent<GUN_CONT>().fire_manager();
            }
        }
    }

    void COMM_point_rg(){
        if(Input.GetMouseButton(1)){ // this should be updated for mult points
            GameObject[] w = weps_1;
            if(sel != 1){w = weps_2;}
            for(int i = 0; i < aimer.Length; i++){
                Vector3 mousePosition = Input.mousePosition;
                mousePosition = cam.ScreenToWorldPoint(mousePosition);
                aimer[i].transform.up = mousePosition - aimer[i].transform.position;
                aimer[i].transform.eulerAngles = new Vector3(0,0,aimer[i].transform.eulerAngles.z);
                // points pointer
                
                for(int ii = 0; ii < shoul.arms.Length; ii++){
                    int ie = ii;
                    if(ie >= w.Length){ie = w.Length-1;}
                    shoul.arms[ii].aim_rg(aimer[i],w[ie]);
                }

            }
        }
    }

    void COMM_swap(){
        if(weapon_status != "swapping"){
            if(Input.GetKeyDown(KeyCode.Q)){
                // if Q is pressed
                weapon_status = "swapping";
                eq = false;
                GameObject[] w = weps_1;
                if(sel != 1){w = weps_2;}
                foreach(GameObject g in w){
                    GUN_CONT e = g.GetComponent<GUN_CONT>();
                    e.target = leg.gameObject; // assigns weapons to go to holster 
                }
            }
        }
        else{
            GameObject[] w = weps_1;
            GameObject[] we = weps_2;
            if(sel != 1){w = weps_2; we = weps_1;}
            // swap logic
            if(Vector2.Distance(w[0].transform.position,leg.transform.position) < 0.1f){
                // we are at the target
                foreach(GameObject g in w){
                    GUN_CONT a = g.GetComponent<GUN_CONT>();
                    a.unassign();
                    a.state = "holstered";
                }
                foreach(GameObject g in we){
                    GUN_CONT a = g.GetComponent<GUN_CONT>();
                    a.unassign();
                    if(a.state != "overheat"){a.state = "ready";}
                }

                if(sel != 1){sel = 1;}
                else{sel = 2;}

                gunner("from");
                weapon_status = "ready";
            }
        }
    }

    public void ragdollser(){
        if(Input.GetKeyDown(KeyCode.K)){ // can only unragdoll if balanced
            rgdoll();
        }
    }

    void rgdoll(){ // arms break when ragdolling
        if(!rg || tors.recovered){ // if we are not ragdolled or recovered
            if(leg != null){leg.rgdoll();} // needs to differentiate if we are dead
            if(tors != null){tors.rgdoll();}
            if(shoul != null){shoul.rgdoll();}
            if(head != null){head.rgdoll();}
            
            if(rg){
                gunner("from");
                rg = false;
            }
            else{
                gunner("to");
                rg = true;
            }
        }
    }

    public void gunner(string type){ // hand isnt attaching to gun properly after first deragdoll
        GameObject[] sel_weps = weps_1;
        GameObject[] holstered_weps = weps_2;
        if(sel == 2){
            sel_weps = weps_2;
            holstered_weps = weps_1;
        }
        if(sel_weps != null){
            if(type == "from"){
                for(int i = 0; i < 2; i++){
                    if(i < sel_weps.Length){
                        GUN_CONT g = sel_weps[i].GetComponent<GUN_CONT>();
                        g.fromragdoll(aimer[i].transform.GetChild(0).gameObject); // assigns to hands
                    }
                    if(i < holstered_weps.Length){
                        GUN_CONT g = holstered_weps[i].GetComponent<GUN_CONT>();
                        if(leg != null){g.fromragdoll(leg.gameObject);}
                    }
                }
                // make gun go to hand
                
                GameObject[] w = weps_1;
                if(sel != 1){w = weps_2;}
                int wep_per_hand = shoul.arms.Length / w.Length;
                for(int i = 0; i < shoul.arms.Length; i++){
                    GUN_CONT sel_wep = w[0].GetComponent<GUN_CONT>();
                    if(i >= wep_per_hand && i < w.Length){sel_wep = w[1].GetComponent<GUN_CONT>();}
                    if(shoul != null){sel_wep.assign_hand(shoul.arms[i]);}
                }
                eq = true;
            }
            else{ // to
                for(int i = 0; i < 2; i++){
                    if(i < sel_weps.Length){
                        GUN_CONT g = sel_weps[i].GetComponent<GUN_CONT>();
                        g.toragdoll(shoul.arms[i].hand); // assigns target
                    }
                    if(i < holstered_weps.Length){
                        GUN_CONT g = holstered_weps[i].GetComponent<GUN_CONT>();
                        if(leg != null){g.toragdoll(leg.gameObject);}
                        
                    }
                }
                eq = false;
            }
        }
    }
}
