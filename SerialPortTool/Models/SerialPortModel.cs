using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialPortTool.Models
{
    class SerialPortModel
    {
        public string? Port { set; get; }

        public string? BaudRate { set; get; }

        public string? ParityBits { set; get; }

        public string? StopBits { set; get; }

        public string? DataBits { set; get; }
    }
}
