using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADProject
{
    public class ClientCommand
    {
        private int clientID;
        private int clientSeqNumber;
        private string message;

        public ClientCommand(int clientID, int clientSeqNumber, string message)
        {
            this.clientID = clientID;
            this.clientSeqNumber = clientSeqNumber;
            this.message = message;
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
    }
}
