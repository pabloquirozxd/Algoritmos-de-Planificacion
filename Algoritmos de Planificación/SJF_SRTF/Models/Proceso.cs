namespace Algoritmos_de_Planificación.SJF_SRTF.Models
{
    public class Proceso
    {
        public string Nombre { get; set; }
        public int TiempoLlegada { get; set; }
        public int RafagaCPU { get; set; }
        public int TiempoInicio { get; set; }
        public int TiempoFin { get; set; }
        public int TiempoRetorno { get; set; }
        public int TiempoEspera { get; set; }
        public int RafagaRestante { get; set; }

        public Proceso(string nombre, int llegada, int rafaga)
        {
            Nombre = nombre;
            TiempoLlegada = llegada;
            RafagaCPU = rafaga;
            RafagaRestante = rafaga;
            TiempoInicio = -1;
            TiempoFin = 0;
        }
    }
}