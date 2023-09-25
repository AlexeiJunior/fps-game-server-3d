using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGenerator : MonoBehaviour
{
    public static PlayerGenerator instance = null;

    public GameObject playerPrefab;

    private void Awake() {
        if (instance != null && instance != this){
            Destroy(this.gameObject);
            return;
        }

        instance = this;
    }

    public void requestSpawnPlayerTS(Packet packet) {
        int id = packet.ReadInt();
        string username = packet.ReadString();

        Client client = Server.instance.getClientById(id);
        client.setUsername(username);

        var rand = new System.Random();
        int x = rand.Next(-16, 16);
        int z = rand.Next(-16, 16);
        Vector3 position = new Vector3(x, 5, z);
        Quaternion rotation = Quaternion.identity;
        Quaternion camRotation = Quaternion.identity;

        ThreadManager.ExecuteOnMainThread(() => {
            PlayerGenerator.instance.instantiatePlayer(client.getId(), client.getUsername(), position, rotation, camRotation);
        });
    }

    public void instantiatePlayer(int id, string username, Vector3 position, Quaternion rotation, Quaternion camRotation) {
        GameObject playerGO = Instantiate(playerPrefab, position, rotation);
        PlayerController playerController = playerGO.GetComponentInChildren<PlayerController>();
        playerController.setId(id);
        playerController.setUsername(username);
        playerController.setPlayerPosition(position);
        playerController.setPlayerRotation(rotation);
        playerController.setCamRotation(camRotation);

        Server.instance.getClientById(id).setPlayer(playerController);

        Packet packetSend = new Packet();
        packetSend.Write("requestSpawnPlayerFS");
        packetSend.Write(id);
        packetSend.Write(position);
        packetSend.Write(rotation);
        packetSend.Write(camRotation);

        Server.instance.sendTcpData(id, packetSend);
        
        Server.instance.getClients().ForEach(clientSend => {
            if(clientSend.getId() != id) {
                packetSend = new Packet();
                packetSend.Write("newEnemyFS");
                packetSend.Write(clientSend.getId());
                packetSend.Write(clientSend.getUsername());
                packetSend.Write(clientSend.getPlayer().getPlayerPosition());
                packetSend.Write(clientSend.getPlayer().getPlayerRotation());
                packetSend.Write(clientSend.getPlayer().getCamRotation());

                Server.instance.sendTcpData(id, packetSend);
            }
        });
    }

    public void newEnemyTS(Packet packet) {
        int id = packet.ReadInt();
        string username = packet.ReadString();

        Client client = Server.instance.getClientById(id);
        client.setUsername(username);

        ThreadManager.ExecuteOnMainThread(() => {
            Packet packetSend = new Packet();
            packetSend.Write("newEnemyFS");
            packetSend.Write(client.getId());
            packetSend.Write(client.getUsername());
            packetSend.Write(client.getPlayer().getPlayerPosition());
            packetSend.Write(client.getPlayer().getPlayerRotation());
            packetSend.Write(client.getPlayer().getCamRotation());

            Server.instance.sendTcpDataToAll(id, packetSend);
        });
    }
}
