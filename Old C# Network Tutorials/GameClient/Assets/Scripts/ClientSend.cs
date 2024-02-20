using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{
    private static void SendTCPData(Packet _packet)
    {
        // ��Ŷ�� ������ �������� ���� �߰�
        _packet.WriteLength();
        // ������ ����
        Client.instance.tcp.SendData(_packet);
    }

    private static void SendUDPData(Packet _packet)
    {
        // ��Ŷ�� ������ ���� ����
        _packet.WriteLength();
        // ������ ����
        Client.instance.udp.SendData(_packet);
    }

    #region Packets
    public static void WelcomeReceived()
    {
        // welcomeReceived ��Ŷ ���� �� ����
        using (Packet _packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            _packet.Write(Client.instance.myId);
            _packet.Write(UIManager.instance.usernameField.text);

            SendTCPData(_packet);
        }
    }

    public static void PlayerMovement(bool[] _inputs)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerMovement))
        {
            // �Է� ���� ����
            _packet.Write(_inputs.Length);
            // �Է� ����
            foreach (bool _input in _inputs)
            {
                _packet.Write(_input);
            }
            // �÷��̾� ���� ����
            _packet.Write(GameManager.players[Client.instance.myId].transform.rotation);

            // UDP ����
            SendUDPData(_packet);
        }
    }

    public static void PlayerShoot(Vector3 facing)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerShoot))
        {
            // ���� ����
            _packet.Write(facing);

            // TCP ����
            SendTCPData(_packet);
        }
    }

    public static void PlayerThrowItem(Vector3 facing)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerThrowItem))
        {
            _packet.Write(facing);

            SendTCPData(_packet);
        }
    }
    #endregion
}
