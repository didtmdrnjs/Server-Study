using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class Server 
{
    public static int MaxPlayers { get; private set; }
    public static int Port { get; private set; }
    public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
    public delegate void PacketHandler(int _fromClient, Packet _packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    private static TcpListener tcpListener;
    private static UdpClient udpListener;

    public static void Start(int _maxPlayers, int _port)
    {
        // �ִ� �÷��̾� �� ��Ʈ ����
        MaxPlayers = _maxPlayers;
        Port = _port;

        // InitializeServerData() ����
        Console.WriteLine("Starting server...");
        InitializeServerData();

        // ������ ��Ʈ�� ���� ������ ��� Ŭ���̾�Ʈ�� ��û�� ���� �� �ֵ��� ����
        tcpListener = new TcpListener(IPAddress.Any, Port);

        // ������ ���� �ޱ� ����
        tcpListener.Start();

        // ������ ������ �޾Ƶ��̴� �񵿱� �۾� ����
        // AsyncCallback : �۾��� �Ϸ�Ǿ��� �� ȣ���� �޼��带 �����ϴ� AsyncCallback �븮��
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        // �ش� ��Ʈ�� ����ϴ� ���ο� UdpClient ����
        udpListener = new UdpClient(Port);
        // ���� ȣ��Ʈ�� ���� �񵿱������� �����ͱ׷��� ����
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Console.WriteLine($"Server started on {Port}.");
    }

    private static void TCPConnectCallback(IAsyncResult _result)
    {
        // ������ ����õ��� �񵿱������� �����ϰ�, ���� ȣ��Ʈ ����� ó���ϱ� ���� �� TcpClient�� �����.
        TcpClient _client = tcpListener.EndAcceptTcpClient(_result);

        // BeginAcceptTcpClient�� �ٽ� ȣ�������ν� ����ؼ� ��û�� �޾Ƶ��� �� �ִ�.
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}...");

        // TcpClient�� �Ҵ���� ���� ���� ã�� Ŭ���̾�Ʈ �Ҵ�
        for (int i = 1; i <= MaxPlayers; i++)
        {
            if (clients[i].tcp.socket == null)
            {
                clients[i].tcp.Connect(_client);
                return;
            }
        }

        Console.WriteLine($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
    }

    private static void UDPReceiveCallback(IAsyncResult _result)
    {
        try
        {
            // ���� ����Ʈ ����
            IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            // �񵿱� ���� ����, ���� ������ ����
            byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
            // �񵿱� �б� �����
            udpListener.BeginReceive(UDPReceiveCallback, null);

            if (_data.Length < 4)
            {
                return;
            }

            using (Packet _packet = new Packet(_data))
            {
                int _clientId = _packet.ReadInt();

                if (_clientId == 0)
                {
                    return;
                }

                if (clients[_clientId].udp.endPoint == null)
                {
                    // Ŭ���̾�Ʈ�� ���� ����Ʈ�� ������ ����
                    clients[_clientId].udp.Connect(_clientEndPoint);
                    return;
                }

                // ���� ����Ʈ�� ������ HandleData ����
                if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                {
                    clients[_clientId].udp.HandleData(_packet);
                }
            }
        }
        catch (Exception _ex)
        {
            Console.WriteLine($"Error receiving UDP data: {_ex}");
        }
    }

    public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
    {
        try
        {
            if (_clientEndPoint != null)
            {
                // �����͸� �񵿱������� ����
                udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
            }
        }
        catch (Exception _ex)
        {
            Console.WriteLine($"Error sending data to {_clientEndPoint} via UDP {_ex}");
        }
    }

    private static void InitializeServerData()
    {
        // ������ �ִ� �÷��̾� ����ŭ Ŭ���̾�Ʈ �ν��Ͻ� ����
        for (int i = 1; i <= MaxPlayers; i++)
        {
            clients.Add(i, new Client(i));
        }

        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            // ��Ŷ ����, ������ �Լ� ����
            {  (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
            {  (int)ClientPackets.playerMovement, ServerHandle.PlayerMovement },
            {  (int)ClientPackets.playerShoot, ServerHandle.PlayerShoot },
            {  (int)ClientPackets.playerThrowItem, ServerHandle.PlayerThrowItem }
        };
        Console.WriteLine("Initialized packets.");
    }

    public static void Stop()
    {
        tcpListener.Stop();
        udpListener.Close();
    }
}
