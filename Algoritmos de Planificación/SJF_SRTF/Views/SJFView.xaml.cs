using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Algoritmos_de_Planificación.SJF_SRTF.Models;

namespace Algoritmos_de_Planificación.SJF_SRTF.Views
{
    public partial class SJFView : UserControl
    {
        private List<Proceso> procesos = new List<Proceso>();
        private bool ejecutando = false;

        private Dictionary<string, Brush> coloresPorProceso = new Dictionary<string, Brush>();

        private Brush[] paletaColores = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(52, 152, 219)), 
            new SolidColorBrush(Color.FromRgb(46, 204, 113)),  
            new SolidColorBrush(Color.FromRgb(155, 89, 182)), 
            new SolidColorBrush(Color.FromRgb(241, 196, 15)), 
            new SolidColorBrush(Color.FromRgb(230, 126, 34)),  
            new SolidColorBrush(Color.FromRgb(231, 76, 60)),  
            new SolidColorBrush(Color.FromRgb(26, 188, 156)), 
            new SolidColorBrush(Color.FromRgb(52, 73, 94)),   
            new SolidColorBrush(Color.FromRgb(243, 156, 18)), 
            new SolidColorBrush(Color.FromRgb(192, 57, 43))    
        };

        public SJFView()
        {
            InitializeComponent();
            CargarEjemploPorDefecto();
            ActualizarDataGridProcesos();
        }

        private void CargarEjemploPorDefecto()
        {
            procesos.Clear();
            coloresPorProceso.Clear();
            procesos.Add(new Proceso("P1", 0, 8));
            procesos.Add(new Proceso("P2", 0, 4));
            ActualizarDataGridProcesos();
        }

        private Brush ObtenerColorProceso(string nombreProceso)
        {
            if (!coloresPorProceso.ContainsKey(nombreProceso))
            {
                int indice = coloresPorProceso.Count % paletaColores.Length;
                coloresPorProceso[nombreProceso] = paletaColores[indice];
            }
            return coloresPorProceso[nombreProceso];
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (ejecutando)
            {
                MessageBox.Show("No puede agregar procesos mientras se ejecuta el algoritmo.", "En ejecución", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Ingrese un nombre para el proceso", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nombreProceso = txtNombre.Text.Trim();

            if (procesos.Any(p => p.Nombre == nombreProceso))
            {
                MessageBox.Show($"Ya existe un proceso con el nombre '{nombreProceso}'. Use un nombre diferente.", "Nombre duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtLlegada.Text, out int llegada))
            {
                MessageBox.Show("Ingrese un valor numérico válido para el Tiempo de Llegada", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(txtRafaga.Text, out int rafaga))
            {
                MessageBox.Show("Ingrese un valor numérico válido para la Ráfaga de CPU", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (llegada < 0)
            {
                MessageBox.Show("El tiempo de llegada no puede ser negativo", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (rafaga <= 0)
            {
                MessageBox.Show("La ráfaga de CPU debe ser mayor a cero", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Proceso p = new Proceso(nombreProceso, llegada, rafaga);
            procesos.Add(p);

            txtNombre.Text = "P" + (procesos.Count + 1);
            txtLlegada.Text = "0";
            txtRafaga.Text = "5";

            ActualizarDataGridProcesos();
        }

        private void btnSimular_Click(object sender, RoutedEventArgs e)
        {
            if (procesos.Count == 0)
            {
                MessageBox.Show("Agregue al menos un proceso", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ejecutando)
            {
                MessageBox.Show("El algoritmo ya se está ejecutando.", "En ejecución", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ejecutando = true;
            CambiarEstadoBotones(false);

            Thread hiloSJF = new Thread(EjecutarSJF);
            hiloSJF.IsBackground = true;
            hiloSJF.Start();
        }

        private void EjecutarSJF()
        {
            var procesosCopia = procesos.Select(p => new Proceso(p.Nombre, p.TiempoLlegada, p.RafagaCPU)).ToList();
            int tiempoActual = 0;
            List<Proceso> resultados = new List<Proceso>();
            int completados = 0;

            Dispatcher.Invoke(() =>
            {
                spGantt.Children.Clear();
                txtPromRetorno.Text = "0";
                txtPromEspera.Text = "0";
                txtProcesoActual.Text = "Iniciando...";
                dgResultados.ItemsSource = null;
            });

            while (completados < procesosCopia.Count)
            {
                var disponibles = procesosCopia.Where(p => p.TiempoLlegada <= tiempoActual && p.TiempoFin == 0).ToList();

                if (!disponibles.Any())
                {
                    int siguienteLlegada = procesosCopia.Where(p => p.TiempoFin == 0).Min(p => p.TiempoLlegada);
                    if (tiempoActual < siguienteLlegada)
                    {
                        int idleInicio = tiempoActual;
                        int idleFin = siguienteLlegada;
                        AgregarBloqueGantt("IDLE", "", idleInicio, idleFin, Brushes.LightGray);
                    }
                    tiempoActual = siguienteLlegada;
                    continue;
                }

                var procesoSeleccionado = disponibles.OrderBy(p => p.RafagaCPU).ThenBy(p => p.TiempoLlegada).First();

                procesoSeleccionado.TiempoInicio = tiempoActual;
                procesoSeleccionado.TiempoFin = tiempoActual + procesoSeleccionado.RafagaCPU;
                procesoSeleccionado.TiempoRetorno = procesoSeleccionado.TiempoFin - procesoSeleccionado.TiempoLlegada;
                procesoSeleccionado.TiempoEspera = procesoSeleccionado.TiempoInicio - procesoSeleccionado.TiempoLlegada;

                int inicio = procesoSeleccionado.TiempoInicio;
                int fin = procesoSeleccionado.TiempoFin;
                string nombreProceso = procesoSeleccionado.Nombre;

                Dispatcher.Invoke(() =>
                {
                    txtProcesoActual.Text = $"{nombreProceso} (Llegó: {procesoSeleccionado.TiempoLlegada})";
                });

                Brush colorProceso = ObtenerColorProceso(nombreProceso);
                AgregarBloqueGantt(nombreProceso, $"Ráfaga: {procesoSeleccionado.RafagaCPU}", inicio, fin, colorProceso);

                // Simulación paso a paso para mejor UX
                for (int i = 0; i < procesoSeleccionado.RafagaCPU; i++)
                {
                    Thread.Sleep(1000);
                }

                tiempoActual = procesoSeleccionado.TiempoFin;
                resultados.Add(procesoSeleccionado);
                completados++;
            }

            double promRetorno = resultados.Average(r => r.TiempoRetorno);
            double promEspera = resultados.Average(r => r.TiempoEspera);

            Dispatcher.Invoke(() =>
            {
                txtPromRetorno.Text = promRetorno.ToString("0.00");
                txtPromEspera.Text = promEspera.ToString("0.00");
                txtProcesoActual.Text = "Todos finalizados";
                MostrarResultados(resultados);
                ejecutando = false;
                CambiarEstadoBotones(true);
            });
        }

        private void MostrarResultados(List<Proceso> resultados)
        {
            var resultadosGrid = resultados.Select(r => new
            {
                r.Nombre,
                Llegada = r.TiempoLlegada,
                Rafaga = r.RafagaCPU,
                Inicio = r.TiempoInicio,
                Fin = r.TiempoFin,
                Retorno = r.TiempoRetorno,
                Espera = r.TiempoEspera
            }).ToList();
            dgResultados.ItemsSource = resultadosGrid;
        }

        private void ActualizarDataGridProcesos()
        {
            var procesosGrid = procesos.Select(p => new
            {
                p.Nombre,
                Llegada = p.TiempoLlegada,
                Rafaga = p.RafagaCPU
            }).ToList();
            dgProcesos.ItemsSource = procesosGrid;
        }

        private void AgregarBloqueGantt(string titulo, string subtitulo, int inicio, int fin, Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                int duracion = fin - inicio;
                int anchoBloque = Math.Max(80, duracion * 20); // Ancho proporcional a la duración

                Border bloque = new Border
                {
                    Background = color,
                    BorderBrush = (Brush)Application.Current.Resources["ColorPrimario"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(7),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 8, 0),
                    Width = anchoBloque,
                    MinWidth = 80
                };

                StackPanel contenido = new StackPanel();

                TextBlock txtTitulo = new TextBlock
                {
                    Text = titulo,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };

                TextBlock txtSubtitulo = new TextBlock
                {
                    Text = subtitulo,
                    FontSize = 11,
                    Foreground = Brushes.White
                };

                TextBlock txtTiempo = new TextBlock
                {
                    Text = $"{inicio} - {fin}",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White
                };

                contenido.Children.Add(txtTitulo);
                if (!string.IsNullOrEmpty(subtitulo))
                    contenido.Children.Add(txtSubtitulo);
                contenido.Children.Add(txtTiempo);

                bloque.Child = contenido;
                spGantt.Children.Add(bloque);

                scrollGantt.ScrollToHorizontalOffset(double.MaxValue);
            });
        }

        private void CambiarEstadoBotones(bool habilitado)
        {
            Dispatcher.Invoke(() =>
            {
                btnAgregar.IsEnabled = habilitado;
                btnSimular.IsEnabled = habilitado;
                btnEjemplo.IsEnabled = habilitado;
                btnLimpiar.IsEnabled = habilitado;
            });
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            if (ejecutando)
            {
                MessageBox.Show("No puede limpiar mientras el algoritmo se está ejecutando.", "En ejecución", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            procesos.Clear();
            coloresPorProceso.Clear();
            spGantt.Children.Clear();
            dgProcesos.ItemsSource = null;
            dgResultados.ItemsSource = null;
            txtNombre.Text = "P1";
            txtLlegada.Text = "0";
            txtRafaga.Text = "5";
            txtPromRetorno.Text = "0";
            txtPromEspera.Text = "0";
            txtProcesoActual.Text = "Ninguno";
        }

        private void btnEjemplo_Click(object sender, RoutedEventArgs e)
        {
            if (ejecutando)
            {
                MessageBox.Show("No puede cargar un ejemplo mientras se ejecuta el algoritmo.", "En ejecución", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            procesos.Clear();
            coloresPorProceso.Clear();
            procesos.Add(new Proceso("P1", 0, 8));
            procesos.Add(new Proceso("P2", 0, 4));
            spGantt.Children.Clear();
            dgResultados.ItemsSource = null;
            txtPromRetorno.Text = "0";
            txtPromEspera.Text = "0";
            txtProcesoActual.Text = "Ninguno";
            ActualizarDataGridProcesos();
            MessageBox.Show("Ejemplo cargado: P1(0,8), P2(0,4)\n\nOrden SJF: P2 (4 min) → P1 (8 min)", "Ejemplo SJF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}