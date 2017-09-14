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
      cbCPU.ItemsSource = Directory.GetFiles(@".\cpu", "*.xml", SearchOption.TopDirectoryOnly).Select(z => System.IO.Path.GetFileNameWithoutExtension(z).ToUpper());
      var phys = (new string[] { "none" }).Union(Directory.GetFiles(@".\phy", "*.xml", SearchOption.TopDirectoryOnly).Select(z => System.IO.Path.GetFileNameWithoutExtension(z).ToUpper())).ToArray();
      cbPhy1.ItemsSource = phys;
      cbPhy1.SelectedIndex = 0;
      cbPhy2.ItemsSource = phys;
      cbPhy2.SelectedIndex = 0;
      cbPhy3.ItemsSource = phys;
      cbPhy3.SelectedIndex = 0;
      cbPhy4.ItemsSource = phys;
      cbPhy4.SelectedIndex = 0;
    }

    private void cbCPU_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      string cpu = cbCPU.SelectedItem as string;
      if(_prj != null && _prj.cpu == cpu) {
        return;
      }
      cbPhy1.SelectedItem = "none";
      cbPhy2.SelectedItem = "none";
      _prj = Project.LoadFromCpu(cpu);
      if(_prj != null) {
        cbPhy1.IsEnabled = true;
        cbPhy2.IsEnabled = true;
        this.Title = System.IO.Path.GetFileNameWithoutExtension(_prj.Path) + " - Node Configurator";
        lvPins.ItemsSource = _prj.pins;
      }
    }
    private void cbPhy1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if(_prj == null) {
        cbPhy1.IsEnabled = false;
        cbPhy2.IsEnabled = false;
        cbPhy3.IsEnabled = false;
        cbPhy4.IsEnabled = false;
        return;
      }
      string phy = cbPhy1.SelectedItem as string;
      if(_prj.phy1 == null || _prj.phy1.name != phy) {
        _prj.phy1 = phyBase.Create(phy, 1);
      }
      this.Title = System.IO.Path.GetFileNameWithoutExtension(_prj.Path) + " - Node Configurator";
      lvPins.ItemsSource = null;
      lvPins.ItemsSource = _prj.pins;
      if(_prj.phy1 != null) {
        cbPhy1.BorderBrush = new SolidColorBrush(_prj.phy1.color);
        cbPhy2.IsEnabled = true;
      } else {
        cbPhy1.BorderBrush = null;
        cbPhy2.IsEnabled = false;
        cbPhy3.IsEnabled = false;
        cbPhy4.IsEnabled = false;
      }
    }
    private void cbPhy2_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if(_prj == null) {
        cbPhy1.IsEnabled = false;
        cbPhy2.IsEnabled = false;
        cbPhy3.IsEnabled = false;
        cbPhy4.IsEnabled = false;
        return;
      }
      string phy = cbPhy2.SelectedItem as string;
      if(_prj.phy2 == null || _prj.phy2.name != phy) {
        _prj.phy2 = phyBase.Create(phy, 2);
      }
      this.Title = System.IO.Path.GetFileNameWithoutExtension(_prj.Path) + " - Node Configurator";
      lvPins.ItemsSource = null;
      lvPins.ItemsSource = _prj.pins;
      if(_prj.phy2 != null) {
        cbPhy2.BorderBrush = new SolidColorBrush(_prj.phy2.color);
        cbPhy3.IsEnabled = true;
      } else {
        cbPhy2.BorderBrush = null;
        cbPhy3.IsEnabled = false;
        cbPhy4.IsEnabled = false;
      }
    }
    private void cbPhy3_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if(_prj == null) {
        cbPhy1.IsEnabled = false;
        cbPhy2.IsEnabled = false;
        cbPhy3.IsEnabled = false;
        cbPhy4.IsEnabled = false;
        return;
      }
      string phy = cbPhy3.SelectedItem as string;
      if(_prj.phy3 == null || _prj.phy3.name != phy) {
        _prj.phy3 = phyBase.Create(phy, 3);
      }
      this.Title = System.IO.Path.GetFileNameWithoutExtension(_prj.Path) + " - Node Configurator";
      lvPins.ItemsSource = null;
      lvPins.ItemsSource = _prj.pins;
      if(_prj.phy3 != null) {
        cbPhy3.BorderBrush = new SolidColorBrush(_prj.phy2.color);
        cbPhy4.IsEnabled = true;
      } else {
        cbPhy3.BorderBrush = null;
        cbPhy4.IsEnabled = false;
      }
    }
    private void cbPhy4_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if(_prj == null) {
        cbPhy1.IsEnabled = false;
        cbPhy2.IsEnabled = false;
        cbPhy3.IsEnabled = false;
        cbPhy4.IsEnabled = false;
        return;
      }
      string phy = cbPhy4.SelectedItem as string;
      if(_prj.phy4 == null || _prj.phy4.name != phy) {
        _prj.phy4 = phyBase.Create(phy, 4);
      }
      this.Title = System.IO.Path.GetFileNameWithoutExtension(_prj.Path) + " - Node Configurator";
      lvPins.ItemsSource = null;
      lvPins.ItemsSource = _prj.pins;
      if(_prj.phy4 != null) {
        cbPhy4.BorderBrush = new SolidColorBrush(_prj.phy4.color);
      } else {
        cbPhy4.BorderBrush = null;
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
          cbPhy1.IsEnabled = true;
          cbPhy2.IsEnabled = true;
          cbCPU.SelectedItem = _prj.cpu;
          cbPhy1.SelectedItem = _prj.phy1 == null ? "none" : _prj.phy1.name;
          cbPhy2.SelectedItem = _prj.phy2 == null ? "none" : _prj.phy2.name;
          cbPhy3.SelectedItem = _prj.phy3 == null ? "none" : _prj.phy3.name;
          cbPhy4.SelectedItem = _prj.phy4 == null ? "none" : _prj.phy4.name;
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
    private void Button_Click(object sender, RoutedEventArgs e) {
      if(_prj == null) {
        return;
      }
      var dlg = new Microsoft.Win32.SaveFileDialog();
      dlg.DefaultExt = "*.xml";
      dlg.Filter = "Project (*.xml)|*.xml|All Files|*.*";
      dlg.CheckPathExists = true;
      dlg.OverwritePrompt = true;
      dlg.InitialDirectory = System.IO.Path.GetFullPath("projects");
      if(!Directory.Exists(dlg.InitialDirectory)) {
        Directory.CreateDirectory(dlg.InitialDirectory);
      }
      dlg.FileName = System.IO.Path.GetFullPath(_prj.Path);
      var res = dlg.ShowDialog();
      if(res == true) {
        var curDir = System.IO.Directory.GetCurrentDirectory();
        var oPath = dlg.FileName;
        if(oPath.StartsWith(curDir) && oPath.Length > curDir.Length) {
          _prj.Path = oPath.Substring(curDir.Length + 1);
        } else {
          _prj.Path = oPath;
        }
        this.Title = System.IO.Path.GetFileNameWithoutExtension(_prj.Path) + " - Node Configurator";
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
