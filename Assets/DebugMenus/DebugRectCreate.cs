using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugRectCreate : MonoBehaviour
{
    public float deleteCounter = 60f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(this.tag == "oldrect"){
            deleteCounter -= Time.deltaTime;
            if(deleteCounter <= 0f){
                Destroy(this.gameObject);
            }
        }
    }
}
