using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUN_CONT : MonoBehaviour
{
    public GameObject target;
    public GameObject[] list_of_held_points;
    public List<arm> arms_to_held_points = new List<arm>();
    Rigidbody2D rb;

    public shoulder shoul;

    public GameObject eject_point;
    public GameObject barrel_point;

    public GameObject casing;
    public GameObject projectile;

    public GameObject[] fire_effects;
    public AudioClip[] fire_noises;
    
    Material barrel_mat;
    

    Animation anim;
    AudioSource aud;

    bool rg;

    public string state; // holstered, ready, overheated

    [Header("hold pos Settings")] // this adds a header :D

    public float hold_dist; // distance from furthest hold point to center
    public float hold_perc; // percentage of how far weapon should be held from person
    public float depression; // how low the gun is wielded

    public float rest_dist;
    public float rest_depression;
    public float rest_ang;

    float positionDamper = 15;   // Damping to prevent oscillation
    float maxForce = 1000f;

    [Header("fire Settings")] 

    public float fire_rate_time;
    float cooldown;
    
    public int burst_amount = 1;
    public float burst_delay;

    public int max_rounds;
    int curr_rounds;

    [Header("heat Settings")] 

    public float max_heat;
    public float heat_per_round;

    public float heat;
    public float heat_material_limit;
    
    public ParticleSystem smoke_effect;
    public float max_smoke;

    [Header("recoil Settings")] 

    public float mass;

    public float backwards_recoil;
    public float torque_recoil;
    string rember;

    private static readonly int HeatProp = Shader.PropertyToID("_Heat");

    void Awake(){
        INIT();
    }

    void INIT(){
        rb = GetComponent<Rigidbody2D>();
        rb.mass = mass;
        arms_to_held_points = new List<arm>();
        barrel_mat = barrel_point.transform.parent.GetComponent<SpriteRenderer>().material;
        curr_rounds = max_rounds;
        hold_dist = Mathf.Abs(transform.position.x-list_of_held_points[0].transform.position.x);
        foreach(GameObject g in list_of_held_points){
            float d = Mathf.Abs(transform.position.x-g.transform.position.x);
            if(d < hold_dist){
                hold_dist = d;
            }
        }
        anim = gameObject.GetComponent<Animation>();
        aud = gameObject.GetComponent<AudioSource>();

        var v = smoke_effect.emission;
        v.rateOverTime = 0;
        barrel_mat.SetFloat(HeatProp,0);
    }

    // Update is called once per frame
    void Update()
    {
        if(!rg){
            gun_move();
        }
        if(shoul != null && shoul.tors != null){
            weapon_cooldown();
            heat_effects();
        }
    }

    void gun_move(){

        bool check = false;

        float MOVE_force = 0;
        float ROT_force = 0;
        float counter_force = 0;

        if(state == "holstered" || rember == "holstered"){
            MOVE_force = 90; // instant movement
            ROT_force = 20;
            counter_force = 1;
            check = true;
        }
        else{
            if(arms_to_held_points.Count > 0 && shoul != null){
                check = true;
                // can be adjusted later for num of hands

                float iteg = 0;
                foreach(arm a in arms_to_held_points){
                    if(a.integrity <= 1 && !a.rg){
                        iteg += a.integrity;
                    }
                }
                iteg = iteg / arms_to_held_points.Count;
                // iteg should be a 0-1 scale
                MOVE_force = shoul.MOVE_force*iteg;
                ROT_force = shoul.ROT_force*iteg;

                counter_force = shoul.precision*iteg;
            }
        }
        if(ROT_force == 0 || MOVE_force == 0){
            check = false;
        }
        if(check){
            if(arms_to_held_points.Count > 0){
                MOVE_force = MOVE_force*arms_to_held_points.Count; // more hands = more stability/responsiveness
                ROT_force = ROT_force*arms_to_held_points.Count;
                counter_force = counter_force*arms_to_held_points.Count;
            }
            // position nonsense
                Vector2 positionError = (Vector2)target.transform.position - rb.position;
                Vector2 velocityError = Vector2.zero - rb.velocity;

                Vector2 forcepos = (positionError * MOVE_force) + (velocityError * positionDamper);
                forcepos = Vector2.ClampMagnitude(forcepos, maxForce);

                rb.AddForce(forcepos, ForceMode2D.Force);

            // rotation nonsense
                float diff = Vector2.Angle(transform.up,target.transform.up);
                float meas_Tar_ang = target.transform.eulerAngles.z;
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

                float force = ROT_force;
                
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
            // rotation counterforce
                if((dir > 0 && rb.angularVelocity+(force*dir) < 0) || dir < 0 && rb.angularVelocity+(force*dir) > 0){
                    // we are travelling in the wrong direction
                    float mag = Mathf.Abs(rb.angularVelocity);
                    force = Mathf.Clamp(force,-mag,mag);
                    // applies counterforce
                    rb.AddTorque((force*dir)*counter_force);
                }
            // rotation counterforce
        }
        else{
            // gravity
            rb.AddForce(-Vector2.up*rb.mass*rb.drag*5);
        }
    }

    void weapon_cooldown(){
        if(heat > max_heat){
            if(state != "overheat"){rember = state;}
            state = "overheat";
            
        }
        if(heat - shoul.tors.cooling_assistance*Time.deltaTime < 0){
            if(state == "overheat"){state = rember; rember = "";}
        }
        else{
            heat -= shoul.tors.cooling_assistance*Time.deltaTime;
        }
    }

    void heat_effects(){
        float val = heat/max_heat;
        var v = smoke_effect.emission;
        v.rateOverTime = val*max_smoke;
        if(heat > 0){
            barrel_mat.SetFloat(HeatProp,val*heat_material_limit);
        }
    }

    public void assign_hand(arm hand){ // gonna have to rethink weapon hold distances and how they are effected by the number of arms
        if(!arms_to_held_points.Contains(hand)){
            foreach(GameObject h in list_of_held_points){
                if(h.transform.childCount == 0){
                    hand.target.transform.parent = h.transform;
                    hand.target.transform.position = h.transform.position;

                    shoul = hand.shoul;
                    arms_to_held_points.Add(hand);

                    //hold_dist = Mathf.Abs(h.transform.position.x - transform.position.x); // distance from last hand placed on the gun
                    break;
                }
            }
        }
    }

    public void unassign(){
        foreach(arm a in arms_to_held_points){ // resets hands
            a.target.transform.parent = null;
        }
        arms_to_held_points = new List<arm>();
    }

    public void toragdoll(GameObject parent){
        rg = true;
        HingeJoint2D h = gameObject.GetComponent<HingeJoint2D>();
        h.enabled = true;
        h.connectedBody = parent.transform.parent.GetComponent<Rigidbody2D>();
        if(state != "holstered"){
            transform.position = parent.transform.position;
        }
        else{
            transform.position = parent.transform.position;
        }
        foreach(GameObject g in list_of_held_points){
            g.transform.DetachChildren();
        }
    }

    public void fromragdoll(GameObject slot){
        rg = false;
        HingeJoint2D h = gameObject.GetComponent<HingeJoint2D>();
        h.enabled = false;
        target = slot;
        //transform.parent = target.transform;
        // tisnt ragdolling
    }

    public void eject_casing(){ // need to add debris when firing and set effects to erase of their own accord
        GameObject cas = Instantiate(casing,eject_point.transform.position,eject_point.transform.rotation);

        Vector2 force_vect = new Vector2(Random.Range(-50,-20),Random.Range(50,30));
        force_vect = force_vect * 0.1f;

        cas.GetComponent<Rigidbody2D>().AddForce(new Vector2(-transform.localScale.x,1)*force_vect,ForceMode2D.Impulse);
        cas.GetComponent<Rigidbody2D>().angularVelocity = Random.Range(-120,120);
    }

    public void fire_manager(){
        if(cooldown < Time.time && shoul != null){
            if(curr_rounds > 0){
                // we can fire
                cooldown = Time.time + fire_rate_time;
                for(int i = 0; i < burst_amount; i++){
                    Invoke("Fire_projectile", burst_delay*i);
                }
            }
        }
    }

    public void Fire_projectile(){
        if(state == "ready"){
            anim.Play();
            aud.PlayOneShot(fire_noises[Random.Range(0,fire_noises.Length)]);
            if(barrel_point.transform.childCount > 0){
                Transform[] t = barrel_point.GetComponentsInChildren<Transform>();
                foreach(Transform te in t){
                    if(te.gameObject.tag == "smoke"){
                        Destroy(te.gameObject); // prevents smoke buildup
                    }
                }
            }
            foreach(GameObject g in fire_effects){
                Instantiate(g,barrel_point.transform.position,barrel_point.transform.rotation).transform.parent = barrel_point.transform;
            }
            

            projectile p = Instantiate(projectile,barrel_point.transform.position,barrel_point.transform.rotation).GetComponent<projectile>();
            p.FIRE();

            curr_rounds --;
            heat += heat_per_round;

            float iteg = 0;
            foreach(arm a in arms_to_held_points){
                if(a.integrity <= 1 && !a.rg){
                    iteg += a.integrity;
                }
            }
            iteg = iteg / arms_to_held_points.Count;

            float rec_f = (shoul.recoil_cont*iteg) + (shoul.tors.leg.recoil_assist_vertical*shoul.tors.leg.tot_integrity);
            float torq_f = (shoul.torque_cont*iteg) + (shoul.tors.leg.recoil_assist_retation*shoul.tors.leg.tot_integrity); // need to fix counterforce

            if(arms_to_held_points.Count > 0){
                rec_f = rec_f * arms_to_held_points.Count;
                torq_f = torq_f * arms_to_held_points.Count;
            }

            // add physics
            Vector3 force = -transform.up*backwards_recoil; // needs further tweaking so values are consistent
            rb.AddForce(force,ForceMode2D.Impulse);
            // broken, doesnt fire leftward
            float force_perc = rec_f/(rb.mass*backwards_recoil);
            force_perc = Mathf.Clamp(force_perc,0,1);
            force = -transform.up*(force_perc*backwards_recoil);
            rb.AddForce(-force,ForceMode2D.Impulse);

            force = Vector2.up * torque_recoil;
            int dir = 1;
            if(Random.Range(-5,5) < 0){dir = -1;}
            rb.AddForceAtPosition(force*dir,barrel_point.transform.position,ForceMode2D.Impulse);

            force_perc = torq_f/(rb.mass*torque_recoil);
            force_perc = Mathf.Clamp(force_perc,0,1);
            force = Vector2.up * (force_perc*torque_recoil);
            rb.AddForceAtPosition(force*-dir,barrel_point.transform.position,ForceMode2D.Impulse);
        }
    }
}
