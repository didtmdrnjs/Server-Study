using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ServerSend 
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
            _packet.Write(_player.transform.position);
            _packet.Write(_player.transform.rotation);

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
            _packet.Write(_player.transform.position);

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
            _packet.Write(_player.transform.rotation);

            // 자신 제외 모든 클라이언트한테 UDP로 전송
            SendUDPDataToAll(_player.id, _packet);
        }
    }

    public static void PlayerDisconnect(int _playerId)
    {
        // playerDisconnect 패킷 생성
        using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            // 플레이어 id 삽입
            _packet.Write(_playerId);

            // 모든 클라이언트한테 TCP로 전송
            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerHealth(Player _player)
    {
        // playerHealth 패킷 생성
        using (Packet _packet = new Packet((int)ServerPackets.playerHealth))
        {
            // 플레이어 id, health 삽입
            _packet.Write(_player.id);
            _packet.Write(_player.health);

            // 모든 클라이언트한테 TCP로 전송
            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerRespawned(Player _player)
    {
        // playerRespawned 패킷 생성
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawned))
        {
            // 플레이어 id, health 삽입
            _packet.Write(_player.id);
            
            // 모든 클라이언트한테 TCP로 전송
            SendTCPDataToAll(_packet);
        }
    }

    public static void CreateItemSpawner(int _toClient, int _spawnerId, Vector3 _spawnerPosition, bool _hasItem)
    {
        // CreateItemSpawner 패킷 생성
        using (Packet _packet = new Packet((int)ServerPackets.createItemSpawner))
        {
            // ItemSpawner 데이터 삽입
            _packet.Write(_spawnerId);
            _packet.Write(_spawnerPosition);
            _packet.Write(_hasItem);

            // TCP 전송
            SendTCPData(_toClient, _packet);
        }
    }

    public static void ItemSpawned(int _spawnerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.itemSpawned))
        {
            _packet.Write(_spawnerId);

            SendTCPDataToAll(_packet);
        }
    }

    public static void ItemPickedUp(int _spawnerId, int _byPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.itemPickedUp))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_byPlayer);

            SendTCPDataToAll(_packet);
        }
    }

    public static void SpawnProjectile(Projectile _projectile, int _thrownByPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnProjectile))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);
            _packet.Write(_thrownByPlayer);

            SendTCPDataToAll(_packet);
        }
    }

    public static void ProjectilePosition(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectilePosition))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            SendUDPDataToAll(_packet);
        }
    }
    
    public static void ProjectileExploded(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectileExploded))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            SendTCPDataToAll(_packet);
        }
    }

    public static void SpawnEnemy(Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnEnemy))
        {
            _packet.Write(_enemy.id);
            _packet.Write(_enemy.transform.position);
            SendTCPDataToAll(_packet);
        }
    }

    public static void SpawnEnemy(int _toClient, Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnEnemy))
        {
            _packet.Write(_enemy.id);
            _packet.Write(_enemy.transform.position);
            SendTCPData(_toClient, _packet);
        }
    }

    public static void EnemyPosition(Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.enemyPosition))
        {
            _packet.Write(_enemy.id);
            _packet.Write(_enemy.transform.position);

            SendUDPDataToAll(_packet);
        }
    }

    public static void EnemyHealth(Enemy _enemy)
    {
        using (Packet _packet = new Packet((int)ServerPackets.enemyHealth))
        {
            _packet.Write(_enemy.id);
            _packet.Write(_enemy.health);

            SendUDPDataToAll(_packet);
        }
    }
    #endregion
}
