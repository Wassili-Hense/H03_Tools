using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace X13 {
  internal class phyBase {
    public static phyBase Create(string name, int nr) {
      if(name == "none") {
        return null;
      }
      var p = new phyBase();
      p._nr = nr;
      try {
        XDocument doc = XDocument.Load(System.IO.Path.GetFullPath(@".\phy\" + name + ".xml"));
        p.signature = doc.Root.Attribute("signature").Value;
        p.name = name;
        p._pins = doc.Root.Elements("pin").Select(z => new phyPin(z)).ToArray();
        var ex=new List<Tuple<string, string[], bool>>();
        foreach(var a in doc.Root.Elements("append")) {
          var op = a.Attribute("optional");
          ex.Add(new Tuple<string, string[], bool>(a.Attribute("fmt").Value, a.Elements("var").Select(z=>z.Attribute("name").Value).ToArray(), op!=null && op.Value=="true"));
        }
        p._exportH = ex.ToArray();
        var xa = doc.Root.Attribute("color");
        int tmp;
        if(xa != null && !string.IsNullOrEmpty(xa.Value) && xa.Value.StartsWith("#") && int.TryParse(xa.Value.Substring(1), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out tmp)) {
          p.color = System.Windows.Media.Color.FromRgb((byte)(tmp >> 16), (byte)(tmp >> 8), (byte)tmp);
        } else {
          p.color = System.Windows.Media.Colors.Black;
        }
      }
      catch(Exception ex) {
        Console.WriteLine(ex.ToString());
        return null;
      }

      return p;
    }

    private int _nr;
    private phyPin[] _pins;
    private Tuple<string, string[], bool>[] _exportH;

    public string signature { get; private set; }
    public string name { get; private set; }
    public System.Windows.Media.Color color { get; private set; }

    public List<enBase> GetLst(Pin pin) {
      // SPI
      bool spi_defined = false;
      bool spi_hw_nss = pin._owner.pins.SelectMany(z => z.entrys).Where(z => z.signal == Signal.SPI_NSS).OfType<enSpi>().Any();
      int spi_channel = 0;
      int spi_config = 0;
      // UART
      bool uart_defined = false;
      int uart_channel = 0;
      int uart_config = 0;

      for(int i = 0; i < _pins.Length; i++) {
        if(_pins[i].pin != null) {
          if(!spi_defined && _pins[i].pin.type == EntryType.spi) {
            var sp = _pins[i].pin as enSpi;
            spi_defined = true;
            spi_channel = sp.channel;
            spi_config = sp.config;
          }
          if(!uart_defined && _pins[i].pin.type == EntryType.serial) {
            var sp = _pins[i].pin as enSerial;
            uart_defined = true;
            uart_channel = sp.channel;
            uart_config = sp.config;
          }
        }
      }

      var lst = new List<enBase>();
      foreach(var en in pin.entrys.Where(z => pin._owner.EntryIsEnabled(z))) {
        if(_pins.Any(z => z.pin == en || (z.pin == null && (en.signal == z.signal || (z.signal == Signal.SPI_NSS && !spi_hw_nss && en.signal == Signal.DIO))))) {
          if(en.type == EntryType.spi && spi_defined && ((en as enSpi).channel != spi_channel || (en as enSpi).config != spi_config)) {
            continue;
          }
          if(en.type == EntryType.serial && uart_defined && ((en as enSerial).channel != uart_channel || (en as enSerial).config != uart_config)) {
            continue;
          }
          lst.Add(en);
        }
      }

      if(lst.Count > 0) {
        lst.Insert(0, enBase.none);
        return lst;
      }
      return null;
    }
    public enBase GetCur(Pin pin) {
      var en = pin.entrys.FirstOrDefault(z => _pins.Any(z1 => z1.pin == z));
      return en ?? enBase.none;
    }
    public bool SetCur(Pin pin, enBase en, string func) {
      var op = _pins.FirstOrDefault(z => pin.entrys.Any(z1 => z1 == z.pin));
      if(en != null && (op == null ?en.signal!=Signal.NONE:op.pin != en)) {
        if(op != null) {
          op.pin.selected = false;
          op.pin.resouces[pin.name + "_used"] = RcUse.Shared;
          op.pin.func = null;
          op.pin = null;
        }
        op = _pins.FirstOrDefault(z => z.pin == null && (func==null || z.name==func) && (z.signal == en.signal
          || (z.signal == Signal.SPI_NSS && !pin._owner.pins.SelectMany(z1 => z1.entrys).Where(z1 => z1.signal == Signal.SPI_NSS).OfType<enSpi>().Any() && en.signal == Signal.DIO)));
        if(op != null) {
          en.selected = true;
          en.resouces[pin.name + "_used"] = RcUse.Exclusive;
          op.pin = en;
          en.func = op.name;
        }
        return true;
      }
      return false;
    }
    public string ExportH() {
      if(_pins.Any(z => z.pin == null && !z.optional)) {
        return "\r\n#error "+ name +"_PHY" + _nr.ToString() + "_NOT_CONFIGURED\r\n";
      }
      var sb = new StringBuilder();
      var vars = new Dictionary<string, string>();
      bool spi_defined = false;
      bool uart_defined = false;

      vars["nr"] = _nr.ToString();

      sb.AppendFormat("\r\n//{0} PHY{1} Section\t", name, _nr);
      for(int i = 0; i < _pins.Length; i++) {
        if(_pins[i].pin == null) {
          continue;
        }
        if(i>0){
          sb.Append(",");
        }
        sb.AppendFormat(" {0}: {1}", _pins[i].name, _pins[i].pin.parent.name);
        if(_pins[i].pin.signal == Signal.SPI_MISO) {
          enSpi sp = _pins[i].pin as enSpi;
          spi_defined = true;
          vars["spi.channel"] = sp.channel.ToString();
          vars["spi.config"] = sp.config.ToString();
        } else if(_pins[i].pin.signal == Signal.UART_RX) {
          enSerial sp = _pins[i].pin as enSerial;
          vars["uart.channel"] = sp.channel.ToString();
          vars["uart.mapping"] = sp.mapping.ToString();
          vars["uart.config"] = sp.config.ToString();
          uart_defined = true;
        }
        vars[_pins[i].name + ".pinnr"] = (_pins[i].pin.parent.idx + _pins[i].pin.parent.port.offset).ToString();
        vars[_pins[i].name + ".pinget"] = string.Format(_pins[i].pin.parent.port.pinget, _pins[i].pin.parent.idx);
        vars[_pins[i].name + ".pinset"] = string.Format(_pins[i].pin.parent.port.pinset, _pins[i].pin.parent.idx);
        vars[_pins[i].name + ".pinrst"] = string.Format(_pins[i].pin.parent.port.pinrst, _pins[i].pin.parent.idx);
      }
      sb.AppendLine();
      if(spi_defined) {
        sb.AppendFormat("#define HAL_USE_SPI{0}                {1}\r\n", vars["spi.channel"], vars["spi.config"]);
      }
      if(uart_defined) {
        sb.AppendFormat("#define HAL_USE_USART{0}              {1}\r\n", vars["uart.channel"], vars["uart.mapping"]);
        if(vars["uart.config"] != "0") {
          sb.AppendFormat("#define HAL_USART{0}_REMAP           {1}\r\n", vars["uart.channel"], vars["uart.config"]);
        }
      }
      bool miss;
      string tmpVal;
      List<string> vals=new List<string>();
      for(int i = 0; i < _exportH.Length; i++) {
        miss = false;
        vals.Clear();
        for(int j = 0; j < _exportH[i].Item2.Length; j++) {
          if(vars.TryGetValue(_exportH[i].Item2[j], out tmpVal)) {
            vals.Add(tmpVal);
          } else {
            miss = true;
          }
        }
        if(miss) {
          if(_exportH[i].Item3) {
            continue;
          } else {
            throw new ApplicationException("Missing variable in " + _exportH[i].Item1);
          }
        }
        sb.AppendFormat(_exportH[i].Item1, vals.ToArray());
        sb.AppendLine();
      }
      sb.AppendFormat("//End {0} PHY{1} Section\r\n", name, _nr);

      return sb.ToString();
    }
    private class phyPin {
      public phyPin(XElement info) {
        var xn = info.Attribute("signal");
        Signal s;
        if(xn != null && Enum.TryParse<Signal>(info.Attribute("signal").Value, out s)) {
          this.signal = s;
          this.type = (EntryType)((int)s >> 2);
        } else {
          throw new ArgumentException("unknown signal in " + info.ToString(SaveOptions.DisableFormatting));
        }
        xn = info.Attribute("name");
        if(xn != null) {
          this.name = xn.Value;
        }
        xn = info.Attribute("optional");
        this.optional = xn != null && xn.Value == "true";
      }
      public readonly EntryType type;
      public readonly Signal signal;
      public readonly string name;
      public readonly bool optional;
      public enBase pin;

    }
  }
}
