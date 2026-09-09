using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MAKER : MonoBehaviour
{

    public bool AI;

    public GameObject[] components_list;

    public GameObject TEST_1;

    Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        MAKE();
    }

    public void MAKE()
    {
        torso m = null;
        legs l = null;
        shoulder sh = null;
        head h = null;
        armory ar1 = null;
        armory ar2 = null;
        // do torso
        for(int i = 0; i < components_list.Length; i++){ // extracts all components
            if(m == null){m = components_list[i].GetComponent<torso>();}
            if(l == null){l = components_list[i].GetComponent<legs>();}
            if(sh == null){sh = components_list[i].GetComponent<shoulder>();}
            if(h == null){h = components_list[i].GetComponent<head>();}
            if(ar1 == null){ar1 = components_list[i].GetComponent<armory>();}
            else if(ar2 == null){ar2 = components_list[i].GetComponent<armory>();}
        }
        
        MonoBehaviour[] componentser = new MonoBehaviour[] {m,l,sh,h,ar1,ar2}; // list of all components
        torso t = null;
        GameObject[] comps = new GameObject[] {null,null,null,null,null,null};

        for(int i = 0; i < componentser.Length; i++){ // instantiates all components
            if(componentser[i] != null){
                GameObject g = Instantiate(componentser[i].gameObject,transform.position,transform.rotation);
                g.transform.parent = transform;
                comps[i] = g;
                if(g.GetComponent<torso>() != null){
                    t = g.GetComponent<torso>();
                }
                if(g.GetComponent<HingeJoint2D>() != null){
                    g.GetComponent<HingeJoint2D>().connectedBody = t.GetComponent<Rigidbody2D>();
                }
                if(t != null){
                    g.transform.parent = t.transform;
                }
            }
        }
        if(!AI){
            t.gameObject.AddComponent(typeof(CONTROL));

            CONTROL c = t.GetComponent<CONTROL>();
            
            cam = FindObjectOfType<Camera>();
            c.rb = t.GetComponent<Rigidbody2D>();
            if(componentser[4] != null){ar1 = componentser[4].GetComponent<armory>();}
            else{ar1 = null;}
            if(componentser[5] != null){ar2 = componentser[5].GetComponent<armory>();}
            else{ar2 = null;}
            c.INIT(ar1,ar2);
        }
        else{
            t.gameObject.AddComponent(typeof(ai_control));

            ai_control c = t.GetComponent<ai_control>();
            
            cam = FindObjectOfType<Camera>();
            c.rb = t.GetComponent<Rigidbody2D>();
            if(componentser[4] != null){ar1 = componentser[4].GetComponent<armory>();}
            else{ar1 = null;}
            if(componentser[5] != null){ar2 = componentser[5].GetComponent<armory>();}
            else{ar2 = null;}
            c.INIT(ar1,ar2);
        }
    }

    void Update(){
        if(!AI){Debug_opt();}
    }

    void Debug_opt(){
        // this is where i debug

        // spawn in test dummy
        if(Input.GetKeyDown(KeyCode.Alpha1)){
            Vector3 mousePosition = Input.mousePosition;
            mousePosition = cam.ScreenToWorldPoint(mousePosition);
            GameObject g = Instantiate(TEST_1,transform.position,transform.rotation);
            g.transform.position = new Vector3(mousePosition.x,mousePosition.y + 5,0);
        }
        if(Input.GetKey(KeyCode.Escape)){Application.Quit();}
    }
}
