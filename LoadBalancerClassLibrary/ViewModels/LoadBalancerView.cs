using LoadBalancerClassLibrary.Base;
using LoadBalancerClassLibrary.Models;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace LoadBalancerClassLibrary.ViewModels
{
    public class LoadBalancerView : NotificationBase<LoadBalancerModel>
    {
        private readonly DelegateCommand _removeServerCommand;
        private readonly DelegateCommand _addServerCommand;
        private readonly DelegateCommand _clearLogCommand;
        private readonly DelegateCommand _startStopCommand;
        private readonly DelegateCommand _checkMethodsCommand;
        public ICommand RemoveServerCommand => _removeServerCommand;
        public ICommand AddServerCommand => _addServerCommand;
        public ICommand ClearLogCommand => _clearLogCommand;
        public ICommand StartStopCommand => _startStopCommand;
        public ICommand CheckMethods => _checkMethodsCommand;

        public LoadBalancerView()
        {
            _removeServerCommand = new DelegateCommand(OnRemoveServer);
            _addServerCommand = new DelegateCommand(OnAddServer);
            _clearLogCommand = new DelegateCommand(OnClearLog);
            _startStopCommand = new DelegateCommand(OnStartStop);
            _checkMethodsCommand = new DelegateCommand(OnCheckMethods);
        }

        private void OnClearLog(object commandParameter)
        {
            This.Log.Clear();
        }

        private void OnCheckMethods(object commandParameter)
        {
            This.InitAlgos();
            NotifyPropertyChanged(nameof(This.MethodItems));
        }

        private void OnStartStop(object commandParameter)
        {
            StartStop();
        }
        private void OnAddServer(object commandParameter)
        {
            This.AddServer();
        }

        private void OnRemoveServer(object commandParameter)
        {
            This.RemoveServer(commandParameter);
        }

        public string IP
        {
            get { return This.IP; }
            set { SetProperty(This.IP, value, () => This.IP = value); }
        }

        public int BUFFER_SIZE
        {
            get { return This.BUFFER_SIZE; }
            set { SetProperty(This.BUFFER_SIZE, value, () => This.BUFFER_SIZE = value); }
        }

        public int PORT
        {
            get { return This.PORT; }
            set { SetProperty(This.PORT, value, () => This.PORT = value); }
        }

        public string IP_ADD
        {
            get { return This.IP_ADD; }
            set { SetProperty(This.IP_ADD, value, () => This.IP_ADD = value); }
        }

        public int PORT_ADD
        {
            get { return This.PORT_ADD; }
            set { SetProperty(This.PORT_ADD, value, () => This.PORT_ADD = value); }
        }

        public bool PERSIST
        {
            get { return This.PERSIST; }
            set { SetProperty(This.PERSIST, value, () => This.PERSIST = value); }
        }

        public ObservableCollection<ListBoxItem> Log
        {
            get { return This.Log; }
        }

        public ObservableCollection<ListBoxItem> ServerList
        {
            get { return This.ServerList; }
        }

        public ObservableCollection<ListBoxItem> MethodItems
        {
            get { return This.MethodItems; }
        }

        public ObservableCollection<ListBoxItem> HealthItems
        {
            get { return This.HealthItems; }
        }

        public ObservableCollection<ListBoxItem> PersistItems
        {
            get { return This.PersistItems; }
        }

        public ListBoxItem SelectedItem
        {
            get { return This.SelectedItem; }
        }

        public string SelectedMethodString
        {
            get { return This.SelectedMethodString; }
            set { SetProperty(This.SelectedMethodString, value, () => This.SelectedMethodString = value); }
        }

        public string SelectedPersistString
        {
            get { return This.SelectedPersistString; }
            set { SetProperty(This.SelectedPersistString, value, () => This.SelectedPersistString = value); }
        }

        public string SelectedHealthString
        {
            get { return This.SelectedHealthString; }
            set { SetProperty(This.SelectedHealthString, value, () => This.SelectedHealthString = value); }
        }

        public void StartStop()
        {
            try
            {
                if (!This.ACTIVE && !This.STOPPING)
                {
                    This.Start();
                }
                else if (This.ACTIVE && !This.STOPPING)
                {
                    This.Stop();
                }
            }
            catch
            {
                This.AddToLog("Er is een fout opgetreden.");
            }
        }

    }
}
