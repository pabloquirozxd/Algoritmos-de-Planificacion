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
    public partial class SRTFView : UserControl
    {
        private List<Proceso> procesos = new List<Proceso>();
        private bool ejecutando = false;

        // Diccionario para colores consistentes por proceso
        private Dictionary<string, Brush> coloresPorProceso = new Dictionary<string, Brush>();

        // Paleta de colores profesionales
        private Brush[] paletaColores = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(231, 76, 60)),   // Rojo
            new SolidColorBrush(Color.FromRgb(46, 204, 113)),  // Verde
            new SolidColorBrush(Color.FromRgb(52, 152, 219)),  // Azul
            new SolidColorBrush(Color.FromRgb(155, 89, 182)),  // Morado
            new SolidColorBrush(Color.FromRgb(241, 196, 15)),  // Amarillo
            new SolidColorBrush(Color.FromRgb(230, 126, 34)),  // Naranja
            new SolidColorBrush(Color.FromRgb(26, 188, 156)),  // Turquesa
            new SolidColorBrush(Color.FromRgb(52, 73, 94)),    // Gris oscuro
            new SolidColorBrush(Color.FromRgb(243, 156, 18)),  // Naranja claro
            new SolidColorBrush(Color.FromRgb(192, 57, 43))    // Rojo oscuro
        };

        public SRTFView()
        {
            InitializeComponent();
            CargarEjemploPorDefecto();
            ActualizarDataGridProcesos();
        }

        private void CargarEjemploPorDefecto()
        {
            procesos.Clear();
            coloresPorProceso.Clear();
            procesos.Add(new Proceso("L1", 0, 25));
            procesos.Add(new Proceso("L2", 3, 5));
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
                MessageBox.Show("No puede agregar llamadas mientras se ejecuta el algoritmo.", "En ejecución", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Ingrese un nombre para la llamada", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nombreProceso = txtNombre.Text.Trim();

            if (procesos.Any(p => p.Nombre == nombreProceso))
            {
                MessageBox.Show($"Ya existe una llamada con el nombre '{nombreProceso}'. Use un nombre diferente.", "Nombre duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtLlegada.Text, out int llegada))
            {
                MessageBox.Show("Ingrese un valor numérico válido para el Tiempo de Llegada", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(txtRafaga.Text, out int rafaga))
            {
                MessageBox.Show("Ingrese un valor numérico válido para la Duración", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (llegada < 0)
            {
                MessageBox.Show("El tiempo de llegada no puede ser negativo", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (rafaga <= 0)
            {
                MessageBox.Show("La duración debe ser mayor a cero", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Proceso p = new Proceso(nombreProceso, llegada, rafaga);
            procesos.Add(p);

            txtNombre.Text = "L" + (procesos.Count + 1);
            txtLlegada.Text = "0";
            txtRafaga.Text = "10";

            ActualizarDataGridProcesos();
        }

        private void btnSimular_Click(object sender, RoutedEventArgs e)
        {
            if (procesos.Count == 0)
            {
                MessageBox.Show("Agregue al menos una llamada", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ejecutando)
            {
                MessageBox.Show("El algoritmo ya se está ejecutando.", "En ejecución", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ejecutando = true;
            CambiarEstadoBotones(false);

            Thread hiloSRTF = new Thread(EjecutarSRTF);
            hiloSRTF.IsBackground = true;
            hiloSRTF.Start();
        }

        private void EjecutarSRTF()
        {
            var procesosCopia = procesos.Select(p => new Proceso(p.Nombre, p.TiempoLlegada, p.RafagaCPU)).ToList();
            foreach (var p in procesosCopia)
                p.RafagaRestante = p.RafagaCPU;

            int tiempoActual = 0;
            int completados = 0;
            List<Proceso> resultados = new List<Proceso>();
            Proceso procesoActual = null;
            int inicioActual = 0;

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
                var disponibles = procesosCopia.Where(p => p.TiempoLlegada <= tiempoActual && p.RafagaRestante > 0).ToList();

                if (!disponibles.Any())
                {
                    int siguienteLlegada = procesosCopia.Where(p => p.RafagaRestante > 0).Min(p => p.TiempoLlegada);
                    if (tiempoActual < siguienteLlegada)
                    {
                        int idleInicio = tiempoActual;
                        int idleFin = siguienteLlegada;
                        AgregarBloqueGantt("IDLE", "", idleInicio, idleFin, Brushes.LightGray);
                    }
                    tiempoActual = siguienteLlegada;
                    continue;
                }

                var procesoSeleccionado = disponibles.OrderBy(p => p.RafagaRestante).ThenBy(p => p.TiempoLlegada).First();

                if (procesoActual != procesoSeleccionado)
                {
                    if (procesoActual != null && inicioActual < tiempoActual)
                    {
                        int inicio = inicioActual;
                        int fin = tiempoActual;
                        string nombre = procesoActual.Nombre;
                        int restante = procesoActual.RafagaRestante;
                        Brush colorProceso = ObtenerColorProceso(nombre);
                        AgregarBloqueGantt(nombre, $"Restante: {restante}", inicio, fin, colorProceso);
                    }
                    inicioActual = tiempoActual;
                    procesoActual = procesoSeleccionado;

                    if (procesoSeleccionado.TiempoInicio == -1)
                        procesoSeleccionado.TiempoInicio = tiempoActual;
                }

                procesoSeleccionado.RafagaRestante--;
                tiempoActual++;

                if (procesoSeleccionado.RafagaRestante == 0)
                {
                    procesoSeleccionado.TiempoFin = tiempoActual;
                    procesoSeleccionado.TiempoRetorno = procesoSeleccionado.TiempoFin - procesoSeleccionado.TiempoLlegada;
                    procesoSeleccionado.TiempoEspera = procesoSeleccionado.TiempoRetorno - procesoSeleccionado.RafagaCPU;
                    resultados.Add(procesoSeleccionado);
                    completados++;
                }

                if (procesoActual != null)
                {
                    int actualRestante = procesoActual.RafagaRestante;
                    Dispatcher.Invoke(() => txtProcesoActual.Text = $"{procesoActual.Nombre} (Restan: {actualRestante} min)");
                }

                Thread.Sleep(1000);
            }

            if (procesoActual != null && inicioActual < tiempoActual)
            {
                int inicio = inicioActual;
                int fin = tiempoActual;
                string nombre = procesoActual.Nombre;
                Brush colorProceso = ObtenerColorProceso(nombre);
                AgregarBloqueGantt(nombre, "Finalizado", inicio, fin, colorProceso);
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
                Duracion = r.RafagaCPU,
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
                Duracion = p.RafagaCPU
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
            txtNombre.Text = "L1";
            txtLlegada.Text = "0";
            txtRafaga.Text = "10";
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
            procesos.Add(new Proceso("L1", 0, 25));
            procesos.Add(new Proceso("L2", 3, 5));
            spGantt.Children.Clear();
            dgResultados.ItemsSource = null;
            txtPromRetorno.Text = "0";
            txtPromEspera.Text = "0";
            txtProcesoActual.Text = "Ninguno";
            ActualizarDataGridProcesos();
            MessageBox.Show("Ejemplo cargado: L1(0,25), L2(3,5)\n\nSRTF: L1 se ejecuta de 0 a 3, luego L2 de 3 a 8, luego L1 de 8 a 28.\nL1 es interrumpida UNA vez por L2.", "Ejemplo SRTF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}