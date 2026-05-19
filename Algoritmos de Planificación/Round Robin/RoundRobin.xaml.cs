using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Algoritmos_de_Planificación.Round_Robin
{
    /// <summary>
    /// Interaction logic for RoundRobin.xaml
    /// </summary>
    public partial class RoundRobin : UserControl
    {
        private List<ProcesoRR> listaProcesos = new List<ProcesoRR>();
        private Dictionary<string, SolidColorBrush> coloresProcesos = new Dictionary<string, SolidColorBrush>();
        private Random rnd = new Random();

        public RoundRobin()
        {
            InitializeComponent();
            ActualizarTabla();
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCliente.Text) ||
                string.IsNullOrWhiteSpace(txtProblema.Text) ||
                !int.TryParse(txtBurstTime.Text, out int burstTime) || burstTime <= 0)
            {
                MessageBox.Show("Por favor, ingrese datos válidos. Burst Time debe ser un número mayor a 0.");
                return;
            }

            string nuevoId = "TKT-" + (listaProcesos.Count + 1);

            ProcesoRR nuevoProceso = new ProcesoRR
            {
                IdProceso = nuevoId,
                Cliente = txtCliente.Text,
                Problema = txtProblema.Text,
                BurstTime = burstTime,
                TiempoRestante = burstTime,
                Estado = "Listo"
            };

            listaProcesos.Add(nuevoProceso);
            AsignarColorProceso(nuevoId);

            txtCliente.Clear();
            txtProblema.Clear();
            txtBurstTime.Clear();

            ActualizarTabla();
        }

        private void btnEjemplo_Click(object sender, RoutedEventArgs e)
        {
            btnLimpiar_Click(null, null);

            txtQuantum.Text = "3";

            listaProcesos.Add(new ProcesoRR { IdProceso = "TKT-1", Cliente = "Ana", Problema = "No enciende PC", BurstTime = 5, TiempoRestante = 5, Estado = "Listo" });
            listaProcesos.Add(new ProcesoRR { IdProceso = "TKT-2", Cliente = "Luis", Problema = "Sin Internet", BurstTime = 4, TiempoRestante = 4, Estado = "Listo" });
            listaProcesos.Add(new ProcesoRR { IdProceso = "TKT-3", Cliente = "Marta", Problema = "Impresora", BurstTime = 2, TiempoRestante = 2, Estado = "Listo" });
            listaProcesos.Add(new ProcesoRR { IdProceso = "TKT-4", Cliente = "Juan", Problema = "Virus", BurstTime = 6, TiempoRestante = 6, Estado = "Listo" });

            foreach (var p in listaProcesos)
            {
                AsignarColorProceso(p.IdProceso);
            }

            ActualizarTabla();
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            listaProcesos.Clear();
            coloresProcesos.Clear();
            spGantt.Children.Clear();
            txtQuantum.Clear();
            txtPromedioEspera.Text = "0";
            txtPromedioRetorno.Text = "0";
            txtProcesoActual.Text = "Ninguno";
            ActualizarTabla();
        }

        private async void btnEjecutar_Click(object sender, RoutedEventArgs e)
        {
            if (listaProcesos.Count == 0) return;

            if (!int.TryParse(txtQuantum.Text, out int quantum) || quantum <= 0)
            {
                MessageBox.Show("Ingrese un valor de Quantum numérico y mayor a 0.");
                return;
            }

            spGantt.Children.Clear();
            CambiarEstadoBotones(false);

            foreach (var p in listaProcesos)
            {
                p.TiempoRestante = p.BurstTime;
                p.WaitingTime = 0;
                p.TurnaroundTime = 0;
                p.Estado = "Listo";
            }
            ActualizarTabla();

            await Task.Run(() => EjecutarAlgoritmoRR(quantum));

            CalcularPromedios();
            CambiarEstadoBotones(true);
            txtProcesoActual.Text = "Finalizado";
        }

        private void EjecutarAlgoritmoRR(int quantum)
        {
            Queue<ProcesoRR> colaListos = new Queue<ProcesoRR>(listaProcesos);
            int tiempoGlobal = 0;

            while (colaListos.Count > 0)
            {
                ProcesoRR procesoActual = colaListos.Dequeue();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    procesoActual.Estado = "En Ejecución";
                    txtProcesoActual.Text = procesoActual.IdProceso;
                    ActualizarTabla();
                });

                int tiempoEjecucion = Math.Min(quantum, procesoActual.TiempoRestante);

                // Simulamos el trabajo del hilo en la CPU
                Thread.Sleep(tiempoEjecucion * 500);

                tiempoGlobal += tiempoEjecucion;
                procesoActual.TiempoRestante -= tiempoEjecucion;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    DibujarBloqueGantt(procesoActual.IdProceso, tiempoEjecucion, tiempoGlobal);
                });

                if (procesoActual.TiempoRestante > 0)
                {
                    procesoActual.Estado = "Espera";
                    colaListos.Enqueue(procesoActual);
                }
                else
                {
                    procesoActual.Estado = "Finalizado";
                    procesoActual.TurnaroundTime = tiempoGlobal;
                    procesoActual.WaitingTime = procesoActual.TurnaroundTime - procesoActual.BurstTime;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ActualizarTabla();
                });
            }
        }

        private void DibujarBloqueGantt(string idProceso, int duracion, int tiempoFinal)
        {
            Border bloque = new Border
            {
                Background = coloresProcesos[idProceso],
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1, 1, 1, 1),
                Width = duracion * 30,
                Height = 60,
                Margin = new Thickness(2, 0, 2, 0)
            };

            TextBlock txt = new TextBlock
            {
                Text = $"{idProceso}\n({duracion}q)",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            bloque.Child = txt;
            spGantt.Children.Add(bloque);
        }

        private void AsignarColorProceso(string idProceso)
        {
            if (!coloresProcesos.ContainsKey(idProceso))
            {
                Color colorAleatorio = Color.FromRgb((byte)rnd.Next(50, 200), (byte)rnd.Next(50, 200), (byte)rnd.Next(50, 200));
                coloresProcesos.Add(idProceso, new SolidColorBrush(colorAleatorio));
            }
        }

        private void ActualizarTabla()
        {
            dgProcesos.ItemsSource = null;
            dgProcesos.ItemsSource = listaProcesos;
        }

        private void CalcularPromedios()
        {
            if (listaProcesos.Count > 0)
            {
                double promEspera = listaProcesos.Average(p => p.WaitingTime);
                double promRetorno = listaProcesos.Average(p => p.TurnaroundTime);

                txtPromedioEspera.Text = promEspera.ToString("0.00");
                txtPromedioRetorno.Text = promRetorno.ToString("0.00");
            }
        }

        private void CambiarEstadoBotones(bool estado)
        {
            btnAgregar.IsEnabled = estado;
            btnEjecutar.IsEnabled = estado;
            btnEjemplo.IsEnabled = estado;
            btnLimpiar.IsEnabled = estado;
        }
    }
}
