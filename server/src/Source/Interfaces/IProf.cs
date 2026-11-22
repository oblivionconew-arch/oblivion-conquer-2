using System;

namespace Conquer_Online_Server.Interfaces
{
    public interface IProf
    {
        uint Experience { get; set; }
      
        ushort ID { get; set; }
        byte Level { get; set; }
        byte PreviousLevel { get; set; }
        bool Available { get; set; }
        byte TempLevel { get; set; }
        void Send(Client.GameClient client);
    }
}
