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
using System.Windows.Shapes;

namespace X13 {
  /// <summary>
  /// Interaction logic for CreateDialog.xaml
  /// </summary>
  public partial class CreateDialog : Window {
    internal Project prj { get; private set; }

    public CreateDialog() {
      InitializeComponent();
      cbCPU.ItemsSource = Directory.GetFiles(@".\cpu", "*.xml", SearchOption.TopDirectoryOnly).Select(z => System.IO.Path.GetFileNameWithoutExtension(z));
      var phys = new string[] {"none", "CC1101", "ENC28J60", "RFM69", "RS485", "UART" };
      cbPhy1.ItemsSource = phys;
      cbPhy1.SelectedIndex = 0;
      cbPhy2.ItemsSource = phys;
      cbPhy2.SelectedIndex = 0;
    }

    private void buSelectProject_Click(object sender, RoutedEventArgs e) {
      var dlg = new Microsoft.Win32.SaveFileDialog();
      dlg.DefaultExt = "*.xml";
      dlg.Filter = "Project (*.xml)|*.xml|All Files|*.*";
      dlg.CheckPathExists = true;
      dlg.OverwritePrompt = true;
      dlg.InitialDirectory = System.IO.Path.GetFullPath("projects");
      if(!Directory.Exists(dlg.InitialDirectory)) {
        Directory.CreateDirectory(dlg.InitialDirectory);
      }
      dlg.FileName = System.IO.Path.GetFullPath(prj.Path);
      var res = dlg.ShowDialog();
      if(res == true) {
        var curDir=System.IO.Directory.GetCurrentDirectory();
        var oPath = dlg.FileName;
        if(oPath.StartsWith(curDir) && oPath.Length > curDir.Length) {
          prj.Path = oPath.Substring(curDir.Length + 1);
        } else {
          prj.Path = oPath;
        }
        tbProjectName.Text = prj.Path;
      }
    }

    private void cbCPU_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      prj = Project.LoadFromCpu(cbCPU.SelectedValue as string);
      if(prj != null) {
        cbPhy1.IsEnabled = true;
        cbPhy2.IsEnabled = true;
        buSelectProject.IsEnabled = true;
        tbProjectName.Text = prj.Path;
        //
        buOk.IsEnabled = true;
      }
    }
    private void cbPhy1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if(prj != null) {
        prj.phy1=phyBase.Create(cbPhy1.SelectedValue as string, 1);
        tbProjectName.Text = prj.Path;
      }
    }
    private void cbPhy2_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if(prj != null) {
        prj.phy2 = phyBase.Create(cbPhy2.SelectedValue as string, 2);
        tbProjectName.Text = prj.Path;
      }

    }

    private void buOk_Click(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }


  }
}
