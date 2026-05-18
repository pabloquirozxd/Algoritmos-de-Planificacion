using System;
using System.Collections.Generic;
using System.Text;

namespace Algoritmos_de_Planificación.Prioridad
{
    public class TicketPrioridad
    {
        public string Proceso { get; set; } = "";
        public string Ticket { get; set; } = "";
        public int ArrivalTime { get; set; }
        public int BurstTime { get; set; }
        public int Prioridad { get; set; }
        public int RemainingTime { get; set; }
        public int CompletionTime { get; set; }
        public int WaitingTime { get; set; }
        public int TurnaroundTime { get; set; }
        public string Estado { get; set; } = "";
    }
}
