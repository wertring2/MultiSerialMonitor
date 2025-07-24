namespace MultiSerialMonitor.Exceptions
{
    public class PortNotFoundException : Exception
    {
        public PortNotFoundException() : base() { }
        
        public PortNotFoundException(string message) : base(message) { }
        
        public PortNotFoundException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}