using System;

namespace MudaeFarm
{
    /// <summary>
    /// An exception that indicates that the bot should restart.
    /// </summary>
    public class DummyRestartException : Exception
    {
        public bool Delayed { get; set; } = true;
    }
}