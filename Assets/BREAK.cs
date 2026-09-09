using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BREAK : MonoBehaviour
{
    
    [Header("HP Settings")] 
    
    public float HP;
    public float func_threshold; // when limb starts to lose functionality
    public float broken_threshold;
    
    float rec_pre_HP;
    float rec_post_HP;
    float tot_HP;
    public float repair_thresh;
    
    [Header("armor Settings")] 

    public float armor;
    public float armor_absorbancy;
    
    public float scathing;

    public bool vital;
    public bool broken;
    public bool shattered;

    public List<VANITY> vans = new List<VANITY>();
    float leng;

    public List<ParticleSystem> particle_list = new List<ParticleSystem>();
    float[] particle_vals;

    public void INIT(){
        particle_list = new List<ParticleSystem>();
        for(int i = 0; i < transform.childCount; i++){
            if(transform.GetChild(i).GetComponent<VANITY>() != null){
                vans.Add(transform.GetChild(i).GetComponent<VANITY>());
            }
            if(transform.GetChild(i).GetComponent<ParticleSystem>() != null){
                particle_list.Add(transform.GetChild(i).GetComponent<ParticleSystem>());
            }
        }
        leng = vans.Count;
        particle_vals = new float[particle_list.Count];
        for(int i = 0; i < particle_list.Count; i++){
            var v = particle_list[i].emission;
            v.rateOverTime = particle_vals[i];
        }
        tot_HP = HP;
        rec_pre_HP = HP;
        rec_post_HP = HP;
        if(armor_absorbancy == 0){armor_absorbancy = 1;}
    }

    public bool DAMAGE(projectile p, Vector2 point){
        rec_pre_HP = HP;
        bool rico_check = false;
        bool pierce_check = false;
        
        float hp_damage = p.damage;
        float arm_damage = 0;

        float integ = Mathf.Abs((scathing/armor)-1);
        if(scathing == 0){integ = 1;}
        float effect = 1;

        float min_thresh = armor * 0.6f;
        float max_thresh = armor * 1.25f;

        if(p.ap < min_thresh){
            effect = 0; // falls below min threshold
        }
        else if(p.ap >= max_thresh){
            effect = 0.25f; // falls over max threshold
        }
        else if(p.ap >= armor){
            effect = 0.5f - 0.25f * (p.ap - armor) / (max_thresh - armor); // exceeds armor val
        }
        else{
            effect = 1 - 0.5f * (p.ap - min_thresh) / (armor - min_thresh); // falls short of armor val
        }

        

        hp_damage = p.damage * (1-(((armor_absorbancy/100) * integ)*effect));
        arm_damage = (p.damage/p.ap) * (1-((armor_absorbancy/100) * integ));
        
        if(effect == 0){hp_damage = 0;}

        if(p.ap < armor*0.5f){
            // ricoshet check
            float re = Random.Range(1,3);
            if(re == 2){ // extra checks needed
                rico_check = true;
                arm_damage = arm_damage * 0.2f;
            }
        }
        if(p.ap > armor*1.5f){
            pierce_check = true;
            arm_damage = arm_damage * 0.2f;
        }


        if(scathing > armor){
            hp_damage = p.damage;
            arm_damage = 0;
        }


        int r = 0;
        if(vans.Count > 0){
            for(int i = 0; i < vans.Count; i++){
                if(Vector2.Distance(vans[i].transform.position,point) < Vector2.Distance(vans[r].transform.position,point)){
                    r = i;
                }
            }
        }

        HP -= hp_damage;
        scathing += arm_damage;
        
        damage_health(hp_damage/HP);

        if(vans.Count > 0){
            vans[r].damage_armor(arm_damage/armor);
            if(scathing/armor > 1/leng){
                vans[r].eject(point,10); 

                vans.RemoveAt(r);
            }
        }
        
        if(pierce_check){
            // overpen
            p.damage = p.damage / 2;
            // loses damage
            if(p.ap > armor*2f){
                // doesnt lose damage
                p.damage = p.damage * 2;
            }
        }
        // reeavaluate stuff
        leng = vans.Count;
        if(HP < broken_threshold){broken = true;}
        if(HP < 0){shattered = true;}
        if(rico_check){
            // gonna have to figure out how to do this one
            p.bans.Add(gameObject);
            p.transform.position = point;
            p.transform.eulerAngles = new Vector3(p.transform.eulerAngles.x,p.transform.eulerAngles.y,p.transform.eulerAngles.z*-1); // flips it
            p.FIRE();
        }
        rec_post_HP = HP;
        return pierce_check;
    }

    public void damage_health(float damage){
        for(int i = 0; i < particle_list.Count; i++){
            int r = Random.Range(0,particle_list.Count);
            particle_vals[r] += damage;
            var v = particle_list[r].emission;
            v.rateOverTime = particle_vals[r]*200;
        }
    }

    public void heal(float incr, float tot){
        float pork = (HP - broken_threshold) / (tot_HP - broken_threshold);
        if(!broken && rec_pre_HP >= HP && pork < repair_thresh){
            if((1+tot)*rec_post_HP > HP+incr){
                HP += incr;
            }
            else{
                HP = (1+tot)*rec_post_HP;
            }
        }
    }
}
