using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADProject
{
    public class ClientCommand
    {
        private int slot;
        private int clientID;
        private int clientSeqNumber;
        private string message;

        public ClientCommand(int slot, int clientID, int clientSeqNumber, string message)
        {
            this.slot = slot;
            this.clientID = clientID;
            this.clientSeqNumber = clientSeqNumber;
            this.message = message;
        }

        public int Slot
        {
            get { return slot; }
            set { slot = value; }
        }

        public int ClientID
        {
            get { return clientID; }
            set { clientID = value; }
        }

        public int ClientSeqNumber
        {
            get { return clientSeqNumber; }
            set { clientSeqNumber = value; }
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public ClientCommandGRPC CreateCommandGRPC(int globalSeqNumber)
        {
            return new() { Slot = slot, ClientId = clientID, ClientSeqNumber = clientSeqNumber, Message = message, GlobalSeqNumber = globalSeqNumber };
        }

        public static ClientCommand CreateCommandFromGRPC(ClientCommandGRPC command)
        {
            return new(command.Slot, command.ClientId, command.ClientSeqNumber, command.Message);
        }
    }
}
