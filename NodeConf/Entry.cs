using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace X13 {
  internal abstract class enBase : INotifyPropertyChanged {
    public static readonly enNone none = new enNone();

    public readonly EntryType type;
    public string name { get; protected set; }
    public bool selected { get; set; }
    public Dictionary<string, RcUse> resouces { get; protected set; }

    protected enBase(XElement info, Pin parent, EntryType type) {
      this.type = type;
      resouces = new Dictionary<string, RcUse>();
      if(info != null) {
        var tx = info.Attribute("name");
        if(tx != null) {
          this.name = tx.Value;
        }
      }
    }

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler PropertyChanged;
    protected void PropertyChangedReise([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "") {
      if(PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }
    #endregion INotifyPropertyChanged Members

  }
  internal enum EntryType {
    none,
    dio,
    ain,
    pwm,
    serial,
    twi,
    spi,
    system,
  }

  internal class enNone : enBase {
    public enNone()
      : base(null, null, EntryType.none) {
    }

    public override string ToString() { return "none"; }


  }
  internal class enDIO : enBase {
    public enDIO(XElement info, Pin parent)
      : base(info, parent, EntryType.dio) {
      resouces[parent.name + "_used"] = RcUse.Shared;
    }

  }

  internal class enSystem : enBase {
    public enSystem(XElement info, Pin parent)
      : base(info, parent, EntryType.system) {
      resouces[parent.name + "_used"] = RcUse.Exclusive;
    }
    public override string ToString() {
      return this.name;
    }
  }
  internal class enSerial : enBase {
    private byte _config;
    private int _channel;

    public enSerial(XElement info, Pin parent)
      : base(info, parent, EntryType.serial) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this._channel = int.Parse(info.Attribute("channel").Value);
      this._config = byte.Parse(info.Attribute("config").Value);
      this.name = "UART" + _channel.ToString() + "_" + info.Attribute("name").Value;
      resouces[this.name] = RcUse.Exclusive;
      resouces["UART" + _channel.ToString() + "_CFG"] = (RcUse)(0x100+_config);
    }
    public override string ToString() {
      return this.name + "_CFG" + _config.ToString();
    }
  }
}
