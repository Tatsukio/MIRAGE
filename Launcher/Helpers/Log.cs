using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using MIRAGE_Launcher.Models;

namespace MIRAGE_Launcher.Helpers
{
    static class Log
    {
        private static readonly int Port = Settings.GetI("Launcher", "UDPLogsPort");
        private static readonly UdpClient udpClient = new();
        private static readonly IPEndPoint endPoint = new(IPAddress.Broadcast, Port);

        private static void Print(string p_message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(p_message);
                udpClient.Send(data, data.Length, endPoint);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending log: {ex.Message}");
            }
        }

        public static void Info(string p_message)
        {
            MessageBox.Show(p_message, Locale.warning, MessageBoxButton.OK, MessageBoxImage.Information);
            Print($"MIRAGE.Launcher INFO: {p_message}");
        }

        public static void Warn(string p_message)
        {
            MessageBox.Show(p_message, Locale.warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            Print($"MIRAGE.Launcher WARN: {p_message}");
        }

        public static void Error(string p_message)
        {
            MessageBox.Show(p_message, null, MessageBoxButton.OK, MessageBoxImage.Error);
            Print($"MIRAGE.Launcher ERROR: {p_message}");
        }

        public static void Close()
        {
            udpClient.Close();
        }
    }
}
