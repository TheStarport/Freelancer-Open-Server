using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DPlay = Microsoft.DirectX.DirectPlay;
using Microsoft.DirectX.DirectPlay;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace FLServer
{
    class DPCConnectingToSlaveServerState : DPClientControllerState
    {
        public string StateName()
        {
            return "connecting-to-slave-server-state";
        }

        public void EnterState(DPClientController controller)
        {
            controller.ConnectToSlaveServer();
        }

        public void RxMsgFromClient(DPClientController controller, byte[] msg)
        {
            if (msg[0] == 0x01 && msg.Length == 1)
            {
                if (controller.pendingMsgs.Count > 0)
                {
                    controller.SendMessageToClient(controller.pendingMsgs.Dequeue());
                }
                else
                {
                    // Keepalive
                    DPlay.NetworkPacket keepalive = new DPlay.NetworkPacket();
                    keepalive.Write(new byte[] { 0xFF });
                    controller.SendMessageToClient(keepalive);
                }
            }
        }

        public void RxMsgFromSlaveServer(DPClientController controller, byte[] msg)
        {
            if (msg[0] == 0x01)
            {
                int pos = 2;
                uint iclient = FLMsgType.GetUInt32(msg, ref pos);

                byte[] omsg = new byte[] { 0x01, 0x02 };
                FLMsgType.AddUInt32(ref omsg, iclient);
                DPlay.NetworkPacket connectack = new DPlay.NetworkPacket();
                connectack.Write(omsg); // change to real client ID 
                controller.SendMessageToClient(connectack);
            }
            else if (msg[0] == 0x54)
            {
                byte[] omsg = new byte[] { 0x01, 0x03, 0x01 };
                FLMsgType.AddUnicodeString(ref omsg, controller.accountid);
                DPlay.NetworkPacket login = new DPlay.NetworkPacket();
                login.Write(omsg);
                controller.SendMessageToSlaveServer(login);
            }
            else if (msg[0] == 0x02 && msg[1] == 0x02 && msg[2] == 0x03)
            {
                DPlay.NetworkPacket characterinforequest = new DPlay.NetworkPacket();
                characterinforequest.Write(new byte[] { 0x05, 0x03 });
                controller.SendMessageToSlaveServer(characterinforequest);
            }
            else if (msg[0] == 0x03 && msg[1] == 0x02)
            {
                byte[] omsg = new byte[] { 0x06, 0x03 };
                FLMsgType.AddAsciiString(ref omsg, controller.charfile);

                DPlay.NetworkPacket characterselect = new DPlay.NetworkPacket();
                characterselect.Write(omsg);
                controller.SendMessageToSlaveServer(characterselect);
                controller.SetState(DPCConnectedToSlaveServerState.Instance());
            }
        }

        public void ConnectedToSlaveServer(DPClientController controller)
        {
        }

        public void DisconnectedFromSlaveServer(DPClientController controller)
        {
            // Invalid event, break the connection.
            controller.DisconnectFromSlaveServer();
        }

        static DPCConnectingToSlaveServerState instance;

        public static DPCConnectingToSlaveServerState Instance()
        {
            if (instance == null)
                instance = new DPCConnectingToSlaveServerState();
            return instance;
        }
    }


    class DPCConnectedToSlaveServerState : DPClientControllerState
    {
        public string StateName()
        {
            return "connected-to-slave-server-state";
        }

        public void EnterState(DPClientController controller)
        {
        }

        public void RxMsgFromClient(DPClientController controller, byte[] msg)
        {
            if (msg[0] == 0x05 && msg.Length == 2)
            {
                // Char info request
                controller.DisconnectFromSlaveServer();
                controller.SetState(DPCSelectingCharacterState.Instance());
                return;
            }

            DPlay.NetworkPacket npkt = new DPlay.NetworkPacket();
            npkt.Write(msg);
            controller.SendMessageToSlaveServer(npkt);
        }

        public void RxMsgFromSlaveServer(DPClientController controller, byte[] msg)
        {
            DPlay.NetworkPacket npkt = new DPlay.NetworkPacket();
            npkt.Write(msg);
            controller.SendMessageToClient(npkt);
        }

        public void ConnectedToSlaveServer(DPClientController controller)
        {
        }

        public void DisconnectedFromSlaveServer(DPClientController controller)
        {
        }

        static DPCConnectedToSlaveServerState instance;

        public static DPCConnectedToSlaveServerState Instance()
        {
            if (instance == null)
                instance = new DPCConnectedToSlaveServerState();
            return instance;
        }
    }

    class DPTestSlaveServerState : DPClientControllerState
    {
        public string StateName()
        {
            return "test-slave-server-state";
        }

        public void EnterState(DPClientController controller)
        {
            controller.ConnectToSlaveServer();
        }

        public void RxMsgFromClient(DPClientController controller, byte[] msg)
        {
            DPlay.NetworkPacket npkt = new DPlay.NetworkPacket();
            npkt.Write(msg);
            controller.SendMessageToSlaveServer(npkt);
        }

        public void RxMsgFromSlaveServer(DPClientController controller, byte[] msg)
        {
            DPlay.NetworkPacket npkt = new DPlay.NetworkPacket();
            npkt.Write(msg);
            controller.SendMessageToClient(npkt);
        }

        public void ConnectedToSlaveServer(DPClientController controller)
        {
        }

        public void DisconnectedFromSlaveServer(DPClientController controller)
        {
        }

        static DPTestSlaveServerState instance;

        public static DPTestSlaveServerState Instance()
        {
            if (instance == null)
                instance = new DPTestSlaveServerState();
            return instance;
        }
    }
}