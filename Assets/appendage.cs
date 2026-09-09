using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class appendage : MonoBehaviour
{

    public GameObject target;
    public GameObject end;

    public GameObject des_point;
    public GameObject init_point;

    GameObject assigned;

    public void ASSIGN_TO(GameObject ass_obj){
        if(assigned != null){
            drop_assigned();
        }
        assigned = Instantiate(ass_obj,target.transform.position,target.transform.rotation);
        assigned.transform.parent = end.transform;
        assigned.transform.localScale = new Vector2(1,1);
    }

    public void drop_assigned(){
        // object is dropped
        assigned.AddComponent(typeof(Rigidbody2D));
        assigned.transform.parent = null;
        assigned = null;
    }

    public void ze_transfer_assigned(GameObject ze_transfer_obj){
        if(assigned != null){
            assigned.transform.parent = ze_transfer_obj.transform;
            assigned.transform.position = ze_transfer_obj.transform.position;
            assigned.transform.rotation = ze_transfer_obj.transform.rotation;
            assigned.transform.localScale = new Vector2(1,1);
            assigned = null;
        }
    }

    public void Update(){
        if(des_point != null){
            // needs to be effected by speed stats
            target.transform.position = Vector2.Lerp(target.transform.position,des_point.transform.position,Time.deltaTime*4);
        }
    }

    public void reset(){
        des_point = init_point;
        target.transform.parent = init_point.transform;
    }

    public bool check_progress(){
        if(des_point != null){
            if(Vector2.Distance(target.transform.position,des_point.transform.position) < 0.05f){
                return true;
            }
        }
        return false;
    }

}
