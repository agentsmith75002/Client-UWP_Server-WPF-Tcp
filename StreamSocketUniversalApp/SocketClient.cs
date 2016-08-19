﻿using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace StreamSocketUniversalApp
{
    public sealed class SocketClient : BindableBase
    {

        

        private readonly string _ip;
        private readonly int _port;
        private StreamSocket _socket;
        private Socket _secSocket;
        private BackgroundWorker bgw;
        private DataWriter _writer;
        private DataReader _reader;
        private Random rnd;
        public bool IsAlive;

        private string numClt;
        public string NumClt
        {
            get { return numClt; }
            set { SetProperty(ref numClt, value); }
        }

        private string message;
        public string Message
        {
            get { return message; }
            set { SetProperty(ref message, value); }
        }

        private string received;
        public string Received
        {
            get { return received; }
            set { SetProperty(ref received, value); }
        }


        public delegate void Error(string message);
        public event Error OnError;


        private bool isConnected;
        public bool IsConnected
        {
            get { return isConnected; }
            set { SetProperty(ref isConnected, value); }
        }

        public string Ip { get { return _ip; } }
        public int Port { get { return _port; } }

        public SocketClient(string ip, int port)
        {
            _ip = ip;
            _port = port;
            rnd = new Random();
            NumClt = "Client "+rnd.Next(1000, 9000).ToString();
            Message = "";
            Received = "";
            bgw = new BackgroundWorker();
            bgw.DoWork += Bgw_DoWork;

            
        }

        private async void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            DataReader reader;
           
            using (reader = new DataReader(_socket.InputStream))
            {
                try
                {



                    
                   
                    string result = "";
                    reader.InputStreamOptions = Windows.Storage.Streams.InputStreamOptions.Partial;
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;
                    
                    while(IsConnected)
                    {

                        
                        
                            await reader.LoadAsync(1);
                            while (reader.UnconsumedBufferLength > 0 )
                            {
                                result += reader.ReadString(reader.UnconsumedBufferLength);
                                
                                await reader.LoadAsync(1);
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                {
                                    Received = result;
                                });
                            }
                            
                            reader.DetachStream();
                            if (Received.Contains("Exit")) IsConnected= false;

                        
                    }
                        
                    




                }
                finally
                {
                    reader.DetachStream();
                }
            }
           
        }

        public async void Connect()
        {
            try
            {
                IsAlive = true;
                IsConnected = true;
                var hostName = new HostName(Ip);
                _socket = new StreamSocket();
                await _socket.ConnectAsync(hostName, Port.ToString());
                _writer = new DataWriter(_socket.OutputStream);
                Read();
                Send("Start\n");
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ex.Message);
            }
        }

        public async void Send(string message)
        {
            _writer.WriteString(message);

            try
            {

                await _writer.StoreAsync();

                await _writer.FlushAsync();
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ex.Message);
            }
        }

        public async void Send()
        {
           
            _writer.WriteString(Message+"\n");
            
            try
            {
                
                await _writer.StoreAsync();
                
                await _writer.FlushAsync();
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ex.Message);
            }
        }

        private void Read()
        {
            bgw.RunWorkerAsync();
        }

        public void Close()
        {
            try
            {
                IsConnected = false;
                IsAlive = false;
                Message = "exit\n";
                this.Send();
                _writer.DetachStream();
                _writer.Dispose();

                _reader.DetachStream();
                _reader.Dispose();

                _socket.Dispose();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            
            
        }
    }
}
