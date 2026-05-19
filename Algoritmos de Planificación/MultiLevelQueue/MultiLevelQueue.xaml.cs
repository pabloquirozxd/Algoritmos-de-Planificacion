using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Algoritmos_de_Planificación.MultiLevelQueue
{
    public partial class MultiLevelQueue : UserControl
    {
        private readonly ObservableCollection<PacienteMLQ> listaPacientes = new();

        private class Bloque
        {
            public int Id { get; set; }
            public int Inicio { get; set; }
            public int Fin { get; set; }
            public bool Inactivo { get; set; }
            public bool Interrumpido { get; set; }
        }

        public MultiLevelQueue()
        {
            InitializeComponent();
            dgPacientes.ItemsSource = listaPacientes;
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtArrivalTime.Text, out int arrival) || !int.TryParse(txtBurstTime.Text, out int burst) ||
                string.IsNullOrWhiteSpace(txtPaciente.Text) || string.IsNullOrWhiteSpace(txtCaso.Text) || arrival < 0 || burst <= 0)
            {
                MessageBox.Show("Complete los datos correctamente. Arrival debe ser >= 0 y Burst debe ser > 0.");
                return;
            }

            AgregarPaciente(txtPaciente.Text, txtCaso.Text, cmbCola.SelectedIndex + 1, arrival, burst);
            txtPaciente.Clear();
            txtCaso.Clear();
            txtArrivalTime.Clear();
            txtBurstTime.Clear();
            cmbCola.SelectedIndex = 0;
        }

        private void btnEjecutarNoExpropiativo_Click(object sender, RoutedEventArgs e)
        {
            Ejecutar(false);
        }

        private void btnEjecutarExpropiativo_Click(object sender, RoutedEventArgs e)
        {
            Ejecutar(true);
        }

        private void Ejecutar(bool expropiativo)
        {
            if (listaPacientes.Count == 0)
            {
                MessageBox.Show("Primero cargue pacientes.");
                return;
            }

            ReiniciarResultados();
            List<Bloque> bloques = expropiativo ? MLQExpropiativo() : MLQNoExpropiativo();

            foreach (PacienteMLQ p in listaPacientes)
                p.Estado = "Finalizado";

            dgPacientes.Items.Refresh();
            DibujarGantt(bloques);
            MostrarResumen(expropiativo);
        }

        private List<Bloque> MLQNoExpropiativo()
        {
            List<Bloque> bloques = new();
            bool[] terminado = new bool[listaPacientes.Count];
            int tiempo = 0, completados = 0;

            while (completados < listaPacientes.Count)
            {
                int i = ElegirPaciente(tiempo, x => !terminado[x]);

                if (i == -1)
                {
                    int siguiente = SiguienteLlegada(x => !terminado[x], tiempo);
                    bloques.Add(new Bloque { Id = -1, Inicio = tiempo, Fin = siguiente, Inactivo = true });
                    tiempo = siguiente;
                    continue;
                }

                int inicio = tiempo;
                tiempo += listaPacientes[i].BurstTime;
                GuardarTiempos(i, tiempo);
                terminado[i] = true;
                completados++;

                bloques.Add(new Bloque { Id = i, Inicio = inicio, Fin = tiempo });
            }

            return bloques;
        }

        private List<Bloque> MLQExpropiativo()
        {
            List<Bloque> bloques = new();
            int[] restante = listaPacientes.Select(p => p.BurstTime).ToArray();
            int tiempo = 0, completados = 0;

            while (completados < listaPacientes.Count)
            {
                int i = ElegirPaciente(tiempo, x => restante[x] > 0);

                if (i == -1)
                {
                    int siguiente = SiguienteLlegada(x => restante[x] > 0, tiempo);
                    bloques.Add(new Bloque { Id = -1, Inicio = tiempo, Fin = siguiente, Inactivo = true });
                    tiempo = siguiente;
                    continue;
                }

                int llegadaPrioritaria = ProximaLlegadaMayorPrioridad(i, restante, tiempo);
                int duracion = restante[i];
                bool seInterrumpe = llegadaPrioritaria < tiempo + duracion;

                if (seInterrumpe)
                    duracion = llegadaPrioritaria - tiempo;

                bloques.Add(new Bloque { Id = i, Inicio = tiempo, Fin = tiempo + duracion, Interrumpido = seInterrumpe });
                tiempo += duracion;
                restante[i] -= duracion;

                if (restante[i] == 0)
                {
                    GuardarTiempos(i, tiempo);
                    completados++;
                }
            }

            return bloques;
        }

        private int ElegirPaciente(int tiempo, Func<int, bool> disponible)
        {
            return Enumerable.Range(0, listaPacientes.Count)
                .Where(i => disponible(i) && listaPacientes[i].ArrivalTime <= tiempo)
                .OrderBy(i => listaPacientes[i].NivelCola)
                .ThenBy(i => listaPacientes[i].ArrivalTime)
                .ThenBy(i => i)
                .DefaultIfEmpty(-1)
                .First();
        }

        private int SiguienteLlegada(Func<int, bool> disponible, int tiempo)
        {
            return Enumerable.Range(0, listaPacientes.Count)
                .Where(i => disponible(i) && listaPacientes[i].ArrivalTime > tiempo)
                .Min(i => listaPacientes[i].ArrivalTime);
        }

        private int ProximaLlegadaMayorPrioridad(int actual, int[] restante, int tiempo)
        {
            int nivelActual = listaPacientes[actual].NivelCola;

            return Enumerable.Range(0, listaPacientes.Count)
                .Where(i => restante[i] > 0 && listaPacientes[i].NivelCola < nivelActual && listaPacientes[i].ArrivalTime > tiempo)
                .Select(i => listaPacientes[i].ArrivalTime)
                .DefaultIfEmpty(int.MaxValue)
                .Min();
        }

        private void GuardarTiempos(int i, int completion)
        {
            PacienteMLQ p = listaPacientes[i];
            p.CompletionTime = completion;
            p.TurnaroundTime = completion - p.ArrivalTime;
            p.WaitingTime = p.TurnaroundTime - p.BurstTime;
        }

        private void ReiniciarResultados()
        {
            spGantt.Children.Clear();

            foreach (PacienteMLQ p in listaPacientes)
            {
                p.CompletionTime = 0;
                p.WaitingTime = 0;
                p.TurnaroundTime = 0;
                p.Estado = "En espera";
            }
        }

        private void MostrarResumen(bool expropiativo)
        {
            txtPromedioEspera.Text = listaPacientes.Average(p => p.WaitingTime).ToString("0.00");
            txtPromedioRetorno.Text = listaPacientes.Average(p => p.TurnaroundTime).ToString("0.00");
            txtPacienteActual.Text = "Todos finalizados";
            txtColaActual.Text = expropiativo ? "MLQ Expropiativo" : "MLQ No expropiativo";
            txtDescripcionGantt.Text = expropiativo
                ? "Expropiativo: si llega una cola de mayor prioridad, interrumpe al paciente actual."
                : "No expropiativo: si un paciente empieza, termina aunque llegue una emergencia.";
            txtExplicacionFinal.Text = "Las colas se ordenan por prioridad: 1 Emergencia, 2 Urgencia y 3 Consulta. Dentro de cada cola se usa FCFS.";
        }

        private void DibujarGantt(List<Bloque> bloques)
        {
            foreach (Bloque b in bloques)
            {
                if (b.Inactivo)
                    spGantt.Children.Add(CrearTarjeta("Inactivo", "Sin pacientes listos", "", b.Inicio, b.Fin, (Brush)Application.Current.Resources["InputColor"]));
                else
                {
                    PacienteMLQ p = listaPacientes[b.Id];
                    string pausa = b.Interrumpido ? " | pausado" : "";
                    spGantt.Children.Add(CrearTarjeta(p.Proceso + " - " + p.Paciente, p.Caso, p.Cola, b.Inicio, b.Fin, ColorCola(p.NivelCola), pausa));
                }
            }
        }

        private Border CrearTarjeta(string titulo, string detalle, string cola, int inicio, int fin, Brush fondo, string extra = "")
        {
            StackPanel contenido = new();
            contenido.Children.Add(Texto(titulo, 13, FontWeights.Bold, "TextoOscuro"));
            contenido.Children.Add(Texto(detalle, 12, FontWeights.Normal, "TextoClaro"));

            if (cola != "")
                contenido.Children.Add(Texto(cola, 12, FontWeights.SemiBold, "ColorPrimario"));

            contenido.Children.Add(Texto($"{inicio} - {fin}{extra}", 12, FontWeights.Bold, "TextoOscuro"));

            return new Border
            {
                Background = fondo,
                BorderBrush = (Brush)Application.Current.Resources["ColorPrimario"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 145,
                Child = contenido
            };
        }

        private TextBlock Texto(string texto, int size, FontWeight peso, string recurso)
        {
            return new TextBlock
            {
                Text = texto,
                FontSize = size,
                FontWeight = peso,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources[recurso]
            };
        }

        private Brush ColorCola(int nivel)
        {
            if (nivel == 1) return (Brush)Application.Current.Resources["AcentoRosa"];
            if (nivel == 2) return (Brush)Application.Current.Resources["AcentoVerde"];
            return (Brush)Application.Current.Resources["InputColor"];
        }

        private string NombreCola(int nivel)
        {
            if (nivel == 1) return "1 - Emergencia grave";
            if (nivel == 2) return "2 - Urgencia media";
            return "3 - Consulta normal";
        }

        private void AgregarPaciente(string nombre, string caso, int cola, int arrival, int burst)
        {
            listaPacientes.Add(new PacienteMLQ
            {
                Proceso = "P" + (listaPacientes.Count + 1),
                Paciente = nombre.Trim(),
                Caso = caso.Trim(),
                NivelCola = cola,
                Cola = NombreCola(cola),
                ArrivalTime = arrival,
                BurstTime = burst,
                Estado = "En espera"
            });
        }

        private void btnEjemplo_Click(object sender, RoutedEventArgs e)
        {
            listaPacientes.Clear();
            spGantt.Children.Clear();

            AgregarPaciente("María", "Control general", 3, 0, 4);
            AgregarPaciente("Ana", "Accidente grave", 1, 1, 5);
            AgregarPaciente("Carlos", "Dolor fuerte", 2, 2, 3);
            AgregarPaciente("Luis", "Fiebre alta", 2, 3, 4);
            AgregarPaciente("Sofía", "Revisión médica", 3, 4, 2);

            txtPromedioEspera.Text = "0";
            txtPromedioRetorno.Text = "0";
            txtPacienteActual.Text = "Ninguno";
            txtColaActual.Text = "Ninguna";
            txtDescripcionGantt.Text = "Ejecuta los dos modos para comparar cómo cambia el orden de atención.";
            txtExplicacionFinal.Text = "Este ejemplo permite explicar claramente la diferencia entre MLQ no expropiativo y MLQ expropiativo.";
            dgPacientes.Items.Refresh();
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            listaPacientes.Clear();
            spGantt.Children.Clear();
            txtPaciente.Clear();
            txtCaso.Clear();
            txtArrivalTime.Clear();
            txtBurstTime.Clear();
            cmbCola.SelectedIndex = 0;
            txtPromedioEspera.Text = "0";
            txtPromedioRetorno.Text = "0";
            txtPacienteActual.Text = "Ninguno";
            txtColaActual.Text = "Ninguna";
            txtDescripcionGantt.Text = "Muestra cómo el hospital atiende a los pacientes según el modo seleccionado.";
            txtExplicacionFinal.Text = "No expropiativo: no interrumpe. Expropiativo: sí interrumpe si llega una cola de mayor prioridad.";
        }
    }
}
