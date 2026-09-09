using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VANITY : MonoBehaviour
{

    float tot_iteg = 1;

    public void eject(Vector2 origin, float knockback){ // make phys
        gameObject.AddComponent(typeof(Rigidbody2D));
        //gameObject.AddComponent(typeof(PixelCollider2D));
        gameObject.AddComponent(typeof(expire));
        expire ex = gameObject.GetComponent<expire>();
        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        //PixelCollider2D pcl = gameObject.GetComponent<PixelCollider2D>();
        //Collider2D col = gameObject.GetComponent<Collider2D>();

        ex.time = 10;
        //pcl.Regenerate();

        //Vector2 vect = (col.ClosestPoint(origin))-origin;
        Vector2 vect = transform.position-(new Vector3(origin.x,origin.y,0));

        rb.AddForceAtPosition(vect*knockback,origin,ForceMode2D.Impulse); // force aint forcing
        transform.parent = null;
    }

    public void damage_armor(float damage){
        tot_iteg -= damage;
        IntegritySpriteController isc = gameObject.GetComponent<IntegritySpriteController>();
        isc.integrity = tot_iteg;
        isc.Apply();
    }
}