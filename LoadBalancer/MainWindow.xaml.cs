﻿using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LoadBalancerClassLibrary.ViewModels;

namespace LoadBalancer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LoadBalancerView VM;

        public MainWindow()
        {
            InitializeComponent();
            VM = new LoadBalancerView();
            DataContext = VM;
        }
    }
}