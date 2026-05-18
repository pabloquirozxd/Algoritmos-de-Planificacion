using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Algoritmos_de_Planificación.Prioridad
{
    /// <summary>
    /// Lógica de interacción para Prioridad.xaml
    /// </summary>
    public partial class Prioridad : UserControl
    {
        ObservableCollection<TicketPrioridad> listaTickets = new ObservableCollection<TicketPrioridad>();

        bool ejecutando = false;
        int tiempoActual = 0;
        string tipoAlgoritmo = "";

        public Prioridad()
        {
            InitializeComponent();
            dgTickets.ItemsSource = listaTickets;
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            string ticket = txtTicket.Text.Trim();
            string textoArrival = txtArrivalTime.Text.Trim();
            string textoBurst = txtBurstTime.Text.Trim();

            int arrivalTime;
            int burstTime;
            int prioridad;

            if (ejecutando)
            {
                if (!ValidarDatosEnEjecucion(ticket, textoBurst, out burstTime))
                {
                    return;
                }

                arrivalTime = tiempoActual;
            }
            else
            {
                if (!ValidarDatosIniciales(ticket, textoArrival, textoBurst, out arrivalTime, out burstTime))
                {
                    return;
                }
            }

            prioridad = ObtenerPrioridadSeleccionada();

            TicketPrioridad nuevoTicket = new TicketPrioridad();

            nuevoTicket.Proceso = "P" + (listaTickets.Count + 1);
            nuevoTicket.Ticket = ticket;
            nuevoTicket.ArrivalTime = arrivalTime;
            nuevoTicket.BurstTime = burstTime;
            nuevoTicket.Prioridad = prioridad;
            nuevoTicket.RemainingTime = burstTime;
            nuevoTicket.CompletionTime = 0;
            nuevoTicket.WaitingTime = 0;
            nuevoTicket.TurnaroundTime = 0;
            nuevoTicket.Estado = ejecutando ? "Nuevo" : "Pendiente";

            listaTickets.Add(nuevoTicket);

            txtTicket.Clear();
            txtArrivalTime.Clear();
            txtBurstTime.Clear();
            cmbPrioridad.SelectedIndex = 0;
            txtTicket.Focus();

            dgTickets.Items.Refresh();
        }

        private bool ValidarDatosIniciales(string ticket, string textoArrival, string textoBurst, out int arrivalTime, out int burstTime)
        {
            arrivalTime = 0;
            burstTime = 0;

            if (ticket == "")
            {
                MessageBox.Show("Debe ingresar el nombre del ticket o problema.");
                txtTicket.Focus();
                return false;
            }

            if (ticket.Length < 3)
            {
                MessageBox.Show("El ticket debe tener al menos 3 caracteres.");
                txtTicket.Focus();
                return false;
            }

            if (textoArrival == "")
            {
                MessageBox.Show("Debe ingresar el Arrival Time.");
                txtArrivalTime.Focus();
                return false;
            }

            if (!int.TryParse(textoArrival, out arrivalTime))
            {
                MessageBox.Show("El Arrival Time debe ser un número entero.");
                txtArrivalTime.Focus();
                return false;
            }

            if (arrivalTime < 0)
            {
                MessageBox.Show("El Arrival Time no puede ser negativo.");
                txtArrivalTime.Focus();
                return false;
            }

            if (textoBurst == "")
            {
                MessageBox.Show("Debe ingresar el Burst Time.");
                txtBurstTime.Focus();
                return false;
            }

            if (!int.TryParse(textoBurst, out burstTime))
            {
                MessageBox.Show("El Burst Time debe ser un número entero.");
                txtBurstTime.Focus();
                return false;
            }

            if (burstTime <= 0)
            {
                MessageBox.Show("El Burst Time debe ser mayor a 0.");
                txtBurstTime.Focus();
                return false;
            }

            if (burstTime > 20)
            {
                MessageBox.Show("Para la simulación, el Burst Time no debe ser mayor a 20.");
                txtBurstTime.Focus();
                return false;
            }

            return true;
        }

        private bool ValidarDatosEnEjecucion(string ticket, string textoBurst, out int burstTime)
        {
            burstTime = 0;

            if (ticket == "")
            {
                MessageBox.Show("Debe ingresar el nombre del ticket o problema.");
                txtTicket.Focus();
                return false;
            }

            if (ticket.Length < 3)
            {
                MessageBox.Show("El ticket debe tener al menos 3 caracteres.");
                txtTicket.Focus();
                return false;
            }

            if (textoBurst == "")
            {
                MessageBox.Show("Debe ingresar el Burst Time.");
                txtBurstTime.Focus();
                return false;
            }

            if (!int.TryParse(textoBurst, out burstTime))
            {
                MessageBox.Show("El Burst Time debe ser un número entero.");
                txtBurstTime.Focus();
                return false;
            }

            if (burstTime <= 0)
            {
                MessageBox.Show("El Burst Time debe ser mayor a 0.");
                txtBurstTime.Focus();
                return false;
            }

            if (burstTime > 20)
            {
                MessageBox.Show("Para la simulación, el Burst Time no debe ser mayor a 20.");
                txtBurstTime.Focus();
                return false;
            }

            return true;
        }

        private int ObtenerPrioridadSeleccionada()
        {
            if (cmbPrioridad.SelectedIndex == 0)
            {
                return 1;
            }
            else if (cmbPrioridad.SelectedIndex == 1)
            {
                return 2;
            }
            else
            {
                return 3;
            }
        }

        private void btnEjecutar_Click(object sender, RoutedEventArgs e)
        {
            if (listaTickets.Count == 0)
            {
                MessageBox.Show("Debe agregar al menos un ticket antes de ejecutar el algoritmo.");
                return;
            }

            if (ejecutando)
            {
                MessageBox.Show("El algoritmo ya se está ejecutando.");
                return;
            }

            ejecutando = true;
            tiempoActual = 0;

            tipoAlgoritmo = ((ComboBoxItem)cmbTipoPrioridad.SelectedItem).Content.ToString();

            CambiarEstadoControlesEnEjecucion();

            Thread hiloPrioridad = new Thread(EjecutarPrioridad);
            hiloPrioridad.IsBackground = true;
            hiloPrioridad.Start();
        }

        private void EjecutarPrioridad()
        {
            Dispatcher.Invoke(() =>
            {
                ReiniciarValoresTabla();

                spGantt.Children.Clear();
                txtPromedioEspera.Text = "0";
                txtPromedioRetorno.Text = "0";
                txtTicketActual.Text = "Iniciando...";
                txtModo.Text = tipoAlgoritmo;

                dgTickets.Items.Refresh();
            });

            if (tipoAlgoritmo == "No expropiativa")
            {
                EjecutarNoExpropiativaDinamica();
            }
            else
            {
                EjecutarExpropiativaDinamica();
            }

            Dispatcher.Invoke(() =>
            {
                CalcularPromedios();

                txtTicketActual.Text = "Todos finalizados";
                ejecutando = false;

                CambiarEstadoControlesFinalizado();
            });
        }

        private void EjecutarNoExpropiativaDinamica()
        {
            int indiceActual = -1;
            int inicioBloque = 0;

            while (!TodosFinalizados())
            {
                if (indiceActual == -1)
                {
                    indiceActual = BuscarTicketMayorPrioridadDisponible();

                    if (indiceActual == -1)
                    {
                        tiempoActual++;
                        Thread.Sleep(1000);
                        continue;
                    }

                    inicioBloque = tiempoActual;

                    Dispatcher.Invoke(() =>
                    {
                        listaTickets[indiceActual].Estado = "En atención";
                        txtTicketActual.Text = listaTickets[indiceActual].Proceso + " - " + listaTickets[indiceActual].Ticket;
                        dgTickets.Items.Refresh();
                    });
                }

                Thread.Sleep(1000);

                Dispatcher.Invoke(() =>
                {
                    listaTickets[indiceActual].RemainingTime--;
                    dgTickets.Items.Refresh();
                });

                tiempoActual++;

                bool termino = false;

                Dispatcher.Invoke(() =>
                {
                    if (listaTickets[indiceActual].RemainingTime == 0)
                    {
                        int completionTime = tiempoActual;
                        int turnaroundTime = completionTime - listaTickets[indiceActual].ArrivalTime;
                        int waitingTime = turnaroundTime - listaTickets[indiceActual].BurstTime;

                        listaTickets[indiceActual].CompletionTime = completionTime;
                        listaTickets[indiceActual].TurnaroundTime = turnaroundTime;
                        listaTickets[indiceActual].WaitingTime = waitingTime;
                        listaTickets[indiceActual].Estado = "Finalizado";

                        AgregarBloqueGantt(listaTickets[indiceActual], inicioBloque, tiempoActual);

                        termino = true;

                        dgTickets.Items.Refresh();
                    }
                });

                if (termino)
                {
                    indiceActual = -1;
                }
            }
        }

        private void EjecutarExpropiativaDinamica()
        {
            int procesoAnterior = -1;
            int inicioBloque = 0;

            while (!TodosFinalizados())
            {
                int indiceSeleccionado = BuscarTicketMayorPrioridadDisponible();

                if (indiceSeleccionado == -1)
                {
                    tiempoActual++;
                    Thread.Sleep(1000);
                    continue;
                }

                if (procesoAnterior != indiceSeleccionado)
                {
                    if (procesoAnterior != -1)
                    {
                        int procesoGantt = procesoAnterior;
                        int inicio = inicioBloque;
                        int fin = tiempoActual;

                        Dispatcher.Invoke(() =>
                        {
                            AgregarBloqueGantt(listaTickets[procesoGantt], inicio, fin);
                        });
                    }

                    inicioBloque = tiempoActual;
                    procesoAnterior = indiceSeleccionado;
                }

                Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < listaTickets.Count; i++)
                    {
                        if (listaTickets[i].RemainingTime > 0 && listaTickets[i].ArrivalTime <= tiempoActual)
                        {
                            listaTickets[i].Estado = "En espera";
                        }
                    }

                    listaTickets[indiceSeleccionado].Estado = "En atención";
                    txtTicketActual.Text = listaTickets[indiceSeleccionado].Proceso + " - " + listaTickets[indiceSeleccionado].Ticket;
                    dgTickets.Items.Refresh();
                });

                Thread.Sleep(1000);

                Dispatcher.Invoke(() =>
                {
                    listaTickets[indiceSeleccionado].RemainingTime--;
                    dgTickets.Items.Refresh();
                });

                tiempoActual++;

                Dispatcher.Invoke(() =>
                {
                    if (listaTickets[indiceSeleccionado].RemainingTime == 0)
                    {
                        int completionTime = tiempoActual;
                        int turnaroundTime = completionTime - listaTickets[indiceSeleccionado].ArrivalTime;
                        int waitingTime = turnaroundTime - listaTickets[indiceSeleccionado].BurstTime;

                        listaTickets[indiceSeleccionado].CompletionTime = completionTime;
                        listaTickets[indiceSeleccionado].TurnaroundTime = turnaroundTime;
                        listaTickets[indiceSeleccionado].WaitingTime = waitingTime;
                        listaTickets[indiceSeleccionado].Estado = "Finalizado";

                        dgTickets.Items.Refresh();
                    }
                });
            }

            if (procesoAnterior != -1)
            {
                int procesoGanttFinal = procesoAnterior;
                int inicio = inicioBloque;
                int fin = tiempoActual;

                Dispatcher.Invoke(() =>
                {
                    AgregarBloqueGantt(listaTickets[procesoGanttFinal], inicio, fin);
                });
            }
        }

        private int BuscarTicketMayorPrioridadDisponible()
        {
            int indiceSeleccionado = -1;
            int mejorPrioridad = int.MaxValue;

            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < listaTickets.Count; i++)
                {
                    if (listaTickets[i].ArrivalTime <= tiempoActual && listaTickets[i].RemainingTime > 0)
                    {
                        if (listaTickets[i].Prioridad < mejorPrioridad)
                        {
                            mejorPrioridad = listaTickets[i].Prioridad;
                            indiceSeleccionado = i;
                        }
                        else if (listaTickets[i].Prioridad == mejorPrioridad && indiceSeleccionado != -1)
                        {
                            if (listaTickets[i].ArrivalTime < listaTickets[indiceSeleccionado].ArrivalTime)
                            {
                                indiceSeleccionado = i;
                            }
                        }
                    }
                }
            });

            return indiceSeleccionado;
        }

        private bool TodosFinalizados()
        {
            bool todosFinalizados = true;

            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < listaTickets.Count; i++)
                {
                    if (listaTickets[i].RemainingTime > 0)
                    {
                        todosFinalizados = false;
                        break;
                    }
                }
            });

            return todosFinalizados;
        }

        private void ReiniciarValoresTabla()
        {
            for (int i = 0; i < listaTickets.Count; i++)
            {
                listaTickets[i].RemainingTime = listaTickets[i].BurstTime;
                listaTickets[i].CompletionTime = 0;
                listaTickets[i].WaitingTime = 0;
                listaTickets[i].TurnaroundTime = 0;
                listaTickets[i].Estado = "Pendiente";
            }
        }

        private void CalcularPromedios()
        {
            int sumaWaiting = 0;
            int sumaTurnaround = 0;
            int cantidad = listaTickets.Count;

            for (int i = 0; i < listaTickets.Count; i++)
            {
                sumaWaiting += listaTickets[i].WaitingTime;
                sumaTurnaround += listaTickets[i].TurnaroundTime;
            }

            if (cantidad > 0)
            {
                double promedioWaiting = (double)sumaWaiting / cantidad;
                double promedioTurnaround = (double)sumaTurnaround / cantidad;

                txtPromedioEspera.Text = promedioWaiting.ToString("0.00");
                txtPromedioRetorno.Text = promedioTurnaround.ToString("0.00");
            }
        }

        private void CambiarEstadoControlesEnEjecucion()
        {
            btnAgregar.IsEnabled = true;

            btnEjecutar.IsEnabled = false;
            btnEjemplo.IsEnabled = false;
            btnLimpiar.IsEnabled = false;

            cmbTipoPrioridad.IsEnabled = false;

            txtArrivalTime.IsEnabled = false;

            txtTicket.IsEnabled = true;
            txtBurstTime.IsEnabled = true;
            cmbPrioridad.IsEnabled = true;
        }

        private void CambiarEstadoControlesFinalizado()
        {
            btnAgregar.IsEnabled = true;
            btnEjecutar.IsEnabled = true;
            btnEjemplo.IsEnabled = true;
            btnLimpiar.IsEnabled = true;

            cmbTipoPrioridad.IsEnabled = true;

            txtArrivalTime.IsEnabled = true;

            txtTicket.IsEnabled = true;
            txtBurstTime.IsEnabled = true;
            cmbPrioridad.IsEnabled = true;
        }

        private void AgregarBloqueGantt(TicketPrioridad ticket, int inicio, int fin)
        {
            if (inicio == fin)
            {
                return;
            }

            Border bloque = new Border();

            bloque.Background = (Brush)Application.Current.Resources["AcentoRosa"];
            bloque.BorderBrush = (Brush)Application.Current.Resources["ColorPrimario"];
            bloque.BorderThickness = new Thickness(1);
            bloque.CornerRadius = new CornerRadius(7);
            bloque.Padding = new Thickness(12);
            bloque.Margin = new Thickness(0, 0, 8, 0);
            bloque.MinWidth = 130;

            StackPanel contenido = new StackPanel();

            TextBlock txtProceso = new TextBlock();
            txtProceso.Text = ticket.Proceso;
            txtProceso.FontWeight = FontWeights.Bold;
            txtProceso.Foreground = (Brush)Application.Current.Resources["TextoOscuro"];

            TextBlock txtTicket = new TextBlock();
            txtTicket.Text = ticket.Ticket;
            txtTicket.FontSize = 12;
            txtTicket.TextWrapping = TextWrapping.Wrap;
            txtTicket.Foreground = (Brush)Application.Current.Resources["TextoClaro"];

            TextBlock txtPrioridad = new TextBlock();
            txtPrioridad.Text = "Prioridad: " + ticket.Prioridad;
            txtPrioridad.FontSize = 12;
            txtPrioridad.Foreground = (Brush)Application.Current.Resources["TextoClaro"];

            TextBlock txtTiempo = new TextBlock();
            txtTiempo.Text = inicio + " - " + fin;
            txtTiempo.FontSize = 12;
            txtTiempo.FontWeight = FontWeights.SemiBold;
            txtTiempo.Foreground = (Brush)Application.Current.Resources["ColorPrimario"];

            contenido.Children.Add(txtProceso);
            contenido.Children.Add(txtTicket);
            contenido.Children.Add(txtPrioridad);
            contenido.Children.Add(txtTiempo);

            bloque.Child = contenido;

            spGantt.Children.Add(bloque);
        }

        private void btnEjemplo_Click(object sender, RoutedEventArgs e)
        {
            if (ejecutando)
            {
                MessageBox.Show("No puede cargar un ejemplo mientras se ejecuta el algoritmo.");
                return;
            }

            listaTickets.Clear();
            spGantt.Children.Clear();

            listaTickets.Add(new TicketPrioridad
            {
                Proceso = "P1",
                Ticket = "Cambio de contraseña",
                ArrivalTime = 0,
                BurstTime = 8,
                Prioridad = 3,
                RemainingTime = 8,
                CompletionTime = 0,
                WaitingTime = 0,
                TurnaroundTime = 0,
                Estado = "Pendiente"
            });

            listaTickets.Add(new TicketPrioridad
            {
                Proceso = "P2",
                Ticket = "Servidor caído",
                ArrivalTime = 2,
                BurstTime = 4,
                Prioridad = 1,
                RemainingTime = 4,
                CompletionTime = 0,
                WaitingTime = 0,
                TurnaroundTime = 0,
                Estado = "Pendiente"
            });

            listaTickets.Add(new TicketPrioridad
            {
                Proceso = "P3",
                Ticket = "Error en reporte",
                ArrivalTime = 3,
                BurstTime = 2,
                Prioridad = 2,
                RemainingTime = 2,
                CompletionTime = 0,
                WaitingTime = 0,
                TurnaroundTime = 0,
                Estado = "Pendiente"
            });

            listaTickets.Add(new TicketPrioridad
            {
                Proceso = "P4",
                Ticket = "Configurar impresora",
                ArrivalTime = 5,
                BurstTime = 3,
                Prioridad = 3,
                RemainingTime = 3,
                CompletionTime = 0,
                WaitingTime = 0,
                TurnaroundTime = 0,
                Estado = "Pendiente"
            });

            txtPromedioEspera.Text = "0";
            txtPromedioRetorno.Text = "0";
            txtTicketActual.Text = "Ninguno";
            txtModo.Text = "No expropiativa";

            dgTickets.Items.Refresh();
        }


        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            if (ejecutando)
            {
                MessageBox.Show("No puede limpiar mientras el algoritmo se está ejecutando.");
                return;
            }

            listaTickets.Clear();
            spGantt.Children.Clear();

            txtTicket.Clear();
            txtArrivalTime.Clear();
            txtBurstTime.Clear();

            cmbPrioridad.SelectedIndex = 0;
            cmbTipoPrioridad.SelectedIndex = 0;

            txtPromedioEspera.Text = "0";
            txtPromedioRetorno.Text = "0";
            txtTicketActual.Text = "Ninguno";
            txtModo.Text = "No expropiativa";

            tiempoActual = 0;
        }
    }
}
