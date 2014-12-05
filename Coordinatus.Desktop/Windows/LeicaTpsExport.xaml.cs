using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Coordinatus.Windows
{
    /// <summary>
    /// Interaction logic for LeicaTpsExport.xaml
    /// </summary>
    public partial class LeicaTpsExport : Window
    {
        public LeicaTpsExport()
        {
            InitializeComponent();

            var ports =SerialPort.GetPortNames();
            this.SerialPortsBox.ItemsSource = ports;
            if (this.SerialPortsBox.Items.Count > 0)
                this.SerialPortsBox.SelectedIndex = 0;
            
        }


        private string EndMark = "\u001a"; // ((char)0x1A).ToString();

        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                                  new Action(delegate { }));
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var sp = new GSISerialPort(this.SerialPortsBox.Text))
            {
                sp.Open();

                sp.Connect();
                sp.Beep();
                MessageBox.Show(sp.InstrumentName);


                var jobs = new ObservableCollection<string>();
                JobsBox.ItemsSource = jobs;

                var resp = sp.GetResponse("JOB/CONF/LIST"); 
                while (resp != EndMark)
                {
                    jobs.Add(resp);
                    DoEvents();
                    resp = sp.GetResponse("?");
                }
                
            }
        }
    }

    public class GSIError 
    {
        public GSIError ()
	    {

	    }

    }

}
