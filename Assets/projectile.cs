using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class projectile : MonoBehaviour
{

    bool going;

    public float damage;
    public float ap;

    public GameObject bullet_trail;
    public GameObject impact;
    public LayerMask mask;

    public List<GameObject> bans = new List<GameObject>();

    public void FIRE(){
        going = true;
        RaycastHit2D[] h = Physics2D.RaycastAll(transform.position, transform.up, 50, mask);
        
        foreach(RaycastHit2D hit in h){
            // damage checks

            // this is where hit effects can go

            // bullet_trail
            GameObject g = Instantiate(bullet_trail,transform.position,transform.rotation);
            g.transform.localScale = new Vector2(g.transform.localScale.x,Vector2.Distance(hit.point,transform.position));

            g = Instantiate(impact,hit.point,transform.rotation);
            g.transform.up = hit.normal;

            
            bool check = false;
            BREAK br = hit.collider.GetComponent<BREAK>();
            if(br != null && !bans.Contains(hit.collider.gameObject)){
                check = br.DAMAGE(gameObject.GetComponent<projectile>(),hit.point);
            }

            if(!check){
                break;
            }
        }
        Invoke("des",0.2f);
        going = false;
    }

    public void des(){if(!going){Destroy(gameObject);}}
}
