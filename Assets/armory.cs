using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

public class armory : MonoBehaviour
{
    public GUN_CONT[] weapons;


    public GameObject[] INIT_to_hand(GameObject shoulder){
        GameObject[] wep_obj = new GameObject[weapons.Length];
        for(int i = 0; i < weapons.Length; i++){
            wep_obj[i] = Instantiate(weapons[i].gameObject,shoulder.transform.position,shoulder.transform.rotation);
        } // spawns weapons

        arm[] targets = shoulder.transform.parent.GetComponentsInChildren<arm>();

        int wep_per_hand = targets.Length / weapons.Length;

        // do another day

        for(int i = 0; i < targets.Length; i++){
            GUN_CONT sel = weapons[0];
            if(i >= wep_per_hand && i < weapons.Length){sel = weapons[1];}
            sel.assign_hand(targets[i]);
        }

        for(int i = 0; i < wep_obj.Length; i++){
            // GameObject h = Lhand;
            // if(i > 0){h = Rhand;}
            wep_obj[i].GetComponent<GUN_CONT>().state = "ready";
            
        }

        return wep_obj;
    }

    public GameObject[] INIT_to_holster(GameObject hip){
        GameObject[] wep_obj = new GameObject[weapons.Length];
        for(int i = 0; i < weapons.Length; i++){
            wep_obj[i] = Instantiate(weapons[i].gameObject,hip.transform.position,hip.transform.rotation);
        } // spawns weapons

        foreach(GameObject g in wep_obj){
            GUN_CONT ge = g.GetComponent<GUN_CONT>();
            ge.state = "holstered";
        }
        return wep_obj;
    }
}
