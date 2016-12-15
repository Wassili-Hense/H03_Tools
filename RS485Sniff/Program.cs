using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace X13 {
  class Program {
    static void Main(string[] args) {
      if(args.Length == 0 || string.IsNullOrWhiteSpace(args[0])) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("USE: RS485Sniff.exe <serial port name>");
        Console.Beep();
        Console.ReadKey();
        Console.ResetColor();
        return;
      }
      string sn = args[0];
      SerialPort port;
      byte[] buf = new byte[1024];
      int len = 0;
      int cnt = 0;
      int b;

      try {
        port = new SerialPort(sn, 250000, Parity.None, 8, StopBits.One);
        port.ReadBufferSize = 300;
        port.WriteBufferSize = 300;
        port.ReadTimeout = 5;
        port.Open();
      }
      catch(Exception ex) {
        Log.Error("SerialPort({0}).Open - {1}", sn, ex.Message);
        Console.Beep();
        Console.ReadKey();
        Log.Finish();
        return;
      }
      Log.Info("SerialPort({0}) Opened", sn);

      do {
        if(port.BytesToRead == 0) {
          Thread.Sleep(10);
          continue;
        }
        b = port.ReadByte();
        if(cnt == 0 && b != 0xC3) {
          continue;
        }
        if(cnt == 1) {
          len = b;
        }
        buf[cnt] = (byte)b;
        if(cnt - 2 == len) {
          if(len > 1) {
            Log.Debug(BitConverter.ToString(buf, 0, cnt + 1));
            try {
              var msg = X13.Periphery.MsMessage.Parse(buf, 4, cnt);
              if(msg != null) {
                Log.Info("{0:X2}>{1:X2} {2} ", buf[2], buf[3], msg.ToString());
              } else {
                Log.Info("{0:X2}>{1:X2} *{2}", buf[2], buf[3], ((X13.Periphery.MsMessageType)buf[5]).ToString());
              }
            }
            catch(Exception ex) {
              Log.Warning("BAD message - {0}", ex.Message);
            }
          //} else {
          //  Console.Write("*" + buf[2].ToString("X2"));
          }
          cnt = 0;
          len = 0;
        } else {
          cnt++;
        }
      } while(!Console.KeyAvailable);

      port.Close();
      Log.Finish();
    }
  }
}
