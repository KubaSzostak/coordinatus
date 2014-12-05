using Coordinatus.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Coordinatus.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            App.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {

            MessageBox.Show(e.Exception.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }


        private void Button2_Click(object sender, RoutedEventArgs eArgs)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (dlg.ShowDialog() != true)
                return;

            var txtLines = System.IO.File.ReadAllLines(dlg.FileName);
            var pts = new ObservableCollection<NehPoint>();

            var gsiFile = Path.ChangeExtension(dlg.FileName, ".gsi");
            using (var gsiStream = new FileStream(gsiFile, FileMode.Create))
            {
                using (var gsiWriter = new GSIFileWriter(gsiStream))
                {
                    foreach (var ln in txtLines)
                    {
                        if (!string.IsNullOrEmpty(ln.Trim())) {
                            var pt = new NehPoint(ln);
                            gsiWriter.AddPoint(pt.Id, pt.E, pt.N, pt.H);
                            pts.Add(pt); 
                        }
                    }
                }
            }
            PtsGrid.ItemsSource = pts;
            MessageBox.Show("Saved to: \r\n" + gsiFile);
        }

        private void Button3_Click(object sender, RoutedEventArgs eArgs)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (dlg.ShowDialog() != true)
                return;

            var txtLines = System.IO.File.ReadAllLines(dlg.FileName);
            var pts = new ObservableCollection<NehPoint>();

            var idexFile = Path.ChangeExtension(dlg.FileName, ".idx");
            using (var idexStream = new FileStream(idexFile, FileMode.Create))
            {
                using (var gsiWriter = new IDEXFileWriter(idexStream))
                {
                    foreach (var ln in txtLines)
                    {
                        if (!string.IsNullOrEmpty(ln.Trim())) {
                        var pt = new NehPoint(ln);
                        gsiWriter.AddPoint(pt.Id, pt.E, pt.N, pt.H, null);
                        pts.Add(pt);
                        }
                    }
                }
            }
            PtsGrid.ItemsSource = pts;
            MessageBox.Show("Saved to: \r\n" + idexFile);

        }

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new LeicaTpsExport();
            wnd.ShowDialog();
        }

        private void GoToWebsite_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/kubaszostak/coordinatus/");
        }


    }

    public class NehPoint
    {
        public string Id { get; set; }
        public double N { get; set; }
        public double E { get; set; }
        public double H { get; set; }

        public NehPoint(string ln)
        {
            try
            {
                var lnValues = ln.Trim().Split(" \t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                Id = lnValues[0];

                N = ToDouble(lnValues[1]);
                E = ToDouble(lnValues[2]);
                H = ToDouble(lnValues[3]);
            }
            catch (Exception ex)
            {
                throw new Exception("Cannot load point information from text: \r\n" + ln, ex);
            }
        }

        private static string DecSep  = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;

        public double ToDouble(string s)
        {
            s = s.Replace(".", DecSep).Replace(",", DecSep);
            return Convert.ToDouble(s);
        }
    }
}
