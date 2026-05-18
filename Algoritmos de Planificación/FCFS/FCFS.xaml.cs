using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Algoritmos_de_Planificación.FCFS
{
    /// <summary>
    /// Lógica de interacción para FCFS.xaml
    /// </summary>
    public partial class FCFS : UserControl
    {
        ObservableCollection<PedidoFCFS> listaPedidos = new ObservableCollection<PedidoFCFS>();

        bool ejecutando = false;

        public FCFS()
        {
            InitializeComponent();
            dgPedidos.ItemsSource = listaPedidos;
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            string cliente = txtCliente.Text.Trim();
            string pedido = txtPedido.Text.Trim();
            string textoBurstTime = txtBurstTime.Text.Trim();

            int burstTime;

            if (!ValidarDatos(cliente, pedido, textoBurstTime, out burstTime))
            {
                return;
            }

            PedidoFCFS nuevoPedido = new PedidoFCFS();

            nuevoPedido.Proceso = "P" + (listaPedidos.Count + 1);
            nuevoPedido.Cliente = cliente;
            nuevoPedido.Pedido = pedido;
            nuevoPedido.BurstTime = burstTime;
            nuevoPedido.WaitingTime = 0;
            nuevoPedido.TurnaroundTime = 0;
            nuevoPedido.Estado = "En espera";

            listaPedidos.Add(nuevoPedido);

            txtCliente.Clear();
            txtPedido.Clear();
            txtBurstTime.Clear();
            txtCliente.Focus();
        }

        private bool ValidarDatos(string cliente, string pedido, string textoBurstTime, out int burstTime)
        {
            burstTime = 0;

            if (cliente == "")
            {
                MessageBox.Show("Debe ingresar el nombre del cliente.");
                txtCliente.Focus();
                return false;
            }

            if (cliente.Length < 2)
            {
                MessageBox.Show("El nombre del cliente debe tener al menos 2 letras.");
                txtCliente.Focus();
                return false;
            }

            if (!Regex.IsMatch(cliente, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$"))
            {
                MessageBox.Show("El nombre del cliente solo debe contener letras y espacios. No ingrese números ni símbolos.");
                txtCliente.Focus();
                return false;
            }

            if (pedido == "")
            {
                MessageBox.Show("Debe ingresar el nombre del pedido.");
                txtPedido.Focus();
                return false;
            }

            if (pedido.Length < 2)
            {
                MessageBox.Show("El pedido debe tener al menos 2 caracteres.");
                txtPedido.Focus();
                return false;
            }

            if (textoBurstTime == "")
            {
                MessageBox.Show("Debe ingresar el Burst Time o tiempo de preparación.");
                txtBurstTime.Focus();
                return false;
            }

            if (!int.TryParse(textoBurstTime, out burstTime))
            {
                MessageBox.Show("El Burst Time debe ser un número entero. Ejemplo: 3, 5, 8.");
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
                MessageBox.Show("Para la simulación, el Burst Time no debe ser mayor a 20 segundos.");
                txtBurstTime.Focus();
                return false;
            }

            return true;
        }

        private void btnEjecutar_Click(object sender, RoutedEventArgs e)
        {
            if (listaPedidos.Count == 0)
            {
                MessageBox.Show("Debe agregar pedidos antes de ejecutar el algoritmo.");
                return;
            }

            if (ejecutando)
            {
                MessageBox.Show("El algoritmo ya se está ejecutando.");
                return;
            }

            ejecutando = true;
            CambiarEstadoBotones(false);

            Thread hiloFCFS = new Thread(EjecutarFCFS);
            hiloFCFS.IsBackground = true;
            hiloFCFS.Start();
        }

        private void EjecutarFCFS()
        {
            int n = 0;

            Dispatcher.Invoke(() =>
            {
                n = listaPedidos.Count;

                spGantt.Children.Clear();
                txtPromedioEspera.Text = "0";
                txtPromedioRetorno.Text = "0";
                txtPedidoActual.Text = "Iniciando...";

                for (int i = 0; i < listaPedidos.Count; i++)
                {
                    listaPedidos[i].WaitingTime = 0;
                    listaPedidos[i].TurnaroundTime = 0;
                    listaPedidos[i].Estado = "En espera";
                }

                dgPedidos.Items.Refresh();
            });

            int[] burstTime = new int[n];
            int[] waitingTime = new int[n];
            int[] turnaroundTime = new int[n];

            int totalWaitingTime = 0;
            int totalTurnaroundTime = 0;

            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < n; i++)
                {
                    burstTime[i] = listaPedidos[i].BurstTime;
                }
            });

            for (int i = 0; i < n; i++)
            {
                int temp = burstTime[i];

                waitingTime[i] = totalWaitingTime;
                totalWaitingTime += temp;

                turnaroundTime[i] = totalTurnaroundTime + temp;
                totalTurnaroundTime += temp;

                int indice = i;
                int inicio = waitingTime[i];
                int fin = turnaroundTime[i];

                Dispatcher.Invoke(() =>
                {
                    listaPedidos[indice].WaitingTime = waitingTime[indice];
                    listaPedidos[indice].TurnaroundTime = turnaroundTime[indice];
                    listaPedidos[indice].Estado = "En preparación";

                    txtPedidoActual.Text = listaPedidos[indice].Proceso + " - " + listaPedidos[indice].Pedido;

                    AgregarBloqueGantt(listaPedidos[indice], inicio, fin);

                    dgPedidos.Items.Refresh();
                });

                Thread.Sleep(temp * 1000);

                Dispatcher.Invoke(() =>
                {
                    listaPedidos[indice].Estado = "Finalizado";
                    dgPedidos.Items.Refresh();
                });
            }

            double promedioWaitingTime = (double)SumarWaitingTime() / n;
            double promedioTurnaroundTime = (double)SumarTurnaroundTime() / n;

            Dispatcher.Invoke(() =>
            {
                txtPromedioEspera.Text = promedioWaitingTime.ToString("0.00");
                txtPromedioRetorno.Text = promedioTurnaroundTime.ToString("0.00");
                txtPedidoActual.Text = "Todos finalizados";

                ejecutando = false;
                CambiarEstadoBotones(true);
            });
        }

        private void CambiarEstadoBotones(bool habilitado)
        {
            btnAgregar.IsEnabled = habilitado;
            btnEjecutar.IsEnabled = habilitado;
            btnEjemplo.IsEnabled = habilitado;
            btnLimpiar.IsEnabled = habilitado;
        }

        private int SumarWaitingTime()
        {
            int suma = 0;

            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < listaPedidos.Count; i++)
                {
                    suma += listaPedidos[i].WaitingTime;
                }
            });

            return suma;
        }

        private int SumarTurnaroundTime()
        {
            int suma = 0;

            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < listaPedidos.Count; i++)
                {
                    suma += listaPedidos[i].TurnaroundTime;
                }
            });

            return suma;
        }

        private void AgregarBloqueGantt(PedidoFCFS pedido, int inicio, int fin)
        {
            Border bloque = new Border();

            bloque.Background = (Brush)Application.Current.Resources["AcentoRosa"];
            bloque.BorderBrush = (Brush)Application.Current.Resources["ColorPrimario"];
            bloque.BorderThickness = new Thickness(1);
            bloque.CornerRadius = new CornerRadius(7);
            bloque.Padding = new Thickness(12);
            bloque.Margin = new Thickness(0, 0, 8, 0);
            bloque.MinWidth = 120;

            StackPanel contenido = new StackPanel();

            TextBlock txtProceso = new TextBlock();
            txtProceso.Text = pedido.Proceso + " - " + pedido.Cliente;
            txtProceso.FontWeight = FontWeights.Bold;
            txtProceso.Foreground = (Brush)Application.Current.Resources["TextoOscuro"];

            TextBlock txtPedido = new TextBlock();
            txtPedido.Text = pedido.Pedido;
            txtPedido.FontSize = 12;
            txtPedido.Foreground = (Brush)Application.Current.Resources["TextoClaro"];

            TextBlock txtTiempo = new TextBlock();
            txtTiempo.Text = inicio + " - " + fin;
            txtTiempo.FontSize = 12;
            txtTiempo.FontWeight = FontWeights.SemiBold;
            txtTiempo.Foreground = (Brush)Application.Current.Resources["ColorPrimario"];

            contenido.Children.Add(txtProceso);
            contenido.Children.Add(txtPedido);
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

            listaPedidos.Clear();
            spGantt.Children.Clear();

            listaPedidos.Add(new PedidoFCFS
            {
                Proceso = "P1",
                Cliente = "Ana",
                Pedido = "Café",
                BurstTime = 3,
                WaitingTime = 0,
                TurnaroundTime = 0,
                Estado = "En espera"
            });

            listaPedidos.Add(new PedidoFCFS
            {
                Proceso = "P2",
                Cliente = "Luis",
                Pedido = "Ensalada",
                BurstTime = 10,
                WaitingTime = 0,
                TurnaroundTime = 0,
                Estado = "En espera"
            });

            listaPedidos.Add(new PedidoFCFS
            {
                Proceso = "P3",
                Cliente = "Carla",
                Pedido = "Té",
                BurstTime = 2,
                WaitingTime = 0,
                TurnaroundTime = 0,
                Estado = "En espera"
            });

            listaPedidos.Add(new PedidoFCFS
            {
                Proceso = "P4",
                Cliente = "Diego",
                Pedido = "Brownie",
                BurstTime = 4,
                WaitingTime = 0,
                TurnaroundTime = 0,
                Estado = "En espera"
            });

            txtPromedioEspera.Text = "0";
            txtPromedioRetorno.Text = "0";
            txtPedidoActual.Text = "Ninguno";

            dgPedidos.Items.Refresh();
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            if (ejecutando)
            {
                MessageBox.Show("No puede limpiar mientras el algoritmo se está ejecutando.");
                return;
            }

            listaPedidos.Clear();
            spGantt.Children.Clear();

            txtCliente.Clear();
            txtPedido.Clear();
            txtBurstTime.Clear();

            txtPromedioEspera.Text = "0";
            txtPromedioRetorno.Text = "0";
            txtPedidoActual.Text = "Ninguno";
        }
    }
}
