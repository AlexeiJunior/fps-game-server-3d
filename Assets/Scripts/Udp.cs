using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

public class Udp {
    UdpClient udpListener;

    public Udp(UdpClient udpListener) {
        this.udpListener = udpListener;
    }

    public void connect() {
        udpListener.BeginReceive(udpReceiveCallback, null);
    }

    private void udpReceiveCallback(System.IAsyncResult result) {
        try {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
            udpListener.BeginReceive(udpReceiveCallback, null);

            if (data.Length < 4) {
                Debug.Log("Err. receiving udp data!");
                return;
            }

            handleData(data, clientEndPoint);
        } catch  {
            Debug.Log("Err. receiving udp data!");
        }
    }

    public void handleData(byte[] data, IPEndPoint clientEndPoint) {
        Packet packet = new Packet(data);

        int i = packet.ReadInt(); //só para remover o id do pacote
        string method = packet.ReadString();
        int id = packet.ReadInt();
        
        Client client = Server.instance.getClientById(id);
        if (client.getEndPointUdp() == null) client.setEndPointUdp(clientEndPoint);

        MethodInfo theMethod = Server.instance.GetType().GetMethod(method);
        theMethod.Invoke(Server.instance, new object[]{id, packet});
    }

    public void sendData(Packet packet, IPEndPoint endPoint) {
        try {
            if (endPoint == null) return;
            udpListener.BeginSend(packet.ToArray(), packet.Length(), endPoint, null, null);
        } catch {
            Debug.Log("Err. sending udp to client!");
        }
    }
}
