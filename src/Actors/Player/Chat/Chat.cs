using Akka.Actor;
using FLServer.Chat;

namespace FLServer.Actors.Player.Chat
{
    class Chat : TypedActor, IHandle<SystemMessage>,IHandle<LocalMessage>,IHandle<ConsoleMessage>
    {
        public void Handle(SystemMessage message)
        {
            
        }

        public void Handle(LocalMessage message)
        {
            var rdl = new Rdl();
            rdl.AddTRA(0xFFFFFF00, 0xFFFFFFFF);
            rdl.AddText(message.Name + ": ");
            rdl.AddTRA(0xFF8F4000, 0xFFFFFFFF);
            rdl.AddText(message.Message);
            SendChatToPlayer(rdl);
        }

        public void Handle(ConsoleMessage message)
        {
            
        }

        public void SendChatToPlayer(Rdl rdl)
        {
            byte[] omsg = { 0x05, 0x01 };
            FLMsgType.AddInt32(ref omsg, rdl.GetBytes().Length);
            FLMsgType.AddArray(ref omsg, rdl.GetBytes());
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddUInt32(ref omsg, 0); 
            Context.Sender.Tell(omsg);
        }

        public void SendChat(byte[] msg)
        {
            byte[] omsg = { 0x05, 0x01 };
            FLMsgType.AddInt32(ref omsg, msg.Length);
            FLMsgType.AddArray(ref omsg, msg);
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddUInt32(ref omsg, 0);
            Context.Sender.Tell(omsg);
        }
    }
}
