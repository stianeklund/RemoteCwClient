using System.IO.Ports;
using System.Net.Sockets;
using System.Text;

namespace RemoteCwClient;

// Compatible with the RemoteCW client
public static class CwClient
{
    private static TcpClient _tcpClient;
    private static SerialPort _serialPort = new();
    private static readonly string User = new("MG:Callsign MyCallsign;\r\n");
    private static readonly string Password = new("MG:Password MyPassword;\r\n");
    private const string HostName = "";
    private const int Port = 5555;


    private static void Main()
    {
        _serialPort = new SerialPort
        {
            BaudRate = 1200,
            DataBits = 8,
            Handshake = Handshake.RequestToSend,
            Parity = Parity.None,
            PortName = "COM3",
            StopBits = StopBits.One
        };

        _serialPort.Open();

        _tcpClient = new TcpClient(HostName, Port);
        _tcpClient.Client.Send(Encoding.Default.GetBytes(User));
        _tcpClient.Client.Send(Encoding.Default.GetBytes(Password));

        // Set contest spacing, no auto space, paddle echo watchdog on, etc.
        var winKeyConnect = new byte[]
        {
            0x55, 0x1F, 0x8A, 0xC0
        };
        var enablePtt = new byte[] { 0x8A, 0xC0 };

        // Host enable
        _serialPort.Write(new byte[] { 0x00, 0x02 }, 0, 2);
        Thread.Sleep(50);

        // Set register values
        _serialPort.Write(winKeyConnect, 0, winKeyConnect.Length);
        Thread.Sleep(50);
        // Enable ptt 
        _serialPort.Write(enablePtt, 0, enablePtt.Length);

        // TODO destruct / decode this
        const string keyerSpeed = "WS:25|25|40|23|1;\r\n";

        // turn server echo on
        _tcpClient.Client.Send(Encoding.Default.GetBytes("E:1\r\n"));
        _tcpClient.Client.Send(Encoding.Default.GetBytes(keyerSpeed));

        void OnSerialPortOnDataReceived(object o, SerialDataReceivedEventArgs e)
        {
            if (!_serialPort.IsOpen) return;

            var serialData = _serialPort.ReadExisting();

            // only send real ascii resulting in proper morse code chars
            // TODO consider handling commands separately
            // if (!serialData.Any(c => char.IsLetter(c) || char.IsNumber(c) || c is ' ' or '/' or '?')) return;
            if (!serialData.Any(c => char.IsLetter(c) || char.IsNumber(c) || c is ' ' or '/')) return;
            var data = $"WK:{serialData};\r\n";
            Console.WriteLine($"{data}\r\n");
            _tcpClient.GetStream().Write(Encoding.ASCII.GetBytes(data));
        }

        _serialPort.DataReceived += OnSerialPortOnDataReceived;

        var bytes = new byte[1000];

        do
        {
            var stream = _tcpClient.GetStream();
            int i;
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                // TODO handle keyer speed etc..
                var msg = Encoding.Default.GetString(bytes, 0, i);
                // Console.WriteLine($"Received: {msg}");
                //    _serialPort.Write(msg);
            }
        } while (_tcpClient.Connected);
    }
}