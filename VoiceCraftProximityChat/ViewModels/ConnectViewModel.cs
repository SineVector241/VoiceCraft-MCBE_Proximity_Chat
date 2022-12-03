using System;
using System.Net;
using System.Windows.Forms;
using System.Windows.Input;
using VoiceCraftProximityChat.Models;

namespace VoiceCraftProximityChat.ViewModels
{
    public class ConnectViewModel : ViewModelBase
    {
        //Fields
        private string _ip;
        private string _port;
        private string _key;
        private string _errorMessage;
        private string _connectButtonMessage = "Connect";
        private bool _isViewVisible = true;
        private UdpClientModel udpClient = new UdpClientModel();

        public string Ip { get => _ip; set { _ip = value; OnPropertyChanged(nameof(Ip)); } }
        public string Port { get => _port; set { _port = value; OnPropertyChanged(nameof(Port)); } }
        public string Key { get => _key; set { _key = value; OnPropertyChanged(nameof(Key)); } }
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); } }
        public string ConnectButtonMessage { get => _connectButtonMessage; set { _connectButtonMessage = value; OnPropertyChanged(nameof(ConnectButtonMessage)); } }
        public bool IsViewVisible { get => _isViewVisible; set { _isViewVisible = value; OnPropertyChanged(nameof(IsViewVisible)); } }

        //Commands
        public ICommand ConnectCommand { get; }

        //Constructor
        public ConnectViewModel()
        {
            ConnectCommand = new DelegateCommand(ExecuteConnectCommand, CanExecuteConnectCommand);
        }

        private bool CanExecuteConnectCommand(object obj)
        {
            bool validData;
            if (string.IsNullOrWhiteSpace(Ip) || Ip.Length < 9 || Port == null || Port.Length < 4 || Key == null || Key.Length < 5)
                validData = false;
            else
                validData = true;
            return validData;
        }

        private void ExecuteConnectCommand(object obj)
        {
            try
            {
                if (ConnectButtonMessage != "Connecting...")
                {
                    ErrorMessage = "";
                    ConnectButtonMessage = "Connecting...";
                    udpClient.Connect(IPAddress.Parse(_ip), Convert.ToInt16(_port));
                    udpClient.Login(_key, CheckForConnection);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "An error has occured...", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckForConnection(bool isConnected)
        {
            if (isConnected)
            {
                ConnectButtonMessage = "Connect";
                IsViewVisible = false;
            }
            else
            {
                ErrorMessage = "Error: Could not connect to server or key was invalid";
                ConnectButtonMessage = "Connect";
                udpClient.Dispose();
            }
        }
    }
}