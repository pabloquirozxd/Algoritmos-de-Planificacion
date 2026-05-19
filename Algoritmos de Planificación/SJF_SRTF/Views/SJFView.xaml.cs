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
    public partial class SJFView : UserControl
    {
        private List<Proceso> procesos = new List<Proceso>();
        private Dictionary<string, Brush> coloresPorProceso = new Dictionary<string, Brush>();
        private Brush[] coloresBase = {
            Brushes.SteelBlue, Brushes.ForestGreen, Brushes.Orange,
            Brushes.Purple, Brushes.Crimson, Brushes.Teal,
            Brushes.Goldenrod, Brushes.IndianRed, Brushes.DodgerBlue
        };

        public SJFView()
        {
            InitializeComponent();
            CargarEjemploPorDefecto();
            ActualizarContador();
        }

        private void CargarEjemploPorDefecto()
        {
            procesos.Clear();
            procesos.Add(new Proceso("P1", 0, 8));
            procesos.Add(new Proceso("P2", 0, 4));
            ActualizarListBox();
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            // Validación de campos vacíos
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Ingrese un nombre para el proceso", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nombreProceso = txtNombre.Text.Trim();

            // MEJORA NUEVA: Validar que no exista un proceso con el mismo nombre
            if (procesos.Any(p => p.Nombre == nombreProceso))
            {
                MessageBox.Show($"Ya existe un proceso con el nombre '{nombreProceso}'. Use un nombre diferente.",
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
                MessageBox.Show("Ingrese un valor numérico válido para la Ráfaga de CPU", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("La ráfaga de CPU debe ser mayor a cero", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Proceso p = new Proceso(nombreProceso, llegada, rafaga);
            procesos.Add(p);

            // Resetear inputs para el próximo ingreso (sugiriendo el siguiente nombre)
            txtNombre.Text = "P" + (procesos.Count + 1);
            txtLlegada.Text = "0";
            txtRafaga.Text = "5";

            ActualizarListBox();
            ActualizarContador();
        }

        private void btnSimular_Click(object sender, RoutedEventArgs e)
        {
            if (procesos.Count == 0)
            {
                MessageBox.Show("Agregue al menos un proceso", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            EjecutarSJF();
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
            ActualizarContador();

            // MEJORA 5: Resetear inputs también
            txtNombre.Text = "P1";
            txtLlegada.Text = "0";
            txtRafaga.Text = "5";
        }

        private void btnEjemplo_Click(object sender, RoutedEventArgs e)
        {
            CargarEjemploPorDefecto();
            coloresPorProceso.Clear();
            ActualizarContador();
            MessageBox.Show("Ejemplo cargado: P1(0,8), P2(0,4)\n\nOrden SJF: P2 (4 min) → P1 (8 min)",
                          "Ejemplo SJF", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ActualizarListBox()
        {
            lstProcesos.Items.Clear();
            foreach (var p in procesos)
                lstProcesos.Items.Add($"{p.Nombre,-12} | Llegada: {p.TiempoLlegada,2} | Rafaga: {p.RafagaCPU,2}");
        }

        private void ActualizarContador()
        {
            txtTotalProcesos.Text = $"Total de procesos: {procesos.Count}";
        }

        private Brush ObtenerColorPorProceso(string nombreProceso)
        {
            if (!coloresPorProceso.ContainsKey(nombreProceso))
            {
                coloresPorProceso[nombreProceso] = coloresBase[coloresPorProceso.Count % coloresBase.Length];
            }
            return coloresPorProceso[nombreProceso];
        }

        private void EjecutarSJF()
        {
            var procesosCopia = procesos.Select(p => new Proceso(p.Nombre, p.TiempoLlegada, p.RafagaCPU)).ToList();
            int tiempoActual = 0;
            List<Proceso> resultados = new List<Proceso>();
            List<Tuple<string, int, int>> gantt = new List<Tuple<string, int, int>>();
            int completados = 0;

            while (completados < procesosCopia.Count)
            {
                var disponibles = procesosCopia.Where(p => p.TiempoLlegada <= tiempoActual && p.TiempoFin == 0).ToList();

                if (!disponibles.Any())
                {
                    int siguienteLlegada = procesosCopia.Where(p => p.TiempoFin == 0).Min(p => p.TiempoLlegada);
                    if (tiempoActual < siguienteLlegada)
                    {
                        gantt.Add(Tuple.Create("IDLE", tiempoActual, siguienteLlegada));
                    }
                    tiempoActual = siguienteLlegada;
                    continue;
                }

                // MEJORA 2: Criterio de desempate por tiempo de llegada
                var siguiente = disponibles
                    .OrderBy(p => p.RafagaCPU)
                    .ThenBy(p => p.TiempoLlegada)
                    .First();

                siguiente.TiempoInicio = tiempoActual;
                siguiente.TiempoFin = tiempoActual + siguiente.RafagaCPU;
                siguiente.TiempoRetorno = siguiente.TiempoFin - siguiente.TiempoLlegada;
                siguiente.TiempoEspera = siguiente.TiempoInicio - siguiente.TiempoLlegada;

                gantt.Add(Tuple.Create(siguiente.Nombre, siguiente.TiempoInicio, siguiente.TiempoFin));
                tiempoActual = siguiente.TiempoFin;
                resultados.Add(siguiente);
                completados++;
            }

            MostrarResultados(resultados, gantt);
        }

        private void MostrarResultados(List<Proceso> resultados, List<Tuple<string, int, int>> gantt)
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

            double promRetorno = resultados.Average(r => r.TiempoRetorno);
            double promEspera = resultados.Average(r => r.TiempoEspera);

            txtPromRetorno.Text = $"Retorno Promedio: {promRetorno:F2}";
            txtPromEspera.Text = $"Espera Promedio: {promEspera:F2}";

            DibujarGantt(gantt);
        }

        private void DibujarGantt(List<Tuple<string, int, int>> gantt)
        {
            canvasGantt.Children.Clear();
            if (gantt.Count == 0) return;

            int anchoBloque = 65, y = 20, altura = 55;
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

                // MEJORA 4: Mejor centrado del texto
                double textoX = x + (duracion * anchoBloque) / 2;
                TextBlock txtProceso = new TextBlock
                {
                    Text = nombreProceso == "IDLE" ? "IDLE" : $"{nombreProceso}\n[{inicio}-{fin}]",
                    FontSize = nombreProceso == "IDLE" ? 10 : 9,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = nombreProceso == "IDLE" ? Brushes.Black : Brushes.White
                };

                // Medir el texto y centrarlo
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