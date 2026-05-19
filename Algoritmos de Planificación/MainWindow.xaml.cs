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

namespace Algoritmos_de_Planificación
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnFCFS_Click(object sender, RoutedEventArgs e)
        {
            ContenedorContenido.Content = new FCFS.FCFS();
        }

        private void BtnPrioridad_Click(object sender, RoutedEventArgs e)
        {
            ContenedorContenido.Content = new Prioridad.Prioridad();
        }

        private void BtnSJF_Click(object sender, RoutedEventArgs e)
        {
            ContenedorContenido.Content = new SJF_SRTF.Views.SJFView();
        }

        private void BtnSRTF_Click(object sender, RoutedEventArgs e)
        {
            ContenedorContenido.Content = new SJF_SRTF.Views.SRTFView();
        }

        private void BtnRoundRobin_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnMultiLevelQueue_Click(object sender, RoutedEventArgs e)
        {
            ContenedorContenido.Content = new MultiLevelQueue.MultiLevelQueue();
        }
    }
}