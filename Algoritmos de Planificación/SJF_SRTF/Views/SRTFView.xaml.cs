using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Algoritmos_de_Planificación.SJF_SRTF.Models;

namespace Algoritmos_de_Planificación.SJF_SRTF.Views
{
    public partial class SRTFView : UserControl
    {
        private List<Proceso> procesos = new List<Proceso>();
        private Dictionary<string, Brush> coloresPorProceso = new Dictionary<string, Brush>();
        private Brush[] coloresBase = {
            Brushes.Crimson, Brushes.ForestGreen, Brushes.SteelBlue,
            Brushes.Orange, Brushes.Purple, Brushes.Teal,
            Brushes.Goldenrod, Brushes.IndianRed, Brushes.DodgerBlue
        };

        public SRTFView()
        {
            InitializeComponent();
            CargarEjemploPorDefecto();
            ActualizarContador();
        }

        private void CargarEjemploPorDefecto()
        {
            procesos.Clear();
            coloresPorProceso.Clear();
            procesos.Add(new Proceso("L1", 0, 25));
            procesos.Add(new Proceso("L2", 3, 5));
            ActualizarListBox();
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            // Validación de campos vacíos
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Ingrese un nombre para la llamada", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nombreProceso = txtNombre.Text.Trim();

            // Validación de nombre duplicado
            if (procesos.Any(p => p.Nombre == nombreProceso))
            {
                MessageBox.Show($"Ya existe una llamada con el nombre '{nombreProceso}'. Use un nombre diferente.",
                                "Nombre duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validación con TryParse (evita que rompa con letras)
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

            // Validación de valores positivos
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

            // Resetear inputs para el próximo ingreso
            txtNombre.Text = "L" + (procesos.Count + 1);
            txtLlegada.Text = "0";
            txtRafaga.Text = "10";

            ActualizarListBox();
            ActualizarContador();
        }

        private void btnSimular_Click(object sender, RoutedEventArgs e)
        {
            if (procesos.Count == 0)
            {
                MessageBox.Show("Agregue al menos una llamada", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            EjecutarSRTF();
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            procesos.Clear();
            coloresPorProceso.Clear();
            ActualizarListBox();
            dgResultados.ItemsSource = null;
            canvasGantt.Children.Clear();
            txtPromRetorno.Text = "";
            txtPromEspera.Text = "";
            txtInterrupciones.Text = "";
            txtInterrupciones.Visibility = Visibility.Collapsed;
            ActualizarContador();

            // Resetear inputs
            txtNombre.Text = "L1";
            txtLlegada.Text = "0";
            txtRafaga.Text = "10";
        }

        private void btnEjemplo_Click(object sender, RoutedEventArgs e)
        {
            CargarEjemploPorDefecto();
            coloresPorProceso.Clear();
            ActualizarContador();

            // Limpiar resultados anteriores
            dgResultados.ItemsSource = null;
            canvasGantt.Children.Clear();
            txtPromRetorno.Text = "";
            txtPromEspera.Text = "";
            txtInterrupciones.Text = "";
            txtInterrupciones.Visibility = Visibility.Collapsed;

            MessageBox.Show("Ejemplo cargado: L1(0,25), L2(3,5)\n\n" +
                          "SRTF: L1 se ejecuta de 0 a 3, luego L2 de 3 a 8, luego L1 de 8 a 28.\n" +
                          "L1 es interrumpida UNA vez por L2.",
                          "Ejemplo SRTF", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ActualizarListBox()
        {
            lstProcesos.Items.Clear();
            foreach (var p in procesos)
                lstProcesos.Items.Add($"{p.Nombre,-12} | Llegada: {p.TiempoLlegada,2} | Duracion: {p.RafagaCPU,2}");
        }

        private void ActualizarContador()
        {
            txtTotalProcesos.Text = $"Total de llamadas: {procesos.Count}";
        }

        private Brush ObtenerColorPorProceso(string nombreProceso)
        {
            if (!coloresPorProceso.ContainsKey(nombreProceso))
            {
                coloresPorProceso[nombreProceso] = coloresBase[coloresPorProceso.Count % coloresBase.Length];
            }
            return coloresPorProceso[nombreProceso];
        }

        private void EjecutarSRTF()
        {
            var procesosCopia = procesos.Select(p => new Proceso(p.Nombre, p.TiempoLlegada, p.RafagaCPU)).ToList();
            foreach (var p in procesosCopia)
                p.RafagaRestante = p.RafagaCPU;

            int tiempoActual = 0;
            int completados = 0;
            List<Proceso> resultados = new List<Proceso>();
            List<Tuple<string, int, int>> gantt = new List<Tuple<string, int, int>>();

            Proceso procesoActual = null;
            int inicioActual = 0;

            while (completados < procesosCopia.Count)
            {
                var disponibles = procesosCopia.Where(p => p.TiempoLlegada <= tiempoActual && p.RafagaRestante > 0).ToList();

                if (!disponibles.Any())
                {
                    // Registrar tiempo IDLE
                    int siguienteLlegada = procesosCopia.Where(p => p.RafagaRestante > 0).Min(p => p.TiempoLlegada);
                    if (tiempoActual < siguienteLlegada)
                    {
                        gantt.Add(Tuple.Create("IDLE", tiempoActual, siguienteLlegada));
                    }
                    tiempoActual = siguienteLlegada;
                    continue;
                }

                // Criterio de desempate: menor ráfaga restante, luego menor tiempo de llegada
                var siguiente = disponibles
                    .OrderBy(p => p.RafagaRestante)
                    .ThenBy(p => p.TiempoLlegada)
                    .First();

                if (procesoActual != siguiente)
                {
                    if (procesoActual != null && inicioActual < tiempoActual)
                    {
                        gantt.Add(Tuple.Create(procesoActual.Nombre, inicioActual, tiempoActual));
                    }
                    inicioActual = tiempoActual;
                    procesoActual = siguiente;

                    if (siguiente.TiempoInicio == -1)
                        siguiente.TiempoInicio = tiempoActual;
                }

                siguiente.RafagaRestante--;
                tiempoActual++;

                if (siguiente.RafagaRestante == 0)
                {
                    siguiente.TiempoFin = tiempoActual;
                    siguiente.TiempoRetorno = siguiente.TiempoFin - siguiente.TiempoLlegada;
                    siguiente.TiempoEspera = siguiente.TiempoRetorno - siguiente.RafagaCPU;
                    resultados.Add(siguiente);
                    completados++;
                }
            }

            if (procesoActual != null && inicioActual < tiempoActual)
            {
                gantt.Add(Tuple.Create(procesoActual.Nombre, inicioActual, tiempoActual));
            }

            MostrarResultados(resultados, gantt);
        }

        private void MostrarResultados(List<Proceso> resultados, List<Tuple<string, int, int>> gantt)
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

            double promRetorno = resultados.Average(r => r.TiempoRetorno);
            double promEspera = resultados.Average(r => r.TiempoEspera);

            txtPromRetorno.Text = $"Retorno Promedio: {promRetorno:F2}";
            txtPromEspera.Text = $"Espera Promedio: {promEspera:F2}";

            // Mostrar interrupciones en un TextBlock separado
            var grupos = gantt.Where(g => g.Item1 != "IDLE").GroupBy(g => g.Item1);
            string mensajeInterrupciones = "";
            foreach (var grupo in grupos)
            {
                if (grupo.Count() > 1)
                {
                    mensajeInterrupciones += $"• {grupo.Key} fue interrumpido y aparece en {grupo.Count()} bloques\n";
                }
            }
            if (!string.IsNullOrEmpty(mensajeInterrupciones))
            {
                txtInterrupciones.Text = mensajeInterrupciones;
                txtInterrupciones.Visibility = Visibility.Visible;
            }
            else
            {
                txtInterrupciones.Visibility = Visibility.Collapsed;
            }

            DibujarGantt(gantt);
        }

        private void DibujarGantt(List<Tuple<string, int, int>> gantt)
        {
            canvasGantt.Children.Clear();
            if (gantt.Count == 0) return;

            int anchoBloque = 70, y = 20, altura = 55;
            int maxTiempo = gantt.Max(g => g.Item3);
            canvasGantt.Width = Math.Max(700, (maxTiempo + 2) * anchoBloque);

            for (int i = 0; i < gantt.Count; i++)
            {
                var bloque = gantt[i];
                string nombreProceso = bloque.Item1;
                int inicio = bloque.Item2, fin = bloque.Item3;
                int duracion = fin - inicio;
                int x = inicio * anchoBloque + 35;

                Brush color;
                if (nombreProceso == "IDLE")
                {
                    color = Brushes.LightGray;
                }
                else
                {
                    color = ObtenerColorPorProceso(nombreProceso);
                }

                Rectangle rect = new Rectangle
                {
                    Width = duracion * anchoBloque,
                    Height = altura,
                    Fill = color,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1.5,
                    RadiusX = 6,
                    RadiusY = 6
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                canvasGantt.Children.Add(rect);

                // Texto centrado
                TextBlock txtProceso = new TextBlock
                {
                    Text = nombreProceso == "IDLE" ? "IDLE" : $"{nombreProceso}\n[{inicio}-{fin}]",
                    FontSize = nombreProceso == "IDLE" ? 10 : 9,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = nombreProceso == "IDLE" ? Brushes.Black : Brushes.White
                };

                txtProceso.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double textoAncho = txtProceso.DesiredSize.Width;
                Canvas.SetLeft(txtProceso, x + (duracion * anchoBloque - textoAncho) / 2);
                Canvas.SetTop(txtProceso, y + (nombreProceso == "IDLE" ? 20 : 12));
                canvasGantt.Children.Add(txtProceso);
            }

            // Eje de tiempo
            for (int i = 0; i <= maxTiempo + 1; i++)
            {
                Line line = new Line
                {
                    X1 = i * anchoBloque + 35,
                    Y1 = y + altura,
                    X2 = i * anchoBloque + 35,
                    Y2 = y + altura + 10,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                canvasGantt.Children.Add(line);

                TextBlock tiempo = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(tiempo, i * anchoBloque + 30);
                Canvas.SetTop(tiempo, y + altura + 8);
                canvasGantt.Children.Add(tiempo);
            }

            Line baseLine = new Line
            {
                X1 = 35,
                Y1 = y + altura,
                X2 = (maxTiempo + 1) * anchoBloque + 35,
                Y2 = y + altura,
                Stroke = Brushes.Black,
                StrokeThickness = 1.5
            };
            canvasGantt.Children.Add(baseLine);
        }
    }
}