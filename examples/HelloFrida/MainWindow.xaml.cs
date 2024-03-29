﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace HelloTelco
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Telco.DeviceManager deviceManager;

        public ObservableCollection<Telco.Device> Devices { get; private set; }
        public ObservableCollection<Telco.Process> Processes { get; private set; }
        private Telco.Session session;
        private Telco.Script script;
        private bool scriptLoaded;

        public MainWindow()
        {
            InitializeComponent();
            Devices = new ObservableCollection<Telco.Device>();
            Processes = new ObservableCollection<Telco.Process>();
            DataContext = this;
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        private void RefreshAllowedActions()
        {
            deviceList.IsEnabled = session == null;
            refreshButton.IsEnabled = session == null && deviceList.SelectedItem != null;

            processList.IsEnabled = session == null;
            spawnButton.IsEnabled = processList.SelectedItem != null;
            resumeButton.IsEnabled = processList.SelectedItem != null;
            attachButton.IsEnabled = processList.SelectedItem != null && session == null;
            detachButton.IsEnabled = session != null;

            scriptSource.IsEnabled = session != null && script == null;
            createScriptButton.IsEnabled = session != null && script == null;
            loadScriptButton.IsEnabled = script != null && !scriptLoaded;
            unloadScriptButton.IsEnabled = script != null;
            postToScriptButton.IsEnabled = script != null && scriptLoaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            deviceManager = new Telco.DeviceManager(Dispatcher);
            deviceManager.Changed += new EventHandler(deviceManager_Changed);
            RefreshDeviceList();
            RefreshAllowedActions();
        }

        private void RefreshDeviceList()
        {
            var devices = deviceManager.EnumerateDevices();
            debugConsole.Items.Add(String.Format("Got {0} devices", devices.Length));
            Array.Sort(devices, delegate(Telco.Device a, Telco.Device b)
            {
                var aHasIcon = a.Icon != null;
                var bHasIcon = b.Icon != null;
                if (aHasIcon == bHasIcon)
                    return a.Id.CompareTo(b.Id);
                else
                    return bHasIcon.CompareTo(aHasIcon);
            });
            Devices.Clear();
            foreach (var device in devices)
                Devices.Add(device);
        }

        private void RefreshProcessList()
        {
            var device = deviceList.SelectedItem as Telco.Device;
            if (device == null)
            {
                Processes.Clear();
                return;
            }

            try
            {
                var processes = device.EnumerateProcesses(Telco.Scope.Full);
                Array.Sort(processes, delegate(Telco.Process a, Telco.Process b) {
                    var aHasIcon = a.Icons.Length != 0;
                    var bHasIcon = b.Icons.Length != 0;
                    if (aHasIcon == bHasIcon)
                        return a.Name.CompareTo(b.Name);
                    else
                        return bHasIcon.CompareTo(aHasIcon);
                });
                Processes.Clear();
                foreach (var process in processes)
                    Processes.Add(process);
            }
            catch (Exception ex)
            {
                debugConsole.Items.Add("EnumerateProcesses failed: " + ex.Message);
                Processes.Clear();
            }
        }

        private void deviceManager_Changed(object sender, EventArgs e)
        {
            debugConsole.Items.Add("DeviceManager Changed");
            RefreshDeviceList();
        }

        private void deviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshAllowedActions();
            RefreshProcessList();
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcessList();
        }

        private void processList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshAllowedActions();
        }

        private void spawnButton_Click(object sender, RoutedEventArgs e)
        {
            var device = deviceList.SelectedItem as Telco.Device;
            try
            {
                device.Spawn("C:\\Windows\\notepad.exe", new string[] { "C:\\Windows\\notepad.exe", "C:\\document.txt" }, null, null, null);
            }
            catch (Exception ex)
            {
                debugConsole.Items.Add("Spawn failed: " + ex.Message);
            }
        }

        private void resumeButton_Click(object sender, RoutedEventArgs e)
        {
            var device = deviceList.SelectedItem as Telco.Device;
            try
            {
                device.Resume(1337);
            }
            catch (Exception ex)
            {
                debugConsole.Items.Add("Resume failed: " + ex.Message);
            }
        }

        private void attachButton_Click(object sender, RoutedEventArgs e)
        {
            var device = deviceList.SelectedItem as Telco.Device;
            var process = processList.SelectedItem as Telco.Process;

            try
            {
                session = device.Attach(process.Pid);
            }
            catch (Exception ex)
            {
                debugConsole.Items.Add("Attach failed: " + ex.Message);
                return;
            }
            session.Detached += new Telco.SessionDetachedHandler(session_Detached);
            debugConsole.Items.Add("Attached to " + session.Pid);
            RefreshAllowedActions();
        }

        private void detachButton_Click(object sender, RoutedEventArgs e)
        {
            session.Detach();
            session = null;
            script = null;
            RefreshAllowedActions();
        }

        private void session_Detached(object sender, Telco.SessionDetachedEventArgs e)
        {
            if (sender == session)
            {
                debugConsole.Items.Add($"Detached from Session with PID {session.Pid} ({e.Reason})");
                session = null;
                script = null;
                RefreshAllowedActions();
            }
        }

        private void createScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (script != null)
            {
                try
                {
                    script.Unload();
                }
                catch (Exception ex)
                {
                    debugConsole.Items.Add("Failed to unload previous script: " + ex.Message);
                }
                script = null;
                scriptLoaded = false;
                RefreshAllowedActions();
            }

            try
            {
                script = session.CreateScript(scriptSource.Text);
                scriptLoaded = false;
                RefreshAllowedActions();
            }
            catch (Exception ex)
            {
                debugConsole.Items.Add("CreateScript failed: " + ex.Message);
                return;
            }
            script.Message += new Telco.ScriptMessageHandler(script_Message);
        }

        private void loadScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                script.Load();
                scriptLoaded = true;
                RefreshAllowedActions();
            }
            catch (Exception ex)
            {
                debugConsole.Items.Add("Load failed: " + ex.Message);
            }
        }

        private void unloadScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                script.Unload();
            }
            catch (Exception ex)
            {
                debugConsole.Items.Add("Failed to unload script: " + ex.Message);
            }
            script = null;
            scriptLoaded = false;
            RefreshAllowedActions();
        }

        private void postToScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                script.Post("{\"type\":\"banana\"}");
            }
            catch (Exception ex)
            {
                debugConsole.Items.Add("PostMessage failed: " + ex.Message);
            }
        }

        private void script_Message(object sender, Telco.ScriptMessageEventArgs e)
        {
            if (sender == script)
            {
                debugConsole.Items.Add(String.Format("Message from Script: {0}", e.Message));
                debugConsole.Items.Add(String.Format("  Data: {0}", e.Data == null ? "null" : String.Join(", ", e.Data)));
            }
        }
    }
}
