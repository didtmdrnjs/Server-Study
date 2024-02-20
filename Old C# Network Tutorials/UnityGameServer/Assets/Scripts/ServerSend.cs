using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ServerSend 
{
    private static void SendTCPData(int _toClient, Packet _packet)
    {
        // ��Ŷ�� ���̸� ��Ŷ�� ���Խ�Ŵ
        _packet.WriteLength();
        // _toClient���� SendData()�� ����
        Server.clients[_toClient].tcp.SendData(_packet);
    }

    private static void SendUDPData(int _toClient, Packet _packet)
    {
        // ��Ŷ ���� ����
        _packet.WriteLength();
        Server.clients[_toClient].udp.SendData(_packet);
    }

    private static void SendTCPDataToAll(Packet _packet)
    {
        // ��Ŷ�� ���̸� ��Ŷ�� ���Խ�Ŵ
        _packet.WriteLength();
        // ��� Ŭ���̾�Ʈ�� SendData() ����
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(_packet);
        }
    }

    private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
    {
        // ��Ŷ�� ���̸� ��Ŷ�� ���Խ�Ŵ
        _packet.WriteLength();
        // Ư�� Ŭ���̾�Ʈ�� ������ ������ Ŭ���̾�Ʈ�� SendData() ����
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
        // ��Ŷ�� ���̸� ��Ŷ�� ���Խ�Ŵ
        _packet.WriteLength();
        // ��� Ŭ���̾�Ʈ�� SendData() ����
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].udp.SendData(_packet);
        }
    }

    private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
    {
        // ��Ŷ�� ���̸� ��Ŷ�� ���Խ�Ŵ
        _packet.WriteLength();
        // Ư�� Ŭ���̾�Ʈ�� ������ ������ Ŭ���̾�Ʈ�� SendData() ����
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
        // welcome ��Ŷ ����
        using (Packet _packet = new Packet((int)ServerPackets.welcome))
        {
            // ��Ŷ�� �޽���, Ŭ���̾�Ʈ ��ȣ ����
            _packet.Write(_msg);
            _packet.Write(_toClient);

            // SendTCPData() ����
            SendTCPData(_toClient, _packet);
        }
    }

    public static void SpawnPlayer(int _toClient, Player _player)
    {
        // SpawnPlayer ��Ŷ ����
        using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            // ��Ŷ�� �÷��̾� ���� ����
            _packet.Write(_player.id);
            _packet.Write(_player.username);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.transform.rotation);

            // SendTCPData() ����
            SendTCPData(_toClient, _packet);
        }
    }

    public static void PlayerPosition(Player _player)
    {
        // playerPosition ��Ŷ ����
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            // �÷��̾� id, ���� ��ġ ����
            _packet.Write(_player.id);
            _packet.Write(_player.transform.position);

            // ��� Ŭ���̾�Ʈ���� UDP�� ����
            SendUDPDataToAll(_packet);
        }
    }

    public static void PlayerRotation(Player _player)
    {
        // playerPosition ��Ŷ ����
        using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
        {
            // �÷��̾� id, ���� ����
            _packet.Write(_player.id);
            _packet.Write(_player.transform.rotation);

            // �ڽ� ���� ��� Ŭ���̾�Ʈ���� UDP�� ����
            SendUDPDataToAll(_player.id, _packet);
        }
    }

    public static void PlayerDisconnect(int _playerId)
    {
        // playerDisconnect ��Ŷ ����
        using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            // �÷��̾� id ����
            _packet.Write(_playerId);

            // ��� Ŭ���̾�Ʈ���� TCP�� ����
            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerHealth(Player _player)
    {
        // playerHealth ��Ŷ ����
        using (Packet _packet = new Packet((int)ServerPackets.playerHealth))
        {
            // �÷��̾� id, health ����
            _packet.Write(_player.id);
            _packet.Write(_player.health);

            // ��� Ŭ���̾�Ʈ���� TCP�� ����
            SendTCPDataToAll(_packet);
        }
    }

    public static void PlayerRespawned(Player _player)
    {
        // playerRespawned ��Ŷ ����
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawned))
        {
            // �÷��̾� id, health ����
            _packet.Write(_player.id);
            
            // ��� Ŭ���̾�Ʈ���� TCP�� ����
            SendTCPDataToAll(_packet);
        }
    }

    public static void CreateItemSpawner(int _toClient, int _spawnerId, Vector3 _spawnerPosition, bool _hasItem)
    {
        // CreateItemSpawner ��Ŷ ����
        using (Packet _packet = new Packet((int)ServerPackets.createItemSpawner))
        {
            // ItemSpawner ������ ����
            _packet.Write(_spawnerId);
            _packet.Write(_spawnerPosition);
            _packet.Write(_hasItem);

            // TCP ����
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
