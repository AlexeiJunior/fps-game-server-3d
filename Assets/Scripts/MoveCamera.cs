using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour {

    public Transform player;

    void Update() {
        transform.position = player.transform.position;
    }

    public void setPlayer(Transform player){
        this.player = player;
    }
}
