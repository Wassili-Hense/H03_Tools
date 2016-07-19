﻿using System;
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
    public string name { get; set; }
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

  }

  internal class enDIO : enBase {
    public enDIO(XElement info, Pin parent)
      : base(info, parent, Signal.DIO) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      base.name = parent.name;
    }
  }

  internal class enSystem : enBase {
    public enSystem(XElement info, Pin parent)
      : base(info, parent, Signal.SYSTEM) {
      resouces[parent.name + "_used"] = (RcUse)(0x100);
    }
  }
  internal class enAin : enBase {
    private int _channel;

    public enAin(XElement info, Pin parent)
      : base(info, parent, Signal.AIN) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this._channel = int.Parse(info.Attribute("channel").Value);
      this.name = "AIN " + _channel.ToString("00");
      resouces[this.name] = RcUse.Exclusive;
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
  }
  internal class enSerial : enBase {
    public readonly byte config;
    public readonly int channel;

    public enSerial(XElement info, Pin parent)
      : base(info, parent, (Signal)Enum.Parse(typeof(Signal), "UART_" + info.Attribute("name").Value)) {
      resouces[parent.name + "_used"] = RcUse.Shared;
      this.channel = int.Parse(info.Attribute("channel").Value);
      this.config = byte.Parse(info.Attribute("config").Value);
      this.name = info.Attribute("name").Value + " " + channel.ToString() + "." + config.ToString();
      resouces["UART" + channel.ToString() + "_CFG"] = (RcUse)(0x100 + config);
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