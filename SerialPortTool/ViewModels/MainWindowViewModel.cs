using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using SerialPortTool.Models;
using SMK.CRC;
using static System.Runtime.InteropServices.JavaScript.JSType;
using MessageBox = HandyControl.Controls.MessageBox;

namespace SerialPortTool.ViewModels
{
    partial class MainWindowViewModel : ObservableObject
    {
        private SerialPort _serialPort = new();

        [ObservableProperty]
        private SerialPortModel serialPortModel = new SerialPortModel();

        [ObservableProperty]
        private string[]? portArry;

        [ObservableProperty]
        private string[]? baudRateArry;

        [ObservableProperty]
        private string[]? parityBitsArry;

        [ObservableProperty]
        private string[]? stopBitsArry;

        [ObservableProperty]
        private string[]? dataBitsArry;

        [ObservableProperty]
        private string? sendMessageText;

        [ObservableProperty]
        private string? reciveMessageText;

        [ObservableProperty]
        private bool showSendTime;

        [ObservableProperty]
        private bool showReciveTime;

        [ObservableProperty]
        private string? buttonText;

        /// <summary>
        /// 0:utf-8 1:hex 2:ascll
        /// </summary>
        [ObservableProperty]
        private int sendMessageEnCoding;

        /// <summary>
        /// 0:utf-8 1:hex 2:ascll
        /// </summary>
        [ObservableProperty]
        private int reciveMessageEnCoding;

        [ObservableProperty]
        private bool isCRC;

        [RelayCommand]
        public void WindowClosed()
        {
            CloseConnction();
        }

        [RelayCommand]
        public void TryConnection()
        {
            switch (_serialPort.IsOpen)
            {
                case false:
                {
                    Connection();
                    break;
                }
                case true:
                {
                    CloseConnction();
                    break;
                }
            }
        }

        [RelayCommand]
        public void WindowLoaded()
        {
            InitData();
        }

        [RelayCommand]
        public void SendMessage()
        {
            if (!_serialPort.IsOpen || string.IsNullOrWhiteSpace(SendMessageText))
            {
                return;
            }
            Byte[] data;
            switch (SendMessageEnCoding)
            {
                case 0:
                {
                    data = Encoding.Default.GetBytes(SendMessageText);
                    _serialPort.Write(data, 0, data.Length);
                    break;
                }
                case 1:
                {
                    SendMessageText = Regex.Replace(SendMessageText, "[^0-9A-Fa-f]", " ");
                    string[] hexArry = SendMessageText.Split(' ');
                    hexArry = hexArry.Where(str => !string.IsNullOrEmpty(str)).ToArray();
                    byte[] bytes = new byte[hexArry.Length];
                    int I = bytes.Length;
                    for (int i = 0; i < hexArry.Length; i++)
                    {
                        try
                        {
                            bytes[i] = Convert.ToByte(hexArry[i], 16);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Error($"请输入有效的十六进制字符串{ex.Message}");
                        }
                    }
                        if (IsCRC)
                        {
                            var crc = CRC16.GetModBus(bytes);
                            byte[] crcBytes = BitConverter.GetBytes(crc);
                            bytes = bytes.Concat(crcBytes).ToArray();
                        }
                    _serialPort.Write(bytes, 0, bytes.Length);
                    break;
                }
                case 2:
                {
                    data = Encoding.ASCII.GetBytes(SendMessageText);
                    _serialPort.Write(data, 0, data.Length);
                    break;
                }
            }
            SendMessageText = ShowSendTime
                ? $"{DateTime.Now}:  TX: {SendMessageText}"
                : $"TX: {SendMessageText}";
            ReciveMessageText += $"\n{SendMessageText}";
            SendMessageText = string.Empty;
        }

        private bool CheckSerialPortProperty(SerialPortModel serialPortModel)
        {
            bool result = false;
            if (serialPortModel == null || string.IsNullOrWhiteSpace(serialPortModel.Port))
            {
                MessageBox.Error("请输入端口号");
            }
            else if (string.IsNullOrWhiteSpace(serialPortModel.BaudRate))
            {
                MessageBox.Error("请输入波特率");
            }
            else if (string.IsNullOrWhiteSpace(serialPortModel.DataBits))
            {
                MessageBox.Error("请输入数据位");
            }
            else if (string.IsNullOrWhiteSpace(serialPortModel.StopBits))
            {
                MessageBox.Error("请输入停止位");
            }
            else if (string.IsNullOrWhiteSpace(serialPortModel.ParityBits))
            {
                MessageBox.Error("请输入校验位");
            }
            else
            {
                result = true;
            }
            return result;
        }

        private void InitData()
        {
            IsCRC = true;
            ReciveMessageEnCoding = 2;
            SendMessageEnCoding = 2;
            ShowSendTime = true;
            ShowReciveTime = true;
            ButtonText = "打开";
            int baudRate = 1200;
            BaudRateArry = new string[12];
            for (int i = 0; i < 12; i++)
            {
                BaudRateArry[i] = baudRate.ToString();
                baudRate *= 2;
            }
            PortArry = SerialPort.GetPortNames(); 
            ParityBitsArry = Enum.GetNames(typeof(Parity));
            StopBitsArry = Enum.GetNames(typeof(StopBits));
            DataBitsArry = ["5", "6", "7", "8"];
            SerialPortModel.BaudRate = 9600.ToString();
            SerialPortModel.BaudRate = "9600";
            SerialPortModel.DataBits = "8";
            SerialPortModel.StopBits = StopBits.One.ToString();
            SerialPortModel.ParityBits = Parity.None.ToString();
            OnPropertyChanged(nameof(SerialPortModel));
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var bytes = new byte[_serialPort.BytesToRead];
            _serialPort.Read(bytes, 0, bytes.Length);
            string message = string.Empty;
            switch (ReciveMessageEnCoding)
            {
                case 0:
                {
                    message = Encoding.UTF8.GetString(bytes);
                    break;
                }
                case 1:
                {
                    message = BitConverter.ToString(bytes).Replace("-", "");
                    break;
                }
                case 2:
                {
                    message = Encoding.ASCII.GetString(bytes);
                    break;
                }
            }
            string dataTime = ShowReciveTime ? DateTime.Now + ":" : string.Empty;
            ReciveMessageText += $"\n{dataTime}  RX: {message}";
        }

        private void CloseConnction()
        {
            _serialPort.Close();
            ButtonText = "打开";
        }

        private void Connection()
        {
            if (!CheckSerialPortProperty(SerialPortModel))
            {
                return;
            }
            ;
            _serialPort.PortName = SerialPortModel.Port;
            _serialPort.BaudRate = int.Parse(SerialPortModel.BaudRate);
            _serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), SerialPortModel.StopBits);
            _serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), SerialPortModel.ParityBits);
            _serialPort.DataBits = int.Parse(SerialPortModel.DataBits);
            try
            {
                _serialPort.Open();
                ButtonText = "关闭";
                _serialPort.DataReceived += SerialPort_DataReceived;
            }
            catch (Exception ex)
            {
                MessageBox.Error($"连接端口失败{ex.Message}");
            }
        }
    }
}
