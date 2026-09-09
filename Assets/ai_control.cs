using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ai_control : MonoBehaviour
{

    // testing and then mapping
    // make min hieght just be average des_hieght
    // make hieght not change instantly

    public Rigidbody2D rb;
    Camera cam;

    public bool death;
    bool shat;

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
        if(death){
            if(!rg){rgdoll();}
        }
        else{
            BREAK[] br = GetComponentsInChildren<BREAK>();
            foreach(BREAK b in br){
                if(b.vital && (b.shattered || b.broken)){
                    death = true;
                    if(b.shattered){
                        shat = true;
                    }
                }
                if(shat){b.shattered = true;}
            }
            if(rg){rgdoll();}
        }
    }

    public void ragdollser(){
        if(Input.GetKeyDown(KeyCode.K)){ // can only unragdoll if balanced
            rgdoll();
        }
    }

    void rgdoll(){ // arms break when ragdolling
        if(!rg || tors.recovered){ // if we are not ragdolled or recovered
            if(leg != null){leg.rgdoll();}
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

    public void gunner(string type){
        GameObject[] sel_weps = weps_1;
        GameObject[] holstered_weps = weps_2;
        if(sel == 2){
            sel_weps = weps_2;
            holstered_weps = weps_1;
        }
        if(sel_weps != null){
            if(type == "from"){
                for(int i = 0; i < 2; i++){
                    //sel_weps[i].GetComponent<Rigidbody2D>.
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
                    if(shoul != null){sel_wep.assign_hand(shoul.arms[i].GetComponent<arm>());}
                    
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
