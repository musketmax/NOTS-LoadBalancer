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
        public ICommand RemoveServerCommand => _removeServerCommand;
        public ICommand AddServerCommand => _addServerCommand;

        public LoadBalancerView()
        {
            _removeServerCommand = new DelegateCommand(OnRemoveServer);
            _addServerCommand = new DelegateCommand(OnAddServer);
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

        public ObservableCollection<ListBoxItem> Log
        {
            get { return This.Log; }
        }

        public ObservableCollection<ListBoxItem> ServerList
        {
            get { return This.ServerList; }
        }

        public ListBoxItem SelectedItem
        {
            get { return This.SelectedItem; }
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

        public void ClearLog()
        {
            This.Log.Clear();
        }

    }
}
