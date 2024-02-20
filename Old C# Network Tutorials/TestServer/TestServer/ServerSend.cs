using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    internal class ServerSend
    {
        private static void SendTCPData(int _toClient, Packet _packet)
        {
            // 패킷의 길이를 패킷에 포함시킴
            _packet.WriteLength();
            // _toClient에의 SendData()를 실행
            Server.clients[_toClient].tcp.SendData(_packet);
        }

        private static void SendUDPData(int _toClient, Packet _packet)
        {
            // 패킷 길이 삽입
            _packet.WriteLength();
            Server.clients[_toClient].udp.SendData(_packet);
        }

        private static void SendTCPDataToAll(Packet _packet)
        {
            // 패킷의 길이를 패킷에 포함시킴
            _packet.WriteLength();
            // 모든 클라이언트의 SendData() 실행
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }

        private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
        {
            // 패킷의 길이를 패킷에 포함시킴
            _packet.WriteLength();
            // 특정 클라이언트를 제외한 나머지 클라이언트의 SendData() 실행
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].tcp.SendData(_packet);
                }
            }
        }

        private static void SendUDPDataToAll(Packet _packet)
        {
            // 패킷의 길이를 패킷에 포함시킴
            _packet.WriteLength();
            // 모든 클라이언트의 SendData() 실행
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }

        private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
        {
            // 패킷의 길이를 패킷에 포함시킴
            _packet.WriteLength();
            // 특정 클라이언트를 제외한 나머지 클라이언트의 SendData() 실행
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].udp.SendData(_packet);
                }
            }
        }

        #region Packets
        public static void Welcome(int _toClient, string _msg)
        {
            // welcome 패킷 생성
            using (Packet _packet = new Packet((int)ServerPackets.welcome))
            {
                // 패킷에 메시지, 클라이언트 번호 삽입
                _packet.Write(_msg);
                _packet.Write(_toClient);
                
                // SendTCPData() 실행
                SendTCPData(_toClient, _packet);
            }
        }

        public static void SpawnPlayer(int _toClient, Player _player)
        {
            // SpawnPlayer 패킷 생성
            using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
            {
                // 패킷에 플레이어 정보 삽입
                _packet.Write(_player.id);
                _packet.Write(_player.username);
                _packet.Write(_player.position);
                _packet.Write(_player.rotation);

                // SendTCPData() 실행
                SendTCPData(_toClient, _packet);
            }
        }

        public static void PlayerPosition(Player _player)
        {
            // playerPosition 패킷 생성
            using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
            {
                // 플레이어 id, 계산된 위치 삽입
                _packet.Write(_player.id);
                _packet.Write(_player.position);

                // 모든 클라이언트한테 UDP로 전송
                SendUDPDataToAll(_packet);
            }
        }

        public static void PlayerRotation(Player _player)
        {
            // playerPosition 패킷 생성
            using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
            {
                // 플레이어 id, 방향 삽입
                _packet.Write(_player.id);
                _packet.Write(_player.rotation);

                // 자신 제외 모든 클라이언트한테 UDP로 전송
                SendUDPDataToAll(_player.id, _packet);
            }
        }
        #endregion
    }
}
