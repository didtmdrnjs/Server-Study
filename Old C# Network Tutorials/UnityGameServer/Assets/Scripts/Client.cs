using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class Client
{
    public static int dataBufferSize = 4096;
    public int id;
    public Player player;
    public TCP tcp;
    public UDP udp;

    public Client(int _clientId)
    {
        id = _clientId;
        tcp = new TCP(id);
        udp = new UDP(id);
    }

    public class TCP
    {
        public TcpClient socket;

        private readonly int id;
        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        public TCP(int _id)
        {
            id = _id;
        }

        public void Connect(TcpClient _socket)
        {
            // socket�� TcpClient �Ҵ� �� �ۼ��� buffer ũ�� �Ҵ�
            socket = _socket;
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            // �����͸� ������ �޴� NetworkStream �Ҵ�
            stream = socket.GetStream();

            receivedData = new Packet();
            receiveBuffer = new byte[dataBufferSize];

            // NetworkStream���� �񵿱� �б� ����
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

            // �������� welcome ��Ŷ ��������
            ServerSend.Welcome(id, "Welcome to the server!");
        }

        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    // ��Ʈ���� ���� �񵿱� ���� ����
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                // �񵿱� �б⸦ ������ ���� ����Ʈ �� ��ȯ
                int _byteLength = stream.EndRead(_result);
                if (_byteLength <= 0)
                {
                    Server.clients[id].Disconnect();
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                receivedData.Reset(HandleData(_data));
                // �ٽ� NetworkStream���� �񵿱� �б� ����
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error receiving TCP data: {_ex}");
                Server.clients[id].Disconnect();
            }
        }

        private bool HandleData(byte[] _data)
        {
            int _packetLength = 0;

            // receivedData�� ���� ������ ����
            receivedData.SetBytes(_data);

            // �� ���� �����ͱ��̰� 4 �̻��̸�
            if (receivedData.UnreadLength() >= 4)
            {
                // ���� ��(ù �κ��̴ϱ� ��Ŷ�� ����)�� 0 ���ϸ�(��Ŷ�� ���� ������) true ��ȯ
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0)
                {
                    return true;
                }
            }

            // ��Ŷ�� ���̰�
            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                // ���� ��Ŷ ������ �״�� ������
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                // ���� �����忡 �Ʒ� ���� �߰�
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        // ��Ŷ�� ���� ����
                        int _packetId = _packet.ReadInt();
                        // ��Ŷ�� ������ ���� �Լ� ����
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });

                _packetLength = 0;
                // �� ���� ���̰� 4 �̻��̸�
                if (receivedData.UnreadLength() >= 4)
                {
                    // ���� ��(ù �κ��̴ϱ� ��Ŷ�� ����)�� 0 ���ϸ�(��Ŷ�� ���� ������) true ��ȯ
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            // �� �а� ��Ŷ�� �����Ͱ� ������ true, �ƴϸ� false
            if (_packetLength <= 1)
            {
                return true;
            }
            return false;
        }

        public void Disconnect()
        {
            socket.Close();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        public IPEndPoint endPoint;

        private int id;

        public UDP(int _id)
        {
            id = _id;
        }

        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
        }

        public void SendData(Packet _packet)
        {
            // ���� ����Ʈ�� ������ ����
            Server.SendUDPData(endPoint, _packet);
        }

        public void HandleData(Packet _packetData)
        {
            int _packetLength = _packetData.ReadInt();
            byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

            // ���� �����忡 ���
            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_packetBytes))
                {
                    int _packetId = _packet.ReadInt();
                    // ��Ŷ ������ ���� �Լ� ȣ��
                    Server.packetHandlers[_packetId](id, _packet);
                }
            });
        }

        public void Disconnect()
        {
            endPoint = null;
        }
    }

    public void SendIntoGame(string _playerName)
    {
        // �� �÷��̾� ���� �ؼ� Ŭ���̾�Ʈ�� �Ҵ�
        player = NetworkManager.instance.InstantiatePlayer();
        player.Initialize(id, _playerName);

        // ����� �ٸ� ��� �÷��̾��� ������ �ڽſ��� ����
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                if (_client.id != id)
                {
                    ServerSend.SpawnPlayer(id, _client.player);
                }
            }
        }

        // ����� �ٸ� ��� Ŭ���̾�Ʈ���� �ڽ��� ���� ����
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                ServerSend.SpawnPlayer(_client.id, player);
            }
        }

        foreach (ItemSpawner _itemSpawner in ItemSpawner.spawners.Values)
        {
            ServerSend.CreateItemSpawner(id, _itemSpawner.spawnerId, _itemSpawner.transform.position, _itemSpawner.hasItem);
        }

        foreach (Enemy _enemy in Enemy.enemies.Values)
        {
            ServerSend.SpawnEnemy(id, _enemy);
        }
    }

    private void Disconnect()
    {
        Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

        ThreadManager.ExecuteOnMainThread(() =>
        {
            // ���� �����忡�� �����ǵ���
            UnityEngine.Object.Destroy(player.gameObject);
            player = null;
        });

        tcp.Disconnect();
        udp.Disconnect();

        ServerSend.PlayerDisconnect(id);
    }
}
