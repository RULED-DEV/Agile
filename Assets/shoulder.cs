using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

public class shoulder : MonoBehaviour
{

    [Header("balance stats")]  // handles the balance assist and mass

    public float MASS;
    public float balance_assist;

    [Header("phys move stats")] // handles the movement of the arm

    public float MOVE_force;
    public float ROT_force;
    public float precision;

    [Header("fire Settings")] // handles the recoil control of the arm

    public float recoil_cont;
    public float torque_cont;

    public arm[] arms;
    public torso tors;

    bool rg = false;

    // Start is called before the first frame update
    public void INIT(torso t)
    {
        if(t != null){tors = t;}
        arms = GetComponentsInChildren<arm>();
        foreach(arm a in arms){
            a.shoul = gameObject.GetComponent<shoulder>();
            a.INIT();
        }
        transform.parent = transform.parent.GetChild(0);
        transform.position = transform.parent.position;
    }

    public void rgdoll(){ // arms break when ragdolling
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        HingeJoint2D hinge = GetComponent<HingeJoint2D>();
        if(rg){
            // stops being ragdoll
            rg = false;
            rb.isKinematic = true;
            rb.angularVelocity = 0;
            hinge.enabled = false;
        }
        else{
            // starts being ragdoll
            rg = true;
            rb.isKinematic = false;
            hinge.enabled = true;
        }
        foreach(arm a in arms){a.rgdoll();}
    }
}
