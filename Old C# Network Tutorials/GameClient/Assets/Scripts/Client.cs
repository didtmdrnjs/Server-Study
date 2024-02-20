using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using Unity.VisualScripting;

public class Client : MonoBehaviour
{
    public static Client instance;
    public static int dataBufferSize = 4096;

    public string ip = "127.0.0.1";
    public int port = 26950;
    public int myId = 0;
    public TCP tcp;
    public UDP udp;

    private bool isConnected = false;
    private delegate void PacketHandler(Packet _packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    // ���ø����̼��� ����� �� ������ ���� �Ǿ����� ������ ���ο��� ���� ����
    private void OnApplicationQuit()
    {
        Disconnect();
    }

    public void ConnectToServer()
    {
        tcp = new TCP();
        udp = new UDP();

        InitializeClientData();

        isConnected = true;
        tcp.Connect();
    }

    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        public void Connect()
        {
            // socket�� TcpClient ��ü�� �����ؼ� �Ҵ�
            socket = new TcpClient()
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            // ���� ȣ��Ʈ ���ῡ ���� �񵿱� ��û ����
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
        }

        private void ConnectCallback(IAsyncResult _result)
        {
            // ���� ���� �񵿱� ���� �õ��� ����
            socket.EndConnect(_result);

            if (!socket.Connected)
            {
                return;
            }

            // �����͸� ������ �޴� NetworkStream �Ҵ�
            stream = socket.GetStream();

            receivedData = new Packet();

            // NetworkStream���� �񵿱� �б� ����
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
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
                Debug.Log($"Error sending data to server via TCP: {_ex}");
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
                    instance.Disconnect();
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                receivedData.Reset(HandleData(_data));
                // �ٽ� NetworkStream���� �񵿱� �б� ����
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                Disconnect();
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
                        packetHandlers[_packetId](_packet);
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

        private void Disconnect()
        {
            instance.Disconnect();

            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            // �����͸� ���� ��Ʈ��ũ ���� ����Ʈ�� ����
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        public void Connect(int _localPort)
        {
            socket = new UdpClient(_localPort);

            // ��������Ʈ�� ����Ͽ� �⺻ ���� ȣ��Ʈ ����
            socket.Connect(endPoint);
            // ���� ȣ��Ʈ�� ���� �񵿱������� �����ͱ׷��� ����
            socket.BeginReceive(ReceiveCallback, null);

            // ������ ����
            using (Packet _packet = new Packet())
            {
                SendData(_packet);
            }
        }

        public void SendData(Packet _packet)
        {
            try
            {
                // ��Ŷ�� Ŭ���̾�Ʈ Id����
                _packet.InsertInt(instance.myId);
                if (socket != null)
                {
                    // socket���� ������ ����
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via UDP: {_ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                // ���� ���� �񵿱� ������ �����ϰ�, ���� �����ͱ׷� ����
                byte[] _data = socket.EndReceive(_result, ref endPoint);
                // �񵿱� ���� �����
                socket.BeginReceive(ReceiveCallback, null);

                if (_data.Length < 4)
                {
                    instance.Disconnect();
                    return;
                }

                HandleData(_data);
            }
            catch
            {
                Disconnect();
            }
        }

        private void HandleData(byte[] _data)
        {
            using (Packet _packet = new Packet(_data))
            {
                int _packetLength = _packet.ReadInt();
                _data = _packet.ReadBytes(_packetLength);
            }

            // ���� �����忡 ���
            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_data))
                {
                    int _packetId = _packet.ReadInt();
                    // ��Ŷ ������ ���� ����
                    packetHandlers[_packetId](_packet);
                }
            });
        }

        private void Disconnect()
        {
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }

    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            // ��Ŷ ����, ������ �Լ� ����
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.spawnPlayer, ClientHandle.SpawnPlayer },
            { (int)ServerPackets.playerPosition, ClientHandle.PlayerPosition },
            { (int)ServerPackets.playerRotation, ClientHandle.PlayerRotation },
            { (int)ServerPackets.playerDisconnected, ClientHandle.PlayerDisconnected },
            { (int)ServerPackets.playerHealth, ClientHandle.PlayerHealth },
            { (int)ServerPackets.playerRespawned, ClientHandle.PlayerRespawned },
            { (int)ServerPackets.createItemSpawner, ClientHandle.CreateItemSpawner },
            { (int)ServerPackets.itemSpawned, ClientHandle.ItemSpawned },
            { (int)ServerPackets.itemPickedUp, ClientHandle.ItemPickedUp },
            { (int)ServerPackets.spawnProjectile, ClientHandle.SpawnProjectile },
            { (int)ServerPackets.projectilePosition, ClientHandle.ProjectilePosition },
            { (int)ServerPackets.projectileExploded, ClientHandle.ProjectileExploded },
            { (int)ServerPackets.spawnEnemy, ClientHandle.SpawnEnemy },
            { (int)ServerPackets.enemyPosition, ClientHandle.EnemyPosition },
            { (int)ServerPackets.enemyHealth, ClientHandle.EnemyHealth }
        };
        Debug.Log("Initialized packets.");
    }

    private void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close();
            udp.socket.Close();

            Debug.Log("Disconnected from server.");
        }
    }
}