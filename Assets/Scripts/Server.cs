using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;

public class Server : MonoBehaviour {
    public static Server instance = null;
    private TcpListener tcpListener;
    private UdpClient udpListener;
    private Udp udp;

    Dictionary<int, Client> clients = new Dictionary<int, Client>();

    private int maxPlayers = 50;
    private int totalPlayers = 0;
    private int port = 8800;

    public bool started = false;

    private void Awake() {
        if (instance != null && instance != this){
            Destroy(this.gameObject);
            return;
        }

        instance = this;
    }

    void Start() {
        Physics.autoSimulation = false;
        startServer();
    }

    void FixedUpdate() {
        if(!started) return;

        getClients().ForEach(client => {
            if(client == null || client.getPlayer() == null) return;
            client.getPlayer().executePlayer();
        });

        Physics.Simulate(Time.fixedDeltaTime);

        getClients().ForEach(client => {
            if(client == null || client.getPlayer() == null) return;
            client.getPlayer().sendPlayerPosition();
        });
    }

    private void startServer() {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Debug.Log("Starting server...");

        MapGenerator.instance.newMap();

        udpListener = new UdpClient(port);
        udp = new Udp(udpListener);
        udp.connect();

        tcpListener = new TcpListener(IPAddress.Any, port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new System.AsyncCallback(tcpConnectCallback), null);

        Debug.Log("Server started!");
        started = true;
    }

    private void tcpConnectCallback(System.IAsyncResult result) {
        TcpClient socket = tcpListener.EndAcceptTcpClient(result);
        tcpListener.BeginAcceptTcpClient(new System.AsyncCallback(tcpConnectCallback), null);
        
        if(totalPlayers >= maxPlayers) {
            Debug.Log("Server is full.");
            return;
        }

        var rand = new System.Random();
        totalPlayers++;

        Client client = new Client(rand.Next(0, 1000));
        Tcp tcp = new Tcp(socket, client.getId());
        client.setTcp(tcp);
        client.getTcp().connect();

        addClients(client);
        sendConnectByTcp(client.getId());

        Debug.Log("New client connected by tcp!");
    }

    private void sendConnectByTcp(int id) {
        Packet packet = new Packet();
        packet.Write("connectByTcpFS");
        packet.Write(id);

        sendTcpData(id, packet);
    }

    public void connectByUdpTS(int id, Packet packet) {
        Debug.Log("new connection UDP client: " + id);
        Packet packetSend = new Packet();
        packetSend.Write("connectByUdpFS");
        packetSend.Write(id);
        sendUdpData(id, packetSend);
    }
    
    public void requestSpawnPlayerTS(Packet packet) {
        PlayerGenerator.instance.requestSpawnPlayerTS(packet);
    }

    public void newEnemyTS(Packet packet) {
        PlayerGenerator.instance.newEnemyTS(packet);
    }

    public void playerKeysTS(int id, Packet packet) {
        Client client = Server.instance.getClientById(id);
        PlayerController player = client.getPlayer();
        if(player == null) return;

        player.playerKeys(packet);
    }

    public void disconnectServer() {
        udp = null;
        getClients().ForEach(client => disconnectPlayer(client));
    }

    public void disconnectPlayer(int id) {
        Client client = getClientById(id);
        if (client == null) return;

        disconnectPlayer(client);
    }

    public void disconnectPlayer(Client client) {
        PlayerController player = client.getPlayer();
        player.removePlayer();
        
        String name = client.getUsername();
        int id = client.getId();

        client.disconnect();
        client.setPlayer(null);
        removeClients(client);

        Packet packet = new Packet();
        packet.Write("playerDisconnect");
        packet.Write(client.getId());

        sendTcpDataToAll(id, packet);

        Debug.Log("Player [id: "+id+" name: "+name+"] has disconnect!");
    }
    
    private void OnApplicationQuit() {
        disconnectServer();
    }

    public void addClients(Client client) {
        clients.Add(client.getId(), client);
    }

    public void removeClients(Client client) {
        clients.Remove(client.getId());
    }

    public List<Client> getClients() {
        return clients.Select(client => client.Value).ToList();
    }

    public Client getClientById(int id) {
        return clients[id];
    }

    public void sendTcpData(int id, Packet packet) {
        packet.WriteLength();
        getClientById(id).sendTcpData(packet);
    }

    public void sendTcpDataToAll(Packet packet) {
        packet.WriteLength();
        getClients().ForEach(client => {
            client.sendTcpData(packet);
        });
    }

    public void sendTcpDataToAll(int exceptClient, Packet packet) {
        packet.WriteLength();
        getClients().ForEach(client => {
            if(client.getId() != exceptClient) {
                client.sendTcpData(packet);
            }
        });
    }

    public void sendUdpData(int id, Packet packet) {
        packet.WriteLength();
        sendUdpData(packet, getClientById(id).getEndPointUdp());
    }

    public void sendUdpDataToAll(Packet packet) {
        packet.WriteLength();
        getClients().ForEach(client => {
            sendUdpData(packet, client.getEndPointUdp());
        });
    }

    public void sendUdpDataToAll(int exceptClient, Packet packet) {
        packet.WriteLength();
        getClients().ForEach(client => {
            if(client.getId() != exceptClient) {
                sendUdpData(packet, client.getEndPointUdp());
            }
        });
    }

    public void sendUdpData(Packet packet, IPEndPoint endPoint) {
        udp.sendData(packet, endPoint);
    }
}
