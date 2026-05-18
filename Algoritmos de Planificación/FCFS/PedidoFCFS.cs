using System;
using System.Collections.Generic;
using System.Text;

namespace Algoritmos_de_Planificación.FCFS
{
    public class PedidoFCFS
    {
        public string Proceso { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Pedido { get; set; } = "";
        public int BurstTime { get; set; }
        public int WaitingTime { get; set; }
        public int TurnaroundTime { get; set; }
        public string Estado { get; set; } = "";
    }
}
