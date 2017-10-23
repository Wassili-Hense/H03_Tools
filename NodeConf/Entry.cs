///<remarks>This file is part of the <see cref="https://github.com/X13home">X13.Home</see> project.<remarks>
using JSC = NiL.JS.Core;
using JSL = NiL.JS.BaseLibrary;
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

    protected readonly Pin _parent;
    public readonly EntryType type;
    public readonly Signal signal;
    public string name { get; protected set; }
    public string func;
    public bool selected { get; set; }
    public bool isAvailable {
      get {
        return _parent == null || _parent._owner.EntryIsEnabled(this);
      }
    }
    public Dictionary<string, RcUse> resouces { get; protected set; }

    private enBase() {
      this.type = EntryType.none;
      this.signal = Signal.NONE;
      this.resouces = new Dictionary<string, RcUse>();
      this.name = "--";
    }

    protected enBase(XElement info, Pin parent, Signal signal) {
      this.type = (EntryType)((int)signal >> 2);
      this.signal = signal;
      _parent = parent;
      resouces = new Dictionary<string, RcUse>();
      if(info != null) {
        var tx = info.Attribute("name");
        if(tx != null) {
          this.name = tx.Value;
        }
      }
    }

    public virtual void ExportX04(Dictionary<string, JSC.JSObject> children) {
    }
    protected void ExportS(Dictionary<string, JSC.JSObject> children, string rc, string lt, string kt, JSC.JSValue defVal) {
      var t1 = JSC.JSObject.CreateObject();
      var t2 = JSC.JSObject.CreateObject();
      var t3 = JSC.JSObject.CreateObject();
      t1["default"] = defVal;
      t1["manifest"] = t2;
      t2["MQTT-SN"] = t3;
      if(defVal.IsNumber) {
        t2["type"] = "Integer";
      }
      t3["tag"] = kt + _parent._addr.ToString();
      t1["menu"] = lt;
      t1["rc"] = rc;
      if(!string.IsNullOrWhiteSpace(_parent.titelCur)) {
        t1["hint"] = lt + ", " + _parent.titelCur;
      }
      children[kt + _parent._addr.ToString("00")] = t1;
    }

    public Pin parent { get { return _parent; } }
    public override string ToString() {
      return this.func ?? this.name;
    }
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
  internal enum Signal {
    NONE = ((int)EntryType.none << 2) | 0,
    DIO = ((int)EntryType.dio << 2) | 0,
    AIN = ((int)EntryType.ain << 2) | 0,
    PWM = ((int)EntryType.pwm << 2) | 0,
    UART_RX = ((int)EntryType.serial << 2) | 1,
    UART_TX = ((int)EntryType.serial << 2) | 2,
    UART_DE = ((int)EntryType.serial << 2) | 3,
    TWI_SDA = ((int)EntryType.twi << 2) | 1,
    TWI_SCL = ((int)EntryType.twi << 2) | 2,
    SPI_MISO = ((int)EntryType.spi << 2) | 0,
    SPI_MOSI = ((int)EntryType.spi << 2) | 1,
    SPI_SCK = ((int)EntryType.spi << 2) | 2,
    SPI_NSS = ((int)EntryType.spi << 2) | 3,
    SYSTEM = ((int)EntryType.system << 2) | 0,
    SYS_LED = ((int)EntryType.system << 2) | 1,

  }

  internal class enDIO : enBase {
    public enDIO(XElement info, Pin parent)
      : base(info, parent, Signal.DIO) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      base.name = parent.name;
    }
    public override void ExportX04(Dictionary<string, JSC.JSObject> children) {
      if(_parent.config == PinCfg.IO && selected) {
        string rc = "X" + _parent._owner.ExIndex(parent.name + "_used").ToString();
        ExportS(children, rc, "Digital Input", "Ip", false);
        ExportS(children, rc, "Digital inverted input", "In", false);
        ExportS(children, rc, "Digital output", "Op", false);
        ExportS(children, rc, "Digital inverted output", "On", false);
      }
    }
  }

  internal class enSystem : enBase {
    protected enSystem(Pin parent, Signal signal)
      : base(null, parent, signal) {
    }
    public enSystem(XElement info, Pin parent)
      : base(info, parent, Signal.SYSTEM) {
      resouces[parent.name + "_used"] = (RcUse)(0x100);
      var xn = info.Attribute("src");
      if(xn != null) {
        this.src = xn.Value;
      } else {
        this.src = null;
      }
    }
    public string src { get; private set; }
  }
  internal class enSysLed : enSystem {
    public enSysLed(enDIO dio, bool pnp)
      : base(dio.parent, Signal.SYS_LED) {
      this.pnp = pnp;
      resouces[parent.name + "_used"] = (RcUse)(0x100);
      resouces["SYSTEM_LED"] = RcUse.Exclusive;
      base.name = pnp ? "LED_P" : "LED_N";
    }
    public bool pnp { get; private set; }
  }
  internal class enAin : enBase {
    private int _channel;
    public readonly int ainRef;

    public enAin(XElement info, Pin parent)
      : base(info, parent, Signal.AIN) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this._channel = int.Parse(info.Attribute("channel").Value);
      var xn = info.Attribute("ainref");
      if(xn == null || xn.Value.Length <= 2 || !int.TryParse(xn.Value.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ainRef)) {
        ainRef = parent._owner.ainRef;
      }
      this.name = "AIN " + _channel.ToString("00");
      resouces[this.name] = RcUse.Exclusive;
    }
    public int GetConfig() {
      return _channel;
    }
    public override void ExportX04(Dictionary<string, JSC.JSObject> children) {
      if(_parent.config == PinCfg.IO && selected) {
        var rc = "X" + _parent._owner.ExIndex(parent.name + "_used").ToString() + ",X" + _parent._owner.ExIndex(name).ToString();
        if((ainRef & 1) == 1) {
          ExportS(children, rc, "Analog input external reference", "Ae", 0);
        }
        if((ainRef & 2) == 2) {
          ExportS(children, rc, "Analog input AVcc reference", "Av", 0);
        }
        if((ainRef & 4) == 4) {
          ExportS(children, rc, "Analog input internal reference", "Ai", 0);
        }
        if((ainRef & 8) == 8) {
          ExportS(children, rc, "Analog input internal2 reference", "AI", 0);
        }
      }
    }
  }
  internal class enPwm : enBase {
    private int _channel;
    private int _timer;
    private int _af;

    public enPwm(XElement info, Pin parent)
      : base(info, parent, Signal.PWM) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this._channel = int.Parse(info.Attribute("channel").Value);
      this._timer = int.Parse(info.Attribute("timer").Value);
      var xn = info.Attribute("af");
      if(xn != null) {
        this._af = int.Parse(xn.Value);
      } else {
        this._af = 0;
      }
      this.name = "PWM " + _timer.ToString("00") + "." + _channel.ToString();
      resouces[this.name] = RcUse.Exclusive;
    }
    public int GetConfig() {
      return (_af << 8) | (_timer << 3) | (_channel);
    }
    public override void ExportX04(Dictionary<string, JSC.JSObject> children) {
      if(_parent.config == PinCfg.IO && selected) {
        string rc = "X" + _parent._owner.ExIndex(parent.name + "_used").ToString() + ",X" + _parent._owner.ExIndex(name).ToString();
        ExportS(children, rc, "PWM output", "Pp", 0);
        ExportS(children, rc, "PWM inverted output", "Pn", 0);
      }
    }
  }
  internal class enSerial : enBase {
    public readonly byte config;
    public readonly int channel;
    public int mapping;

    public enSerial(XElement info, Pin parent)
      : base(info, parent, (Signal)Enum.Parse(typeof(Signal), "UART_" + info.Attribute("name").Value)) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this.channel = int.Parse(info.Attribute("channel").Value);
      this.config = byte.Parse(info.Attribute("config").Value);
      this.name = info.Attribute("name").Value + " " + channel.ToString() + "." + config.ToString();
      resouces["UART" + channel.ToString() + "_CFG"] = (RcUse)(0x100 + config);
    }
    public override void ExportX04(Dictionary<string, JSC.JSObject> children) {
      if(_parent.config == PinCfg.IO && selected) {
        string uName = "Serial " + mapping.ToString();
        int r_pin = parent._owner.ExIndex(parent.name + "_used");
        int r_s0 = parent._owner.ExIndex("UART" + channel.ToString() + "_s0");
        int r_s1 = parent._owner.ExIndex("UART" + channel.ToString() + "_s1");
        int r_s2 = parent._owner.ExIndex("UART" + channel.ToString() + "_s2");
        int r_s3 = parent._owner.ExIndex("UART" + channel.ToString() + "_s3");
        int r_s4 = parent._owner.ExIndex("UART" + channel.ToString() + "_s4");
        if(signal == Signal.UART_RX) {
          ExportS(children, string.Format("X{0},S{1},B{2},B{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4), "RxD 2400", "Sr" + mapping.ToString(), "¤BA", uName);
          ExportS(children, string.Format("X{0},B{1},S{2},B{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4), "RxD 4800", "Sr" + (mapping + 16).ToString(), "¤BA", uName);
          ExportS(children, string.Format("X{0},B{1},B{2},S{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4), "RxD 9600", "Sr" + (mapping + 32).ToString(), "¤BA", uName);
          ExportS(children, string.Format("X{0},B{1},B{2},B{3},S{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4), "RxD 19200", "Sr" + (mapping + 48).ToString(), "¤BA", uName);
          ExportS(children, string.Format("X{0},B{1},B{2},B{3},B{4},S{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4), "RxD 38400", "Sr" + (mapping + 64).ToString(), "¤BA", uName);
        } else if(signal == Signal.UART_TX) {
          ExportS(children, string.Format("X{0},S{1},B{2},B{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4), "TxD 2400", "St" + mapping.ToString(), "¤BA", uName);
          ExportS(children, string.Format("X{0},B{1},S{2},B{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4), "TxD 4800", "St" + (mapping + 16).ToString(), "¤BA", uName);
          ExportS(children, string.Format("X{0},B{1},B{2},S{3},B{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4), "TxD 9600", "St" + (mapping + 32).ToString(), "¤BA", uName);
          ExportS(children, string.Format("X{0},B{1},B{2},B{3},S{4},B{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4), "TxD 19200", "St" + (mapping + 48).ToString(), "¤BA", uName);
          ExportS(children, string.Format("X{0},B{1},B{2},B{3},B{4},S{5}", r_pin, r_s0, r_s1, r_s2, r_s3, r_s4), "TxD 38400", "St" + (mapping + 64).ToString(), "¤BA", uName);
        }
      }
    }
    protected void ExportS(Dictionary<string, JSC.JSObject> children, string rc, string lt, string kt, JSC.JSValue defVal, string me) {
      var t1 = JSC.JSObject.CreateObject();
      var t2 = JSC.JSObject.CreateObject();
      var t3 = JSC.JSObject.CreateObject();
      t1["default"] = defVal;
      t1["manifest"] = t2;
      t2["MQTT-SN"] = t3;
      t3["tag"] = kt;
      t1["menu"] = me;
      t1["rc"] = rc;
      if(string.IsNullOrWhiteSpace(_parent.titelCur)) {
        children[kt] = t1;
      } else {
        t1["hint"] = lt + ", " + _parent.titelCur;
        children[_parent.titelCur + "_" + kt] = t1;
      }
    }

  }
  internal class enSpi : enBase {
    public readonly byte config;
    public readonly int channel;

    public enSpi(XElement info, Pin parent)
      : base(info, parent, (Signal)Enum.Parse(typeof(Signal), "SPI_" + info.Attribute("name").Value)) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this.channel = int.Parse(info.Attribute("channel").Value);
      this.config = byte.Parse(info.Attribute("config").Value);
      this.name = info.Attribute("name").Value + " " + channel.ToString() + "." + config.ToString();
      resouces["SPI" + channel.ToString() + "_CFG"] = (RcUse)(0x100 + config);
    }
  }
  internal class enTwi : enBase {
    public readonly byte config;
    public readonly int channel;

    public enTwi(XElement info, Pin parent)
      : base(info, parent, (Signal)Enum.Parse(typeof(Signal), "TWI_" + info.Attribute("name").Value)) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this.channel = int.Parse(info.Attribute("channel").Value);
      this.config = byte.Parse(info.Attribute("config").Value);
      this.name = info.Attribute("name").Value + " " + channel.ToString() + "." + config.ToString();
      resouces["TWI_CFG"] = (RcUse)(0x100 + channel * 16 + config);
    }
  }
}
