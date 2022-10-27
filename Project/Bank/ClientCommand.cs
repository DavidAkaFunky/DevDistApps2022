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

        //Identification
        private int clientID;
        private int clientSeqNumber;

        //Content
        private string type;
        private double amount;

        public ClientCommand(int slot, int clientID, int clientSeqNumber, string type, double amount)
        {
            this.slot = slot;
            this.clientID = clientID;
            this.clientSeqNumber = clientSeqNumber;
            this.type = type;
            this.amount = amount;
        }

        public ClientCommand(ClientCommand cc)
        {
            this.slot = cc.slot;
            this.clientID = cc.clientID;
            this.clientSeqNumber = cc.clientSeqNumber;
            this.type = cc.type;
            this.amount = cc.amount;
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

        public ClientCommandGRPC CreateCommandGRPC(int globalSeqNumber)
        {
            return new() { 
                Slot = slot, 
                ClientId = clientID, 
                ClientSeqNumber = clientSeqNumber, 
                Type = type, 
                Amount = amount, 
                GlobalSeqNumber = globalSeqNumber };
        }

        public static ClientCommand CreateCommandFromGRPC(ClientCommandGRPC command)
        {
            return new(command.Slot, command.ClientId, command.ClientSeqNumber, command.Type, command.Amount);
        }
    }
}
