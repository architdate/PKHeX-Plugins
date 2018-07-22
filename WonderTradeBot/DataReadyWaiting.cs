namespace pkmn_ntr.Helpers
{
    public class DataReadyWaiting
    {
        public byte[] Data;
        public object Arguments;
        public DataHandler Handler;

        public delegate void DataHandler(object data_arguments);

        public DataReadyWaiting(byte[] data, DataHandler handler, object arguments)
        {
            Data = data;
            Handler = handler;
            Arguments = arguments;
        }
    }
}