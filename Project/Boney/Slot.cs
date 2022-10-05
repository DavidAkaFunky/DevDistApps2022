namespace DADProject
{
    public class Slot{
        private int currentValue;
        private int writeTimestamp;
        private int readTimestamp;

        public Slot(int currentValue, int writeTimestamp, int readTimestamp)
        {
            this.currentValue = currentValue;
            this.writeTimestamp = writeTimestamp;
            this.readTimestamp = readTimestamp;
        }

        public int CurrentValue {
            get { return currentValue; }
            set { currentValue = value; }
        }

        public int WriteTimestamp {
            get { return writeTimestamp; }
            set { writeTimestamp = value; }
        }

        public int ReadTimestamp {
            get { return readTimestamp; } 
            set { readTimestamp = value; }
        }
    }
}