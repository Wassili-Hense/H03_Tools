using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace X13 {
  internal class enBase {
    public static readonly enBase none = new enBase();

    public readonly EntryType type;
    public string name { get; protected set; }
    public bool selected { get; set; }
    public Dictionary<string, RcUse> resouces { get; protected set; }

    private enBase() {
      this.type = EntryType.none;
      this.resouces = new Dictionary<string, RcUse>();
      this.name = "none";
    }

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

    public override string ToString() { return this.name; }
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

  internal class enDIO : enBase {
    public enDIO(XElement info, Pin parent)
      : base(info, parent, EntryType.dio) {
      resouces[parent.name + "_used"] = RcUse.Shared;
    }

  }

  internal class enSystem : enBase {
    public enSystem(XElement info, Pin parent)
      : base(info, parent, EntryType.system) {
      resouces[parent.name + "_used"] = (RcUse)(0x100);
    }
  }
  internal class enAin : enBase {
    private int _channel;

    public enAin(XElement info, Pin parent)
      : base(info, parent, EntryType.ain) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this._channel = int.Parse(info.Attribute("channel").Value);
      this.name = "AIN" + _channel.ToString();
      resouces[this.name] = RcUse.Exclusive;
    }
  }
  internal class enPwm : enBase {
    private int _channel;
    private int _timer;
    private int _af;

    public enPwm(XElement info, Pin parent)
      : base(info, parent, EntryType.pwm) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this._channel = int.Parse(info.Attribute("channel").Value);
      this._timer = int.Parse(info.Attribute("timer").Value);
      this._af = int.Parse(info.Attribute("af").Value);
      this.name = "PWM" + _timer.ToString() + "_" + _channel.ToString();
      resouces[this.name] = RcUse.Exclusive;
    }
  }
  internal class enSerial : enBase {
    public readonly byte config;
    public readonly int channel;
    public readonly int signal; // 1 - rx, 2 - tx, 3 - de

    public enSerial(XElement info, Pin parent)
      : base(info, parent, EntryType.serial) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this.channel = int.Parse(info.Attribute("channel").Value);
      this.config = byte.Parse(info.Attribute("config").Value);
      this.name = "UART" + channel.ToString() + "_" + info.Attribute("name").Value;
      switch(info.Attribute("name").Value) {
      case "RX":
        signal = 1;
        break;
      case "TX":
        signal = 2;
        break;
      case "DE":
        signal = 3;
        break;
      }
      resouces["UART" + channel.ToString() + "_CFG"] = (RcUse)(0x100 + config);
    }
    public override string ToString() {
      return this.name + "_CFG" + config.ToString();
    }
  }
  internal class enSpi : enBase {
    private byte _config;
    private int _channel;

    public enSpi(XElement info, Pin parent)
      : base(info, parent, EntryType.spi) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this._channel = int.Parse(info.Attribute("channel").Value);
      this._config = byte.Parse(info.Attribute("config").Value);
      this.name = "SPI" + _channel.ToString() + "_" + info.Attribute("name").Value;
      resouces["SPI" + _channel.ToString() + "_CFG"] = (RcUse)(0x100 + _config);
    }
    public override string ToString() {
      return this.name + "_CFG" + _config.ToString();
    }
  }
  internal class enTwi : enBase {
    private byte _config;
    private int _channel;

    public enTwi(XElement info, Pin parent)
      : base(info, parent, EntryType.twi) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this._channel = int.Parse(info.Attribute("channel").Value);
      this._config = byte.Parse(info.Attribute("config").Value);
      this.name = "TWI" + _channel.ToString() + "_" + info.Attribute("name").Value;
      resouces["TWI" + _channel.ToString() + "_CFG"] = (RcUse)(0x100 + _config);
    }
    public override string ToString() {
      return this.name + "_CFG" + _config.ToString();
    }
  }

}
