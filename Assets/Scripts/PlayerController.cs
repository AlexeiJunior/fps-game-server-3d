using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public GameObject player;
    public Transform orientation;
    public Rigidbody rb;
    public Transform playerCam;
    public Keys keys { get; set; }

    //PlayerStats
    private float health = 100f;
    private float block = 80f;
    public string username = "";
    public int id = -1;

    //Shoot
    private float damage = 10f;
    private float range = 100f;
    private float fireRate = 2f;
    private float nextTimeToFire = 0f;

    //Dash
    private float dashExecutionTimeDelay = 0.1f;
    private float dashForce = 2000f;
    private float dashDelay = 0.8f;
    private bool finishDashDelay = true;
    private bool isDashing = false;

    //Jump
    private float jumpHeight = 1000f;
    private float jumpDelay = 0.6f;
    private bool readyToJump = true;
    
    //Movement
    private float maxSpeed = 10f;
    private float moveSpeed = 2500f;
    private float counterMovement = 0.175f;
    private float threshold = 0.001f;
    
    //Look
    private float mouseSensitivity = 70f;
    private float xRotation = 0f;

    //GroundCheck
    private bool isGrounded;
    private float groundCheckRadius = 0.1f;
    public Transform groundCheck;
    public LayerMask whatIsGround;

    void Start() {
        keys = new Keys();
    }

    private void shoot() {
        if(keys.mouseLeft && Time.time >= nextTimeToFire) {
            nextTimeToFire = Time.time + 2f / fireRate;

            RaycastHit hit;
            if (Physics.Raycast(playerCam.position, playerCam.forward, out hit, range)) {
                // Debug.Log(hit.transform.name);
                PlayerController playerController = hit.transform.gameObject.GetComponent<PlayerController>();
                if(playerController) playerController.doDamage(10);
                sendShootImpactLocation(hit.point, Quaternion.LookRotation(hit.normal), playerController != null);
            }
        }
    }

    public void sendShootImpactLocation(Vector3 hitPoint, Quaternion rotationPoint, bool isPlayer) {
        Packet packet = new Packet();
        packet.Write("shootImpactLocationFS");
        packet.Write(id);
        packet.Write(hitPoint);
        packet.Write(rotationPoint);
        packet.Write(isPlayer);

        Server.instance.sendUdpDataToAll(packet);
    }

    private void look(){
        float mX = keys.mouseX * mouseSensitivity * Time.deltaTime;
        float mY = keys.mouseY * mouseSensitivity * Time.deltaTime;

        Vector3 rot = playerCam.localRotation.eulerAngles;
        float desiredX = rot.y + mX;

        xRotation -= mY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCam.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

    private void movement() {
        isGrounded = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, whatIsGround).Length > 0;

        Vector2 mag = FindVelRelativeToLook();
        CounterMovement(keys.x, keys.y, mag);

        if (keys.x > 0 && mag.x > maxSpeed) keys.x = 0;
        if (keys.x < 0 && mag.x < -maxSpeed) keys.x = 0;
        if (keys.y > 0 && mag.y > maxSpeed) keys.y = 0;
        if (keys.y < 0 && mag.y < -maxSpeed) keys.y = 0;

        float multiplier = 1f;
        
        if (!isGrounded) multiplier = 0.5f;

        rb.AddForce(orientation.forward * keys.y * moveSpeed * rb.mass * Time.deltaTime * multiplier);
        rb.AddForce(orientation.right * keys.x * moveSpeed * rb.mass * Time.deltaTime * multiplier);
    }

    private void CounterMovement(float x, float y, Vector2 mag) {
        if (!isGrounded) return;
        
        if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0)) {
            rb.AddForce(moveSpeed * orientation.right * rb.mass * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0)) {
            rb.AddForce(moveSpeed * orientation.forward * rb.mass * Time.deltaTime * -mag.y * counterMovement);
        }
        
        //Limit diagonal running
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed) {
            float fallspeed = rb.velocity.y;
            Vector3 n = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    private Vector2 FindVelRelativeToLook() {
        float lookAngle = orientation.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);
        
        return new Vector2(xMag, yMag);
    }

    private void jump() {
        if(keys.jumping && isGrounded && readyToJump){
            rb.AddForce(Vector3.up * jumpHeight * rb.mass);

            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0) 
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

            Invoke("restartJump", jumpDelay);
            readyToJump = false;
        }
    }

    private void dash() {
        if(keys.mouseRight && !isGrounded && !isDashing && finishDashDelay){
            isDashing = true;
            finishDashDelay = false;
            Invoke("restartDash", dashExecutionTimeDelay);
            Invoke("resetFinishDashDelay", dashDelay);
        }

        if(isDashing){
            if (keys.y != 0) rb.AddForce(rb.transform.forward * keys.y * dashForce, ForceMode.Acceleration);
            if (keys.x != 0) rb.AddForce(rb.transform.right * keys.x * dashForce, ForceMode.Acceleration);
        }
    }  

    private void restartJump() {
        readyToJump = true;
    }

    private void restartDash() {
        isDashing = false;
    }

    private void resetFinishDashDelay() {
        finishDashDelay = true;
    }

    public void doDamage(float value) {
        block = block - value <= 0 ? 0 : block - value;
        health = health - (value - (block / value)) <= 0 ? 0 : health - (value - (block / value));

        if(health == 0){
            Packet packet = new Packet();
            packet.Write("killPlayerFS");
            packet.Write(id);
            Server.instance.sendTcpDataToAll(packet);
            player.SetActive(false);
            Invoke("respawnPlayer", 10f);
        }
    }

    public void respawnPlayer() {
        Packet packet = new Packet();
        packet.Write("respawnPlayerFS");
        packet.Write(id);
        player.SetActive(true);
        resetPlayerLife();
        Server.instance.sendTcpDataToAll(packet);
    }

    public void resetPlayerLife() {
        health = 100f;
        block = 80f;
    }

    public void sendPlayerPosition() {
        if(packetsToSend <= 0) return;
        packetsToSend--;

        Packet packet = new Packet();
        packet.Write("playerPositionFS");
        packet.Write(id);
        packet.Write(rb.transform.position);
        packet.Write(orientation.localRotation);
        packet.Write(playerCam.localRotation);
        packet.Write(rb.velocity);
        packet.Write(rb.angularVelocity);
        packet.Write(isGrounded);
        packet.Write(health);
        packet.Write(block);
        packet.Write(keys);

        Server.instance.sendUdpDataToAll(packet);
    }

    public void playerKeys(Packet packet) {
        Keys keys = packet.ReadKeys();
        setKeys(keys);
        
        tickToExecuted++;
        packetsToSend++;
    }

    public float tickToExecuted = 0;
    public float packetsToSend = 0;

    public void executePlayer() {
        if(tickToExecuted <= 0) return;
        tickToExecuted --;
        
        ThreadManager.ExecuteOnMainThread(() => { 
            setPlayerRotation(keys.playerRotation);
            setCamRotation(keys.camRotation);
            movement();
            look();
            jump();
            dash();
            shoot();
        });
    }

    public void setKeys(Keys keys){
        this.keys = keys;
    }

    public void removePlayer() {
        ThreadManager.ExecuteOnMainThread(() => Destroy(player));
    }

    public void setPlayerPosition(Vector3 position) {
        rb.transform.position = position;
    }

    public void setPlayerRotation(Quaternion rotation) {
        orientation.localRotation = rotation;
    }

    public void setCamRotation(Quaternion rotation) {
        playerCam.localRotation = rotation;
    }

    public Vector3 getPlayerPosition() {
        return rb.transform.position;
    }

    public Quaternion getPlayerRotation() {
        return orientation.localRotation;
    }

    public Quaternion getCamRotation() {
        return playerCam.localRotation;
    }

    public void setId(int id) {
        this.id = id;
    }

    public void setUsername(string username) {
        this.username = username;
    }
}