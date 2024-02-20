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
            // socket에 TcpClient 할당 및 송수신 buffer 크기 할당
            socket = _socket;
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            // 데이터를 보내고 받는 NetworkStream 할당
            stream = socket.GetStream();

            receivedData = new Packet();
            receiveBuffer = new byte[dataBufferSize];

            // NetworkStream에서 비동기 읽기 시작
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

            // 서버에서 welcome 패킷 보내도록
            ServerSend.Welcome(id, "Welcome to the server!");
        }

        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    // 스트림에 대한 비동기 쓰기 시작
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
                // 비동기 읽기를 끝내고 읽은 바이트 수 반환
                int _byteLength = stream.EndRead(_result);
                if (_byteLength <= 0)
                {
                    Server.clients[id].Disconnect();
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                receivedData.Reset(HandleData(_data));
                // 다시 NetworkStream에서 비동기 읽기 시작
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

            // receivedData에 읽은 데이터 삽입
            receivedData.SetBytes(_data);

            // 안 읽은 데이터길이가 4 이상이면
            if (receivedData.UnreadLength() >= 4)
            {
                // 읽은 값(첫 부분이니까 패킷의 길이)이 0 이하면(패킷에 담긴게 없으면) true 반환
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0)
                {
                    return true;
                }
            }

            // 패킷의 길이가
            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                // 읽은 패킷 데이터 그대로 가져옴
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                // 메인 스레드에 아래 내용 추가
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        // 패킷의 종류 읽음
                        int _packetId = _packet.ReadInt();
                        // 패킷의 종류에 따라 함수 실행
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });

                _packetLength = 0;
                // 안 읽은 길이가 4 이상이면
                if (receivedData.UnreadLength() >= 4)
                {
                    // 읽은 값(첫 부분이니까 패킷의 길이)이 0 이하면(패킷에 담긴게 없으면) true 반환
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            // 다 읽고 패킷에 데이터가 없으면 true, 아니면 false
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
            // 엔드 포인트로 데이터 전송
            Server.SendUDPData(endPoint, _packet);
        }

        public void HandleData(Packet _packetData)
        {
            int _packetLength = _packetData.ReadInt();
            byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

            // 메인 스레드에 등록
            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_packetBytes))
                {
                    int _packetId = _packet.ReadInt();
                    // 패킷 종류에 따라 함수 호출
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
        // 새 플레이어 생성 해서 클라이언트에 할당
        player = NetworkManager.instance.InstantiatePlayer();
        player.Initialize(id, _playerName);

        // 연결된 다른 모든 플레이어의 정보를 자신에게 전달
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

        // 연결된 다른 모든 클라이언트에게 자신의 정보 전달
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
            // 메인 스레드에서 삭제되도록
            UnityEngine.Object.Destroy(player.gameObject);
            player = null;
        });

        tcp.Disconnect();
        udp.Disconnect();

        ServerSend.PlayerDisconnect(id);
    }
}
