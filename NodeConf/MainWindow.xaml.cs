using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace X13 {
  public partial class MainWindow : Window {
    private Project _prj;

    public MainWindow() {
      InitializeComponent();
    }

    private void buNew_Click(object sender, RoutedEventArgs e) {
      var cd = new CreateDialog();
      var rez = cd.ShowDialog();
      if(rez == true && cd.prj!=null) {
        _prj = cd.prj;
        this.Title = System.IO.Path.GetFileNameWithoutExtension(_prj.Path)+" - Node Configurator";
        lvPins.ItemsSource = _prj.pins;
      }
    }
    private void buLoadProject_Click(object sender, RoutedEventArgs e) {
      var dlg = new Microsoft.Win32.OpenFileDialog();
      dlg.DefaultExt = "*.xml";
      dlg.Filter = "Project (*.xml)|*.xml|All Files|*.*";
      dlg.CheckFileExists = true;
      dlg.InitialDirectory = System.IO.Path.GetFullPath("projects");
      if(!Directory.Exists(dlg.InitialDirectory)) {
        Directory.CreateDirectory(dlg.InitialDirectory);
      }
      var res = dlg.ShowDialog();
      if(res == true) {
        var curDir = System.IO.Directory.GetCurrentDirectory();
        var oPath = dlg.FileName;
        if(oPath.StartsWith(curDir) && oPath.Length > curDir.Length) {
          oPath = oPath.Substring(curDir.Length + 1);
        }
        _prj = Project.Load(oPath);
        if(_prj != null) {
          this.Title = System.IO.Path.GetFileNameWithoutExtension(_prj.Path) + " - Node Configurator";
          lvPins.ItemsSource = _prj.pins;
        }
      }
    }
    private void buSaveProject_Click(object sender, RoutedEventArgs e) {
      if(_prj != null) {
        _prj.Save();
      }
    }

    private void buExport_Click(object sender, RoutedEventArgs e) {
      if(_prj != null) {
        _prj.Export();
      }

    }

  }
}
