﻿using System;
using System.Collections.Generic;
using Client.Connections;
using Client.Exceptions;
using Client.Views;

namespace Client.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        public Action CloseAction { get; set; }       
        public static Dictionary<string, IList<string>> RoleForWorkflow { get; set; }

        private bool _loginStarted;
        private readonly Uri _serverAddress;

        public LoginViewModel()
        {
            var settings = Settings.LoadSettings();
            Username = settings.Username;
            _serverAddress = new Uri(settings.ServerAddress);

            _status = "";
            _password = "Password";
        }

        #region Databindings

        private string _username;
        public string Username
        {
            get { return _username; }
            set
            {
                _username = value;
                NotifyPropertyChanged("Username");
            }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged("Status");
            }
        }
        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                NotifyPropertyChanged("Password");
            }
        }
        #endregion

        #region Actions

        public async void Login()
        {
            if (_loginStarted) return;
            _loginStarted = true;
            Status = "Attempting login...";

            try
            {
                using (IServerConnection connection = new ServerConnection(_serverAddress))
                {
                    RoleForWorkflow = (await connection.Login(Username, Password)).RolesOnWorkflows;
                }
                Status = "Login successful";

                // Save settings
                new Settings
                {
                    ServerAddress = _serverAddress.AbsoluteUri,
                    Username = _username,
                }.SaveSettings();

                new MainWindow().Show();
                CloseAction.Invoke();
            }
            catch (LoginFailedException)
            {
                _loginStarted = false;
                Status = "The provided username and password does not correspond to a user in Flow";
            }
            catch (HostNotFoundException)
            {
                _loginStarted = false;
                Status = "The server is not available, or the settings file is pointing to an invalid address";
            }
            catch (Exception)
            {
                _loginStarted = false;
                Status = "An unexpected error occured. Try again in a while.";
            }
        }

        #endregion

        
    }
}
