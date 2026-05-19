using System;
using System.Collections.Generic;
using System.Text;

namespace Algoritmos_de_Planificación.MultiLevelQueue
{
    public class PacienteMLQ
    {
        public string Proceso { get; set; } = "";
        public string Paciente { get; set; } = "";
        public string Caso { get; set; } = "";
        public int NivelCola { get; set; }
        public string Cola { get; set; } = "";
        public int ArrivalTime { get; set; }
        public int BurstTime { get; set; }
        public int CompletionTime { get; set; }
        public int WaitingTime { get; set; }
        public int TurnaroundTime { get; set; }
        public string Estado { get; set; } = "";
    }
}
