using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class expire : MonoBehaviour
{
    public float time;

    void Start()
    {
        Invoke("Expire",time);
    }

    void Update(){
        SpriteRenderer spr = gameObject.GetComponentInChildren<SpriteRenderer>();
        if(spr != null){
            Color c = spr.color;
            c.a = Mathf.Lerp(c.a,0,Time.deltaTime*(1/time));
            spr.color = c;
        }
    }

    void Expire()
    {
        Destroy(gameObject);
    }
}
