using System;
using System.Collections.Generic;
using System.Text;

namespace Algoritmos_de_Planificación.Round_Robin
{
    public class ProcesoRR
    {
        public string IdProceso { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Problema { get; set; } = "";

        // Tiempo total que requiere el proceso para terminar
        public int BurstTime { get; set; }

        // Guarda el progreso del proceso cuando es interrumpido
        public int TiempoRestante { get; set; }

        public int WaitingTime { get; set; }
        public int TurnaroundTime { get; set; }

        // Manejo de estados (Listo, En Ejecución, Bloqueado, Finalizado) como pide la rúbrica
        public string Estado { get; set; } = "Listo";
    }
}
