﻿

// -------------------------------------------------------------------------------------------

// Copyright (c) 2022 Federico Turco

// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

// -------------------------------------------------------------------------------------------




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Reflection;

//Process
using System.Diagnostics;

//Sockets
using System.Net.Sockets;

//Threading per server ModBus TCP
using System.Threading;

using System.Collections;

//Porta seriale
using System.IO.Ports;

//Comandi apri/chiudi console
using System.Runtime.InteropServices;

//Libreria JSON
//using Newtonsoft.Json;
using System.IO;

using ModBusMaster_Chicco;

// Classe con funzioni di conversione DEC-HEX
using Raccolta_funzioni_parser;

// Ping
using System.Net.NetworkInformation;

// Json LIBs
using System.Web.Script.Serialization;

using LanguageLib; // Libreria custom per caricare etichette in lingue differenti

namespace ModBus_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //-----------------------------------------------------
        //-----------------Variabili globali-------------------
        //-----------------------------------------------------

        public String version = "beta";    // Eventuale etichetta, major.minor lo recupera dall'assembly
        public String title = "ModBus Client";

        String defaultPathToConfiguration = "Generico";
        public String pathToConfiguration;
        public String localPath = "";

        SolidColorBrush colorDefaultReadCell = Brushes.DarkBlue;
        SolidColorBrush colorDefaultWriteCell = Brushes.LightGreen;
        SolidColorBrush colorErrorCell = Brushes.Orange;

        String colorDefaultReadCellStr;
        String colorDefaultWriteCellStr;
        String colorErrorCellStr;

        SolidColorBrush colorDefaultReadCell_Light = Brushes.DarkBlue;
        SolidColorBrush colorDefaultWriteCell_Light = Brushes.LightGreen;
        SolidColorBrush colorErrorCell_Light = Brushes.Orange;

        SolidColorBrush colorDefaultReadCell_Dark = Brushes.DarkBlue;
        SolidColorBrush colorDefaultWriteCell_Dark = Brushes.LightGreen;
        SolidColorBrush colorErrorCell_Dark = Brushes.Orange;

        ObservableCollection<ModBus_Item> list_coilsTable = new ObservableCollection<ModBus_Item>();
        ObservableCollection<ModBus_Item> list_inputsTable = new ObservableCollection<ModBus_Item>();
        ObservableCollection<ModBus_Item> list_inputRegistersTable = new ObservableCollection<ModBus_Item>();
        ObservableCollection<ModBus_Item> list_holdingRegistersTable = new ObservableCollection<ModBus_Item>();

        System.Windows.Forms.ColorDialog colorDialogBox = new System.Windows.Forms.ColorDialog();

        Parser P = new Parser();

        // Elementi per visualizzare/nascondere la finestra della console
        bool statoConsole = false;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        // Disable Console Exit Button
        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        static extern IntPtr DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        const uint SC_CLOSE = 0xF060;
        const uint MF_BYCOMMAND = (uint)0x00000000L;

        //Coda per i comandi seriali da inviare
        //Queue BufferSerialeOut = new Queue();

        public ModBus_Chicco ModBus;
        public ModBus_Def ModBus_Def = new ModBus_Def();

        SerialPort serialPort = new SerialPort();

        SaveFileDialog saveFileDialogBox;
        //OpenFileDialog openFileDialogBox;

        int template_coilsOffset = 0;
        int template_inputsOffset = 0;
        int template_inputRegistersOffset = 0;
        int template_HoldingOffset = 0;

        // Le liste seguenti contengono il registro già convertito in DEC duramte il caricamento del file Template.json
        ObservableCollection<ModBus_Item> list_template_coilsTable = new ObservableCollection<ModBus_Item>();
        ObservableCollection<ModBus_Item> list_template_inputsTable = new ObservableCollection<ModBus_Item>();
        ObservableCollection<ModBus_Item> list_template_holdingRegistersTable = new ObservableCollection<ModBus_Item>();
        ObservableCollection<ModBus_Item> list_template_inputRegistersTable = new ObservableCollection<ModBus_Item>();


        // Stati loop interrogazioni
        public int pauseLoop = 1000;
        public bool loopCoils01 = false;
        public bool loopCoilsRange = false;
        public bool loopInput02 = false;
        public bool loopInputRange = false;
        public bool loopInputRegister04 = false;
        public bool loopInputRegisterRange = false;
        public bool loopHolding03 = false;
        public bool loopHoldingRange = false;
        public bool loopThreadRunning = false;

        Thread threadLoopQuery;

        public string language = "IT";

        public bool logWindowIsOpen = false;
        public bool dequeueExit = false;

        Thread threadDequeue;

        public int readTimeout;

        Language lang;

        public int LogLimitRichTextBox = 2000;

        bool scrolled_log = false;
        int count_log = 0;

        // Variabili di appoggio per gestire le chiamate a thread

        bool useOffsetInTable = false;
        bool correctModbusAddressAuto = false;
        bool colorMode = false;
        bool darkMode = false;

        String textBoxModbusAddress_ = "";
        String comboBoxCoilsRegistri_ = "";
        String textBoxCoilsOffset_ = "";
        String comboBoxCoilsOffset_ = "";
        String comboBoxCoilsAddress01_ = "";
        String textBoxCoilsAddress01_ = "";
        String textBoxCoilNumber_ = "";
        String comboBoxCoilsRange_A_ = "";
        String textBoxCoilsRange_A_ = "";
        //String comboBoxCoilsRange_B_ = "";
        String textBoxCoilsRange_B_ = "";
        String comboBoxCoilsAddress05_ = "";
        String textBoxCoilsAddress05_ = "";
        String textBoxCoilsValue05_ = "";
        String comboBoxCoilsAddress05_b_ = "";
        String textBoxCoilsAddress05_b_ = "";
        String textBoxCoilsValue05_b_ = "";
        String comboBoxCoilsAddress15_A_ = "";
        String textBoxCoilsAddress15_A_ = "";
        String comboBoxCoilsAddress15_B_ = "";
        String textBoxCoilsAddress15_B_ = "";
        String textBoxCoilsValue15_ = "";

        String comboBoxInputRegistri_ = "";
        String comboBoxInputOffset_ = "";
        String textBoxInputOffset_ = "";
        String comboBoxInputAddress02_ = "";
        String textBoxInputAddress02_ = "";
        String textBoxInputNumber_ = "";
        String comboBoxInputRange_A_ = "";
        String textBoxInputRange_A_ = "";
        //String comboBoxInputRange_B_ = "";
        String textBoxInputRange_B_ = "";

        String comboBoxInputRegRegistri_ = "";
        String comboBoxInputRegValori_ = "";
        String comboBoxInputRegOffset_ = "";
        String textBoxInputRegOffset_ = "";
        String comboBoxInputRegisterAddress04_ = "";
        String textBoxInputRegisterAddress04_ = "";
        String textBoxInputRegisterNumber_ = "";
        String comboBoxInputRegisterRange_A_ = "";
        String textBoxInputRegisterRange_A_ = "";
        //String comboBoxInputRegisterRange_B_ = "";
        String textBoxInputRegisterRange_B_ = "";

        String comboBoxHoldingRegistri_ = "";
        String comboBoxHoldingValori_ = "";
        String comboBoxHoldingOffset_ = "";
        String textBoxHoldingOffset_ = "";
        String comboBoxHoldingAddress03_ = "";
        String textBoxHoldingAddress03_ = "";
        String textBoxHoldingRegisterNumber_ = "";
        String comboBoxHoldingRange_A_ = "";
        String textBoxHoldingRange_A_ = "";
        //String comboBoxHoldingRange_B_ = "";
        String textBoxHoldingRange_B_ = "";
        String comboBoxHoldingAddress06_ = "";
        String textBoxHoldingAddress06_ = "";
        String comboBoxHoldingValue06_ = "";
        String textBoxHoldingValue06_ = "";
        String comboBoxHoldingAddress06_b_ = "";
        String textBoxHoldingAddress06_b_ = "";
        String comboBoxHoldingValue06_b_ = "";
        String textBoxHoldingValue06_b_ = "";
        String comboBoxHoldingAddress16_A_ = "";
        String textBoxHoldingAddress16_A_ = "";
        String comboBoxHoldingAddress16_B_ = "";
        String textBoxHoldingAddress16_B_ = "";
        String comboBoxHoldingValue16_ = "";
        String textBoxHoldingValue16_ = "";

        // Dark mode
        public SolidColorBrush ForeGroundDark = new SolidColorBrush(Color.FromArgb(255, (byte)249, (byte)249, (byte)249));
        public SolidColorBrush BackGroundDark = new SolidColorBrush(Color.FromArgb(255, (byte)60, (byte)60, (byte)60));
        public SolidColorBrush BackGroundDark2 = new SolidColorBrush(Color.FromArgb(255, (byte)90, (byte)90, (byte)90));

        // Test dark color v2
        //public SolidColorBrush BackGroundDark = new SolidColorBrush(Color.FromArgb(255, (byte)90, (byte)90, (byte)90));
        //public SolidColorBrush BackGroundDark2 = new SolidColorBrush(Color.FromArgb(255, (byte)60, (byte)60, (byte)60)); 

        public string ForeGroundDarkStr;
        public string BackGroundDarkStr;

        // Light mode
        public SolidColorBrush ForeGroundLight = new SolidColorBrush(Color.FromArgb(255, (byte)10, (byte)10, (byte)10));
        public SolidColorBrush BackGroundLight = new SolidColorBrush(Color.FromArgb(255, (byte)229, (byte)229, (byte)229));
        public SolidColorBrush BackGroundLight2 = new SolidColorBrush(Color.FromArgb(255, (byte)255, (byte)255, (byte)255));

        public string ForeGroundLightStr;
        public string BackGroundLightStr;
        public string BackGroundLight2Str;

        public int MaxJsonLength = 104857600; // 200 MB, sufficienti per 65536*4 etichette Template.json
                                              // see https://learn.microsoft.com/it-it/dotnet/api/system.web.script.serialization.javascriptserializer.maxjsonlength?view=netframework-4.8.1

        public ThreadPriority threadPriority;

        public MainWindow()
        {
            localPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            InitializeComponent();

            version = Assembly.GetEntryAssembly().GetName().Version.Major.ToString() + "." + Assembly.GetEntryAssembly().GetName().Version.Minor.ToString();

            lang = new Language(this);

            threadPriority = ThreadPriority.Highest;

            // Creo evento di chiusura del form
            this.Closing += Form1_FormClosing;

            //this.textBoxHoldingRegisterNumber.KeyUp = new new KeyEventHandler(buttonReadHolding03_Click);

            pathToConfiguration = defaultPathToConfiguration;

            // Aspetti grafici
            comboBoxDiagnosticFunction.Items.Add("00 Return Query Data");
            comboBoxDiagnosticFunction.Items.Add("01 Restart Comunications Option");
            comboBoxDiagnosticFunction.Items.Add("02 Return Diagnostic Register");
            comboBoxDiagnosticFunction.Items.Add("03 Change ASCII Input Delimeter");
            comboBoxDiagnosticFunction.Items.Add("04 Force Listen Only Mode");
            comboBoxDiagnosticFunction.Items.Add("10 Clear Counters and Diagnostic Register");
            comboBoxDiagnosticFunction.Items.Add("11 Return Bus Message Count");
            comboBoxDiagnosticFunction.Items.Add("12 Return Bus Comunication Error Count");
            comboBoxDiagnosticFunction.Items.Add("13 Return Bus Exception Error Count");
            comboBoxDiagnosticFunction.Items.Add("14 Return Slave Message Count");
            comboBoxDiagnosticFunction.Items.Add("15 Return Slave No Response Count");
            comboBoxDiagnosticFunction.Items.Add("16 Return Slave NAK Count");
            comboBoxDiagnosticFunction.Items.Add("17 Return Slave Busy Count");
            comboBoxDiagnosticFunction.Items.Add("20 Clear Overrun Counter and Flag");

            pictureBoxSerial.Background = Brushes.LightGray;
            pictureBoxTcp.Background = Brushes.LightGray;

            dataGridViewCoils.ItemsSource = list_coilsTable;
            dataGridViewInput.ItemsSource = list_inputsTable;
            dataGridViewInputRegister.ItemsSource = list_inputRegistersTable;
            dataGridViewHolding.ItemsSource = list_holdingRegistersTable;

            // Aspetti grafici di default
            comboBoxSerialSpeed.SelectedIndex = 7;
            comboBoxSerialParity.SelectedIndex = 0;
            comboBoxSerialStop.SelectedIndex = 0;

            textBoxTcpClientIpAddress.Text = "192.168.1.100";
            textBoxTcpClientPort.Text = "502";

            comboBoxCoilsRegistri.SelectedIndex = 0;
            comboBoxCoilsOffset.SelectedIndex = 0;
            textBoxCoilsOffset.Text = "0";

            comboBoxInputRegistri.SelectedIndex = 0;
            comboBoxInputOffset.SelectedIndex = 0;
            textBoxInputOffset.Text = "0";

            comboBoxInputRegOffset.SelectedIndex = 0;
            comboBoxInputRegValori.SelectedIndex = 0;
            comboBoxInputRegRegistri.SelectedIndex = 0;
            textBoxInputRegOffset.Text = "0";

            comboBoxHoldingRegistri.SelectedIndex = 0;
            comboBoxHoldingValori.SelectedIndex = 0;
            comboBoxHoldingOffset.SelectedIndex = 0;
            textBoxHoldingOffset.Text = "0";

            comboBoxCoilsAddress01.SelectedIndex = 0;
            comboBoxCoilsRange_A.SelectedIndex = 0;
            //comboBoxCoilsRange_B.SelectedIndex = 0;
            comboBoxCoilsAddress05.SelectedIndex = 0;
            comboBoxCoilsAddress05_b.SelectedIndex = 0;
            //comboBoxCoilsValue05.SelectedIndex = 0;
            //comboBoxCoilsValue05_b.SelectedIndex = 0;
            comboBoxCoilsAddress15_A.SelectedIndex = 0;
            comboBoxCoilsAddress15_B.SelectedIndex = 0;
            //comboBoxCoilsValue15.SelectedIndex = 0;

            comboBoxInputAddress02.SelectedIndex = 0;
            comboBoxInputRange_A.SelectedIndex = 0;
            //comboBoxInputRange_B.SelectedIndex = 0;

            comboBoxInputRegisterAddress04.SelectedIndex = 0;
            comboBoxInputRegisterRange_A.SelectedIndex = 0;
            //comboBoxInputRegisterRange_B.SelectedIndex = 0;

            comboBoxHoldingAddress03.SelectedIndex = 0;
            comboBoxHoldingRange_A.SelectedIndex = 0;
            //comboBoxHoldingRange_B.SelectedIndex = 0;
            comboBoxHoldingAddress06.SelectedIndex = 0;
            comboBoxHoldingValue06.SelectedIndex = 0;
            comboBoxHoldingAddress06_b.SelectedIndex = 0;
            comboBoxHoldingValue06_b.SelectedIndex = 0;
            comboBoxHoldingAddress16_A.SelectedIndex = 0;
            comboBoxHoldingAddress16_B.SelectedIndex = 0;
            comboBoxHoldingValue16.SelectedIndex = 0;


            pictureBoxRunningAs.Background = Brushes.LightGray;
            pictureBoxIsSending.Background = Brushes.LightGray;
            pictureBoxIsResponding.Background = Brushes.LightGray;

            richTextBoxStatus.AppendText("\n");

            radioButtonModeSerial.IsChecked = true;

            checkBoxAddLinesToEnd.Visibility = Visibility.Hidden;

            // Disabilita il pulsante di chiusura della console
            disableConsoleExitButton();

            // Centro la finestra
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;

            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);

            ForeGroundDarkStr = ForeGroundDark.ToString();
            BackGroundDarkStr = BackGroundDark.ToString();

            ForeGroundLightStr = ForeGroundLight.ToString();
            BackGroundLightStr = BackGroundLight.ToString();
            BackGroundLight2Str = BackGroundLight2.ToString();
        }

        private void Form1_FormClosing(object sender, EventArgs e)
        {
            dequeueExit = true;

            //SaveConfiguration_v1(false);
            SaveConfiguration_v2(false);

            try
            {
                if (ModBus != null)
                {
                    ModBus.close(); // Se non attivo niente ModBUs risulta null
                }
            }
            catch
            {

            }

            try
            {
                if (threadLoopQuery != null)
                {
                    if (threadLoopQuery.IsAlive)
                    {
                        threadLoopQuery.Abort();
                    }
                }
            }
            catch
            {

            }
        }

        private void Form1_Load(object sender, RoutedEventArgs e)
        {
            threadDequeue = new Thread(new ThreadStart(LogDequeue));
            threadDequeue.IsBackground = true;
            threadDequeue.Start();

            richTextBoxPackets.Document.Blocks.Clear();
            richTextBoxPackets.AppendText("\n");
            richTextBoxPackets.Document.PageWidth = 5000;

            // Console.WriteLine(this.Title + "\n");

            this.Title = title + " " + version;

            try
            {
                // Aggiornamento lista porte seriale
                string[] SerialPortList = System.IO.Ports.SerialPort.GetPortNames();
                // comboBoxSerialPort.Items.Add("Seleziona porta seriale ...");

                foreach (String port in SerialPortList)
                {
                    comboBoxSerialPort.Items.Add(port);
                }

                comboBoxSerialPort.SelectedIndex = 0;
            }
            catch
            {
                Console.WriteLine("Nessuna porta seriale trovata");
            }

            // Menu lingua
            languageToolStripMenu.Items.Clear();

            foreach (string lang in Directory.GetFiles(localPath + "//Lang"))
            {
                var tmp = new MenuItem();

                tmp.Header = System.IO.Path.GetFileNameWithoutExtension(lang);
                tmp.IsCheckable = true;
                tmp.Click += MenuItemLanguage_Click;

                languageToolStripMenu.Items.Add(tmp);
            }

            // Se esiste una nuova versione del file di configurazione uso l'ultima, altrimenti carico il modello precedente
            if (File.Exists(localPath + "\\Json\\" + pathToConfiguration + "\\Config.json"))
            {
                LoadConfiguration_v2();
            }
            else
            {
                LoadConfiguration_v1();
            }

            //lang.loadLanguageTemplate(language);

            Thread updateTables = new Thread(new ThreadStart(genera_tabelle_registri));
            updateTables.IsBackground = true;
            updateTables.Start();

            // ToolTips
            // toolTipHelp.SetToolTip(this.comboBoxHoldingAddress03, "Formato indirizzo\nDEC: decimale\nHEX: esadecimale (inserire il valore senza 0x)");

            if ((bool)radioButtonModeSerial.IsChecked)
            {
                buttonSerialActive.Focus();
            }
            else
            {
                buttonTcpActive.Focus();
            }

            changeEnableButtonsConnect(false);

            // Command line parameters
            string[] argv = Environment.GetCommandLineArgs();
            for(int i = 0; i < argv.Length; i++)
            {
                // -h
                // --help
                if (argv[i].IndexOf("-h") != -1 || argv[i].IndexOf("--help") != -1)
                {
                    Console.WriteLine("Command line parameters:");
                    Console.WriteLine("");
                    Console.WriteLine("Load profile:");
                    Console.WriteLine("--profile \"Profile name\"");
                    Console.WriteLine(":");
                    Console.WriteLine("TCP connection:");
                    Console.WriteLine("--tcp 192.168.1.40 502");
                    Console.WriteLine("");
                    Console.WriteLine("RTU connection:");
                    Console.WriteLine("--rtu COM3 19200 8N1");
                    Console.WriteLine("");
                    Console.WriteLine("--profile \"Custom\" --tcp 192.168.1.40 502");
                    Console.WriteLine("--profile \"Custom\" --rtu COM3 9600 8E2");
                    Console.WriteLine("");
                    apriConsole();
                }

                // --profile "test 2"
                if (argv[i].IndexOf("--profile") != -1)
                {
                    radioButtonModeTcp.IsChecked = true;

                    if ((i + 1) < argv.Length)
                    {
                        LoadProfile(argv[i + 1].Replace("\"", ""));
                    }
                    else
                    {
                        Console.WriteLine("Not enough parameter for --profile");
                    }
                }

                // --tcp 127.0.0.1 502
                if (argv[i].IndexOf("--tcp") != -1)
                {
                    radioButtonModeTcp.IsChecked = true;

                    if ((i + 2) < argv.Length)
                    {
                        textBoxTcpClientIpAddress.Text = argv[i + 1];
                        textBoxTcpClientPort.Text = argv[i + 2];

                        buttonTcpActive_Click(null, null);
                    }
                    else
                    {
                        Console.WriteLine("Not enough parameter for --tcp");
                    }
                }

                // --rtu COM3 19200 8N1
                if (argv[i].IndexOf("--rtu") != -1)
                {
                    radioButtonModeSerial.IsChecked = true;

                    if ((i + 1) < argv.Length)
                    {
                        for(int ii = 0; ii < comboBoxSerialPort.Items.Count; ii++)
                        {
                            if (comboBoxSerialPort.Items[ii].ToString().IndexOf(argv[i + 1]) != -1)
                                comboBoxSerialPort.SelectedIndex = ii;
                        }

                        for (int ii = 0; ii < comboBoxSerialSpeed.Items.Count; ii++)
                        {
                            if (comboBoxSerialSpeed.Items[ii].ToString().IndexOf(argv[i + 2]) != -1)
                                comboBoxSerialSpeed.SelectedIndex = ii;
                        }

                        for (int ii = 0; ii < comboBoxSerialStop.Items.Count; ii++)
                        {
                            if (comboBoxSerialStop.Items[ii].ToString().IndexOf(argv[i + 3].Substring(2)) != -1)
                                comboBoxSerialStop.SelectedIndex = ii;
                        }

                        if (argv[i + 3].Substring(1, 1) == "N")
                            comboBoxSerialParity.SelectedIndex = 0;
                        if (argv[i + 3].Substring(1, 1) == "E")
                            comboBoxSerialParity.SelectedIndex = 1;
                        if (argv[i + 3].Substring(1, 1) == "O")
                            comboBoxSerialParity.SelectedIndex = 2;

                        buttonTcpActive_Click(null, null);
                    }
                    else
                    {
                        Console.WriteLine("Not enough parameter for --rtu");
                    }
                }

                if (argv[i].IndexOf("--tab") != -1)
                {
                    if((i + 1) < argv.Length)
                    { 
                        tabControlMain.SelectedIndex = int.Parse(argv[i + 1]);
                    }
                    else
                    {
                        Console.WriteLine("Not enough parameter for --tab");
                    }
                }
            }

            // Aggiorno grafica colori
            CheckBoxDarkMode_Checked(null, null);
        }

        private void radioButtonModeSerial_CheckedChanged(object sender, RoutedEventArgs e)
        {

            // Serial ON

            // radioButtonModeASCII.IsEnabled = radioButtonModeSerial.IsChecked;
            // radioButtonModeRTU.IsEnabled = radioButtonModeSerial.IsChecked;

            comboBoxSerialParity.IsEnabled = (bool)radioButtonModeSerial.IsChecked;
            comboBoxSerialPort.IsEnabled = (bool)radioButtonModeSerial.IsChecked;
            comboBoxSerialSpeed.IsEnabled = (bool)radioButtonModeSerial.IsChecked;
            comboBoxSerialStop.IsEnabled = (bool)radioButtonModeSerial.IsChecked;

            buttonUpdateSerialList.IsEnabled = (bool)radioButtonModeSerial.IsChecked;
            buttonSerialActive.IsEnabled = (bool)radioButtonModeSerial.IsChecked;


            // Tcp OFF
            // radioButtonTcpSlave.IsEnabled = !radioButtonModeSerial.IsChecked;

            //richTextBoxStatus.IsEnabled = !(bool)radioButtonModeSerial.IsChecked;
            buttonTcpActive.IsEnabled = !(bool)radioButtonModeSerial.IsChecked;

            if ((bool)radioButtonModeSerial.IsChecked)
            {
                textBoxTcpClientIpAddress.IsEnabled = false;
                textBoxTcpClientPort.IsEnabled = false;
            }
            else
            {

                textBoxTcpClientIpAddress.IsEnabled = true;
                textBoxTcpClientPort.IsEnabled = true;

            }
        }

        private void buttonSerialActive_Click(object sender, RoutedEventArgs e)
        {
            if (pictureBoxSerial.Background == Brushes.LightGray)
            {
                // Attivazione comunicazione seriale
                pictureBoxSerial.Background = Brushes.Lime;
                pictureBoxRunningAs.Background = Brushes.Lime;


                textBlockSerialActive.Text = lang.languageTemplate["strings"]["disconnect"];
                // holdingSuiteToolStripMenuItem.IsEnabled = true;

                menuItemToolBit.IsEnabled = true;
                menuItemToolWord.IsEnabled = true;
                menuItemToolByte.IsEnabled = true;
                salvaConfigurazioneNelDatabaseToolStripMenuItem.IsEnabled = false;
                caricaConfigurazioneDalDatabaseToolStripMenuItem.IsEnabled = false;
                gestisciDatabaseToolStripMenuItem.IsEnabled = false;
                 

                try
                {
                    // ---------------------------------------------------------------------------------
                    // ----------------------Apertura comunicazione seriale-----------------------------
                    // ---------------------------------------------------------------------------------

                    // Create a new SerialPort object with default settings.
                    serialPort = new SerialPort();
                    serialPort.PortName = comboBoxSerialPort.SelectedItem.ToString();

                    // debug
                    //Console.WriteLine(comboBoxSerialSpeed.SelectedValue.ToString());
                    //Console.WriteLine(comboBoxSerialSpeed.SelectedItem.ToString());

                    serialPort.BaudRate = int.Parse(comboBoxSerialSpeed.SelectedValue.ToString().Split(' ')[1]);

                    // DEBUG
                    //Console.WriteLine("comboBoxSerialParity.SelectedIndex:" + comboBoxSerialParity.SelectedIndex.ToString());

                    switch (comboBoxSerialParity.SelectedIndex)
                    {
                        case 0:
                            serialPort.Parity = Parity.None;
                            break;
                        case 1:
                            serialPort.Parity = Parity.Even;
                            break;
                        case 2:
                            serialPort.Parity = Parity.Odd;
                            break;
                        default:
                            serialPort.Parity = Parity.None;
                            break;
                    }

                    serialPort.DataBits = 8;

                    // DEBUG
                    Console.WriteLine("comboBoxSerialStop.SelectedIndex:" + comboBoxSerialStop.SelectedIndex.ToString());

                    switch (comboBoxSerialStop.SelectedIndex)
                    {
                        case 0:
                            serialPort.StopBits = StopBits.One;
                            break;
                        case 1:
                            serialPort.StopBits = StopBits.OnePointFive;
                            break;
                        case 2:
                            serialPort.StopBits = StopBits.Two;
                            break;
                        default:
                            serialPort.StopBits = StopBits.One;
                            break;
                    }

                    serialPort.Handshake = Handshake.None;

                    // Timeout porta
                    serialPort.ReadTimeout = 50;
                    serialPort.WriteTimeout = 50;

                    ModBus = new ModBus_Chicco(serialPort, textBoxTcpClientIpAddress.Text, textBoxTcpClientPort.Text, ModBus_Def.TYPE_RTU, pictureBoxIsResponding, pictureBoxIsSending);
                    ModBus.open();

                    serialPort.Open();
                    richTextBoxAppend(richTextBoxStatus, lang.languageTemplate["strings"]["connectedTo"] + " " + comboBoxSerialPort.SelectedItem.ToString());

                    radioButtonModeSerial.IsEnabled = false;
                    radioButtonModeTcp.IsEnabled = false;


                    comboBoxSerialPort.IsEnabled = false;
                    comboBoxSerialSpeed.IsEnabled = false;
                    comboBoxSerialParity.IsEnabled = false;
                    comboBoxSerialStop.IsEnabled = false;
                    languageToolStripMenu.IsEnabled = false;
                }
                catch(Exception err)
                {
                    pictureBoxSerial.Background = Brushes.LightGray;
                    pictureBoxRunningAs.Background = Brushes.LightGray;


                    textBlockSerialActive.Text = lang.languageTemplate["strings"]["connect"];
                    //holdingSuiteToolStripMenuItem.IsEnabled = false;

                    menuItemToolBit.IsEnabled = false;
                    menuItemToolWord.IsEnabled = false;
                    menuItemToolByte.IsEnabled = false;
                    salvaConfigurazioneNelDatabaseToolStripMenuItem.IsEnabled = true;
                    caricaConfigurazioneDalDatabaseToolStripMenuItem.IsEnabled = true;
                    gestisciDatabaseToolStripMenuItem.IsEnabled = true;

                    comboBoxSerialPort.IsEnabled = true;
                    comboBoxSerialSpeed.IsEnabled = true;
                    comboBoxSerialParity.IsEnabled = true;
                    comboBoxSerialStop.IsEnabled = true;
                    languageToolStripMenu.IsEnabled = true;

                    Console.WriteLine("Errore apertura porta seriale");
                    Console.WriteLine(err);

                    richTextBoxAppend(richTextBoxStatus, lang.languageTemplate["strings"]["failedToConnect"]);
                }
            }
            else
            {
                // Disattivazione comunicazione seriale
                pictureBoxSerial.Background = Brushes.LightGray;
                pictureBoxRunningAs.Background = Brushes.LightGray;


                textBlockSerialActive.Text = lang.languageTemplate["strings"]["connect"];

                radioButtonModeSerial.IsEnabled = true;
                radioButtonModeTcp.IsEnabled = true;

                menuItemToolBit.IsEnabled = false;
                menuItemToolWord.IsEnabled = false;
                menuItemToolByte.IsEnabled = false;
                salvaConfigurazioneNelDatabaseToolStripMenuItem.IsEnabled = true;
                caricaConfigurazioneDalDatabaseToolStripMenuItem.IsEnabled = true;
                gestisciDatabaseToolStripMenuItem.IsEnabled = true;

                comboBoxSerialPort.IsEnabled = true;
                comboBoxSerialSpeed.IsEnabled = true;
                comboBoxSerialParity.IsEnabled = true;
                comboBoxSerialStop.IsEnabled = true;
                languageToolStripMenu.IsEnabled = true;

                // Fermo eventuali loop
                disableAllLoops();

                // ---------------------------------------------------------------------------------
                // ----------------------Chiusura comunicazione seriale-----------------------------
                // ---------------------------------------------------------------------------------
                serialPort.Close();
                richTextBoxAppend(richTextBoxStatus, "Port closed");
            }

            changeEnableButtonsConnect(pictureBoxRunningAs.Background == Brushes.Lime);
        }

        private void buttonUpdateSerialList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string[] SerialPortList = System.IO.Ports.SerialPort.GetPortNames();

                //comboBoxSerialPort.Items.Add("Seleziona porta seriale ...");
                comboBoxSerialPort.Items.Clear();

                foreach(String port in SerialPortList)
                {
                    comboBoxSerialPort.Items.Add(port);
                }
                
                comboBoxSerialPort.SelectedIndex = 0;
            }
            catch
            {
                Console.WriteLine("Nessuna porta seriale disponibile");
            }
        }

        // Visualizza console programma da menu tendina
        private void apriConsoleToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            apriConsole();
        }

        // Nasconde console programma da menu tendina
        private void chiudiConsoleToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            chiudiConsole();
        }

        public void chiudiConsole()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            statoConsole = false;
        }

        public void apriConsole()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);

            statoConsole = true;
        }

        // Disabilita il pulsante di chiusura della console
        public void disableConsoleExitButton()
        {
            IntPtr handle = GetConsoleWindow();
            IntPtr exitButton = GetSystemMenu(handle, false);
            if (exitButton != null) DeleteMenu(exitButton, SC_CLOSE, MF_BYCOMMAND);
        }

        // ----------------------------------------------------------------------------------
        // ---------------------------SALVATAGGIO CONFIGURAZIONE-----------------------------
        // ----------------------------------------------------------------------------------

        /*public void SaveConfiguration_v1(bool alert)   //Se alert true visualizza un messaggio di info salvataggio avvenuto
        {
            //DEBUG
            //MessageBox.Show("Salvataggio configurazione");

            try
            {
                // Caricamento variabili
                var config = new SAVE();

                config.modbusAddress = textBoxModbusAddress.Text;
                config.usingSerial = (bool)radioButtonModeSerial.IsChecked;

                //config.serialMaster = radioButtonSerialMaster.IsChecked;
                //config.serialRTU = radioButtonModeRTU.IsChecked;

                // Serial port
                config.serialPort = comboBoxSerialPort.SelectedIndex;
                config.serialSpeed = comboBoxSerialSpeed.SelectedIndex;
                config.serialParity = comboBoxSerialParity.SelectedIndex;
                config.serialStop = comboBoxSerialStop.SelectedIndex;

                // TCP
                config.tcpClientIpAddress = textBoxTcpClientIpAddress.Text;
                config.tcpClientPort = textBoxTcpClientPort.Text;
                //config.tcpServerIpAddress = textBoxTcpServerIpAddress.Text;
                //config.tcpServerPort = textBoxTcpServerPort.Text;

                // GRAFICA
                // TabPage1 (Coils)
                config.textBoxCoilsAddress01 = textBoxCoilsAddress01.Text;
                config.textBoxCoilNumber = textBoxCoilNumber.Text;
                config.textBoxCoilsRange_A = textBoxCoilsRange_A.Text;
                config.textBoxCoilsRange_B = textBoxCoilsRange_B.Text;
                config.textBoxCoilsAddress05 = textBoxCoilsAddress05.Text;
                config.textBoxCoilsValue05 = textBoxCoilsValue05.Text;
                config.textBoxCoilsAddress15_A = textBoxCoilsAddress15_A.Text;
                config.textBoxCoilsAddress15_B = textBoxCoilsAddress15_B.Text;
                config.textBoxCoilsValue15 = textBoxCoilsValue15.Text;
                config.textBoxGoToCoilAddress = textBoxGoToCoilAddress.Text;

                // TabPage2 (inputs)
                config.textBoxInputAddress02 = textBoxInputAddress02.Text;
                config.textBoxInputNumber = textBoxInputNumber.Text;
                config.textBoxInputRange_A = textBoxInputRange_A.Text;
                config.textBoxInputRange_B = textBoxInputRange_B.Text;
                config.textBoxGoToInputAddress = textBoxGoToInputAddress.Text;

                // TabPage3 (input registers)
                config.textBoxInputRegisterAddress04 = textBoxInputRegisterAddress04.Text;
                config.textBoxInputRegisterNumber = textBoxInputRegisterNumber.Text;
                config.textBoxInputRegisterRange_A = textBoxInputRegisterRange_A.Text;
                config.textBoxInputRegisterRange_B = textBoxInputRegisterRange_B.Text;
                config.textBoxGoToInputRegisterAddress = textBoxGoToInputRegisterAddress.Text;

                // TabPage4 (holding registers)
                config.textBoxHoldingAddress03 = textBoxHoldingAddress03.Text;
                config.textBoxHoldingRegisterNumber = textBoxHoldingRegisterNumber.Text;
                config.textBoxHoldingRange_A = textBoxHoldingRange_A.Text;
                config.textBoxHoldingRange_B = textBoxHoldingRange_B.Text;
                config.textBoxHoldingAddress06 = textBoxHoldingAddress06.Text;
                config.textBoxHoldingValue06 = textBoxHoldingValue06.Text;
                config.textBoxHoldingAddress16_A = textBoxHoldingAddress16_A.Text;
                config.textBoxHoldingAddress16_B = textBoxHoldingAddress16_B.Text;
                config.textBoxHoldingValue16 = textBoxHoldingValue16.Text;
                config.textBoxGoToHoldingAddress = textBoxGoToHoldingAddress.Text;

                config.statoConsole = statoConsole;

                // Funzioni aggiunte in seguito
                config.comboBoxCoilsAddress01_ = comboBoxCoilsAddress01.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxCoilsRange_A_ = comboBoxCoilsRange_A.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxCoilsRange_B_ = comboBoxCoilsRange_B.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxCoilsAddress05_ = comboBoxCoilsAddress05.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxCoilsValue05_ = "DEC"; // comboBoxCoilsValue05.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxCoilsAddress15_A_ = comboBoxCoilsAddress15_A.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxCoilsAddress15_B_ = comboBoxCoilsAddress15_B.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxCoilsValue15_ = "DEC"; // comboBoxCoilsValue15.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputAddress02_ = comboBoxInputAddress02.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputRange_A_ = comboBoxInputRange_A.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputRange_B_ = comboBoxInputRange_B.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputRegisterAddress04_ = comboBoxInputRegisterAddress04.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputRegisterRange_A_ = comboBoxInputRegisterRange_A.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputRegisterRange_B_ = comboBoxInputRegisterRange_B.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingAddress03_ = comboBoxHoldingAddress03.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingRange_A_ = comboBoxHoldingRange_A.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingRange_B_ = comboBoxHoldingRange_B.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingAddress06_ = comboBoxHoldingAddress06.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingValue06_ = comboBoxHoldingValue06.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingAddress16_A_ = comboBoxHoldingAddress16_A.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingAddress16_B_ = comboBoxHoldingAddress16_B.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingValue16_ = comboBoxHoldingValue16.SelectedValue.ToString().Split(' ')[1];

                //comboBox visualizzazione tabelle
                config.comboBoxCoilsRegistri_ = comboBoxCoilsRegistri.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputRegistri_ = comboBoxInputRegistri.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputRegRegistri_ = comboBoxInputRegRegistri.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputRegValori_ = comboBoxInputRegValori.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingRegistri_ = comboBoxHoldingRegistri.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingValori_ = comboBoxHoldingValori.SelectedValue.ToString().Split(' ')[1];

                config.comboBoxCoilsOffset_ = comboBoxCoilsOffset.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputOffset_ = comboBoxInputOffset.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxInputRegOffset_ = comboBoxInputRegOffset.SelectedValue.ToString().Split(' ')[1];
                config.comboBoxHoldingOffset_ = comboBoxHoldingOffset.SelectedValue.ToString().Split(' ')[1];

                // Funzioni doppie per force coil e preset holding aggiunte dopo
                config.comboBoxCoilsAddress05_b_ = comboBoxCoilsAddress05_b.SelectedValue.ToString().Split(' ')[1];
                config.textBoxCoilsAddress05_b_ = textBoxCoilsAddress05_b.Text;
                config.comboBoxCoilsValue05_b_ = "DEC"; //comboBoxCoilsValue05_b.SelectedValue.ToString().Split(' ')[1];
                config.textBoxCoilsValue05_b_ = textBoxCoilsValue05_b.Text;

                config.comboBoxHoldingAddress06_b_ = comboBoxHoldingAddress06_b.SelectedValue.ToString().Split(' ')[1];
                config.textBoxHoldingAddress06_b_ = textBoxHoldingAddress06_b.Text;
                config.comboBoxHoldingValue06_b_ = comboBoxHoldingValue06_b.SelectedValue.ToString().Split(' ')[1];
                config.textBoxHoldingValue06_b_ = textBoxHoldingValue06_b.Text;

                config.textBoxCoilsOffset_ = textBoxCoilsOffset.Text;
                config.textBoxInputOffset_ = textBoxInputOffset.Text;
                config.textBoxInputRegOffset_ = textBoxInputRegOffset.Text;
                config.textBoxHoldingOffset_ = textBoxHoldingOffset.Text;

                config.checkBoxUseOffsetInTables_ = (bool)checkBoxUseOffsetInTables.IsChecked;
                config.checkBoxUseOffsetInTextBox_ = (bool)checkBoxUseOffsetInTextBox.IsChecked;
                config.checkBoxFollowModbusProtocol_ = (bool)checkBoxFollowModbusProtocol.IsChecked;
                //config.checkBoxSavePackets_ = (bool)checkBoxSavePackets.IsChecked;
                config.checkBoxCloseConsolAfterBoot_ = (bool)checkBoxCloseConsolAfterBoot.IsChecked;
                config.checkBoxCellColorMode_ = (bool)checkBoxCellColorMode.IsChecked;
                config.checkBoxViewTableWithoutOffset_ = (bool)checkBoxViewTableWithoutOffset.IsChecked;

                //config.textBoxSaveLogPath_ = textBoxSaveLogPath.Text;


                config.comboBoxDiagnosticFunction_ = comboBoxDiagnosticFunction.SelectedValue.ToString().Split(' ')[1];

                config.textBoxDiagnosticFunctionManual_ = textBoxDiagnosticFunctionManual.Text;

                config.colorDefaultReadCell_ = colorDefaultReadCell.ToString();
                config.colorDefaultWriteCell_ = colorDefaultWriteCell.ToString();
                config.colorErrorCell_ = colorErrorCell.ToString();

                config.TextBoxPollingInterval_ = TextBoxPollingInterval.Text;
                config.TextBoxPollingInterval_ = TextBoxPollingInterval.Text;

                config.CheckBoxSendValuesOnEditCoillsTable_ = (bool)CheckBoxSendValuesOnEditCoillsTable.IsChecked;
                config.CheckBoxSendValuesOnEditHoldingTable_ = (bool)CheckBoxSendValuesOnEditHoldingTable.IsChecked;

                config.language = language;

                config.textBoxReadTimeout = textBoxReadTimeout.Text;

                var jss = new JavaScriptSerializer();
                jss.MaxJsonLength = this.MaxJsonLength;
                jss.RecursionLimit = 1000;
                string file_content = jss.Serialize(config);

                File.WriteAllText("Json/" + pathToConfiguration + "/CONFIGURAZIONE.json", file_content);

                if (alert)
                {
                    MessageBox.Show(lang.languageTemplate["strings"]["infoSaveConfig"], "Info");
                }

                Console.WriteLine("Salvata configurazione");
            }
            catch(Exception err)
            {
                Console.WriteLine("Errore salvataggio configurazione");
                Console.WriteLine(err);
            }
        }*/

        // ----------------------------------------------------------------------------------
        // ---------------------------CARICAMENTO CONFIGURAZIONE-----------------------------
        // ----------------------------------------------------------------------------------

        public void LoadConfiguration_v1()
        {
            try
            {
                string file_content = File.ReadAllText("Json/" + pathToConfiguration + "/CONFIGURAZIONE.json");

                JavaScriptSerializer jss = new JavaScriptSerializer();
                SAVE config = jss.Deserialize<SAVE>(file_content);

                textBoxModbusAddress.Text = config.modbusAddress;

                // Scheda configurazione seriale
                radioButtonModeSerial.IsChecked = config.usingSerial;
                radioButtonModeTcp.IsChecked = !config.usingSerial;

                //radioButtonSerialMaster.IsChecked = config.serialMaster;
                //radioButtonSerialSlave.IsChecked = !config.serialMaster;
                //radioButtonModeRTU.IsChecked = config.serialRTU;
                //radioButtonModeASCII.IsChecked = !config.serialRTU;

                comboBoxSerialSpeed.SelectedIndex = config.serialSpeed;
                comboBoxSerialParity.SelectedIndex = config.serialParity;
                comboBoxSerialStop.SelectedIndex = config.serialStop;

                // Scheda configurazione TCP
                //radioButtonTcpMaster.IsChecked = config.serialMaster;
                //radioButtonTcpSlave.IsChecked = !config.serialMaster;

                textBoxTcpClientIpAddress.Text = config.tcpClientIpAddress;
                textBoxTcpClientPort.Text = config.tcpClientPort;
                //textBoxTcpServerIpAddress.Text = config.tcpServerIpAddress;
                //textBoxTcpServerPort.Text = config.tcpServerPort;

                comboBoxSerialPort.SelectedIndex = config.serialPort;

                // GRAFICA
                // TabPage1 (Coils)
                textBoxCoilsAddress01.Text = config.textBoxCoilsAddress01;
                textBoxCoilNumber.Text = config.textBoxCoilNumber;
                textBoxCoilsRange_A.Text = config.textBoxCoilsRange_A;
                textBoxCoilsRange_B.Text = config.textBoxCoilsRange_B;
                textBoxCoilsAddress05.Text = config.textBoxCoilsAddress05;
                textBoxCoilsValue05.Text = config.textBoxCoilsValue05;
                textBoxCoilsAddress15_A.Text = config.textBoxCoilsAddress15_A;
                textBoxCoilsAddress15_B.Text = config.textBoxCoilsAddress15_B;
                textBoxCoilsValue15.Text = config.textBoxCoilsValue15;
                textBoxGoToCoilAddress.Text = config.textBoxGoToCoilAddress;


                // TabPage2 (inputs)
                textBoxInputAddress02.Text = config.textBoxInputAddress02;
                textBoxInputNumber.Text = config.textBoxInputNumber;
                textBoxInputRange_A.Text = config.textBoxInputRange_A;
                textBoxInputRange_B.Text = config.textBoxInputRange_B;
                textBoxGoToInputAddress.Text = config.textBoxGoToInputAddress;

                // TabPage3 (input registers)
                textBoxInputRegisterAddress04.Text = config.textBoxInputRegisterAddress04;
                textBoxInputRegisterNumber.Text = config.textBoxInputRegisterNumber;
                textBoxInputRegisterRange_A.Text = config.textBoxInputRegisterRange_A;
                textBoxInputRegisterRange_B.Text = config.textBoxInputRegisterRange_B;
                textBoxGoToInputRegisterAddress.Text = config.textBoxGoToInputRegisterAddress;

                // TabPage4 (holding registers)
                textBoxHoldingAddress03.Text = config.textBoxHoldingAddress03;
                textBoxHoldingRegisterNumber.Text = config.textBoxHoldingRegisterNumber;
                textBoxHoldingRange_A.Text = config.textBoxHoldingRange_A;
                textBoxHoldingRange_B.Text = config.textBoxHoldingRange_B;
                textBoxHoldingAddress06.Text = config.textBoxHoldingAddress06;
                textBoxHoldingValue06.Text = config.textBoxHoldingValue06;
                textBoxHoldingAddress16_A.Text = config.textBoxHoldingAddress16_A;
                textBoxHoldingAddress16_B.Text = config.textBoxHoldingAddress16_B;
                textBoxHoldingValue16.Text = config.textBoxHoldingValue16;
                textBoxGoToHoldingAddress.Text = config.textBoxGoToHoldingAddress;

                statoConsole = config.statoConsole;

                // Funzioni aggiunte in seguito
                comboBoxCoilsAddress01.SelectedIndex = config.comboBoxCoilsAddress01_ == "HEX" ? 1 : 0;
                comboBoxCoilsRange_A.SelectedIndex = config.comboBoxCoilsRange_A_ == "HEX" ? 1 : 0;
                //comboBoxCoilsRange_B.SelectedIndex = config.comboBoxCoilsRange_B_ == "HEX" ? 1 : 0;
                comboBoxCoilsAddress05.SelectedIndex = config.comboBoxCoilsAddress05_ == "HEX" ? 1 : 0;
                //comboBoxCoilsValue05.SelectedIndex = config.comboBoxCoilsValue05_ == "HEX" ? 1 : 0;
                comboBoxCoilsAddress15_A.SelectedIndex = config.comboBoxCoilsAddress15_A_ == "HEX" ? 1 : 0;
                comboBoxCoilsAddress15_B.SelectedIndex = config.comboBoxCoilsAddress15_B_ == "HEX" ? 1 : 0;
                //comboBoxCoilsValue15.SelectedIndex = config.comboBoxCoilsValue15_ == "HEX" ? 1 : 0;
                comboBoxInputAddress02.SelectedIndex = config.comboBoxInputAddress02_ == "HEX" ? 1 : 0;
                comboBoxInputRange_A.SelectedIndex = config.comboBoxInputRange_A_ == "HEX" ? 1 : 0;
                //comboBoxInputRange_B.SelectedIndex = config.comboBoxInputRange_B_ == "HEX" ? 1 : 0;
                comboBoxInputRegisterAddress04.SelectedIndex = config.comboBoxInputRegisterAddress04_ == "HEX" ? 1 : 0;
                comboBoxInputRegisterRange_A.SelectedIndex = config.comboBoxInputRegisterRange_A_ == "HEX" ? 1 : 0;
                //comboBoxInputRegisterRange_B.SelectedIndex = config.comboBoxInputRegisterRange_B_ == "HEX" ? 1 : 0;
                comboBoxHoldingAddress03.SelectedIndex = config.comboBoxHoldingAddress03_ == "HEX" ? 1 : 0;
                comboBoxHoldingRange_A.SelectedIndex = config.comboBoxHoldingRange_A_ == "HEX" ? 1 : 0;
                //comboBoxHoldingRange_B.SelectedIndex = config.comboBoxHoldingRange_B_ == "HEX" ? 1 : 0;
                comboBoxHoldingAddress06.SelectedIndex = config.comboBoxHoldingAddress06_ == "HEX" ? 1 : 0;
                comboBoxHoldingValue06.SelectedIndex = config.comboBoxHoldingValue06_ == "HEX" ? 1 : 0;
                comboBoxHoldingAddress16_A.SelectedIndex = config.comboBoxHoldingAddress16_A_ == "HEX" ? 1 : 0;
                comboBoxHoldingAddress16_B.SelectedIndex = config.comboBoxHoldingAddress16_B_ == "HEX" ? 1 : 0;
                comboBoxHoldingValue16.SelectedIndex = config.comboBoxHoldingValue16_ == "HEX" ? 1 : 0;

                comboBoxCoilsOffset.SelectedIndex = config.comboBoxCoilsOffset_ == "HEX" ? 1 : 0;
                comboBoxInputOffset.SelectedIndex = config.comboBoxInputOffset_ == "HEX" ? 1 : 0;
                comboBoxInputRegOffset.SelectedIndex = config.comboBoxInputRegOffset_ == "HEX" ? 1 : 0;
                comboBoxHoldingOffset.SelectedIndex = config.comboBoxHoldingOffset_ == "HEX" ? 1 : 0;

                //comboBox visualizzazione tabelle
                comboBoxCoilsRegistri.SelectedIndex = config.comboBoxCoilsRegistri_ == "HEX" ? 1 : 0;
                comboBoxInputRegistri.SelectedIndex = config.comboBoxInputRegistri_ == "HEX" ? 1 : 0;
                comboBoxInputRegRegistri.SelectedIndex = config.comboBoxInputRegRegistri_ == "HEX" ? 1 : 0;
                comboBoxInputRegValori.SelectedIndex = config.comboBoxInputRegValori_ == "HEX" ? 1 : 0;
                comboBoxHoldingRegistri.SelectedIndex = config.comboBoxHoldingRegistri_ == "HEX" ? 1 : 0;
                comboBoxHoldingValori.SelectedIndex = config.comboBoxHoldingValori_ == "HEX" ? 1 : 0;

                textBoxCoilsOffset.Text = config.textBoxCoilsOffset_;
                textBoxInputOffset.Text = config.textBoxInputOffset_;
                textBoxInputRegOffset.Text = config.textBoxInputRegOffset_;
                textBoxHoldingOffset.Text = config.textBoxHoldingOffset_;

                // Funzioni doppie per force coil e preset holding aggiunte dopo
                comboBoxCoilsAddress05_b.SelectedIndex = config.comboBoxCoilsAddress05_b_ == "HEX" ? 1 : 0;
                textBoxCoilsAddress05_b.Text = config.textBoxCoilsAddress05_b_;
                //comboBoxCoilsValue05_b.SelectedIndex = config.comboBoxCoilsValue05_b_ == "HEX" ? 1 : 0;
                textBoxCoilsValue05_b.Text = config.textBoxCoilsValue05_b_;

                comboBoxHoldingAddress06_b.SelectedIndex = config.comboBoxHoldingAddress06_b_ == "HEX" ? 1 : 0;
                textBoxHoldingAddress06_b.Text = config.textBoxHoldingAddress06_b_;
                comboBoxHoldingValue06_b.SelectedIndex = config.comboBoxHoldingValue06_b_ == "HEX" ? 1 : 0;
                textBoxHoldingValue06_b.Text = config.textBoxHoldingValue06_b_;

                textBoxCoilsOffset.Text = config.textBoxCoilsOffset_;
                textBoxInputOffset.Text = config.textBoxInputOffset_;
                textBoxInputRegOffset.Text = config.textBoxInputRegOffset_;
                textBoxHoldingOffset.Text = config.textBoxHoldingOffset_;

                checkBoxUseOffsetInTextBox.IsChecked = config.checkBoxUseOffsetInTextBox_;
                checkBoxFollowModbusProtocol.IsChecked = config.checkBoxFollowModbusProtocol_;
                checkBoxCloseConsolAfterBoot.IsChecked = config.checkBoxCloseConsolAfterBoot_;
                checkBoxCellColorMode.IsChecked = config.checkBoxCellColorMode_;
                checkBoxViewTableWithoutOffset.IsChecked = config.checkBoxViewTableWithoutOffset_;

                comboBoxDiagnosticFunction.SelectedIndex = 0;

                textBoxDiagnosticFunctionManual.Text = config.textBoxDiagnosticFunctionManual_;

                BrushConverter bc = new BrushConverter();

                colorDefaultReadCell = (SolidColorBrush)bc.ConvertFromString(config.colorDefaultReadCell_);
                colorDefaultWriteCell = (SolidColorBrush)bc.ConvertFromString(config.colorDefaultWriteCell_);
                colorErrorCell = (SolidColorBrush)bc.ConvertFromString(config.colorErrorCell_);

                labelColorCellRead.Background = colorDefaultReadCell;
                labelColorCellWrote.Background = colorDefaultWriteCell;
                labelColorCellError.Background = colorErrorCell;

                colorDefaultReadCellStr = colorDefaultReadCell.ToString();
                colorDefaultWriteCellStr = colorDefaultWriteCell.ToString();
                colorErrorCellStr = colorErrorCell.ToString();

                if (!(config.TextBoxPollingInterval_ is null))
                {
                    TextBoxPollingInterval.Text = config.TextBoxPollingInterval_;
                }

                if (config.CheckBoxSendValuesOnEditCoillsTable_ != null)
                {
                    CheckBoxSendValuesOnEditCoillsTable.IsChecked = config.CheckBoxSendValuesOnEditCoillsTable_;
                }

                if (config.CheckBoxSendValuesOnEditHoldingTable_ != null)
                {
                    CheckBoxSendValuesOnEditHoldingTable.IsChecked = config.CheckBoxSendValuesOnEditHoldingTable_;
                }


                if (statoConsole)
                {
                    apriConsole();
                }

                // Scelgo quale richTextBox di status abilitare
                //richTextBoxStatus.IsEnabled = (bool)radioButtonModeSerial.IsChecked;
                //richTextBoxStatus.IsEnabled = !(bool)radioButtonModeSerial.IsChecked;

                if (config.language != null)
                {
                    language = config.language;

                    foreach (MenuItem tmp in languageToolStripMenu.Items)
                    {
                        if (tmp.Header.ToString().IndexOf(language) != -1)
                        {
                            tmp.IsChecked = true;
                        }
                    }
                }

                if (config.textBoxReadTimeout != null)
                {
                    textBoxReadTimeout.Text = config.textBoxReadTimeout;
                }

                textBoxCurrentLanguage.Text = language;
                //lang.loadLanguageTemplate(language);

                Console.WriteLine("Caricata configurazione precedente\n");
            }
            catch
            {
                Console.WriteLine("Error loading configuration\n");
            }

            try
            {
                string file_content = File.ReadAllText("Json/" + pathToConfiguration + "/Template.json");

                JavaScriptSerializer jss = new JavaScriptSerializer();
                jss.MaxJsonLength = this.MaxJsonLength;
                TEMPLATE template = jss.Deserialize<TEMPLATE>(file_content);

                template_coilsOffset = 0;
                template_inputsOffset = 0;
                template_inputRegistersOffset = 0;
                template_HoldingOffset = 0;

                list_template_coilsTable.Clear();
                list_template_inputsTable.Clear();
                list_template_inputRegistersTable.Clear();
                list_template_holdingRegistersTable.Clear();

                UInt16 tmp = 0;

                // Coils
                template_coilsOffset = int.Parse(template.textBoxCoilsOffset_, template.comboBoxCoilsOffset_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.HexNumber);

                // Inputs
                template_inputsOffset = int.Parse(template.textBoxInputOffset_, template.comboBoxInputOffset_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);

                // Input registers
                template_inputRegistersOffset = int.Parse(template.textBoxInputRegOffset_, template.comboBoxInputRegOffset_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);

                // Holding registers
                template_HoldingOffset = int.Parse(template.textBoxHoldingOffset_, template.comboBoxHoldingOffset_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);

                // Tabella coils
                for (int i = 0; i < template.dataGridViewCoils.Count(); i++)
                {
                    if (UInt16.TryParse(template.dataGridViewCoils[i].Register, template.comboBoxCoilsRegistri_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out tmp))
                    {
                        template.dataGridViewCoils[i].RegisterUInt = tmp;
                        template.dataGridViewCoils[i].Register = tmp.ToString();
                        list_template_coilsTable.Add(template.dataGridViewCoils[i]);
                    }
                }

                // Tabella inputs
                for (int i = 0; i < template.dataGridViewInput.Count(); i++)
                {
                    if (UInt16.TryParse(template.dataGridViewInput[i].Register, template.comboBoxInputRegistri_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out tmp))
                    {
                        template.dataGridViewInput[i].RegisterUInt = tmp;
                        template.dataGridViewInput[i].Register = tmp.ToString();
                        list_template_inputsTable.Add(template.dataGridViewInput[i]);
                    }
                }

                // Tabella input registers
                for (int i = 0; i < template.dataGridViewInputRegister.Count(); i++)
                {
                    if (UInt16.TryParse(template.dataGridViewInputRegister[i].Register, template.comboBoxInputRegRegistri_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out tmp))
                    {
                        template.dataGridViewInputRegister[i].RegisterUInt = tmp;
                        template.dataGridViewInputRegister[i].Register = tmp.ToString();
                        list_template_inputRegistersTable.Add(template.dataGridViewInputRegister[i]);
                    }
                }

                // Tabella holdings
                for (int i = 0; i < template.dataGridViewHolding.Count(); i++)
                {
                    if (UInt16.TryParse(template.dataGridViewHolding[i].Register, template.comboBoxHoldingRegistri_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out tmp))
                    {
                        template.dataGridViewHolding[i].RegisterUInt = tmp;
                        template.dataGridViewHolding[i].Register = tmp.ToString();
                        list_template_holdingRegistersTable.Add(template.dataGridViewHolding[i]);
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Error loading configuration\n");
                Console.WriteLine(err);
            }

        }

        private void salvaToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //SaveConfiguration_v1(true);
            SaveConfiguration_v2(true);
        }

        private void genera_tabelle_registri()
        {
            buttonReadCoils01.Dispatcher.Invoke((Action)delegate
            {
                // Disattivazione pulsanti fino al termine della generaizone delle tabelle
                /*buttonReadCoils01.IsEnabled = false;
                buttonReadCoilsRange.IsEnabled = false;
                buttonWriteCoils05.IsEnabled = false;
                buttonWriteCoils15.IsEnabled = false;*/
                buttonGoToCoilAddress.IsEnabled = false;

                /*buttonReadInput02.IsEnabled = false;
                buttonReadInputRange.IsEnabled = false;*/
                buttonGoToInputAddress.IsEnabled = false;

                /*buttonReadInputRegister04.IsEnabled = false;
                buttonReadInputRegisterRange.IsEnabled = false;*/
                buttonGoToInputRegisterAddress.IsEnabled = false;

                /*buttonReadHolding03.IsEnabled = false;
                buttonReadHoldingRange.IsEnabled = false;
                buttonWriteHolding06.IsEnabled = false;
                buttonWriteHolding16.IsEnabled = false;*/
                buttonGoToHoldingAddress.IsEnabled = false;

                list_coilsTable.Clear();
                list_inputsTable.Clear();
                list_inputRegistersTable.Clear();
                list_holdingRegistersTable.Clear();
            });

            ModBus_Item row = new ModBus_Item();

            buttonReadCoils01.Dispatcher.Invoke((Action)delegate
            {
                // Attivazione pulsanti al termine della generaizone delle tabelle
                /*buttonReadCoils01.IsEnabled = true;
                buttonReadCoilsRange.IsEnabled = true;
                buttonWriteCoils05.IsEnabled = true;
                buttonWriteCoils15.IsEnabled = true;*/
                buttonGoToCoilAddress.IsEnabled = true;

                /*buttonReadInput02.IsEnabled = true;
                buttonReadInputRange.IsEnabled = true;*/
                buttonGoToInputAddress.IsEnabled = true;

                /*buttonReadInputRegister04.IsEnabled = true;
                buttonReadInputRegisterRange.IsEnabled = true;*/
                buttonGoToInputRegisterAddress.IsEnabled = true;

                /*buttonReadHolding03.IsEnabled = true;
                buttonReadHoldingRange.IsEnabled = true;
                buttonWriteHolding06.IsEnabled = true;
                buttonWriteHolding16.IsEnabled = true;*/
                buttonGoToHoldingAddress.IsEnabled = true;


                // Dopo 2.5s secondi chiudo la console

                if ((bool)checkBoxCloseConsolAfterBoot.IsChecked)
                {
                    var handle = GetConsoleWindow();
                    ShowWindow(handle, SW_HIDE);
                }
            });
            Thread.Sleep(500);
            Thread.CurrentThread.Abort();
        }

        private void buttonTcpActive_Click(object sender, RoutedEventArgs e)
        {
            buttonTcpActive.IsEnabled = false;

            Thread t = new Thread(new ThreadStart(openCloseTcpConnection));
            t.Start();
        }

        private void openCloseTcpConnection()
        {
            String ip_address = "";
            String port = "";
            bool check = false;

            this.Dispatcher.Invoke((Action)delegate
            {
                ip_address = textBoxTcpClientIpAddress.Text;
                port = textBoxTcpClientPort.Text;
                check = pictureBoxTcp.Background == Brushes.LightGray;

                richTextBoxAppend(richTextBoxStatus, lang.languageTemplate["strings"]["connectingTo"] + " " + ip_address + ":" + port);
            });

            if (check)
            {
                try
                {
                    // Open, test the connection and close
                    TcpClient client = new TcpClient(ip_address, int.Parse(port));
                    client.Close();

                    this.Dispatcher.Invoke((Action)delegate
                    {
                        // Initialise Modbus Object
                        ModBus = new ModBus_Chicco(serialPort, textBoxTcpClientIpAddress.Text, textBoxTcpClientPort.Text, ModBus_Def.TYPE_TCP_REOPEN, pictureBoxIsResponding, pictureBoxIsSending);
                        ModBus.open();

                        pictureBoxTcp.Background = Brushes.Lime;
                        pictureBoxRunningAs.Background = Brushes.Lime;
                        radioButtonModeSerial.IsEnabled = false;
                        radioButtonModeTcp.IsEnabled = false;

                        richTextBoxAppend(richTextBoxStatus, lang.languageTemplate["strings"]["connectedTo"] + " " + ip_address + ":" + port);

                        textBlockTcpActive.Text = lang.languageTemplate["strings"]["disconnect"];
                        menuItemToolBit.IsEnabled = true;
                        menuItemToolWord.IsEnabled = true;
                        menuItemToolByte.IsEnabled = true;
                        salvaConfigurazioneNelDatabaseToolStripMenuItem.IsEnabled = false;
                        caricaConfigurazioneDalDatabaseToolStripMenuItem.IsEnabled = false;
                        gestisciDatabaseToolStripMenuItem.IsEnabled = false;

                        textBoxTcpClientIpAddress.IsEnabled = false;
                        textBoxTcpClientPort.IsEnabled = false;
                        languageToolStripMenu.IsEnabled = false;
                    });
                }
                catch
                {
                    Console.WriteLine("Impossibile stabilire una connessione con il server");

                    this.Dispatcher.Invoke((Action)delegate
                    {
                        richTextBoxAppend(richTextBoxStatus, lang.languageTemplate["strings"]["failedToConnect"] + " " + ip_address + ":" + port);
                    });

                    return;
                }
            }
            else
            {
                // Close connection
                this.Dispatcher.Invoke((Action)delegate
                {
                    pictureBoxTcp.Background = Brushes.LightGray;
                    pictureBoxRunningAs.Background = Brushes.LightGray;

                    richTextBoxAppend(richTextBoxStatus, lang.languageTemplate["strings"]["disconnected"]);

                    textBlockTcpActive.Text = lang.languageTemplate["strings"]["connect"];
                    menuItemToolBit.IsEnabled = false;
                    menuItemToolWord.IsEnabled = false;
                    menuItemToolByte.IsEnabled = false;
                    salvaConfigurazioneNelDatabaseToolStripMenuItem.IsEnabled = true;
                    caricaConfigurazioneDalDatabaseToolStripMenuItem.IsEnabled = true;
                    gestisciDatabaseToolStripMenuItem.IsEnabled = true;

                    radioButtonModeSerial.IsEnabled = true;
                    radioButtonModeTcp.IsEnabled = true;

                    textBoxTcpClientIpAddress.IsEnabled = true;
                    textBoxTcpClientPort.IsEnabled = true;
                    languageToolStripMenu.IsEnabled = true;

                    // Close the connection
                    ModBus.close();

                    // Fermo eventuali loop
                    disableAllLoops();
                });
            }

            this.Dispatcher.Invoke((Action)delegate
            {
                changeEnableButtonsConnect(pictureBoxRunningAs.Background == Brushes.Lime);
                buttonTcpActive.IsEnabled = true;
            });
        }

        //----------------------------------------------------------------------------------
        //------------------------------- COILS --------------------------------------------
        //----------------------------------------------------------------------------------

        private void buttonReadCoils01_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                buttonReadCoils01.IsEnabled = false;
            }

            Thread t = new Thread(new ThreadStart(readCoils));
            t.Priority = threadPriority;
            t.Start();
        }

        public void readCoils()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_) + P.uint_parser(textBoxCoilsAddress01_, comboBoxCoilsAddress01_);

                if (uint.Parse(textBoxCoilNumber_) > 123)
                {
                    MessageBox.Show(lang.languageTemplate["strings"]["maxRegNumber"], "Info");
                }
                else
                {
                    UInt16[] response = ModBus.readCoilStatus_01(byte.Parse(textBoxModbusAddress_), address_start, uint.Parse(textBoxCoilNumber_), readTimeout);

                    if (response != null)
                    {
                        if (response.Length > 0)
                        {
                            // Cancello la tabella e inserisco le nuove righe
                            if (useOffsetInTable)
                            {
                                insertRowsTable(list_coilsTable, list_template_coilsTable, template_coilsOffset, address_start, response, colorDefaultReadCellStr, comboBoxCoilsRegistri_, "DEC");
                            }
                            else
                            {
                                insertRowsTable(list_coilsTable, list_template_coilsTable, template_coilsOffset, address_start, response, colorDefaultReadCellStr, comboBoxCoilsRegistri_, "DEC");
                            }
                        }
                    }
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadCoils01.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_coilsTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_coilsTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_coilsTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadCoils01.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_coilsTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadCoils01.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
        }

        private void buttonReadCoilsRange_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                buttonReadCoilsRange.IsEnabled = false;
            }

            Thread t = new Thread(new ThreadStart(readColisRange));
            t.Priority = threadPriority;
            t.Start();
        }

        public void readColisRange()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_) + P.uint_parser(textBoxCoilsRange_A_, comboBoxCoilsRange_A_);
                //uint coil_len = P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_) + P.uint_parser(textBoxCoilsRange_B_, comboBoxCoilsRange_B_) - address_start + 1;
                uint coil_len = P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_) + P.uint_parser(textBoxCoilsRange_B_, comboBoxCoilsRange_A_) - address_start + 1;
                uint read_len = uint.Parse(textBoxCoilNumber_);
                uint repeatQuery = coil_len / read_len;

                if (coil_len % read_len != 0)
                {
                    repeatQuery += 1;
                }

                UInt16[] response = new UInt16[coil_len];

                for (int i = 0; i < repeatQuery; i++)
                {
                    if (i == (repeatQuery - 1) && coil_len % read_len != 0)
                    {
                        UInt16[] read = ModBus.readCoilStatus_01(byte.Parse(textBoxModbusAddress_), address_start + (uint)(read_len * i), coil_len % read_len, readTimeout);

                        // Timeout
                        if (read is null)
                        {
                            SetTableTimeoutError(list_coilsTable);
                        }

                        // CRC error
                        if (read.Length == 0)
                        {
                            SetTableCrcError(list_coilsTable);
                        }

                        Array.Copy(read, 0, response, read_len * i, coil_len % read_len);
                    }
                    else
                    {
                        UInt16[] read = ModBus.readCoilStatus_01(byte.Parse(textBoxModbusAddress_), address_start + (uint)(read_len * i), read_len, readTimeout);

                        Array.Copy(read, 0, response, read_len * i, read_len);
                    }
                }

                // Cancello la tabella e inserisco le nuove righe
                if (useOffsetInTable)
                {
                    insertRowsTable(list_coilsTable, list_template_coilsTable, template_coilsOffset, address_start - P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_), response, colorDefaultReadCellStr, comboBoxCoilsRegistri_, "DEC");
                }
                else
                {
                    insertRowsTable(list_coilsTable, list_template_coilsTable, template_coilsOffset, address_start, response, colorDefaultReadCellStr, comboBoxCoilsRegistri_, "DEC");
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadCoilsRange.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_coilsTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_coilsTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_coilsTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadCoilsRange.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_coilsTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadCoilsRange.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
        }

        private void SetTableInternalError(ObservableCollection<ModBus_Item> list_)
        {
            ModBus_Item tmp = new ModBus_Item();

            tmp.Register = "Internal";
            tmp.Value = "Error";
            tmp.Foreground = ForeGroundLightStr;
            tmp.Background = Brushes.Red.ToString();

            this.Dispatcher.Invoke((Action)delegate
            {
                list_.Clear();
                list_.Add(tmp);
            });
        }

        private void SetTableCrcError(ObservableCollection<ModBus_Item> list_)
        {
            ModBus_Item tmp = new ModBus_Item();

            tmp.Register = "CRC";
            tmp.Value = "Error";
            tmp.Foreground = ForeGroundLightStr;
            tmp.Background = Brushes.Tomato.ToString();

            this.Dispatcher.Invoke((Action)delegate
            {
                list_.Clear();
                list_.Add(tmp);
            });
        }

        private void SetTableTimeoutError(ObservableCollection<ModBus_Item> list_)
        {
            ModBus_Item tmp = new ModBus_Item();

            tmp.Register = "Timeout";
            tmp.Value = "";
            tmp.Foreground = ForeGroundLightStr;
            tmp.Background = Brushes.Violet.ToString();

            this.Dispatcher.Invoke((Action)delegate
            {
                list_.Clear();
                list_.Add(tmp);
            });
        }

        private void SetTableModBusError(ObservableCollection<ModBus_Item> list_, ModbusException err)
        {
            ModBus_Item tmp = new ModBus_Item();

            Console.WriteLine("err.ToString(): " + err.ToString());

            tmp.Register = "ErrCode:";
            tmp.Value = err.ToString().Split('-')[0].Split(':')[2];
            tmp.ValueBin = err.ToString().Split('-')[1].Split('\n')[0].Replace("\r","");
            tmp.Foreground = ForeGroundLightStr;
            tmp.Background = Brushes.OrangeRed.ToString();

            this.Dispatcher.Invoke((Action)delegate
            {
                list_.Clear();
                list_.Add(tmp);
            });
        }

        private void buttonWriteCoils05_Click(object sender, RoutedEventArgs e)
        {
            buttonWriteCoils05.IsEnabled = false;

            Thread t = new Thread(new ThreadStart(writeCoil_01));
            t.Priority = threadPriority;
            t.Start();
        }

        public void writeCoil_01()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_) + P.uint_parser(textBoxCoilsAddress05_, comboBoxCoilsAddress05_);

                bool? result = ModBus.forceSingleCoil_05(byte.Parse(textBoxModbusAddress_), address_start, uint.Parse(textBoxCoilsValue05_), readTimeout);

                if (result != null)
                {
                    if (result == true)
                    {
                        UInt16[] value = { UInt16.Parse(textBoxCoilsValue05_) };

                        // Cancello la tabella e inserisco le nuove righe
                        if (useOffsetInTable)
                        {
                            insertRowsTable(list_coilsTable, list_template_coilsTable, template_coilsOffset, address_start - P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_), value, colorDefaultWriteCellStr, comboBoxCoilsRegistri_, "DEC");
                        }
                        else
                        {
                            insertRowsTable(list_coilsTable, list_template_coilsTable, template_coilsOffset, address_start, value, colorDefaultWriteCellStr, comboBoxCoilsRegistri_, "DEC");
                        }
                    }
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils05.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_coilsTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_coilsTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_coilsTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils05.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch(Exception err)
            {
                SetTableInternalError(list_coilsTable);

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils05.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
        }
        
        private void buttonWriteCoils05_B_Click(object sender, RoutedEventArgs e)
        {
            buttonWriteCoils05_B.IsEnabled = false;

            Thread t = new Thread(new ThreadStart(writeCoil_02));
            t.Priority = threadPriority;
            t.Start();
        }

        public void writeCoil_02()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_) + P.uint_parser(textBoxCoilsAddress05_b_, comboBoxCoilsAddress05_b_);

                bool? result = ModBus.forceSingleCoil_05(byte.Parse(textBoxModbusAddress_), address_start, uint.Parse(textBoxCoilsValue05_b_), readTimeout);

                if (result != null)
                {
                    if (result == true)
                    {
                        UInt16[] value = { UInt16.Parse(textBoxCoilsValue05_b_) };

                        // Cancello la tabella e inserisco le nuove righe
                        if (useOffsetInTable)
                        {
                            insertRowsTable(list_coilsTable, list_template_coilsTable, template_coilsOffset, address_start - P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_), value, colorDefaultWriteCellStr, comboBoxCoilsRegistri_, "DEC");
                        }
                        else
                        {
                            insertRowsTable(list_coilsTable, list_template_coilsTable, template_coilsOffset, address_start, value, colorDefaultWriteCellStr, comboBoxCoilsRegistri_, "DEC");
                        }
                    }
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils05_B.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_coilsTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_coilsTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_coilsTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils05_B.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_coilsTable);

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils05_B.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
        }

        private void buttonWriteCoils15_Click(object sender, RoutedEventArgs e)
        {
            buttonWriteCoils15.IsEnabled = false;

            Thread t = new Thread(new ThreadStart(writeMultipleCoils));
            t.Priority = threadPriority;
            t.Start();
        }

        public void writeMultipleCoils()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxCoilsOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxCoilsAddress15_A_, comboBoxCoilsAddress15_A_);

                uint stop = uint.Parse(textBoxCoilsAddress15_B_);

                bool[] buffer = new bool[stop];

                for (int i = 0; i < stop; i++)
                {
                    buffer[i] = uint.Parse(textBoxCoilsValue15_.Substring(i, 1)) > 0;
                }

                bool? result = ModBus.forceMultipleCoils_15(byte.Parse(textBoxModbusAddress_), address_start, buffer, readTimeout);

                if (result != null)
                {
                    if (result == true)
                    {
                        UInt16[] value = new UInt16[stop];

                        for (int i = 0; i < stop; i++)
                        {
                            value[i] = uint.Parse(textBoxCoilsValue15_.Substring(i, 1)) > 0 ? (UInt16)(1) : (UInt16)(0);
                        }

                        // Cancello la tabella e inserisco le nuove righe
                        if (useOffsetInTable)
                        {
                            insertRowsTable(list_coilsTable, list_template_coilsTable, template_coilsOffset, address_start - P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_), value, colorDefaultWriteCellStr, comboBoxCoilsRegistri_, null);
                        }
                        else
                        {
                            insertRowsTable(list_coilsTable, list_template_coilsTable, template_coilsOffset, address_start, value, colorDefaultWriteCellStr, comboBoxCoilsRegistri_, null);
                        }
                    }
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils15.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_coilsTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_coilsTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_coilsTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils15.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_coilsTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils15.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
        }

        private void buttonGoToCoilAddress_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(textBoxGoToCoilAddress.Text);

            //dataGridViewCoils.Scroo = dataGridViewCoils.Rows[index].Cells[0];
        }

        //----------------------------------------------------------------------------------
        //--------------------------------DIGITAL INPUTS------------------------------------
        //----------------------------------------------------------------------------------

        // Read digital input
        private void buttonReadInput02_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                buttonReadInput02.IsEnabled = false;
            }

            Thread t = new Thread(new ThreadStart(readInputs));
            t.Priority = threadPriority;
            t.Start();
        }

        public void readInputs()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxInputOffset_, comboBoxInputOffset_) + P.uint_parser(textBoxInputAddress02_, comboBoxInputAddress02_);

                if (address_start > 9999 && correctModbusAddressAuto)    // Se indirizzo espresso in 10001+ imposto offset a 0
                {
                    address_start = address_start - 10001;
                }

                if (uint.Parse(textBoxInputNumber_) > 123)
                {
                    MessageBox.Show(lang.languageTemplate["strings"]["maxRegNumber"], "Info");
                }
                else
                {
                    UInt16[] response = ModBus.readInputStatus_02(byte.Parse(textBoxModbusAddress_), address_start, uint.Parse(textBoxInputNumber_), readTimeout);

                    if (response != null)
                    {
                        if (response.Length > 0)
                        {
                            // Cancello la tabella e inserisco le nuove righe
                            if (useOffsetInTable)
                            {
                                insertRowsTable(list_inputsTable, list_template_inputsTable, template_inputsOffset, address_start - P.uint_parser(textBoxInputOffset_, comboBoxInputOffset_), response, colorDefaultReadCellStr, comboBoxInputRegistri_, "DEC");
                            }
                            else
                            {
                                insertRowsTable(list_inputsTable, list_template_inputsTable, template_inputsOffset, address_start, response, colorDefaultReadCellStr, comboBoxInputRegistri_, "DEC");
                            }
                        }
                    }
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInput02.IsEnabled = true;

                    dataGridViewInput.ItemsSource = null;
                    dataGridViewInput.ItemsSource = list_inputsTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_inputsTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_inputsTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_inputsTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInput02.IsEnabled = true;

                    dataGridViewInput.ItemsSource = null;
                    dataGridViewInput.ItemsSource = list_inputsTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_inputsTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInput02.IsEnabled = true;

                    dataGridViewInput.ItemsSource = null;
                    dataGridViewInput.ItemsSource = list_inputsTable;
                });
            }
        }

        // Read input range
        private void buttonReadInputRange_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                buttonReadInputRange.IsEnabled = false;
            }

            Thread t = new Thread(new ThreadStart(readInputsRange));
            t.Priority = threadPriority;
            t.Start();
        }

        public void readInputsRange()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxInputOffset_, comboBoxInputOffset_) + P.uint_parser(textBoxInputRange_A_, comboBoxInputRange_A_);
                //uint input_len = P.uint_parser(textBoxInputOffset_, comboBoxInputOffset_) + P.uint_parser(textBoxInputRange_B_, comboBoxInputRange_B_) - address_start + 1;
                uint input_len = P.uint_parser(textBoxInputOffset_, comboBoxInputOffset_) + P.uint_parser(textBoxInputRange_B_, comboBoxInputRange_A_) - address_start + 1;

                if (address_start > 9999 && correctModbusAddressAuto)    // Se indirizzo espresso in 10001+ imposto offset a 0
                {
                    address_start = address_start - 10001;
                }

                uint read_len = uint.Parse(textBoxInputNumber_);
                uint repeatQuery = input_len / read_len;

                if (input_len % read_len != 0)
                {
                    repeatQuery += 1;
                }

                UInt16[] response = new UInt16[input_len];

                for (int i = 0; i < repeatQuery; i++)
                {
                    if (i == (repeatQuery - 1) && input_len % read_len != 0)
                    {
                        UInt16[] read = ModBus.readInputStatus_02(byte.Parse(textBoxModbusAddress_), address_start + (uint)(read_len * i), input_len % read_len, readTimeout);

                        // Timeout
                        if (read is null)
                        {
                            SetTableTimeoutError(list_inputsTable);
                            return;
                        }

                        // CRC error
                        if (read.Length == 0)
                        {
                            SetTableCrcError(list_inputsTable);
                            return;
                        }

                        Array.Copy(read, 0, response, read_len * i, input_len % read_len);
                    }
                    else
                    {
                        UInt16[] read = ModBus.readInputStatus_02(byte.Parse(textBoxModbusAddress_), address_start + (uint)(read_len * i), read_len, readTimeout);

                        Array.Copy(read, 0, response, read_len * i, read_len);
                    }
                }

                // Cancello la tabella e inserisco le nuove righe
                if (useOffsetInTable)
                {
                    insertRowsTable(list_inputsTable, list_template_inputsTable, template_inputsOffset, address_start - P.uint_parser(textBoxInputOffset_, comboBoxInputOffset_), response, colorDefaultReadCellStr, comboBoxInputRegistri_, "DEC");
                }
                else
                {
                    insertRowsTable(list_inputsTable, list_template_inputsTable, template_inputsOffset, address_start, response, colorDefaultReadCellStr, comboBoxInputRegistri_, "DEC");
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInputRange.IsEnabled = true;

                    dataGridViewInput.ItemsSource = null;
                    dataGridViewInput.ItemsSource = list_inputsTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_inputsTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_inputsTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_inputsTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInputRange.IsEnabled = true;

                    dataGridViewInput.ItemsSource = null;
                    dataGridViewInput.ItemsSource = list_inputsTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_inputsTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInputRange.IsEnabled = true;

                    dataGridViewInput.ItemsSource = null;
                    dataGridViewInput.ItemsSource = list_inputsTable;
                });
            }
        }
        
        // Go to digital input
        private void buttonGoToInputAddress_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(textBoxGoToInputAddress.Text);

            if (index > 9999 && (bool)checkBoxUseOffsetInTextBox.IsChecked)
                index = index - 10001;

            //dataGridViewInput.FirstDisplayedCell = dataGridViewInput.Rows[index].Cells[0];
        }

        // ----------------------------------------------------------------------------------
        // ---------------------------INPUT REGISTERS----------------------------------------
        // ----------------------------------------------------------------------------------

        // Read input register FC04
        private void buttonReadInputRegister04_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                buttonReadInputRegister04.IsEnabled = false;
            }

            Thread t = new Thread(new ThreadStart(readInputRegisters));
            t.Priority = threadPriority;
            t.Start();
        }

        public void readInputRegisters()
        {
            try
            {

                uint address_start = P.uint_parser(textBoxInputRegOffset_, comboBoxInputRegOffset_) + P.uint_parser(textBoxInputRegisterAddress04_, comboBoxInputRegisterAddress04_);

                if (uint.Parse(textBoxInputRegisterNumber_) > 123)
                {
                    MessageBox.Show(lang.languageTemplate["strings"]["maxRegNumber"], "Info");
                }
                else
                {
                    if (address_start > 9999 && correctModbusAddressAuto)    //Se indirizzo espresso in 30001+ imposto offset a 0
                    {
                        address_start = address_start - 30001;
                    }

                    UInt16[] response = ModBus.readInputRegister_04(byte.Parse(textBoxModbusAddress_), address_start, uint.Parse(textBoxInputRegisterNumber_), readTimeout);

                    if (response != null)
                    {
                        if (response.Length > 0)
                        {
                            // Cancello la tabella e inserisco le nuove righe
                            if (useOffsetInTable)
                            {
                                insertRowsTable(list_inputRegistersTable, list_template_inputRegistersTable, template_inputRegistersOffset, address_start - P.uint_parser(textBoxInputRegOffset_, comboBoxInputRegOffset_), response, colorDefaultReadCellStr, comboBoxInputRegRegistri_, comboBoxInputRegValori_);
                            }
                            else
                            {
                                insertRowsTable(list_inputRegistersTable, list_template_inputRegistersTable, template_inputRegistersOffset, address_start, response, colorDefaultReadCellStr, comboBoxInputRegRegistri_, comboBoxInputRegValori_);
                            }
                        }
                    }
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInputRegister04.IsEnabled = true;

                    dataGridViewInputRegister.ItemsSource = null;
                    dataGridViewInputRegister.ItemsSource = list_inputRegistersTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_inputRegistersTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_inputRegistersTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_inputRegistersTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInputRegister04.IsEnabled = true;

                    dataGridViewInputRegister.ItemsSource = null;
                    dataGridViewInputRegister.ItemsSource = list_inputRegistersTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_inputRegistersTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInputRegister04.IsEnabled = true;

                    dataGridViewInputRegister.ItemsSource = null;
                    dataGridViewInputRegister.ItemsSource = list_inputRegistersTable;
                });
            }
        }

        // Read input register range
        private void buttonReadInputRegisterRange_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                buttonReadInputRegisterRange.IsEnabled = false;
            }

            Thread t = new Thread(new ThreadStart(readInputRegistersRange));
            t.Priority = threadPriority;
            t.Start();
        }

        public void readInputRegistersRange()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxInputRegOffset_, comboBoxInputRegOffset_) + P.uint_parser(textBoxInputRegisterRange_A_, comboBoxInputRegisterRange_A_);
                //uint register_len = P.uint_parser(textBoxInputRegOffset_, comboBoxInputRegOffset_) + P.uint_parser(textBoxInputRegisterRange_B_, comboBoxInputRegisterRange_B_) - address_start + 1;
                uint register_len = P.uint_parser(textBoxInputRegOffset_, comboBoxInputRegOffset_) + P.uint_parser(textBoxInputRegisterRange_B_, comboBoxInputRegisterRange_A_) - address_start + 1;

                if (address_start > 9999 && correctModbusAddressAuto)    //Se indirizzo espresso in 30001+ imposto offset a 0
                {
                    address_start = address_start - 30001;
                }

                uint read_len = uint.Parse(textBoxInputRegisterNumber_);
                uint repeatQuery = register_len / read_len;

                if (register_len % read_len != 0)
                {
                    repeatQuery += 1;
                }

                UInt16[] response = new UInt16[register_len];

                for (int i = 0; i < repeatQuery; i++)
                {
                    if (i == (repeatQuery - 1) && register_len % read_len != 0)
                    {
                        UInt16[] read = ModBus.readInputRegister_04(byte.Parse(textBoxModbusAddress_), address_start + (uint)(read_len * i), register_len % read_len, readTimeout);

                        // Timeout
                        if (read is null)
                        {
                            SetTableTimeoutError(list_inputRegistersTable);
                            return;
                        }

                        // CRC error
                        if (read.Length == 0)
                        {
                            SetTableCrcError(list_inputRegistersTable);
                            return;
                        }

                        Array.Copy(read, 0, response, read_len * i, register_len % read_len);
                    }
                    else
                    {
                        UInt16[] read = ModBus.readInputRegister_04(byte.Parse(textBoxModbusAddress_), address_start + (uint)(read_len * i), read_len, readTimeout);

                        Array.Copy(read, 0, response, read_len * i, read_len);
                    }
                }

                if (address_start > 9999 && correctModbusAddressAuto)    //Se indirizzo espresso in 30001+ imposto offset a 0
                {
                    address_start = address_start - 30001;
                }

                // Cancello la tabella e inserisco le nuove righe
                if (useOffsetInTable)
                {
                    insertRowsTable(list_inputRegistersTable, list_template_inputRegistersTable, template_inputRegistersOffset, address_start - P.uint_parser(textBoxInputRegOffset_, comboBoxInputRegOffset_), response, colorDefaultReadCellStr, comboBoxInputRegRegistri_, comboBoxInputRegValori_);
                }
                else
                {
                    insertRowsTable(list_inputRegistersTable, list_template_inputRegistersTable, template_inputRegistersOffset, address_start, response, colorDefaultReadCellStr, comboBoxInputRegRegistri_, comboBoxInputRegValori_);
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInputRegisterRange.IsEnabled = true;

                    dataGridViewInputRegister.ItemsSource = null;
                    dataGridViewInputRegister.ItemsSource = list_inputRegistersTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_inputRegistersTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_inputRegistersTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_inputRegistersTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInputRegisterRange.IsEnabled = true;

                    dataGridViewInputRegister.ItemsSource = null;
                    dataGridViewInputRegister.ItemsSource = list_inputRegistersTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_inputRegistersTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadInputRegisterRange.IsEnabled = true;

                    dataGridViewInputRegister.ItemsSource = null;
                    dataGridViewInputRegister.ItemsSource = list_inputRegistersTable;
                });
            }
        }

        // Go to input register
        private void buttonGoToInputRegisterAddress_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(textBoxGoToInputRegisterAddress.Text);

            if (index > 9999 && (bool)checkBoxUseOffsetInTextBox.IsChecked)
                index = index - 30001;

            //dataGridViewInputRegister.FirstDisplayedCell = dataGridViewInputRegister.Rows[index].Cells[0];
        }

        // ----------------------------------------------------------------------------------
        // ---------------------------HOLDING REGISTER---------------------------------------
        // ----------------------------------------------------------------------------------

        // Read holding register
        private void buttonReadHolding03_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                buttonReadHolding03.IsEnabled = false;
            }

            Thread t = new Thread(new ThreadStart(readHoldingRegisters));
            t.Priority = threadPriority;
            t.Start();
        }

        public void readHoldingRegisters()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxHoldingAddress03_, comboBoxHoldingAddress03_);

                if (uint.Parse(textBoxHoldingRegisterNumber_) > 123)
                {
                    MessageBox.Show(lang.languageTemplate["strings"]["maxRegNumber"], "Info");
                }
                else
                {
                    if (address_start > 9999 && correctModbusAddressAuto)    // Se indirizzo espresso in 40001+ imposto offset a 0
                    {
                        address_start = address_start - 40001;
                    }

                    UInt16[] response = ModBus.readHoldingRegister_03(byte.Parse(textBoxModbusAddress_), P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxHoldingAddress03_, comboBoxHoldingAddress03_), uint.Parse(textBoxHoldingRegisterNumber_), readTimeout);

                    if (response != null)
                    {
                        if (response.Length > 0)
                        {
                            // Cancello la tabella e inserisco le nuove righe
                            if (useOffsetInTable)
                            {
                                insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxHoldingAddress03_, comboBoxHoldingAddress03_) - P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_), response, colorDefaultReadCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                            }
                            else
                            {
                                insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxHoldingAddress03_, comboBoxHoldingAddress03_), response, colorDefaultReadCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                            }
                        }
                    }
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadHolding03.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_holdingRegistersTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_holdingRegistersTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_holdingRegistersTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadHolding03.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_holdingRegistersTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadHolding03.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
        }

        // Preset single register
        private void buttonWriteHolding06_Click(object sender, RoutedEventArgs e)
        {
            buttonWriteHolding06.IsEnabled = false;

            Thread T = new Thread(new ThreadStart(witeHoldingRegister_01));
            T.Start();
        }

        public void witeHoldingRegister_01()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxHoldingAddress06_, comboBoxHoldingAddress06_);

                if (address_start > 9999 && correctModbusAddressAuto)    //Se indirizzo espresso in 30001+ imposto offset a 0
                {
                    address_start = address_start - 40001;
                }

                bool? result = ModBus.presetSingleRegister_06(byte.Parse(textBoxModbusAddress_), address_start, P.uint_parser(textBoxHoldingValue06_, comboBoxHoldingValue06_), readTimeout);

                if (result != null)
                {
                    if (result == true)
                    {
                        UInt16[] value = { (UInt16)P.uint_parser(textBoxHoldingValue06_, comboBoxHoldingValue06_) };

                        // Cancello la tabella e inserisco le nuove righe
                        if (useOffsetInTable)
                        {
                            insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, address_start - P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_), value, colorDefaultWriteCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                        }
                        else
                        {
                            insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, address_start, value, colorDefaultWriteCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                        }
                    }
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding06.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_holdingRegistersTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_holdingRegistersTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_holdingRegistersTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding06.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_holdingRegistersTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding06.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
        }

        // Preset multiple register
        private void buttonWriteHolding16_Click(object sender, RoutedEventArgs e)
        {
            buttonWriteHolding16.IsEnabled = false;

            Thread t = new Thread(new ThreadStart(writeMultipleRegisters));
            t.Priority = threadPriority;
            t.Start();
        }

        public void writeMultipleRegisters()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxHoldingAddress16_A_, comboBoxHoldingAddress16_A_);

                if (address_start > 9999 && correctModbusAddressAuto)    //Se indirizzo espresso in 30001+ imposto offset a 0
                {
                    address_start = address_start - 40001;
                }

                uint word_count = P.uint_parser(textBoxHoldingAddress16_B_, comboBoxHoldingAddress16_B_);

                UInt16[] buffer = new UInt16[word_count];

                if (comboBoxHoldingValue16_ == "HEX")
                {
                    if (textBoxHoldingValue16_.Split(' ').Length != (word_count * 2))
                    {
                        MessageBox.Show("Formato stringa inserito non corretto" + word_count.ToString(), "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        for (int i = 0; i < word_count; i += 1)
                        {
                            uint HB = P.uint_parser(textBoxHoldingValue16_.Split(' ')[i * 2], comboBoxHoldingValue16_);
                            uint LB = P.uint_parser(textBoxHoldingValue16_.Split(' ')[i * 2 + 1], comboBoxHoldingValue16_);

                            buffer[i] = (UInt16)((HB << 8) + LB);
                        }

                        UInt16[] writtenRegs = ModBus.presetMultipleRegisters_16(byte.Parse(textBoxModbusAddress_), address_start, buffer, readTimeout);

                        if (writtenRegs.Length == word_count)
                        {
                            // Cancello la tabella e inserisco le nuove righe
                            if (useOffsetInTable)
                            {
                                insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, address_start - P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_), writtenRegs, colorDefaultWriteCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                            }
                            else
                            {
                                insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, address_start, writtenRegs, colorDefaultWriteCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                            }
                        }
                        else
                        {
                            MessageBox.Show(lang.languageTemplate["strings"]["errSetReg"], "Alert");
                            list_holdingRegistersTable[(int)(address_start)].Foreground = ForeGroundLight.ToString();
                            list_holdingRegistersTable[(int)(address_start)].Background = Brushes.Red.ToString();
                        }
                    }
                }
                else
                {
                    if (textBoxHoldingValue16_.Split(' ').Length != word_count)
                    {
                        MessageBox.Show("Formato stringa inserito non corretto", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        for (int i = 0; i < word_count; i += 1)
                        {
                            UInt16 WD = (UInt16)P.uint_parser(textBoxHoldingValue16_.Split(' ')[i], comboBoxHoldingValue16_);

                            buffer[i] = WD;
                        }

                        UInt16[] writtenRegs = ModBus.presetMultipleRegisters_16(byte.Parse(textBoxModbusAddress_), address_start, buffer, readTimeout);

                        if (writtenRegs.Length == word_count)
                        {
                            // Cancello la tabella e inserisco le nuove righe
                            if (useOffsetInTable)
                            {
                                insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, address_start - P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_), writtenRegs, colorDefaultWriteCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                            }
                            else
                            {
                                insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, address_start, writtenRegs, colorDefaultWriteCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                            }
                        }
                        else
                        {
                            MessageBox.Show(lang.languageTemplate["strings"]["errSetReg"], "Alert");
                            list_holdingRegistersTable[(int)(address_start)].Foreground = ForeGroundLight.ToString();
                            list_holdingRegistersTable[(int)(address_start)].Background = Brushes.Red.ToString();
                        }
                    }
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding16.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_holdingRegistersTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_holdingRegistersTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_holdingRegistersTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding16.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_holdingRegistersTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding16.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
        }

        // Read holding register range
        private void buttonReadHoldingRange_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                buttonReadHoldingRange.IsEnabled = false;
            }

            Thread t = new Thread(new ThreadStart(readHoldingRegistersRange));
            t.Priority = threadPriority;
            t.Start();
        }

        public void readHoldingRegistersRange()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxHoldingRange_A_, comboBoxHoldingRange_A_);
                //uint register_len = P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxHoldingRange_B_, comboBoxHoldingRange_B_) - address_start + 1;
                uint register_len = P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxHoldingRange_B_, comboBoxHoldingRange_A_) - address_start + 1;

                if (address_start > 9999 && correctModbusAddressAuto)    // Se indirizzo espresso in 40001+ imposto offset a 0
                {
                    address_start = address_start - 40001;
                }

                uint read_len = uint.Parse(textBoxHoldingRegisterNumber_);
                uint repeatQuery = register_len / read_len;

                if (register_len % read_len != 0)
                {
                    repeatQuery += 1;
                }

                UInt16[] response = new UInt16[register_len];

                for (int i = 0; i < repeatQuery; i++)
                {
                    if (i == (repeatQuery - 1) && register_len % read_len != 0)
                    {
                        UInt16[] read = ModBus.readHoldingRegister_03(byte.Parse(textBoxModbusAddress_), address_start + (uint)(read_len * i), register_len % read_len, readTimeout);

                        // Timeout
                        if (read is null)
                        {
                            SetTableTimeoutError(list_holdingRegistersTable);
                            return;
                        }

                        // CRC error
                        if (read.Length == 0)
                        {
                            SetTableCrcError(list_holdingRegistersTable);
                            return;
                        }

                        Array.Copy(read, 0, response, read_len * i, register_len % read_len);
                    }
                    else
                    {
                        UInt16[] read = ModBus.readHoldingRegister_03(byte.Parse(textBoxModbusAddress_), address_start + (uint)(read_len * i), read_len, readTimeout);

                        // Timeout
                        if (read is null)
                        {
                            SetTableTimeoutError(list_holdingRegistersTable);
                            return;
                        }

                        // CRC error
                        if (read.Length == 0)
                        {
                            SetTableCrcError(list_holdingRegistersTable);
                            return;
                        }

                        Array.Copy(read, 0, response, read_len * i, read_len);
                    }
                }

                if (address_start > 9999 && correctModbusAddressAuto)    //Se indirizzo espresso in 30001+ imposto offset a 0
                {
                    address_start = address_start - 40001;
                }

                //Cancello la tabella e inserisco le nuove righe
                if (useOffsetInTable)
                {
                    insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, address_start - P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_), response, colorDefaultReadCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                }
                else
                {
                    insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, address_start, response, colorDefaultReadCellStr, comboBoxHoldingRegistri.SelectedValue.ToString().Split(' ')[1], comboBoxHoldingValori.SelectedValue.ToString().Split(' ')[1]);
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadHoldingRange.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_holdingRegistersTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_holdingRegistersTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_holdingRegistersTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadHoldingRange.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_holdingRegistersTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonReadHoldingRange.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
        }

        // Go to holding register
        private void buttonGoToHoldingAddress_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(textBoxGoToHoldingAddress.Text);

            if (index > 9999 && (bool)checkBoxUseOffsetInTextBox.IsChecked)
                index = index - 40001;

            //dataGridViewHolding.FirstDisplayedCell = dataGridViewHolding.Rows[index].Cells[0];
        }

        // ----------------------------------------------------------------------------------
        // --------------------------- OTHERS MODBUS ----------------------------------------
        // ----------------------------------------------------------------------------------

        private void buttonSendDiagnosticQuery_Click(object sender, RoutedEventArgs e)
        {
            byte[] diagnostic_codes = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x14 };

            //DEBUG
            //MessageBox.Show(comboBoxDiagnosticFunction.SelectedIndex.ToString());
            //SelectedIndex considera -1 la cella vuota e 0 il primo elemento del menu a tendina

            if (comboBoxDiagnosticFunction.SelectedIndex >= 0)
            {
                try
                {
                    textBoxDiagnosticResponse.Text = ModBus.diagnostics_08(byte.Parse(textBoxModbusAddress.Text), diagnostic_codes[comboBoxDiagnosticFunction.SelectedIndex], UInt16.Parse(textBoxDiagnosticData.Text), readTimeout);
                }
                catch (ModbusException err)
                {
                    if (err.Message.IndexOf("Timed out") != -1)
                    {
                        textBoxDiagnosticData.Text = "Timeout";
                    }
                    if (err.Message.IndexOf("ModBus ErrCode") != -1)
                    {
                        textBoxDiagnosticResponse.Text = "ErrCode: " + err.ToString().Split('-')[0].Split(':')[2] + " - " + err.ToString().Split('-')[1].Split('\n')[0].Replace("\r", "");
                    }
                    if (err.Message.IndexOf("CRC Error") != -1)
                    {
                        textBoxDiagnosticData.Text = "CRC Error";
                    }

                    Console.WriteLine(err);

                    this.Dispatcher.Invoke((Action)delegate
                    {
                        buttonReadHolding03.IsEnabled = true;

                        dataGridViewHolding.ItemsSource = null;
                        dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                    });
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);

                    textBoxDiagnosticResponse.Text = "Error executing command";
                }
            }
            else
            {
                MessageBox.Show("Valore scelto non valido", "Alert");
            }
        }
        
        private void buttonSendManualDiagnosticQuery_Click(object sender, RoutedEventArgs e)
        {
            // Elimino eventuali spazi in fondo
            while (textBoxDiagnosticFunctionManual.Text[textBoxDiagnosticFunctionManual.Text.Length - 1] == ' ')
            {
                textBoxDiagnosticFunctionManual.Text = textBoxDiagnosticFunctionManual.Text.Substring(0, textBoxDiagnosticFunctionManual.Text.Length - 1);
            }

            if ((bool)radioButtonModeSerial.IsChecked)
            {
                try
                {
                    string[] bytes = textBoxDiagnosticFunctionManual.Text.Split(' ');
                    byte[] buffer = new byte[textBoxDiagnosticFunctionManual.Text.Split(' ').Count()];

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        buffer[i] = byte.Parse(bytes[i], System.Globalization.NumberStyles.HexNumber);
                    }

                    serialPort.ReadTimeout = readTimeout;
                    serialPort.Write(buffer, 0, buffer.Length);

                    try
                    {
                        byte[] response = new byte[100];
                        int Length = serialPort.Read(response, 0, response.Length);

                        textBoxManualDiagnosticResponse.Text = "";

                        for (int i = 0; i < Length; i++)
                        {
                            textBoxManualDiagnosticResponse.Text += response[i].ToString("X").PadLeft(2, '0') + " ";
                        }
                    }
                    catch
                    {
                        textBoxManualDiagnosticResponse.Text = "Read timed out";
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }
            }
            else
            {
                try
                {
                    TcpClient client = new TcpClient(textBoxTcpClientIpAddress.Text, int.Parse(textBoxTcpClientPort.Text));

                    string[] bytes = textBoxDiagnosticFunctionManual.Text.Split(' ');
                    byte[] buffer = new byte[textBoxDiagnosticFunctionManual.Text.Split(' ').Count()];

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        buffer[i] = byte.Parse(bytes[i], System.Globalization.NumberStyles.HexNumber);
                    }

                    NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = readTimeout;
                    stream.Write(buffer, 0, buffer.Length);

                    Thread.Sleep(50);

                    try
                    {
                        byte[] response = new byte[100];
                        int Length = stream.Read(response, 0, response.Length);

                        textBoxManualDiagnosticResponse.Text = "";

                        for (int i = 0; i < Length; i++)
                        {
                            textBoxManualDiagnosticResponse.Text += response[i].ToString("X").PadLeft(2, '0') + " ";
                        }
                    }
                    catch
                    {
                        textBoxManualDiagnosticResponse.Text = "Read timed out";
                    }

                    client.Close();
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }

            }
        }

        private void buttonSendManualDiagnosticQuery_Copy_Click(object sender, RoutedEventArgs e)
        {
            // Elimino eventuali spazi in fondo
            while (textBoxDiagnosticFunctionManual.Text[textBoxDiagnosticFunctionManual.Text.Length - 1] == ' ')
            {
                textBoxDiagnosticFunctionManual.Text = textBoxDiagnosticFunctionManual.Text.Substring(0, textBoxDiagnosticFunctionManual.Text.Length - 1);
            }

            try
            {
                string[] bytes = textBoxDiagnosticFunctionManual.Text.Split(' ');
                byte[] buffer = new byte[textBoxDiagnosticFunctionManual.Text.Split(' ').Count()];

                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = byte.Parse(bytes[i], System.Globalization.NumberStyles.HexNumber);
                }

                byte[] crc = ModBus.Calcolo_CRC(buffer, buffer.Length);

                textBoxDiagnosticFunctionManual.Text += " " + crc[0].ToString("X").PadLeft(2, '0');
                textBoxDiagnosticFunctionManual.Text += " " + crc[1].ToString("X").PadLeft(2, '0');
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }
        }

        private void ButtonExampleTCP_Click(object sender, RoutedEventArgs e)
        {
            textBoxDiagnosticFunctionManual.Text = "00 01 00 00 00 06 01 03 00 02 00 04";
        }

        private void ButtonExampleRTU_Click(object sender, RoutedEventArgs e)
        {
            textBoxDiagnosticFunctionManual.Text = "01 03 00 02 00 04";
        }

        // Funzione che dal valore costruisce il mapping dei bit associato se è stato fornito un mapping
        // Mapping: b0:Status ON/OFF,b1: .....,
        public string GetMappingValue2(UInt16[] value_list, int list_index, string mappings, out string convertedValue)
        {
            convertedValue = "";

            // If mappings null sets mappings to empty string
            if (mappings == null)
                mappings = "";

            try
            {
                string[] labels = new string[16];
                string result = "";

                // 8 bytes tmp per conversioni UInt32/UInt64
                UInt16[] values_ = { 0, 0, 0, 0 };

                int type = 0; // 0 -> Not found, 1 -> bitmap, 2 -> byte map
                int a = -3;

                if (mappings.IndexOf('+') != -1)
                {
                    a = 0; // Sposto le due word da prendere in la di 1
                }

                int index_start = list_index + a;

                for (int i = 0; i < 4; i++)
                {
                    if ((list_index + i) < value_list.Length)
                        values_[i] = value_list[list_index + i];
                }

                if (mappings.IndexOf(':') == -1 && mappings.Length > 0)
                    mappings += ":";

                foreach (string match in mappings.Split(';'))
                {
                    string test = match.Split(':')[0];

                    if (match.Split(':').Length > 1)
                    {
                        // byte (low byte or high byte)
                        if (test.ToLower().IndexOf("byte") == 0)
                        {
                            // Soluzione bug sul fatto che ragiono a blocchi di 8 byte ma prendo gli utlimi 4
                            if (test.ToLower().IndexOf("+") != -1)
                            {
                                values_[3] = values_[0];
                            }

                            if (test.ToLower().IndexOf("-") != -1)
                                labels[0] = "value (byte): " + ((byte)((values_[3] >> 8) & 0xFF)).ToString(); // High Byte
                            else
                                labels[0] = "value (byte): " + ((byte)(values_[3] & 0xFF)).ToString();      // Low Byte

                            convertedValue = labels[0].Replace("value ", "");
                            type = 4;
                        }

                        // bitmap (type 1)
                        else if (test.IndexOf("b") == 0)
                        {
                            int index = int.Parse(test.Substring(1));

                            labels[index] = match.Split(':')[1];
                            type = 1;
                        }

                        // bytemap (type 2)
                        else if (test.IndexOf("B") == 0)
                        {
                            int index = int.Parse(test.Substring(1));

                            labels[index] = match.Split(':')[1];
                            type = 2;

                            //convertedValue = labels[1] + ((values_[3]) >> 8).ToString() + " " + labels[0] + (values_[3] & 0xFF).ToString();
                            convertedValue = String.Format("H: {0} L: {1}", ((values_[3]) >> 8), (values_[3] & 0xFF));
                        }

                        // float (type 3)
                        else if (test.IndexOf("F") == 0 || test.ToLower().IndexOf("float") == 0)
                        {
                            byte[] tmp = new byte[4];

                            // Soluzione bug sul fatto che ragiono a blocchi di 8 byte ma prendo gli utlimi 4
                            if (test.ToLower().IndexOf("+") != -1)
                            {
                                values_[2] = values_[0];
                                values_[3] = values_[1];
                            }

                            if (test.ToLower().IndexOf("-") != -1 || test.ToLower().IndexOf("_swap") != -1)
                            {
                                tmp[0] = (byte)(values_[2] & 0xFF);
                                tmp[1] = (byte)((values_[2] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[3] & 0xFF);
                                tmp[3] = (byte)((values_[3] >> 8) & 0xFF);
                            }
                            else
                            {
                                tmp[0] = (byte)(values_[3] & 0xFF);
                                tmp[1] = (byte)((values_[3] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[2] & 0xFF);
                                tmp[3] = (byte)((values_[2] >> 8) & 0xFF);
                            }

                            labels[0] = "value (float32): " + BitConverter.ToSingle(tmp, 0).ToString(System.Globalization.CultureInfo.InvariantCulture); // + " " + match.Split(':')[1];
                            convertedValue = labels[0].Replace("value ", "");
                            type = 3;
                        }

                        // double (type 7)
                        else if (test.IndexOf("d") == 0 || test.ToLower().IndexOf("double") == 0)
                        {
                            byte[] tmp = new byte[8];

                            if (test.ToLower().IndexOf("-") != -1 || test.ToLower().IndexOf("_swap") != -1)
                            {
                                tmp[0] = (byte)(values_[0] & 0xFF);
                                tmp[1] = (byte)((values_[0] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[1] & 0xFF);
                                tmp[3] = (byte)((values_[1] >> 8) & 0xFF);
                                tmp[4] = (byte)(values_[2] & 0xFF);
                                tmp[5] = (byte)((values_[2] >> 8) & 0xFF);
                                tmp[6] = (byte)(values_[3] & 0xFF);
                                tmp[7] = (byte)((values_[3] >> 8) & 0xFF);
                            }
                            else
                            {
                                tmp[0] = (byte)(values_[3] & 0xFF);
                                tmp[1] = (byte)((values_[3] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[2] & 0xFF);
                                tmp[3] = (byte)((values_[2] >> 8) & 0xFF);
                                tmp[4] = (byte)(values_[1] & 0xFF);
                                tmp[5] = (byte)((values_[1] >> 8) & 0xFF);
                                tmp[6] = (byte)(values_[0] & 0xFF);
                                tmp[7] = (byte)((values_[0] >> 8) & 0xFF);
                            }

                            labels[0] = "value (double64): " + BitConverter.ToDouble(tmp, 0).ToString(System.Globalization.CultureInfo.InvariantCulture); // + " " + match.Split(':')[1];
                            convertedValue = labels[0].Replace("value ", "");
                            type = 7;
                        }

                        // uint64 (type 6)
                        else if (test.ToLower().IndexOf("uint64") == 0)
                        {
                            byte[] tmp = new byte[8];

                            if (test.ToLower().IndexOf("-") != -1 || test.ToLower().IndexOf("_swap") != -1)
                            {
                                tmp[0] = (byte)(values_[0] & 0xFF);
                                tmp[1] = (byte)((values_[0] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[1] & 0xFF);
                                tmp[3] = (byte)((values_[1] >> 8) & 0xFF);
                                tmp[4] = (byte)(values_[2] & 0xFF);
                                tmp[5] = (byte)((values_[2] >> 8) & 0xFF);
                                tmp[6] = (byte)(values_[3] & 0xFF);
                                tmp[7] = (byte)((values_[3] >> 8) & 0xFF);
                            }
                            else
                            {
                                tmp[0] = (byte)(values_[3] & 0xFF);
                                tmp[1] = (byte)((values_[3] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[2] & 0xFF);
                                tmp[3] = (byte)((values_[2] >> 8) & 0xFF);
                                tmp[4] = (byte)(values_[1] & 0xFF);
                                tmp[5] = (byte)((values_[1] >> 8) & 0xFF);
                                tmp[6] = (byte)(values_[0] & 0xFF);
                                tmp[7] = (byte)((values_[0] >> 8) & 0xFF);
                            }

                            labels[0] = "value (uint64): " + BitConverter.ToUInt64(tmp, 0).ToString(); // + "" + match.Split(':')[1];
                            convertedValue = labels[0].Replace("value ", "");
                            type = 6;
                        }

                        // int64 (type 6)
                        else if (test.ToLower().IndexOf("int64") == 0)
                        {
                            byte[] tmp = new byte[8];

                            if (test.ToLower().IndexOf("-") != -1 || test.ToLower().IndexOf("_swap") != -1)
                            {
                                tmp[0] = (byte)(values_[0] & 0xFF);
                                tmp[1] = (byte)((values_[0] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[1] & 0xFF);
                                tmp[3] = (byte)((values_[1] >> 8) & 0xFF);
                                tmp[4] = (byte)(values_[2] & 0xFF);
                                tmp[5] = (byte)((values_[2] >> 8) & 0xFF);
                                tmp[6] = (byte)(values_[3] & 0xFF);
                                tmp[7] = (byte)((values_[3] >> 8) & 0xFF);
                            }
                            else
                            {
                                tmp[0] = (byte)(values_[3] & 0xFF);
                                tmp[1] = (byte)((values_[3] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[2] & 0xFF);
                                tmp[3] = (byte)((values_[2] >> 8) & 0xFF);
                                tmp[4] = (byte)(values_[1] & 0xFF);
                                tmp[5] = (byte)((values_[1] >> 8) & 0xFF);
                                tmp[6] = (byte)(values_[0] & 0xFF);
                                tmp[7] = (byte)((values_[0] >> 8) & 0xFF);
                            }

                            labels[0] = "value (int64): " + BitConverter.ToInt64(tmp, 0).ToString(); // + "" + match.Split(':')[1];
                            convertedValue = labels[0].Replace("value ", "");
                            type = 6;
                        }

                        // uint32 (type 5)
                        else if (test.ToLower().IndexOf("uint32") == 0)
                        {
                            byte[] tmp = new byte[4];

                            // Soluzione bug sul fatto che ragiono a blocchi di 8 byte ma prendo gli utlimi 4
                            if (test.ToLower().IndexOf("+") != -1)
                            {
                                values_[2] = values_[0];
                                values_[3] = values_[1];
                            }

                            if (test.ToLower().IndexOf("-") != -1 || test.ToLower().IndexOf("_swap") != -1)
                            {
                                tmp[0] = (byte)(values_[2] & 0xFF);
                                tmp[1] = (byte)((values_[2] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[3] & 0xFF);
                                tmp[3] = (byte)((values_[3] >> 8) & 0xFF);
                            }
                            else
                            {
                                tmp[0] = (byte)(values_[3] & 0xFF);
                                tmp[1] = (byte)((values_[3] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[2] & 0xFF);
                                tmp[3] = (byte)((values_[2] >> 8) & 0xFF);
                            }

                            labels[0] = "value (uint32): " + BitConverter.ToUInt32(tmp, 0).ToString(); // + "" + match.Split(':')[1];
                            convertedValue = labels[0].Replace("value ", "");
                            type = 5;
                        }

                        // int32 (type 5)
                        else if (test.ToLower().IndexOf("int32") == 0)
                        {
                            byte[] tmp = new byte[4];

                            // Soluzione bug sul fatto che ragiono a blocchi di 8 byte ma prendo gli utlimi 4
                            if (test.ToLower().IndexOf("+") != -1)
                            {
                                values_[2] = values_[0];
                                values_[3] = values_[1];
                            }

                            if (test.ToLower().IndexOf("-") != -1 || test.ToLower().IndexOf("_swap") != -1)
                            {
                                tmp[0] = (byte)(values_[2] & 0xFF);
                                tmp[1] = (byte)((values_[2] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[3] & 0xFF);
                                tmp[3] = (byte)((values_[3] >> 8) & 0xFF);
                            }
                            else
                            {
                                tmp[0] = (byte)(values_[3] & 0xFF);
                                tmp[1] = (byte)((values_[3] >> 8) & 0xFF);
                                tmp[2] = (byte)(values_[2] & 0xFF);
                                tmp[3] = (byte)((values_[2] >> 8) & 0xFF);
                            }

                            labels[0] = "value (int32): " + BitConverter.ToInt32(tmp, 0).ToString(); // + "" + match.Split(':')[1];
                            convertedValue = labels[0].Replace("value ", "");
                            type = 5;
                        }

                        // uint16 (type 4)
                        else if (test.ToLower().IndexOf("uint") == 0 || test.ToLower().IndexOf("uint16") == 0)
                        {
                            byte[] tmp = new byte[2];

                            // Soluzione bug sul fatto che ragiono a blocchi di 8 byte ma prendo gli utlimi 4
                            if (test.ToLower().IndexOf("+") != -1)
                            {
                                values_[3] = values_[0];
                            }

                            tmp[0] = (byte)(values_[3] & 0xFF);
                            tmp[1] = (byte)((values_[3] >> 8) & 0xFF);

                            labels[0] = "value (uint16): " + BitConverter.ToUInt16(tmp, 0).ToString(); // + "" + match.Split(':')[1];
                            convertedValue = labels[0].Replace("value ", "");
                            type = 4;
                        }

                        // int16 (type 4)
                        else if (test.ToLower().IndexOf("int") == 0 || test.ToLower().IndexOf("int16") == 0)
                        {
                            byte[] tmp = new byte[2];

                            // Soluzione bug sul fatto che ragiono a blocchi di 8 byte ma prendo gli utlimi 4
                            if (test.ToLower().IndexOf("+") != -1)
                            {
                                values_[3] = values_[0];
                            }

                            tmp[0] = (byte)(values_[3] & 0xFF);
                            tmp[1] = (byte)((values_[3] >> 8) & 0xFF);

                            labels[0] = "value (int16): " + BitConverter.ToInt16(tmp, 0).ToString(); // + "" + match.Split(':')[1];
                            convertedValue = labels[0].Replace("value ", "");
                            type = 4;
                        }

                        // String (type 255)
                        else if (test.ToLower().IndexOf("string") == 0)
                        {
                            int length = int.Parse(test.Split(')')[0].Split('(')[1].Split(',')[0]);
                            int offset = 0;

                            if (test.Split(')')[0].Split('(')[1].IndexOf(',') != -1)
                            {
                                int.TryParse(test.Split(')')[0].Split('(')[1].Split(',')[1], out offset);
                            }

                            byte[] tmp = new byte[length];
                            String output = "";

                            int start = 0;
                            int stop = 0;

                            start = list_index - (Math.Abs(offset) / 2);
                            stop = list_index + (length / 2) - (Math.Abs(offset) / 2);

                            for (int i = start; i < stop; i += 1)
                            {
                                UInt16 currrValue = value_list[i];

                                try
                                {
                                    ASCIIEncoding ascii = new ASCIIEncoding();

                                    if (test.ToLower().IndexOf("_swap") != -1)
                                    {
                                        output += ascii.GetString(new byte[] { (byte)(currrValue & 0xFF), (byte)((currrValue >> 8) & 0xFF) });
                                    }
                                    else
                                    {
                                        output += ascii.GetString(new byte[] { (byte)((currrValue >> 8) & 0xFF), (byte)(currrValue & 0xFF) });
                                    }
                                }
                                catch (Exception err)
                                {
                                    Console.WriteLine(err);
                                }
                            }

                            labels[0] = "value (String): " + output;
                            convertedValue = labels[0].Replace("value ", "");
                            type = 7;
                        }
                    }

                    // etichetta generica senza mapping
                    if (mappings.Length < 2)
                    {
                        labels[0] = "value (dec): " + value_list[index_start + a].ToString() + "\nvalue (hex): 0x" + values_[3].ToString("X").PadLeft(4, '0') + "\nvalue (bin): " + Convert.ToString(values_[1] >> 8, 2).PadLeft(8, '0') + " " + Convert.ToString((UInt16)((UInt16)(values_[1]) << 8) >> 8, 2).PadLeft(8, '0');
                        //convertedValue = labels[0];
                        type = 255;
                    }
                }

                // bitmap
                if (type == 1)
                {
                    /*if (value_list[index_start + Math.Abs(a)].Notes != null)
                    {
                        if (value_list[index_start + Math.Abs(a)].Notes.Length > 0)
                        {
                            result = value_list[index_start + Math.Abs(a)].Notes + "\n\n";
                        }
                    }*/

                    for (int i = 15; i >= 0; i--)
                    {
                        if ((values_[3] & (1 << i)) > 0)
                        {
                            if (i < 10)
                            {
                                result += "bit   " + i.ToString() + ":  1 - " + labels[i];
                            }
                            else
                            {
                                result += "bit " + i.ToString() + ":  1 - " + labels[i];
                            }
                        }
                        else
                        {
                            if (i < 10)
                            {
                                result += "bit   " + i.ToString() + ":  0 - " + labels[i];
                            }
                            else
                            {
                                result += "bit " + i.ToString() + ":  0 - " + labels[i];
                            }
                        }

                        if (i > 0)
                        {
                            result += "\n";
                        }
                    }
                }

                // bytemap
                else if (type == 2)
                {
                    /*if (value_list[index_start + Math.Abs(a)].Notes != null)
                    {
                        if (value_list[index_start + Math.Abs(a)].Notes.Length > 0)
                        {
                            result = value_list[index_start + Math.Abs(a)].Notes + "\n\n";
                        }
                    }*/

                    for (int i = 1; i >= 0; i--)
                    {
                        result += "byte " + i.ToString() + ": " + ((values_[3] >> (i * 8)) & 0xFF).ToString() + " - " + labels[i];

                        if (i == 1)
                        {
                            result += "\n";
                        }
                    }
                }

                // conversioni interi
                else if (type == 3 || type == 4 || type == 5 || type == 6 || type == 7)
                {
                    /*if (value_list[index_start - a].Notes != null)
                    {
                        if (value_list[index_start - a].Notes.Length > 0)
                        {
                            result = value_list[index_start - a].Notes + "\n\n";
                        }
                    }*/

                    result += labels[0];
                }

                // etichetta generica
                else if (type == 255)
                {
                    /*if (value_list[index_start - a].Notes != null)
                    {
                        if (value_list[index_start - a].Notes.Length > 0)
                        {
                            result = value_list[index_start - a].Notes + "\n\n";
                        }
                    }*/

                    result += labels[0];
                }

                return result;
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                return "";
            }
        }

        // Funzione inserimento righe nelle collections
        public void insertRowsTable(ObservableCollection<ModBus_Item> tab_1, IEnumerable<ModBus_Item> template, int template_offset, uint address_start, UInt16[] response, String cellBackGround, String formatRegister, String formatVal)
        {
            this.Dispatcher.Invoke((Action)delegate
            {
                tab_1.Clear();
            });

            if (response != null)
            {
                for (int i = 0; i < response.Length; i++)
                {
                    ModBus_Item row = new ModBus_Item();

                    // Cella con numero del registro
                    if (formatRegister == "DEC" || formatRegister == null)
                    {
                        row.Register = (address_start + i).ToString();
                        row.RegisterUInt = (UInt16)(address_start + i);
                    }
                    else
                    {
                        row.Register = "0x" + (address_start + i).ToString("X").PadLeft(4, '0');
                    }

                    // Cella con valore letto
                    if (formatVal == "DEC" || formatVal == null)
                    {
                        row.Value = (response[i]).ToString();
                    }
                    else
                    {
                        row.Value = "0x" + (response[i]).ToString("X").PadLeft(4, '0');
                    }

                    // Colorazione celle
                    if (colorMode)
                    {
                        if (response[i] > 0)
                        {
                            row.Foreground = darkMode ? ForeGroundDarkStr : ForeGroundLightStr;
                            row.Background = cellBackGround;
                        }
                        else
                        {
                            row.Foreground = darkMode ? ForeGroundDarkStr : ForeGroundLightStr;
                            row.Background = darkMode ? BackGroundDarkStr : Brushes.White.ToString();
                        }
                    }
                    else
                    {
                        if (i % 2 == 0)
                        {
                            this.Dispatcher.Invoke((Action)delegate
                            {
                                row.Foreground = darkMode ? BackGroundDarkStr : ForeGroundLightStr;
                                row.Background = cellBackGround;
                            });
                        }
                    }

                    ModBus_Item found = template.FirstOrDefault(x => (x.RegisterUInt + template_offset) == (address_start + i));
                    if (found != null)
                    {
                        String convertedValue;
                        row.Mappings = GetMappingValue2(response, i, found.Mappings, out convertedValue);
                        row.Notes = found.Notes;
                        row.ValueConverted = convertedValue;
                    }
                    else
                    {
                        String convertedValue;
                        row.Mappings = GetMappingValue2(response, i, "", out convertedValue);
                        row.ValueConverted = convertedValue;
                    }

                    // Il valore in binario lo metto sempre tanto poi nelle coils ed inputs è nascosto
                    row.ValueBin = Convert.ToString(response[i] >> 8, 2).PadLeft(8, '0') + " " + Convert.ToString((UInt16)(response[i] << 8) >> 8, 2).PadLeft(8, '0');

                    this.Dispatcher.Invoke((Action)delegate
                    {
                        tab_1.Add(row);
                    });
                }
            }
        }

        // ----------------------------------------------------------------------------------
        // --------------------------- OTHERS -----------------------------------------------
        // ----------------------------------------------------------------------------------

        // Altri pulsanti della grafica
        private void guidaToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(localPath + "\\Manuali\\Guida_ModBus_Client_" + textBoxCurrentLanguage.Text + ".pdf");
            }
            catch
            {
                MessageBox.Show("Ancora da scrivere :-)", "Hey");
            }
        }

        private void infoToolStripMenuItem1_Click(object sender, RoutedEventArgs e)
        {
            Info info = new Info(title, version);

            info.Show();
        }

        private void richTextBoxAppend(RichTextBox richTextBox, String append)
        {
            richTextBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + append + "\n");
        }

        private void buttonClearSerialStatus_Click(object sender, RoutedEventArgs e)
        {
            richTextBoxStatus.Document.Blocks.Clear();
            richTextBoxStatus.AppendText("\n");
        }

        private void esciToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //SaveConfiguration_v1(false);
            SaveConfiguration_v2(false);
            this.Close();
        }

        private void impostazioniToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            tabControlMain.SelectedIndex = 7;
        }

        private void buttonColorCellRead_Click(object sender, RoutedEventArgs e)
        {
            if (colorDialogBox.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if(darkMode) 
                    colorDefaultReadCell_Dark = new SolidColorBrush(Color.FromArgb(colorDialogBox.Color.A, colorDialogBox.Color.R, colorDialogBox.Color.G, colorDialogBox.Color.B));
                else
                    colorDefaultReadCell_Light = new SolidColorBrush(Color.FromArgb(colorDialogBox.Color.A, colorDialogBox.Color.R, colorDialogBox.Color.G, colorDialogBox.Color.B));
                
                colorDefaultReadCell = new SolidColorBrush(Color.FromArgb(colorDialogBox.Color.A, colorDialogBox.Color.R, colorDialogBox.Color.G, colorDialogBox.Color.B));
                labelColorCellRead.Background = colorDefaultReadCell;
                colorDefaultReadCellStr = colorDefaultReadCell.ToString();
            }
        }

        private void buttonCellWrote_Click(object sender, RoutedEventArgs e)
        {
            if (colorDialogBox.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if(darkMode)
                    colorDefaultWriteCell_Dark = new SolidColorBrush(Color.FromArgb(colorDialogBox.Color.A, colorDialogBox.Color.R, colorDialogBox.Color.G, colorDialogBox.Color.B));
                else
                    colorDefaultWriteCell_Light = new SolidColorBrush(Color.FromArgb(colorDialogBox.Color.A, colorDialogBox.Color.R, colorDialogBox.Color.G, colorDialogBox.Color.B));

                colorDefaultWriteCell = new SolidColorBrush(Color.FromArgb(colorDialogBox.Color.A, colorDialogBox.Color.R, colorDialogBox.Color.G, colorDialogBox.Color.B));
                labelColorCellWrote.Background = colorDefaultWriteCell;
            }
        }

        private void buttonColorCellError_Click(object sender, RoutedEventArgs e)
        {
            if (colorDialogBox.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if(darkMode)
                    colorErrorCell_Dark = new SolidColorBrush(Color.FromArgb(colorDialogBox.Color.A, colorDialogBox.Color.R, colorDialogBox.Color.G, colorDialogBox.Color.B));
                else
                    colorErrorCell_Light = new SolidColorBrush(Color.FromArgb(colorDialogBox.Color.A, colorDialogBox.Color.R, colorDialogBox.Color.G, colorDialogBox.Color.B));

                colorErrorCell = new SolidColorBrush(Color.FromArgb(colorDialogBox.Color.A, colorDialogBox.Color.R, colorDialogBox.Color.G, colorDialogBox.Color.B));
                labelColorCellError.Background = colorErrorCell;
            }
        }

        // Salvataggio pacchetti inviati
        private void buttonExportSentPackets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveFileDialogBox = new SaveFileDialog();

                saveFileDialogBox.DefaultExt = ".txt";
                saveFileDialogBox.AddExtension = false;
                saveFileDialogBox.FileName = "Pacchetti_inviati_" + DateTime.Now.Day.ToString().PadLeft(2, '0') + "." + DateTime.Now.Month.ToString().PadLeft(2, '0') + "." + DateTime.Now.Year.ToString().PadLeft(2, '0');
                saveFileDialogBox.Filter = "Text|*.txt|Log|*.log";
                saveFileDialogBox.Title = "Salva Log pacchetti inviati";
                saveFileDialogBox.ShowDialog();

                if (saveFileDialogBox.FileName != "")
                {
                    StreamWriter writer = new StreamWriter(saveFileDialogBox.OpenFile());

                    TextRange textRange = new TextRange(
                        // TextPointer to the start of content in the RichTextBox.
                        richTextBoxPackets.Document.ContentStart,
                        // TextPointer to the end of content in the RichTextBox.
                        richTextBoxPackets.Document.ContentEnd
                    );


                    writer.Write(textRange.Text);
                    writer.Dispose();
                    writer.Close();
                }
            }
            catch { }
        }

        // Salvataggio pacchetti ricevuti
        private void buttonExportReceivedPackets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveFileDialogBox = new SaveFileDialog();

                saveFileDialogBox.DefaultExt = ".txt";
                saveFileDialogBox.AddExtension = false;
                saveFileDialogBox.FileName = "Pacchetti_ricevuti_" + DateTime.Now.Day.ToString().PadLeft(2, '0') + "." + DateTime.Now.Month.ToString().PadLeft(2, '0') + "." + DateTime.Now.Year.ToString().PadLeft(2, '0');
                saveFileDialogBox.Filter = "Text|*.txt|Log|*.log";
                saveFileDialogBox.Title = "Salva Log pacchetti inviati";
                saveFileDialogBox.ShowDialog();

                if (saveFileDialogBox.FileName != "")
                {
                    StreamWriter writer = new StreamWriter(saveFileDialogBox.OpenFile());

                    TextRange textRange = new TextRange(
                        // TextPointer to the start of content in the RichTextBox.
                        richTextBoxPackets.Document.ContentStart,
                        // TextPointer to the end of content in the RichTextBox.
                        richTextBoxPackets.Document.ContentEnd
                    );


                    writer.Write(textRange.Text);
                    writer.Dispose();
                    writer.Close();
                }
            }
            catch { }
        }

        private void buttonWriteHolding06_b_Click(object sender, RoutedEventArgs e)
        {
            buttonWriteHolding06_b.IsEnabled = false;

            Thread t = new Thread(new ThreadStart(writeHoldingRegister_02));
            t.Priority = threadPriority;
            t.Start();
        }

        public void writeHoldingRegister_02()
        {
            try
            {
                uint address_start = P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(textBoxHoldingAddress06_b_, comboBoxHoldingAddress06_b_);

                if (address_start > 9999 && correctModbusAddressAuto)    //Se indirizzo espresso in 30001+ imposto offset a 0
                {
                    address_start = address_start - 40001;
                }

                bool? result = ModBus.presetSingleRegister_06(byte.Parse(textBoxModbusAddress_), address_start, P.uint_parser(textBoxHoldingValue06_b_, comboBoxHoldingValue06_b_), readTimeout);

                if (result != null)
                {
                    if (result == true)
                    {
                        UInt16[] value = { (UInt16)P.uint_parser(textBoxHoldingValue06_b_, comboBoxHoldingValue06_b_) };

                        // Cancello la tabella e inserisco le nuove righe
                        if (useOffsetInTable)
                        {
                            insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, address_start - P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_), value, colorDefaultWriteCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                        }
                        else
                        {
                            insertRowsTable(list_holdingRegistersTable, list_template_holdingRegistersTable, template_HoldingOffset, address_start, value, colorDefaultWriteCellStr, comboBoxHoldingRegistri_, comboBoxHoldingValori_);
                        }
                    }
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding06_b.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_holdingRegistersTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_holdingRegistersTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_holdingRegistersTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding06_b.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_holdingRegistersTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding06_b.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
        }

        private void checkBoxViewTableWithoutOffset_CheckedChanged(object sender, RoutedEventArgs e)
        {
            labelOffsetHiddenCoils.Visibility = (bool)checkBoxViewTableWithoutOffset.IsChecked ? Visibility.Visible : Visibility.Hidden;
            labelOffsetHiddenInput.Visibility = (bool)checkBoxViewTableWithoutOffset.IsChecked ? Visibility.Visible : Visibility.Hidden;
            labelOffsetHiddenInputRegister.Visibility = (bool)checkBoxViewTableWithoutOffset.IsChecked ? Visibility.Visible : Visibility.Hidden;
            labelOffsetHiddenHolding.Visibility = (bool)checkBoxViewTableWithoutOffset.IsChecked ? Visibility.Visible : Visibility.Hidden;

            useOffsetInTable = (bool)checkBoxViewTableWithoutOffset.IsChecked;
        }

        //-----------------------------------------------------------------------------------------
        //------------------FUNZIONE PER AGGIORNARE ELEMENTI GRAFICA DA ALTRI FORM-----------------
        //-----------------------------------------------------------------------------------------

        public void toolSTripMenuEnable(bool enable)
        {
            //simFORMToolStripMenuItem.IsEnabled = enable;
        }

        private void buttonClearAll_Click(object sender, RoutedEventArgs e)
        {
            count_log = 0;

            richTextBoxPackets.Document.Blocks.Clear();
            richTextBoxPackets.AppendText("\n");
        }
        
        private void gestisciDatabaseToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DatabaseManager window = new DatabaseManager(this);
            window.Show();
        }

        private void comboBoxHoldingValue06_b_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingValue06_b_ = comboBoxHoldingValue06_b.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void menuItemToolBit_Click(object sender, RoutedEventArgs e)
        {
            ComandiBit sim = new ComandiBit(ModBus, this);
            sim.Show();
        }

        private void menuItemToolByte_Click(object sender, RoutedEventArgs e)
        {
            ComandiByte sim_byte = new ComandiByte(ModBus, this);
            sim_byte.Show();
        }

        private void menuItemToolWord_Click(object sender, RoutedEventArgs e)
        {
            ComandiWord sim_word = new ComandiWord(ModBus, this);
            sim_word.Show();
        }

        private void salvaConfigurazioneAttualeNelDatabaseToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            String prevoiusPath = pathToConfiguration;

            Salva_impianto form_save = new Salva_impianto(this);
            form_save.ShowDialog();

            //Controllo il risultato del form
            if ((bool)form_save.DialogResult)
            {
                //SaveConfiguration_v1(false);
                SaveConfiguration_v2(false);

                pathToConfiguration = form_save.path;                

                if (pathToConfiguration != defaultPathToConfiguration)
                {
                    this.Title = title + " " + version + " - File: " + pathToConfiguration;
                }


                Directory.CreateDirectory(localPath + "\\Json\\" + pathToConfiguration);

                String[] fileNames = Directory.GetFiles(localPath + "\\Json\\" + prevoiusPath + "\\");

                for (int i = 0; i < fileNames.Length; i++)
                {
                    String newFile = localPath + "\\Json\\" + pathToConfiguration + fileNames[i].Substring(fileNames[i].LastIndexOf('\\'));

                    Console.WriteLine("Copying file: " + fileNames[i] + " to " + newFile);
                    File.Copy(fileNames[i], newFile);
                }
            }
        }

        private void caricaConfigurazioneDaDatabaseToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Carica_impianto form_load = new Carica_impianto(defaultPathToConfiguration, this);
            form_load.ShowDialog();

            //Controllo il risultato del form
            if ((bool)form_load.DialogResult)
            {
                LoadProfile(form_load.path);
            }
        }

        public void LoadProfile(string profile)
        {
            if (!Directory.Exists(localPath + "\\Json\\" + pathToConfiguration))
            {
                Console.WriteLine("Profile: " + profile + " not found");
                return;
            }

           SaveConfiguration_v2(false);

            pathToConfiguration = profile;

            if (pathToConfiguration != defaultPathToConfiguration)
            {
                this.Title = title + " " + version + " - File: " + pathToConfiguration;
            }

            // Se esiste una nuova versione del file di configurazione uso l'ultima, altrimenti carico il modello precedente
            if (File.Exists(localPath + "\\Json\\" + pathToConfiguration + "\\Config.json"))
            {
                LoadConfiguration_v2();
            }
            else
            {
                LoadConfiguration_v1();
            }

            if ((bool)radioButtonModeSerial.IsChecked)
            {
                buttonSerialActive.Focus();
            }
            else
            {
                buttonTcpActive.Focus();
            }
        }

        private void apriTemplateEditor_Click(object sender, RoutedEventArgs e)
        {
            TemplateEditor window = new TemplateEditor(this);
            window.Show();
        }

        private void caricaToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //SaveConfiguration_v1(false);
            SaveConfiguration_v2(false);

            // Se esiste una nuova versione del file di configurazione uso l'ultima, altrimenti carico il modello precedente
            if (File.Exists(localPath + "\\Json\\" + pathToConfiguration + "\\Config.json"))
            {
                LoadConfiguration_v2();
            }
            else
            {
                LoadConfiguration_v1();
            }
        }

        private void buttonLoopCoils01_Click(object sender, RoutedEventArgs e)
        {
            loopCoils01 = !loopCoils01;
            buttonLoopCoils01.Content = loopCoils01 ? "Stop" : "Loop";
            buttonLoopCoils01.Background = loopCoils01 ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
            checkLoop();
        }
       
        private void buttonLoopCoilsRange_Click(object sender, RoutedEventArgs e)
        {
            loopCoilsRange = !loopCoilsRange;
            buttonLoopCoilsRange.Content = loopCoilsRange ? "Stop" : "Loop";
            buttonLoopCoilsRange.Background = loopCoilsRange ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
            checkLoop();
        }

        private void buttonLoopInput02_Click(object sender, RoutedEventArgs e)
        {
            loopInput02 = !loopInput02;
            buttonLoopInput02.Content = loopInput02 ? "Stop" : "Loop";
            buttonLoopInput02.Background = loopInput02 ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
            checkLoop();
        }

        private void buttonLoopInputRange_Click(object sender, RoutedEventArgs e)
        {
            loopInputRange = !loopInputRange;
            buttonLoopInputRange.Content = loopInputRange ? "Stop" : "Loop";
            buttonLoopInputRange.Background = loopInputRange ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
            checkLoop();
        }

        private void buttonLoopInputRegister04_Click(object sender, RoutedEventArgs e)
        {
            loopInputRegister04 = !loopInputRegister04;
            buttonLoopInputRegister04.Content = loopInputRegister04 ? "Stop" : "Loop";
            buttonLoopInputRegister04.Background = loopInputRegister04 ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
            checkLoop();
        }

        private void buttonLoopInputRegisterRange_Click(object sender, RoutedEventArgs e)
        {
            loopInputRegisterRange = !loopInputRegisterRange;
            buttonLoopInputRegisterRange.Content = loopInputRegisterRange ? "Stop" : "Loop";
            buttonLoopInputRegisterRange.Background = loopInputRegisterRange ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
            checkLoop();
        }

        private void buttonLoopHolding03_Click(object sender, RoutedEventArgs e)
        {
            loopHolding03 = !loopHolding03;
            buttonLoopHolding03.Content = loopHolding03 ? "Stop" : "Loop";
            buttonLoopHolding03.Background = loopHolding03 ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
            checkLoop();
        }

        private void buttonLoopHoldingRange_Click(object sender, RoutedEventArgs e)
        {
            loopHoldingRange = !loopHoldingRange;
            buttonLoopHoldingRange.Content = loopHoldingRange ? "Stop" : "Loop";
            buttonLoopHoldingRange.Background = loopHoldingRange ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));
            checkLoop();
        }

        public void disableAllLoops()
        {
            // Fermo eventuali loop
            loopCoils01 = false;
            loopCoilsRange = false;
            loopInput02 = false;
            loopInputRange = false;
            loopInputRegister04 = false;
            loopInputRegisterRange = false;
            loopHolding03 = false;
            loopHoldingRange = false;

            buttonLoopCoils01.Content = loopCoils01 ? "Stop" : "Loop";
            buttonLoopCoils01.Background = loopCoils01 ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));

            buttonLoopCoilsRange.Content = loopCoilsRange ? "Stop" : "Loop";
            buttonLoopCoilsRange.Background = loopCoilsRange ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));

            buttonLoopInput02.Content = loopInput02 ? "Stop" : "Loop";
            buttonLoopInput02.Background = loopInput02 ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));

            buttonLoopInputRange.Content = loopInputRange ? "Stop" : "Loop";
            buttonLoopInputRange.Background = loopInputRange ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));

            buttonLoopInputRegister04.Content = loopInputRegister04 ? "Stop" : "Loop";
            buttonLoopInputRegister04.Background = loopInputRange ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));

            buttonLoopInputRegisterRange.Content = loopInputRegisterRange ? "Stop" : "Loop";
            buttonLoopInputRegisterRange.Background = loopInputRegisterRange ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));

            buttonLoopHolding03.Content = loopHolding03 ? "Stop" : "Loop";
            buttonLoopHolding03.Background = loopHolding03 ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));

            buttonLoopHoldingRange.Content = loopHoldingRange ? "Stop" : "Loop";
            buttonLoopHoldingRange.Background = loopHoldingRange ? Brushes.LightGreen : new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD));

            checkLoop();
        }

        public void loopPollingRegisters()
        {
            while (loopThreadRunning)
            {
                // Coils
                if (loopCoils01)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        buttonReadCoils01_Click(null, null);
                    });
                }

                if (loopCoilsRange)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        buttonReadCoilsRange_Click(null, null);
                    });
                }

                // Inputs
                if (loopInput02)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        buttonReadInput02_Click(null, null);
                    });
                }

                if (loopInputRange)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        buttonReadInputRange_Click(null, null);
                    });
                }

                // Input Registers
                if (loopInputRegister04)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        buttonReadInputRegister04_Click(null, null);
                    });
                }

                if (loopInputRegisterRange)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        buttonReadInputRegisterRange_Click(null, null);
                    });
                }

                // Holding Registers
                if (loopHolding03)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        buttonReadHolding03_Click(null, null);
                    });
                }

                if (loopHoldingRange)
                {
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        buttonReadHoldingRange_Click(null, null);
                    });
                }

                // Pausa tra una query e la successiva
                Thread.Sleep(pauseLoop);
            }
        }

        public bool checkLoopStartStop()
        {
            if(loopCoils01 || loopCoilsRange || loopInput02 || loopInputRange || loopInputRegister04 || loopInputRegisterRange || loopHolding03 || loopHoldingRange)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    
        public void checkLoop()
        {
            if (checkLoopStartStop() != loopThreadRunning)
            {
                // Thread già attivo, lo fermo
                if (loopThreadRunning)
                {
                    Console.WriteLine("Fermo il thread di polling");
                    loopThreadRunning = false;

                    Thread.Sleep(100);

                    try
                    {
                        if (threadLoopQuery.IsAlive)
                        {
                            Console.WriteLine("threadLoopQuery Abort");
                            threadLoopQuery.Abort();
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err);
                    }


                }

                // Thread non attivo, lo avvio
                else
                {
                    Console.WriteLine("Avvio il thread di polling");
                    loopThreadRunning = true;
                    threadLoopQuery = new Thread(new ThreadStart(loopPollingRegisters));
                    threadLoopQuery.IsBackground = true;
                    threadLoopQuery.Priority = ThreadPriority.Normal;
                    threadLoopQuery.Start();
                }
            }
        }

        private void TextBoxPollingINterval_TextChanged(object sender, TextChangedEventArgs e)
        {
            int interval = 0;

            if(int.TryParse(TextBoxPollingInterval.Text, out interval))
            {
                if(interval >= 500)
                {
                    pauseLoop = interval;
                }
            }
        }

        private void buttonPingIp_Click(object sender, RoutedEventArgs e)
        {
            Ping p1 = new Ping();
            PingReply PR = p1.Send(textBoxTcpClientIpAddress.Text, 500);

            // check when the ping is not success
            if (!PR.Status.ToString().Equals("Success"))
            {
                buttonPingIp.Background = Brushes.Red;

                // Rimosso box per comodita, meglio sfondo sul bottone
                //DoEvents();
                //MessageBox.Show("Ping failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                richTextBoxAppend(richTextBoxStatus, "Ping failed");
            }
            else
            {
                buttonPingIp.Background = Brushes.LightGreen;

                // Rimosso box per comodita, meglio sfondo sul bottone
                //DoEvents();
                //MessageBox.Show("Ping ok.\nResponse time: " + PR.RoundtripTime + "ms", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                richTextBoxAppend(richTextBoxStatus, "Ping Ok - Response time: " + PR.RoundtripTime + "ms");
            }
        }

        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new Action(delegate { }));
        }

        private void buttonClearHoldingReg_Click(object sender, RoutedEventArgs e)
        {
            list_holdingRegistersTable.Clear();
            //dataGridViewHolding.Items.Refresh();
        }

        private void buttonClearInputReg_Click(object sender, RoutedEventArgs e)
        {
            list_inputRegistersTable.Clear();
            //dataGridViewInputRegister.Items.Clear();
        }

        private void buttonClearInput_Click(object sender, RoutedEventArgs e)
        {
            list_inputsTable.Clear();
            //dataGridViewInput.Items.Clear();
        }

        private void buttonClearCoils_Click(object sender, RoutedEventArgs e)
        {
            list_coilsTable.Clear();
            //dataGridViewCoils.Items.Clear();
        }

        private void licenseToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            License window = new License();
            window.Show();
        }

        private void CheckBoxPinWIndow_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = (bool)CheckBoxPinWIndow.IsChecked;
        }

        private void dataGridViewHolding_KeyUp(object sender, DataGridCellEditEndingEventArgs e)
        {
            if ((bool)CheckBoxSendValuesOnEditHoldingTable.IsChecked)
            {
                try
                {
                    var tmp = e.EditingElement as TextBox;
                    dataGridViewHolding.SelectedItem = (ModBus_Item)e.Row.Item;

                    UInt16 out_;
                    if(UInt16.TryParse(tmp.Text, out out_))
                    {
                        Thread t = new Thread(new ParameterizedThreadStart(writeRegisterDatagrid));
                        t.Priority = threadPriority;
                        t.Start(tmp.Text);
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }
            }
        }

        public void writeRegisterDatagrid(object obj)
        {
            try
            {
                ModBus_Item currentItem = new ModBus_Item();

                this.Dispatcher.Invoke((Action)delegate
                {
                    currentItem = (ModBus_Item)dataGridViewHolding.SelectedItem;
                    currentItem.Value = obj.ToString();
                });

                // Debug
                Console.WriteLine("Register: " + currentItem.Register);
                Console.WriteLine("Value: " + currentItem.Value);
                Console.WriteLine("Notes: " + currentItem.Notes);

                uint address_start = P.uint_parser(textBoxHoldingOffset_, comboBoxHoldingOffset_) + P.uint_parser(currentItem.Register, comboBoxHoldingRegistri_);

                if (address_start > 9999 && correctModbusAddressAuto)    //Se indirizzo espresso in 30001+ imposto offset a 0
                {
                    address_start = address_start - 40001;
                }

                uint value_ = P.uint_parser(currentItem.Value, comboBoxHoldingValori_);
                bool? result = ModBus.presetSingleRegister_06(byte.Parse(textBoxModbusAddress_), address_start, value_, readTimeout);

                if (result != null)
                {
                    if (result == true)
                    {
                        String[] value = { value_.ToString() };

                        this.Dispatcher.Invoke((Action)delegate
                        {
                            currentItem.ValueBin = Convert.ToString(value_ >> 8, 2).PadLeft(8, '0') + " " + Convert.ToString((UInt16)(value_ << 8) >> 8, 2).PadLeft(8, '0');
                            currentItem.Foreground = ForeGroundLight.ToString();
                            currentItem.Background = colorDefaultWriteCell.ToString();
                        });
                    }
                    else
                    {
                        SetTableCrcError(list_holdingRegistersTable);
                    }
                }
                else
                {
                    SetTableTimeoutError(list_holdingRegistersTable);
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    dataGridViewHolding.Items.Refresh();
                    dataGridViewHolding.SelectedItem = currentItem;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_holdingRegistersTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_holdingRegistersTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_holdingRegistersTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding06_b.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_holdingRegistersTable);
                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteHolding06_b.IsEnabled = true;

                    dataGridViewHolding.ItemsSource = null;
                    dataGridViewHolding.ItemsSource = list_holdingRegistersTable;
                });
            }
        }

        public void MenuItemLanguage_Click(object sender, EventArgs e)
        {
            var currMenuItem = (MenuItem)sender;

            language = currMenuItem.Header.ToString();

            // Passo fuori le lingue disponibili nel menu
            foreach (MenuItem tmp in languageToolStripMenu.Items)
            {
                tmp.IsChecked = tmp == currMenuItem;
            }

            // Carico template selezionato
            textBoxCurrentLanguage.Text = currMenuItem.Header.ToString();
            //lang.loadLanguageTemplate(currMenuItem.Header.ToString());
        }

        private void dataGridViewCoils_KeyUp(object sender, DataGridCellEditEndingEventArgs e)
        {
            if ((bool)CheckBoxSendValuesOnEditCoillsTable.IsChecked)
            {
                try
                {
                    var tmp = e.EditingElement as TextBox;
                    dataGridViewHolding.SelectedItem = (ModBus_Item)e.Row.Item;

                    UInt16 out_;
                    if (UInt16.TryParse(tmp.Text, out out_))
                    {
                        Thread t = new Thread(new ParameterizedThreadStart(writeCoilDataGrid));
                        t.Priority = threadPriority;
                        t.Start(tmp.Text);
                    }
                }
                catch(Exception err)
                {
                    Console.WriteLine(err);
                }
            }
        }

        public void writeCoilDataGrid(object obj)
        {
            try
            {
                ModBus_Item currentItem = new ModBus_Item();

                this.Dispatcher.Invoke((Action)delegate
                {
                    currentItem = (ModBus_Item)dataGridViewCoils.SelectedItem;
                    currentItem.Value = obj as String;
                });

                // Debug
                Console.WriteLine("Register: " + currentItem.Register);
                Console.WriteLine("Value: " + currentItem.Value);
                Console.WriteLine("Notes: " + currentItem.Notes);

                uint address_start = P.uint_parser(textBoxCoilsOffset_, comboBoxCoilsOffset_) + P.uint_parser(currentItem.Register, comboBoxCoilsAddress05_);

                bool? result = ModBus.forceSingleCoil_05(byte.Parse(textBoxModbusAddress_), address_start, uint.Parse(currentItem.Value), readTimeout);

                if (result != null)
                {
                    if (result == true)
                    {
                        this.Dispatcher.Invoke((Action)delegate
                        {
                            currentItem.Foreground = ForeGroundLight.ToString();
                            currentItem.Background = colorDefaultWriteCell.ToString();

                            /*if(index + 1 < dataGridViewCoils.Items.Count)
                                dataGridViewCoils.SelectedItem = list_coilsTable[index + 1];*/

                            dataGridViewCoils.Items.Refresh();
                        });
                    }
                    else
                    {
                        SetTableCrcError(list_coilsTable);
                    }
                }
                else
                {
                    SetTableTimeoutError(list_coilsTable);
                }

                this.Dispatcher.Invoke((Action)delegate
                {
                    dataGridViewCoils.Items.Refresh();
                    dataGridViewCoils.SelectedItem = currentItem;
                });
            }
            catch (ModbusException err)
            {
                if (err.Message.IndexOf("Timed out") != -1)
                {
                    SetTableTimeoutError(list_coilsTable);
                }
                if (err.Message.IndexOf("ModBus ErrCode") != -1)
                {
                    SetTableModBusError(list_coilsTable, err);
                }
                if (err.Message.IndexOf("CRC Error") != -1)
                {
                    SetTableCrcError(list_coilsTable);
                }

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils05_B.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
            catch (Exception err)
            {
                SetTableInternalError(list_coilsTable);

                Console.WriteLine(err);

                this.Dispatcher.Invoke((Action)delegate
                {
                    buttonWriteCoils05_B.IsEnabled = true;

                    dataGridViewCoils.ItemsSource = null;
                    dataGridViewCoils.ItemsSource = list_coilsTable;
                });
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {

            // Vincolato al ctrl
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                switch (e.Key)
                {
                    case Key.D1:
                        tabControlMain.SelectedIndex = 0;
                        break;
                    
                    case Key.D2:
                        tabControlMain.SelectedIndex = 1;
                        break;

                    case Key.D3:
                        tabControlMain.SelectedIndex = 2;
                        break;

                    case Key.D4:
                        tabControlMain.SelectedIndex = 3;
                        break;

                    case Key.D5:
                        tabControlMain.SelectedIndex = 4;
                        break;

                    case Key.D6:
                        tabControlMain.SelectedIndex = 5;
                        break;

                    case Key.D7:
                        tabControlMain.SelectedIndex = 6;
                        break;

                    case Key.D8:
                        tabControlMain.SelectedIndex = 7;
                        break;

                    // Comandi read
                    case Key.R:

                        // Coils
                        if(tabControlMain.SelectedIndex == 1)
                        {
                            if(buttonReadCoils01.IsEnabled)
                                buttonReadCoils01_Click(sender, e);
                        }

                        // Inputs
                        if (tabControlMain.SelectedIndex == 2)
                        {
                            if(buttonReadInput02.IsEnabled)
                                buttonReadInput02_Click(sender, e);
                        }

                        // Input registers
                        if (tabControlMain.SelectedIndex == 3)
                        {
                            if(buttonReadInputRegister04.IsEnabled)
                                buttonReadInputRegister04_Click(sender, e);
                        }

                        // Holding registers
                        if (tabControlMain.SelectedIndex == 4)
                        {
                            if(buttonReadHolding03.IsEnabled)
                                buttonReadHolding03_Click(sender, e);
                        }

                        break;

                    // Comandi read all
                    case Key.E:

                        // Coils
                        if (tabControlMain.SelectedIndex == 1)
                        {
                            if(buttonReadCoilsRange.IsEnabled)
                                buttonReadCoilsRange_Click(sender, e);
                        }

                        // Inputs
                        if (tabControlMain.SelectedIndex == 2)
                        {
                            if(buttonReadInputRange.IsEnabled)
                                buttonReadInputRange_Click(sender, e);
                        }

                        // Input registers
                        if (tabControlMain.SelectedIndex == 3)
                        {
                            if(buttonReadInputRegisterRange.IsEnabled)
                                buttonReadInputRegisterRange_Click(sender, e);
                        }

                        // Holding registers
                        if (tabControlMain.SelectedIndex == 4)
                        {
                            if(buttonReadHoldingRange.IsEnabled)
                                buttonReadHoldingRange_Click(sender, e);
                        }

                        break;

                    // Comandi polling
                    case Key.P:

                        // Polling
                        if (tabControlMain.SelectedIndex == 0)
                        {
                            buttonPingIp_Click(sender, e);
                        }

                        // Coils
                        if (tabControlMain.SelectedIndex == 1)
                        {
                            if(buttonLoopCoils01.IsEnabled)
                                buttonLoopCoils01_Click(sender, e);
                        }

                        // Inputs
                        if (tabControlMain.SelectedIndex == 2)
                        {
                            if(buttonLoopInput02.IsEnabled)
                                buttonLoopInput02_Click(sender, e);
                        }

                        // Input registers
                        if (tabControlMain.SelectedIndex == 3)
                        {
                            if(buttonLoopInputRegister04.IsEnabled)
                                buttonLoopInputRegister04_Click(sender, e);
                        }

                        // Holding registers
                        if (tabControlMain.SelectedIndex == 4)
                        {
                            if(buttonLoopHolding03.IsEnabled)
                                buttonLoopHolding03_Click(sender, e);
                        }

                        break;

                    // Comandi polling
                    case Key.K:

                        // Coils
                        if (tabControlMain.SelectedIndex == 1)
                        {
                            if(buttonLoopCoilsRange.IsEnabled)
                                buttonLoopCoilsRange_Click(sender, e);
                        }

                        // Inputs
                        if (tabControlMain.SelectedIndex == 2)
                        {
                            if(buttonLoopInputRange.IsEnabled)
                                buttonLoopInputRange_Click(sender, e);
                        }

                        // Input registers
                        if (tabControlMain.SelectedIndex == 3)
                        {
                            if(buttonLoopInputRegisterRange.IsEnabled)
                                buttonLoopInputRegisterRange_Click(sender, e);
                        }

                        // Holding registers
                        if (tabControlMain.SelectedIndex == 4)
                        {
                            if(buttonLoopHoldingRange.IsEnabled)
                                buttonLoopHoldingRange_Click(sender, e);
                        }

                        break;

                    // Carica profilo
                    case Key.O:
                        caricaConfigurazioneDaDatabaseToolStripMenuItem_Click(sender, e);
                        break;

                    // Apri log
                    case Key.L:
                        logToolStripMenu_Click(sender, e);
                        break;

                    // DB Manager
                    case Key.D:
                        gestisciDatabaseToolStripMenuItem_Click(sender, e);
                        break;
                        
                    // Info
                    case Key.I:
                        infoToolStripMenuItem1_Click(sender, e);
                        break;

                    // Salva
                    case Key.S:

                        // Salva su database
                        if(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        {
                            salvaConfigurazioneAttualeNelDatabaseToolStripMenuItem_Click(sender, e);
                        }
                        // Salva config. corrente
                        else
                        {
                            salvaToolStripMenuItem_Click(sender, e);
                        }

                        break;

                    // Mode
                    case Key.M:

                        if ((bool)radioButtonModeSerial.IsEnabled && (bool)radioButtonModeTcp.IsEnabled)
                        {
                            if ((bool)radioButtonModeSerial.IsChecked)
                            {
                                radioButtonModeTcp.IsChecked = true;
                            }
                            else
                            {
                                radioButtonModeSerial.IsChecked = true;
                            }
                        }
                        break;

                    // Mode
                    case Key.N:
                    case Key.B:
                        if ((bool)radioButtonModeSerial.IsChecked)
                        {
                            buttonSerialActive_Click(sender, e);
                        }
                        else
                        {
                            buttonTcpActive_Click(sender, e);
                        }
                        break;

                    // Chiudi finestra
                    case Key.W:
                    case Key.Q:
                        this.Close();
                        break;

                    // Template
                    case Key.T:
                        apriTemplateEditor_Click(sender, e);
                        break;

                    // Left arrow
                    /*case Key.Left:
                        if(tabControlMain.SelectedIndex > 0)
                        {
                            tabControlMain.SelectedIndex = tabControlMain.SelectedIndex - 1;
                        }
                        break;

                    // Right arrow
                    case Key.Right:
                        if (tabControlMain.SelectedIndex < 7)
                        {
                            tabControlMain.SelectedIndex = tabControlMain.SelectedIndex + 1;
                        }
                        break;*/

                }

                if(e.Key == Key.C && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                {
                    if (!statoConsole)
                    {
                        apriConsole();

                        this.Focus();
                    }
                    else
                    {
                        chiudiConsole();
                    }
                }

            }

            // Non vincolato al ctrl
            switch (e.Key) 
            {
                // Cancella tabella
                case Key.Delete:

                    // Home
                    if (tabControlMain.SelectedIndex == 0)
                    {
                        buttonClearSerialStatus_Click(sender, e);
                    }

                    // Coils
                    if (tabControlMain.SelectedIndex == 1)
                    {
                        buttonClearCoils_Click(sender, e);
                    }

                    // Inputs
                    if (tabControlMain.SelectedIndex == 2)
                    {
                        buttonClearInput_Click(sender, e);
                    }

                    // Input registers
                    if (tabControlMain.SelectedIndex == 3)
                    {
                        buttonClearInputReg_Click(sender, e);
                    }

                    // Holding registers
                    if (tabControlMain.SelectedIndex == 4)
                    {
                        buttonClearHoldingReg_Click(sender, e);
                    }

                    // Log
                    if(tabControlMain.SelectedIndex == 6)
                    {
                        buttonClearAll_Click(sender, e);
                    }

                    break;
            }
        }

        private void logToolStripMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!logWindowIsOpen)
            {
                logWindowIsOpen = true;
                LogView window = new LogView(this);
                window.Show();
            }
            else
            {
                MessageBox.Show(lang.languageTemplate["strings"]["logIsAlreadyOpen"], "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void textBoxHoldingAddress16_B_TextChanged(object sender, TextChangedEventArgs e)
        {
            string tmp = "";
            int stop = 0;

            System.Globalization.NumberStyles style = comboBoxHoldingAddress16_B_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer;

            if (int.TryParse(textBoxHoldingAddress16_B.Text, style, null, out stop))
            {
                if (comboBoxHoldingValue16_ == "HEX")
                {
                    stop = stop * 2;
                }

                for (int i = 0; i < stop; i++)
                {
                    if (comboBoxHoldingValue16_ == "HEX")
                    {
                        tmp += "00";
                    }
                    else
                    {
                        tmp += "0";
                    }

                    if (i < (stop - 1))
                    {
                        tmp += " ";
                    }
                }

                textBoxHoldingValue16.Text = tmp;
            }

            textBoxHoldingAddress16_B_ = textBoxHoldingAddress16_B.Text;
        }

        public void LogDequeue()
        {
            while (!dequeueExit)
            {
                String content;

                if (this.ModBus != null)
                {
                    if (this.ModBus.log2.TryDequeue(out content))
                    {
                        richTextBoxPackets.Dispatcher.Invoke((Action)delegate
                        {
                            if (count_log > LogLimitRichTextBox)
                            {
                                // Arrivato al limite tolgo una riga ogni volta che aggiungo una riga
                                richTextBoxPackets.Document.Blocks.Remove(richTextBoxPackets.Document.Blocks.FirstBlock);
                            }
                            else
                            {
                                count_log += 1;
                            }

                            richTextBoxPackets.AppendText(content);
                        });

                        scrolled_log = false;
                    }
                    else
                    {
                        if (!scrolled_log)
                        {
                            richTextBoxPackets.Dispatcher.Invoke((Action)delegate
                            {
                                richTextBoxPackets.ScrollToEnd();
                            });

                            scrolled_log = true;
                        }


                        Thread.Sleep(100);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void TextBoxReadTimeout_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!int.TryParse(textBoxReadTimeout.Text, out readTimeout))
            {
                textBoxReadTimeout.Text = "1000";
            }
        }

        private void buttonExportHoldingReg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveFileDialogBox = new SaveFileDialog();

                saveFileDialogBox.DefaultExt = "csv";
                saveFileDialogBox.AddExtension = false;
                saveFileDialogBox.FileName = "" + defaultPathToConfiguration + "_HoldingRegisters";
                saveFileDialogBox.Filter = "CSV|*.csv|JSON|*.json";
                saveFileDialogBox.Title = title;

                if ((bool)saveFileDialogBox.ShowDialog())
                {
                    if (saveFileDialogBox.FileName.IndexOf("csv") != -1)
                    {
                        String file_content = "";

                        for (int i = 0; i < (list_holdingRegistersTable.Count); i++)
                        {
                            if (comboBoxHoldingOffset.SelectedIndex == 0)
                            {
                                file_content += textBoxHoldingOffset.Text + ",";   // DEC
                            }
                            else
                            {
                                file_content += "0x" + textBoxHoldingOffset.Text + ",";    // HEX
                            }

                            file_content += list_holdingRegistersTable[i].Register + ",";
                            file_content += list_holdingRegistersTable[i].Value + ",";
                            file_content += list_holdingRegistersTable[i].Notes + "\n";
                        }

                        StreamWriter writer = new StreamWriter(saveFileDialogBox.OpenFile());

                        writer.Write(file_content);
                        writer.Dispose();
                        writer.Close();
                    }
                    else
                    {
                        // File JSON
                        dataGridJson save = new dataGridJson();

                        save.items = new ModBusItem_Save[list_holdingRegistersTable.Count];

                        for (int i = 0; i < (list_holdingRegistersTable.Count); i++)
                        {
                            try
                            {
                                ModBusItem_Save item = new ModBusItem_Save();

                                if(comboBoxHoldingOffset.SelectedIndex == 0)
                                {
                                    item.Offset = textBoxHoldingOffset.Text;    // DEC
                                }
                                else
                                {
                                    item.Offset = "0x" + textBoxHoldingOffset.Text;    // HEX
                                }
                                
                                item.Register = list_holdingRegistersTable[i].Register;
                                item.Value = list_holdingRegistersTable[i].Value;
                                item.Notes = list_holdingRegistersTable[i].Notes;

                                save.items[i] = item;
                            }
                            catch { }
                        }


                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        jss.MaxJsonLength = this.MaxJsonLength;
                        string file_content = jss.Serialize(save);

                        StreamWriter writer = new StreamWriter(saveFileDialogBox.OpenFile());

                        writer.Write(file_content);
                        writer.Dispose();
                        writer.Close();
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
        }

        private void buttonExportInputReg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveFileDialogBox = new SaveFileDialog();

                saveFileDialogBox.DefaultExt = "csv";
                saveFileDialogBox.AddExtension = false;
                saveFileDialogBox.FileName = "" + defaultPathToConfiguration + "_InputRegisters";
                saveFileDialogBox.Filter = "CSV|*.csv|JSON|*.json";
                saveFileDialogBox.Title = title;

                if ((bool)saveFileDialogBox.ShowDialog())
                {
                    if (saveFileDialogBox.FileName.IndexOf("csv") != -1)
                    {
                        String file_content = "";

                        for (int i = 0; i < (list_inputRegistersTable.Count); i++)
                        {
                            if (comboBoxInputRegOffset.SelectedIndex == 0)
                            {
                                file_content += textBoxInputRegOffset.Text + ",";   // DEC
                            }
                            else
                            {
                                file_content += "0x" + textBoxInputRegOffset.Text + ",";    // HEX
                            }

                            file_content += list_inputRegistersTable[i].Register + ",";
                            file_content += list_inputRegistersTable[i].Value + ",";
                            file_content += list_inputRegistersTable[i].Notes + "\n";
                        }

                        StreamWriter writer = new StreamWriter(saveFileDialogBox.OpenFile());

                        writer.Write(file_content);
                        writer.Dispose();
                        writer.Close();
                    }
                    else
                    {
                        // File JSON
                        dataGridJson save = new dataGridJson();

                        save.items = new ModBusItem_Save[list_inputRegistersTable.Count];

                        for (int i = 0; i < (list_inputRegistersTable.Count); i++)
                        {
                            try
                            {
                                ModBusItem_Save item = new ModBusItem_Save();

                                if (comboBoxInputRegOffset.SelectedIndex == 0)
                                {
                                    item.Offset = textBoxInputRegOffset.Text;    // DEC
                                }
                                else
                                {
                                    item.Offset = "0x" + textBoxInputRegOffset.Text;    // HEX
                                }

                                item.Register = list_inputRegistersTable[i].Register;
                                item.Value = list_inputRegistersTable[i].Value;
                                item.Notes = list_inputRegistersTable[i].Notes;

                                save.items[i] = item;
                            }
                            catch { }
                        }


                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        jss.MaxJsonLength = this.MaxJsonLength;
                        string file_content = jss.Serialize(save);

                        StreamWriter writer = new StreamWriter(saveFileDialogBox.OpenFile());

                        writer.Write(file_content);
                        writer.Dispose();
                        writer.Close();
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
        }

        private void buttonExportInput_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveFileDialogBox = new SaveFileDialog();

                saveFileDialogBox.DefaultExt = "csv";
                saveFileDialogBox.AddExtension = false;
                saveFileDialogBox.FileName = "" + defaultPathToConfiguration + "_InputRegisters";
                saveFileDialogBox.Filter = "CSV|*.csv|JSON|*.json";
                saveFileDialogBox.Title = title;

                if ((bool)saveFileDialogBox.ShowDialog())
                {
                    if (saveFileDialogBox.FileName.IndexOf("csv") != -1)
                    {
                        String file_content = "";

                        for (int i = 0; i < (list_inputsTable.Count); i++)
                        {
                            if (comboBoxInputOffset.SelectedIndex == 0)
                            {
                                file_content += textBoxInputOffset.Text + ",";   // DEC
                            }
                            else
                            {
                                file_content += "0x" + textBoxInputOffset.Text + ",";    // HEX
                            }

                            file_content += list_inputsTable[i].Register + ",";
                            file_content += list_inputsTable[i].Value + ",";
                            file_content += list_inputsTable[i].Notes + "\n";
                        }

                        StreamWriter writer = new StreamWriter(saveFileDialogBox.OpenFile());

                        writer.Write(file_content);
                        writer.Dispose();
                        writer.Close();
                    }
                    else
                    {
                        // File JSON
                        dataGridJson save = new dataGridJson();

                        save.items = new ModBusItem_Save[list_inputsTable.Count];

                        for (int i = 0; i < (list_inputsTable.Count); i++)
                        {
                            try
                            {
                                ModBusItem_Save item = new ModBusItem_Save();

                                if (comboBoxInputOffset.SelectedIndex == 0)
                                {
                                    item.Offset = textBoxInputOffset.Text;    // DEC
                                }
                                else
                                {
                                    item.Offset = "0x" + textBoxInputOffset.Text;    // HEX
                                }

                                item.Register = list_inputsTable[i].Register;
                                item.Value = list_inputsTable[i].Value;
                                item.Notes = list_inputsTable[i].Notes;

                                save.items[i] = item;
                            }
                            catch { }
                        }


                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        jss.MaxJsonLength = this.MaxJsonLength;
                        string file_content = jss.Serialize(save);

                        StreamWriter writer = new StreamWriter(saveFileDialogBox.OpenFile());

                        writer.Write(file_content);
                        writer.Dispose();
                        writer.Close();
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
        }

        private void buttonExportCoils_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveFileDialogBox = new SaveFileDialog();

                saveFileDialogBox.DefaultExt = "csv";
                saveFileDialogBox.AddExtension = false;
                saveFileDialogBox.FileName = "" + defaultPathToConfiguration + "_InputRegisters";
                saveFileDialogBox.Filter = "CSV|*.csv|JSON|*.json";
                saveFileDialogBox.Title = title;

                if ((bool)saveFileDialogBox.ShowDialog())
                {
                    if (saveFileDialogBox.FileName.IndexOf("csv") != -1)
                    {
                        String file_content = "";

                        for (int i = 0; i < (list_coilsTable.Count); i++)
                        {
                            if (comboBoxCoilsOffset.SelectedIndex == 0)
                            {
                                file_content += textBoxCoilsOffset.Text + ",";   // DEC
                            }
                            else
                            {
                                file_content += "0x" + textBoxCoilsOffset.Text + ",";    // HEX
                            }

                            file_content += list_coilsTable[i].Register + ",";
                            file_content += list_coilsTable[i].Value + ",";
                            file_content += list_coilsTable[i].Notes + "\n";
                        }

                        StreamWriter writer = new StreamWriter(saveFileDialogBox.OpenFile());

                        writer.Write(file_content);
                        writer.Dispose();
                        writer.Close();
                    }
                    else
                    {
                        // File JSON
                        dataGridJson save = new dataGridJson();

                        save.items = new ModBusItem_Save[list_coilsTable.Count];

                        for (int i = 0; i < (list_coilsTable.Count); i++)
                        {
                            try
                            {
                                ModBusItem_Save item = new ModBusItem_Save();

                                if (comboBoxCoilsOffset.SelectedIndex == 0)
                                {
                                    item.Offset = textBoxCoilsOffset.Text;    // DEC
                                }
                                else
                                {
                                    item.Offset = "0x" + textBoxCoilsOffset.Text;    // HEX
                                }

                                item.Register = list_coilsTable[i].Register;
                                item.Value = list_coilsTable[i].Value;
                                item.Notes = list_coilsTable[i].Notes;

                                save.items[i] = item;
                            }
                            catch { }
                        }


                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        jss.MaxJsonLength = this.MaxJsonLength;
                        string file_content = jss.Serialize(save);

                        StreamWriter writer = new StreamWriter(saveFileDialogBox.OpenFile());

                        writer.Write(file_content);
                        writer.Dispose();
                        writer.Close();
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
        }

        // Test salvataggio configurazione con descrittore JSON
        public void SaveConfiguration_v2(bool alert)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            
            dynamic toSave = jss.DeserializeObject(File.ReadAllText(localPath + "\\Config\\SettingsToSave.json"));

            Dictionary<string, Dictionary<string, object>> file_ = new Dictionary<string, Dictionary<string, object>>();

            foreach(KeyValuePair<string, object> row in toSave["toSave"])
            {
                // row.key = "textBoxes"
                // row.value = {  }

                switch (row.Key)
                {
                    case "textBoxes":

                        Dictionary<string, object> textBoxes = new Dictionary<string, object>();
                        
                        foreach (KeyValuePair<string, object> sub in toSave["toSave"][row.Key])
                        {
                            // sub.key = "textBoxModbusAddess_1"
                            // sub.value = { "..." }

                            // debug
                            //Console.WriteLine("sub.key: " + sub.Key);
                            //Console.WriteLine("sub.value: " + sub.Value);

                            bool found = false;

                            foreach (KeyValuePair<string, object> prop in toSave["toSave"][row.Key][sub.Key])
                            {
                                // prop.key = "key"
                                // prop.value = "nomeVariabile"

                                // debug
                                //Console.WriteLine("prop.key: " + prop.Key);
                                //Console.WriteLine("prop.value: " + prop.Value as String);

                                if(prop.Key == "key")
                                {
                                    found = true;

                                    if (this.FindName(sub.Key) != null)
                                    {
                                        textBoxes.Add(prop.Value as String, (this.FindName(sub.Key) as TextBox).Text);
                                    }
                                    else
                                    {
                                        Console.WriteLine(sub.Key + " not found in current form");
                                    }
                                }
                            }

                            if (!found)
                            {
                                if (this.FindName(sub.Key) != null) 
                                { 
                                    textBoxes.Add(sub.Key, (this.FindName(sub.Key) as TextBox).Text);
                                }
                                else
                                {
                                    Console.WriteLine(sub.Key + " not found in current form");
                                }
                            }
                        }

                        file_.Add("textBoxes", textBoxes);
                        break;

                    case "checkBoxes":

                        Dictionary<string, object> checkBoxes = new Dictionary<string, object>();

                        foreach (KeyValuePair<string, object> sub in toSave["toSave"][row.Key])
                        {
                            bool found = false;

                            foreach (KeyValuePair<string, object> prop in toSave["toSave"][row.Key][sub.Key])
                            {
                                if (prop.Key == "key")
                                {
                                    found = true;

                                    if (this.FindName(sub.Key) != null)
                                    {
                                        checkBoxes.Add(prop.Value as String, (bool)(this.FindName(sub.Key) as CheckBox).IsChecked);
                                    }
                                    else
                                    {
                                        Console.WriteLine(sub.Key + " not found in current form");
                                    }
                                }
                            }

                            if (!found)
                            {
                                if (this.FindName(sub.Key) != null)
                                {
                                    checkBoxes.Add(sub.Key, (bool)(this.FindName(sub.Key) as CheckBox).IsChecked);
                                }
                                else
                                {
                                    Console.WriteLine(sub.Key + " not found in current form");
                                }
                            }
                        }

                        file_.Add("checkBoxes", checkBoxes);
                        break;

                    case "menuItems":

                        Dictionary<string, object> menuItems = new Dictionary<string, object>();

                        foreach (KeyValuePair<string, object> sub in toSave["toSave"][row.Key])
                        {
                            bool found = false;

                            foreach (KeyValuePair<string, object> prop in toSave["toSave"][row.Key][sub.Key])
                            {
                                if (prop.Key == "key")
                                {
                                    found = true;

                                    if (this.FindName(sub.Key) != null)
                                    {
                                        menuItems.Add(prop.Value as String, (bool)(this.FindName(sub.Key) as MenuItem).IsChecked);
                                    }
                                    else
                                    {
                                        Console.WriteLine(sub.Key + " not found in current form");
                                    }
                                }
                            }

                            if (!found)
                            {
                                if (this.FindName(sub.Key) != null)
                                {
                                    menuItems.Add(sub.Key, (bool)(this.FindName(sub.Key) as MenuItem).IsChecked);
                                }
                                else
                                {
                                    Console.WriteLine(sub.Key + " not found in current form");
                                }
                            }
                        }

                        file_.Add("menuItems", menuItems);
                        break;

                    case "radioButtons":

                        Dictionary<string, object> radioButtons = new Dictionary<string, object>();

                        foreach (KeyValuePair<string, object> sub in toSave["toSave"][row.Key])
                        {
                            bool found = false;

                            foreach (KeyValuePair<string, object> prop in toSave["toSave"][row.Key][sub.Key])
                            {
                                if (prop.Key == "key")
                                {
                                    found = true;

                                    if (this.FindName(sub.Key) != null)
                                    {
                                        radioButtons.Add(prop.Value as String, (this.FindName(sub.Key) as RadioButton).IsChecked);
                                    }
                                    else
                                    {
                                        Console.WriteLine(sub.Key + " not found in current form");
                                    }
                                }
                            }

                            if (!found)
                            {
                                if (this.FindName(sub.Key) != null)
                                {
                                    radioButtons.Add(sub.Key, (this.FindName(sub.Key) as RadioButton).IsChecked);
                                }
                                else
                                {
                                    Console.WriteLine(sub.Key + " not found in current form");
                                }
                            }
                        }

                        file_.Add("radioButtons", radioButtons);
                        break;

                    case "comboBoxes":

                        Dictionary<string, object> comboBoxes = new Dictionary<string, object>();

                        foreach (KeyValuePair<string, object> sub in toSave["toSave"][row.Key])
                        {
                            bool found = false;

                            foreach (KeyValuePair<string, object> prop in toSave["toSave"][row.Key][sub.Key])
                            {
                                if (prop.Key == "key")
                                {
                                    found = true;

                                    if (this.FindName(sub.Key) != null)
                                    {
                                        comboBoxes.Add(prop.Value as String, (this.FindName(sub.Key) as ComboBox).SelectedIndex);
                                    }
                                    else
                                    {
                                        Console.WriteLine(sub.Key + " not found in current form");
                                    }
                                }
                            }

                            if (!found)
                            {
                                if (this.FindName(sub.Key) != null)
                                {
                                    comboBoxes.Add(sub.Key, (this.FindName(sub.Key) as ComboBox).SelectedIndex);
                                }
                                else
                                {
                                    Console.WriteLine(sub.Key + " not found in current form");
                                }
                            }
                        }

                        file_.Add("comboBoxes", comboBoxes);
                        break;
                }
            }

            // Altre variabili custom
            Dictionary<string, object> others = new Dictionary<string, object>();

            others.Add("colorDefaultReadCell", colorDefaultReadCell.ToString());
            others.Add("colorDefaultWriteCell", colorDefaultWriteCell.ToString());
            others.Add("colorErrorCell", colorErrorCell.ToString());

            file_.Add("others", others);

            File.WriteAllText(localPath + "/Json/" + pathToConfiguration + "/Config.json", jss.Serialize(file_));

            if (alert)
            {
                MessageBox.Show(lang.languageTemplate["strings"]["infoSaveConfig"], "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void LoadConfiguration_v2()
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();

            dynamic toSave = jss.DeserializeObject(File.ReadAllText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Config\\SettingsToSave.json"));
            dynamic loaded = jss.DeserializeObject(File.ReadAllText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Json\\" + pathToConfiguration + "\\Config.json"));

            foreach (KeyValuePair<string, object> row in toSave["toSave"])
            {
                // row.key = "textBoxes"
                // row.value = {  }

                switch (row.Key)
                {
                    case "textBoxes":

                        foreach (KeyValuePair<string, object> sub in toSave["toSave"][row.Key])
                        {
                            bool found = false;

                            foreach (KeyValuePair<string, object> prop in toSave["toSave"][row.Key][sub.Key])
                            {

                                if (prop.Key == "key")
                                {
                                    found = true;

                                    if (this.FindName(sub.Key) != null)
                                    {
                                        // debug
                                        //Console.WriteLine(" -- ");
                                        //Console.WriteLine("sub.Key: " + sub.Key);
                                        //Console.WriteLine("prop.Value: " + prop.Value);
                                        //Console.WriteLine(loaded[row.Key][prop.Value.ToString()]);

                                        try
                                        {
                                            (this.FindName(sub.Key) as TextBox).Text = loaded[row.Key][prop.Value.ToString()];
                                        }
                                        catch (Exception err)
                                        {
                                            Console.WriteLine(prop.Value.ToString() + " generated an error");
                                            Console.WriteLine(err);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine(sub.Key + " not found in current form");
                                    }
                                }
                            }

                            if (!found)
                            {
                                if (this.FindName(sub.Key) != null)
                                {
                                    try
                                    {
                                        (this.FindName(sub.Key) as TextBox).Text = loaded[row.Key][sub.Key.ToString()];
                                    }
                                    catch (Exception err)
                                    {
                                        Console.WriteLine(sub.Key.ToString() + " generated an error");
                                        Console.WriteLine(err);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(sub.Key + " not found in current form");
                                }
                            }
                        }

                        break;

                    case "checkBoxes":

                        foreach (KeyValuePair<string, object> sub in toSave["toSave"][row.Key])
                        {
                            bool found = false;

                            foreach (KeyValuePair<string, object> prop in toSave["toSave"][row.Key][sub.Key])
                            {

                                if (prop.Key == "key")
                                {
                                    found = true;

                                    if (this.FindName(sub.Key) != null)
                                    {
                                        try
                                        {
                                            (this.FindName(sub.Key) as CheckBox).IsChecked = loaded[row.Key][prop.Value.ToString()];
                                        }
                                        catch (Exception err)
                                        {
                                            Console.WriteLine(prop.Value.ToString() + " generated an error");
                                            Console.WriteLine(err);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine(sub.Key + " not found in current form");
                                    }
                                }
                            }

                            if (!found)
                            {
                                if (this.FindName(sub.Key) != null)
                                {
                                    try
                                    {
                                        (this.FindName(sub.Key) as CheckBox).IsChecked = loaded[row.Key][sub.Key.ToString()];
                                    }
                                    catch (Exception err)
                                    {
                                        Console.WriteLine(sub.Key.ToString() + " generated an error");
                                        Console.WriteLine(err);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(sub.Key + " not found in current form");
                                }
                            }
                        }

                        break;

                    case "menuItems":

                        foreach (KeyValuePair<string, object> sub in toSave["toSave"][row.Key])
                        {
                            bool found = false;

                            foreach (KeyValuePair<string, object> prop in toSave["toSave"][row.Key][sub.Key])
                            {

                                if (prop.Key == "key")
                                {
                                    found = true;

                                    if (this.FindName(sub.Key) != null)
                                    {
                                        try
                                        {
                                            (this.FindName(sub.Key) as MenuItem).IsChecked = loaded[row.Key][prop.Value.ToString()];
                                        }
                                        catch (Exception err)
                                        {
                                            Console.WriteLine(prop.Value.ToString() + " generated an error");
                                            Console.WriteLine(err);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine(sub.Key + " not found in current form");
                                    }
                                }
                            }

                            if (!found)
                            {
                                if (this.FindName(sub.Key) != null)
                                {
                                    try
                                    {
                                        (this.FindName(sub.Key) as MenuItem).IsChecked = loaded[row.Key][sub.Key.ToString()];
                                    }
                                    catch (Exception err)
                                    {
                                        Console.WriteLine(sub.Key.ToString() + " generated an error");
                                        Console.WriteLine(err);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(sub.Key + " not found in current form");
                                }
                            }
                        }

                        break;

                    case "radioButtons":

                        foreach (KeyValuePair<string, object> sub in toSave["toSave"][row.Key])
                        {
                            bool found = false;

                            foreach (KeyValuePair<string, object> prop in toSave["toSave"][row.Key][sub.Key])
                            {

                                if (prop.Key == "key")
                                {
                                    found = true;

                                    if (this.FindName(sub.Key) != null)
                                    {
                                        try
                                        {
                                            (this.FindName(sub.Key) as RadioButton).IsChecked = loaded[row.Key][prop.Value.ToString()];
                                        }
                                        catch (Exception err)
                                        {
                                            Console.WriteLine(prop.Value.ToString() + " generated an error");
                                            Console.WriteLine(err);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine(sub.Key + " not found in current form");
                                    }
                                }
                            }

                            if (!found)
                            {
                                if (this.FindName(sub.Key) != null)
                                {
                                    try
                                    {
                                        (this.FindName(sub.Key) as RadioButton).IsChecked = loaded[row.Key][sub.Key.ToString()];
                                    }
                                    catch (Exception err)
                                    {
                                        Console.WriteLine(sub.Key.ToString() + " generated an error");
                                        Console.WriteLine(err);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(sub.Key + " not found in current form");
                                }
                            }
                        }

                        break;

                    case "comboBoxes":

                        foreach (KeyValuePair<string, object> sub in toSave["toSave"][row.Key])
                        {
                            bool found = false;

                            foreach (KeyValuePair<string, object> prop in toSave["toSave"][row.Key][sub.Key])
                            {

                                if (prop.Key == "key")
                                {
                                    found = true;

                                    if (this.FindName(sub.Key) != null)
                                    {
                                        try
                                        {
                                            (this.FindName(sub.Key) as ComboBox).SelectedIndex = loaded[row.Key][prop.Value.ToString()];
                                        }
                                        catch (Exception err)
                                        {
                                            Console.WriteLine(prop.Value.ToString() + " generated an error");
                                            Console.WriteLine(err);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine(sub.Key + " not found in current form");
                                    }
                                }
                            }

                            if (!found)
                            {
                                if (this.FindName(sub.Key) != null)
                                {
                                    try
                                    {
                                        (this.FindName(sub.Key) as ComboBox).SelectedIndex = loaded[row.Key][sub.Key.ToString()];
                                    }
                                    catch (Exception err)
                                    {
                                        Console.WriteLine(sub.Key.ToString() + " generated an error");
                                        Console.WriteLine(err);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(sub.Key + " not found in current form");
                                }
                            }
                        }

                        break;
                }

                // Altre variabili custom
                BrushConverter bc = new BrushConverter();

                // Light Mode
                if (loaded["others"].ContainsKey("colorDefaultReadCell_Light"))
                    colorDefaultReadCell_Light = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorDefaultReadCell_Light"]);
                else
                    colorDefaultReadCell_Light = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorDefaultReadCell"]);

                if (loaded["others"].ContainsKey("colorDefaultWriteCell_Light"))
                    colorDefaultWriteCell_Light = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorDefaultWriteCell_Light"]);
                else
                    colorDefaultWriteCell_Light = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorDefaultWriteCell"]);

                if (loaded["others"].ContainsKey("colorErrorCell_Light"))
                    colorErrorCell_Light = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorErrorCell_Light"]);
                else
                    colorErrorCell_Light = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorErrorCell"]);

                // Dark Mode
                if (loaded["others"].ContainsKey("colorDefaultReadCell_Dark"))
                    colorDefaultReadCell_Dark = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorDefaultReadCell_Dark"]);
                else
                    colorDefaultReadCell_Dark = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorDefaultReadCell"]);

                if (loaded["others"].ContainsKey("colorDefaultWriteCell_Dark"))
                    colorDefaultWriteCell_Dark = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorDefaultWriteCell_Dark"]);
                else
                    colorDefaultWriteCell_Dark = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorDefaultWriteCell"]);

                if (loaded["others"].ContainsKey("colorErrorCell_Dark"))
                    colorErrorCell_Dark = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorErrorCell_Dark"]);
                else
                    colorErrorCell_Dark = (SolidColorBrush)bc.ConvertFromString(loaded["others"]["colorErrorCell"]);

                if (darkMode)
                {
                    labelColorCellRead.Background = colorDefaultReadCell_Dark;
                    labelColorCellWrote.Background = colorDefaultWriteCell_Dark;
                    labelColorCellError.Background = colorErrorCell_Dark;

                    colorDefaultReadCell = colorDefaultReadCell_Dark;
                    colorDefaultWriteCell = colorDefaultWriteCell_Dark;
                    colorErrorCell = colorErrorCell_Dark;

                    colorDefaultReadCellStr = colorDefaultReadCell.ToString();
                    colorDefaultWriteCellStr = colorDefaultWriteCell.ToString();
                    colorErrorCellStr = colorErrorCell.ToString();
                }
                else
                {
                    labelColorCellRead.Background = colorDefaultReadCell_Light;
                    labelColorCellWrote.Background = colorDefaultWriteCell_Light;
                    labelColorCellError.Background = colorErrorCell_Light;

                    colorDefaultReadCell = colorDefaultReadCell_Light;
                    colorDefaultWriteCell = colorDefaultWriteCell_Light;
                    colorErrorCell = colorErrorCell_Light;

                    colorDefaultReadCellStr = colorDefaultReadCell.ToString();
                    colorDefaultWriteCellStr = colorDefaultWriteCell.ToString();
                    colorErrorCellStr = colorErrorCell.ToString();
                }
            }

            // Al termine del caricamento della configurazione carico il template
            try
            {
                string file_content = File.ReadAllText(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Json\\" + pathToConfiguration + "\\Template.json");
                jss.MaxJsonLength = this.MaxJsonLength;
                TEMPLATE template = jss.Deserialize<TEMPLATE>(file_content);

                template_coilsOffset = 0;
                template_inputsOffset = 0;
                template_inputRegistersOffset = 0;
                template_HoldingOffset = 0;

                list_template_coilsTable.Clear();
                list_template_inputsTable.Clear();
                list_template_inputRegistersTable.Clear();
                list_template_holdingRegistersTable.Clear();

                UInt16 tmp = 0;

                // Coils
                template_coilsOffset = int.Parse(template.textBoxCoilsOffset_, template.comboBoxCoilsOffset_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);

                // Inputs
                template_inputsOffset = int.Parse(template.textBoxInputOffset_, template.comboBoxInputOffset_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);

                // Input registers
                template_inputRegistersOffset = int.Parse(template.textBoxInputRegOffset_, template.comboBoxInputRegOffset_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);

                // Holding registers
                template_HoldingOffset = int.Parse(template.textBoxHoldingOffset_, template.comboBoxHoldingOffset_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);

                // Tabella coils
                for (int i = 0; i < template.dataGridViewCoils.Count(); i++)
                {
                    if (UInt16.TryParse(template.dataGridViewCoils[i].Register, template.comboBoxCoilsRegistri_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out tmp))
                    {
                        template.dataGridViewCoils[i].RegisterUInt = tmp;
                        template.dataGridViewCoils[i].Register = tmp.ToString();
                        list_template_coilsTable.Add(template.dataGridViewCoils[i]);
                    }
                }

                // Tabella inputs
                for (int i = 0; i < template.dataGridViewInput.Count(); i++)
                {
                    if (UInt16.TryParse(template.dataGridViewInput[i].Register, template.comboBoxInputRegistri_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out tmp))
                    {
                        template.dataGridViewInput[i].RegisterUInt = tmp;
                        template.dataGridViewInput[i].Register = tmp.ToString();
                        list_template_inputsTable.Add(template.dataGridViewInput[i]);
                    }
                }

                // Tabella input registers
                for (int i = 0; i < template.dataGridViewInputRegister.Count(); i++)
                {
                    if (UInt16.TryParse(template.dataGridViewInputRegister[i].Register, template.comboBoxInputRegRegistri_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out tmp))
                    {
                        template.dataGridViewInputRegister[i].RegisterUInt = tmp;
                        template.dataGridViewInputRegister[i].Register = template.dataGridViewInputRegister[i].RegisterUInt.ToString();
                        list_template_inputRegistersTable.Add(template.dataGridViewInputRegister[i]);
                    }
                }

                // Tabella holdings
                for (int i = 0; i < template.dataGridViewHolding.Count(); i++)
                {
                    if (UInt16.TryParse(template.dataGridViewHolding[i].Register, template.comboBoxHoldingRegistri_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out tmp))
                    {
                        template.dataGridViewHolding[i].RegisterUInt = tmp;
                        template.dataGridViewHolding[i].Register = template.dataGridViewHolding[i].RegisterUInt.ToString();
                        list_template_holdingRegistersTable.Add(template.dataGridViewHolding[i]);
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Error loading configuration\n");
                Console.WriteLine(err);
            }
        }

        public void changeColumnVisibility(object sender, RoutedEventArgs e)
        {
            DataGridTextColumn toEdit = (DataGridTextColumn)this.FindName((sender as MenuItem).Name.Replace("view", "dataGrid"));

            if(toEdit != null)
            {
                if((sender as MenuItem).IsChecked)
                {
                    toEdit.Visibility = Visibility.Visible;
                }
                else
                {
                    toEdit.Visibility = Visibility.Collapsed;
                }
            }    
        }

        private void textBoxCoilsAddress15_B_TextChanged(object sender, TextChangedEventArgs e)
        {
            string tmp = "";
            int stop = 0;

            System.Globalization.NumberStyles style = comboBoxCoilsAddress15_B_ == "HEX" ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer;

            if (int.TryParse(textBoxCoilsAddress15_B.Text, style, null,  out stop))
            {
                for (int i = 0; i < stop; i++)
                {
                    tmp += "0";
                }

                textBoxCoilsValue15.Text = tmp;
            }

            textBoxCoilsAddress15_B_ = textBoxCoilsAddress15_B.Text;
        }

        private void TextBoxCurrentLanguage_TextChanged(object sender, TextChangedEventArgs e)
        {
            language = textBoxCurrentLanguage.Text;

            lang.loadLanguageTemplate(language);
        }

        public void changeEnableButtonsConnect(bool enabled) 
        {
            buttonReadCoils01.IsEnabled = enabled;
            buttonLoopCoils01.IsEnabled = enabled;
            buttonReadCoilsRange.IsEnabled = enabled;
            buttonLoopCoilsRange.IsEnabled = enabled;
            buttonWriteCoils05.IsEnabled = enabled;
            buttonWriteCoils05_B.IsEnabled = enabled;
            buttonWriteCoils15.IsEnabled = enabled;

            buttonReadInput02.IsEnabled = enabled;
            buttonLoopInput02.IsEnabled = enabled;
            buttonReadInputRange.IsEnabled = enabled;
            buttonLoopInputRange.IsEnabled = enabled;

            buttonReadInputRegister04.IsEnabled = enabled;
            buttonLoopInputRegister04.IsEnabled = enabled;
            buttonReadInputRegisterRange.IsEnabled = enabled;
            buttonLoopInputRegisterRange.IsEnabled = enabled;

            buttonReadHolding03.IsEnabled = enabled;
            buttonLoopHolding03.IsEnabled = enabled;
            buttonReadHoldingRange.IsEnabled = enabled;
            buttonLoopHoldingRange.IsEnabled = enabled;
            buttonWriteHolding06.IsEnabled = enabled;
            buttonWriteHolding06_b.IsEnabled = enabled;
            buttonWriteHolding16.IsEnabled = enabled;

            buttonSendDiagnosticQuery.IsEnabled = enabled;
            buttonSendManualDiagnosticQuery.IsEnabled = enabled;
        }

        private void comboBoxHoldingAddress03_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingAddress03_ = comboBoxHoldingAddress03.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxHoldingAddress03_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingAddress03_ = textBoxHoldingAddress03.Text;
        }

        private void textBoxHoldingRegisterNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingRegisterNumber_ = textBoxHoldingRegisterNumber.Text;
        }

        private void comboBoxHoldingRange_A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingRange_A_ = comboBoxHoldingRange_A.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxHoldingRange_A_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingRange_A_ = textBoxHoldingRange_A.Text;
        }

        /*private void comboBoxHoldingRange_B_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingRange_B_ = comboBoxHoldingRange_B.SelectedIndex == 0 ? "DEC" : "HEX";
        }*/

        private void textBoxHoldingRange_B_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingRange_B_ = textBoxHoldingRange_B.Text;
        }

        private void comboBoxHoldingAddress06_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingAddress06_ = comboBoxHoldingAddress06.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxHoldingAddress06_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingAddress06_ = textBoxHoldingAddress06.Text;
        }

        private void comboBoxHoldingValue06_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingValue06_ = comboBoxHoldingValue06.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxHoldingValue06_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingValue06_ = textBoxHoldingValue06.Text;
        }

        private void comboBoxHoldingAddress06_b_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingAddress06_b_ = comboBoxHoldingAddress06_b.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxHoldingAddress06_b_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingAddress06_b_ = textBoxHoldingAddress06_b.Text;
        }

        private void textBoxHoldingValue06_b_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingValue06_b_ = textBoxHoldingValue06_b.Text;
        }

        private void comboBoxHoldingAddress16_A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingAddress16_A_ = comboBoxHoldingAddress16_A.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxHoldingAddress16_A_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingAddress16_A_ = textBoxHoldingAddress16_A.Text;
        }

        private void comboBoxHoldingAddress16_B_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingAddress16_B_ = comboBoxHoldingAddress16_B.SelectedIndex == 0 ? "DEC" : "HEX";

            textBoxHoldingAddress16_B_TextChanged(null, null);
        }

        private void comboBoxHoldingValue16_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingValue16_ = comboBoxHoldingValue16.SelectedIndex == 0 ? "DEC" : "HEX";

            textBoxHoldingAddress16_B_TextChanged(null, null);
        }

        private void textBoxHoldingValue16_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingValue16_ = textBoxHoldingValue16.Text;
        }

        private void comboBoxHoldingRegistri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingRegistri_ = comboBoxHoldingRegistri.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void comboBoxHoldingValori_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingValori_ = comboBoxHoldingValori.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void comboBoxHoldingOffset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxHoldingOffset_ = comboBoxHoldingOffset.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxHoldingOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxHoldingOffset_ = textBoxHoldingOffset.Text;
        }

        private void textBoxModbusAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxModbusAddress_ = textBoxModbusAddress.Text;
        }

        private void checkBoxCellColorMode_Checked(object sender, RoutedEventArgs e)
        {
            colorMode = (bool)checkBoxCellColorMode.IsChecked;
        }

        private void comboBoxCoilsRegistri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxCoilsRegistri_ = comboBoxCoilsRegistri.SelectedIndex == 0 ? "DEC":"HEX";
        }

        private void comboBoxCoilsOffset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxCoilsOffset_ = comboBoxCoilsOffset.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxCoilsOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilsOffset_ = textBoxCoilsOffset.Text;
        }

        private void comboBoxCoilsAddress01_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxCoilsAddress01_ = comboBoxCoilsAddress01.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxCoilsAddress01_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilsAddress01_ = textBoxCoilsAddress01.Text;
        }

        private void textBoxCoilNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilNumber_ = textBoxCoilNumber.Text;
        }

        private void comboBoxCoilsRange_A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxCoilsRange_A_ = comboBoxCoilsRange_A.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxCoilsRange_A_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilsRange_A_ = textBoxCoilsRange_A.Text;
        }

        /*private void comboBoxCoilsRange_B_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxCoilsRange_B_ = comboBoxCoilsRange_B.SelectedIndex == 0 ? "DEC" : "HEX";
        }*/

        private void textBoxCoilsRange_B_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilsRange_B_ = textBoxCoilsRange_B.Text;
        }

        private void comboBoxCoilsAddress05_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxCoilsAddress05_ = comboBoxCoilsAddress05.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxCoilsAddress05_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilsAddress05_ = textBoxCoilsAddress05.Text;
        }

        private void textBoxCoilsValue05_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilsValue05_ = textBoxCoilsValue05.Text;
        }

        private void comboBoxCoilsAddress05_b_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxCoilsAddress05_b_ = comboBoxCoilsAddress05_b.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxCoilsAddress05_b_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilsAddress05_b_ = textBoxCoilsAddress05_b.Text;
        }

        private void textBoxCoilsValue05_b_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilsValue05_b_ = textBoxCoilsValue05_b.Text;
        }

        private void comboBoxCoilsAddress15_A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxCoilsAddress15_A_ = comboBoxCoilsAddress15_A.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxCoilsAddress15_A_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilsAddress15_A_ = textBoxCoilsAddress15_A.Text;
        }

        private void comboBoxCoilsAddress15_B_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxCoilsAddress15_B_ = comboBoxCoilsAddress15_B.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxCoilsValue15_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxCoilsValue15_ = textBoxCoilsValue15.Text;
        }

        private void comboBoxInputRegistri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputRegistri_ = comboBoxInputRegistri.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void comboBoxInputOffset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputOffset_ = comboBoxInputOffset.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxInputOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxInputOffset_ = textBoxInputOffset.Text;
        }

        private void comboBoxInputAddress02_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputAddress02_ = comboBoxInputAddress02.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxInputAddress02_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxInputAddress02_ = textBoxInputAddress02.Text;
        }

        private void textBoxInputNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxInputNumber_ = textBoxInputNumber.Text;
        }

        private void comboBoxInputRange_A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputRange_A_ = comboBoxInputRange_A.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxInputRange_A_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxInputRange_A_ = textBoxInputRange_A.Text;
        }

        /*private void comboBoxInputRange_B_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputRange_B_ = comboBoxInputRange_B.SelectedIndex == 0 ? "DEC" : "HEX";
        }*/

        private void textBoxInputRange_B_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxInputRange_B_ = textBoxInputRange_B.Text;
        }

        private void comboBoxInputRegRegistri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputRegRegistri_ = comboBoxInputRegRegistri.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void comboBoxInputRegValori_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputRegValori_ = comboBoxInputRegValori.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void comboBoxInputRegOffset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputRegOffset_ = comboBoxInputRegOffset.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxInputRegOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxInputRegOffset_ = textBoxInputRegOffset.Text;
        }

        private void comboBoxInputRegisterAddress04_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputRegisterAddress04_ = comboBoxInputRegisterAddress04.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxInputRegisterAddress04_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxInputRegisterAddress04_ = textBoxInputRegisterAddress04.Text;
        }

        private void textBoxInputRegisterNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxInputRegisterNumber_ = textBoxInputRegisterNumber.Text;
        }

        private void comboBoxInputRegisterRange_A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputRegisterRange_A_ = comboBoxInputRegisterRange_A.SelectedIndex == 0 ? "DEC" : "HEX";
        }

        private void textBoxInputRegisterRange_A_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxInputRegisterRange_A_ = textBoxInputRegisterRange_A.Text;
        }

        /*private void comboBoxInputRegisterRange_B_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxInputRegisterRange_B_ = comboBoxInputRegisterRange_B.SelectedIndex == 0 ? "DEC" : "HEX";
        }*/

        private void textBoxInputRegisterRange_B_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxInputRegisterRange_B_ = textBoxInputRegisterRange_B.Text;
        }

        private void checkBoxUseOffsetInTextBox_Checked(object sender, RoutedEventArgs e)
        {
            correctModbusAddressAuto = (bool)checkBoxUseOffsetInTextBox.IsChecked;
        }

        private void CheckBoxDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            darkMode = (bool)CheckBoxDarkMode.IsChecked;

            if (darkMode)
            {
                colorDefaultReadCell = colorDefaultReadCell_Dark;
                colorDefaultWriteCell = colorDefaultWriteCell_Dark;
                colorErrorCell = colorErrorCell_Dark;

                colorDefaultReadCellStr = colorDefaultReadCell.ToString();
                colorDefaultWriteCellStr = colorDefaultWriteCell.ToString();
                colorErrorCellStr = colorErrorCell.ToString();
            }
            else
            {
                colorDefaultReadCell = colorDefaultReadCell_Light;
                colorDefaultWriteCell = colorDefaultWriteCell_Light;
                colorErrorCell = colorErrorCell_Light;

                colorDefaultReadCellStr = colorDefaultReadCell.ToString();
                colorDefaultWriteCellStr = colorDefaultWriteCell.ToString();
                colorErrorCellStr = colorErrorCell.ToString();
            }

            // BorderBrush
            Setter SetBorderBrush = new Setter();
            SetBorderBrush.Property = BorderBrushProperty;
            SetBorderBrush.Value = darkMode ? BackGroundDark : BackGroundLight2;

            // Background
            Setter SetBackgroundProperty = new Setter();
            SetBackgroundProperty.Property = BackgroundProperty;
            SetBackgroundProperty.Value = darkMode ? BackGroundDark : BackGroundLight2;

            // Stile custom per cella standard
            Style NewStyle = new Style();
            NewStyle.Setters.Add(SetBorderBrush);
            NewStyle.Setters.Add(SetBackgroundProperty);

            // Main
            this.Background = darkMode ? BackGroundDark2 : BackGroundLight2;
            ToolBarTrayMain.Background = darkMode ? BackGroundDark2 : Brushes.White;

            textBoxModbusAddress.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxModbusAddress.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            tabControlMain.Background = darkMode ? BackGroundDark2 : BackGroundLight2;
            GridConnection.Background = darkMode ? BackGroundDark : BackGroundLight;

            labelSerialRtu.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelPort.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelBaudRate.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelParity.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelStopBits.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            labelTcp.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelIp.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelPort1.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            ToolBarMain.Background = darkMode ? BackGroundDark : new SolidColorBrush(Color.FromArgb(255, (byte)238, (byte)245, (byte)253));

            labelConnection.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            radioButtonModeSerial.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            radioButtonModeTcp.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelModBusAddress.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelRunning.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            CheckBoxPinWIndow.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelTx.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelRx.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            // Coils
            dataGridTabCoilsRegisters.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabCoilsValues.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabCoilsNotes.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            dataGridTabCoilsRegisters.CellStyle = NewStyle;
            dataGridTabCoilsValues.CellStyle = NewStyle;
            dataGridTabCoilsNotes.CellStyle = NewStyle;

            GridCoils.Background = darkMode ? BackGroundDark : BackGroundLight;

            labelCoils_0.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_1.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelOffsetHiddenCoils.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_2.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_5.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_8.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_13.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            CheckBoxSendValuesOnEditCoillsTable.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            StackPanelCoils0.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelCoils1.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelCoils2.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelCoils3.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelCoils4.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelCoils5.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));

            labelCoils_3.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_4.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_6.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_7.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_9.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_10.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_11.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_12.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_14.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_15.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelCoils_16.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            dataGridViewCoils.Background = darkMode ? BackGroundDark : BackGroundLight;

            // Inputs
            dataGridTabInputsRegisters.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabInputsValues.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabInputsNotes.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            dataGridTabInputsRegisters.CellStyle = NewStyle;
            dataGridTabInputsValues.CellStyle = NewStyle;
            dataGridTabInputsNotes.CellStyle = NewStyle;

            GridInputs.Background = darkMode ? BackGroundDark : BackGroundLight;

            labelInputs_0.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputs_1.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelOffsetHiddenInput.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputs_2.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputs_5.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            StackPanelInputs0.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelInputs1.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));

            labelInputs_3.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputs_4.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputs_6.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputs_7.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            dataGridViewInput.Background = darkMode ? BackGroundDark : BackGroundLight;

            // Input register
            dataGridTabInputRegistersRegisters.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabInputRegistersValues.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabInputRegistersBinaryValues.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabInputRegistersConvertedValues.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabInputRegistersNotes.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabInputRegistersMappings.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            dataGridTabInputRegistersRegisters.CellStyle = NewStyle;
            dataGridTabInputRegistersValues.CellStyle = NewStyle;
            dataGridTabInputRegistersBinaryValues.CellStyle = NewStyle;
            dataGridTabInputRegistersConvertedValues.CellStyle = NewStyle;
            dataGridTabInputRegistersNotes.CellStyle = NewStyle;
            dataGridTabInputRegistersMappings.CellStyle = NewStyle;

            GridInputRegisters.Background = darkMode ? BackGroundDark : BackGroundLight;

            labelInputRegisters_0.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputRegisters_1.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputRegisters_2.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelOffsetHiddenInputRegister.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputRegisters_3.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputRegisters_6.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            StackPanelInputRegisters0.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelInputRegisters1.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));

            labelInputRegisters_4.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputRegisters_5.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputRegisters_7.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelInputRegisters_8.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            dataGridViewInputRegister.Background = darkMode ? BackGroundDark : BackGroundLight;

            // Holding register
            dataGridTabHoldingRegistersRegisters.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabHoldingRegistersValues.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabHoldingRegistersBinaryValues.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabHoldingRegistersConvertedValues.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabHoldingRegistersNotes.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            dataGridTabHoldingRegistersMappings.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            dataGridTabHoldingRegistersRegisters.CellStyle = NewStyle;
            dataGridTabHoldingRegistersValues.CellStyle = NewStyle;
            dataGridTabHoldingRegistersBinaryValues.CellStyle = NewStyle;
            dataGridTabHoldingRegistersConvertedValues.CellStyle = NewStyle;
            dataGridTabHoldingRegistersNotes.CellStyle = NewStyle;
            dataGridTabHoldingRegistersMappings.CellStyle = NewStyle;

            GridHoldingRegisters.Background = darkMode ? BackGroundDark : BackGroundLight;

            labelHoldingRegisters_0.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_1.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_2.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelOffsetHiddenHolding.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_3.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_6.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_9.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_14.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            CheckBoxSendValuesOnEditHoldingTable.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            StackPanelHoldingRegisters0.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelHoldingRegisters1.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelHoldingRegisters2.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelHoldingRegisters3.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelHoldingRegisters4.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));
            StackPanelHoldingRegisters5.Background = darkMode ? BackGroundDark2 : new SolidColorBrush(Color.FromArgb(255, (byte)240, (byte)248, (byte)255));

            labelHoldingRegisters_4.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_5.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_7.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_8.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_10.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_11.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_12.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_13.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_15.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_16.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelHoldingRegisters_17.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            dataGridViewHolding.Background = darkMode ? BackGroundDark : BackGroundLight;

            // Diagnostic
            GridDiagnostic.Background = darkMode ? BackGroundDark : BackGroundLight;

            labelDiagnostic_0.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelDiagnostic_1.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelDiagnostic_2.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelDiagnostic_3.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelDiagnostic_4.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelDiagnostic_5.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelDiagnostic_6.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelDiagnostic_7.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelDiagnostic_8.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelDiagnostic_9.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelDiagnostic_10.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            textBoxDiagnosticResponse.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxDiagnosticFunctionManual.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxManualDiagnosticResponse.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            textBoxDiagnosticResponse.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxDiagnosticFunctionManual.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxManualDiagnosticResponse.Background = darkMode ? BackGroundDark : BackGroundLight2;

            // Log
            GridLog.Background = darkMode ? BackGroundDark : BackGroundLight;

            checkBoxAddLinesToEnd.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            // Settings
            GridSettings.Background = darkMode ? BackGroundDark : BackGroundLight;
            labelSettings_0.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelSettings_1.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelSettings_2.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            checkBoxUseOffsetInTextBox.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            checkBoxFollowModbusProtocol.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            checkBoxCellColorMode.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            checkBoxViewTableWithoutOffset.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            checkBoxCloseConsolAfterBoot.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            CheckBoxDarkMode.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelColorCellRead.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelColorCellWrote.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelColorCellError.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelSettings_3.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelSettings_4.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            labelSettings_5.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;

            // Menu
            menuStrip.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            menuStrip.Background = darkMode ? BackGroundDark : BackGroundLight;

            // Controlli
            textBoxCoilsOffset.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsOffset.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilsAddress01.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsAddress01.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilNumber.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilNumber.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilsRange_A.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsRange_A.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilsRange_B.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsRange_B.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilsAddress05.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsAddress05.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilsValue05.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsValue05.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilsAddress05_b.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsAddress05_b.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilsValue05_b.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsValue05_b.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilsAddress15_A.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsAddress15_A.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilsAddress15_B.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsAddress15_B.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxCoilsValue15.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxCoilsValue15.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxGoToCoilAddress.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxGoToCoilAddress.Background = darkMode ? BackGroundDark : BackGroundLight2;

            textBoxInputOffset.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxInputOffset.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxInputAddress02.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxInputAddress02.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxInputNumber.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxInputNumber.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxInputRange_A.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxInputRange_A.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxInputRange_B.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxInputRange_B.Background = darkMode ? BackGroundDark : BackGroundLight2;

            textBoxInputRegOffset.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxInputRegOffset.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxInputRegisterAddress04.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxInputRegisterAddress04.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxInputRegisterNumber.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxInputRegisterNumber.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxInputRegisterRange_A.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxInputRegisterRange_A.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxInputRegisterRange_B.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxInputRegisterRange_B.Background = darkMode ? BackGroundDark : BackGroundLight2;

            textBoxHoldingOffset.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingOffset.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingAddress03.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingAddress03.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingRegisterNumber.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingRegisterNumber.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingRange_A.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingRange_A.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingRange_B.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingRange_B.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingAddress06.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingAddress06.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingValue06.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingValue06.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingAddress06_b.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingAddress06_b.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingValue06_b.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingValue06_b.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingAddress16_A.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingAddress16_A.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingAddress16_B.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingAddress16_B.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxHoldingValue16.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxHoldingValue16.Background = darkMode ? BackGroundDark : BackGroundLight2;
            textBoxGoToHoldingAddress.Foreground = darkMode ? ForeGroundDark : ForeGroundLight;
            textBoxGoToHoldingAddress.Background = darkMode ? BackGroundDark : BackGroundLight2;

            // Cancello le raccolte delle tabelle
            list_holdingRegistersTable.Clear();
            list_inputRegistersTable.Clear();
            list_inputsTable.Clear();
            list_coilsTable.Clear();
        }

        private void ButtonResetLightMode_Click(object sender, RoutedEventArgs e)
        {
            colorDefaultReadCell_Light = new SolidColorBrush(Color.FromArgb(0xFF, 0x87, 0xCE, 0xFA));
            colorDefaultWriteCell_Light = Brushes.LightGreen;
            colorErrorCell_Light = Brushes.Orange;

            labelColorCellRead.Background = colorDefaultReadCell_Light;
            labelColorCellWrote.Background = colorDefaultWriteCell_Light;
            labelColorCellError.Background = colorErrorCell_Light;

            CheckBoxDarkMode.IsChecked = false;
        }

        private void ButtonResetDarkMode_Click(object sender, RoutedEventArgs e)
        {
            colorDefaultReadCell_Dark = new SolidColorBrush(Color.FromArgb(0xFF, 85, 85, 85));
            colorDefaultWriteCell_Dark = new SolidColorBrush(Color.FromArgb(0xFF, 0, 128, 0));
            colorErrorCell_Dark = Brushes.Orange;

            labelColorCellRead.Background = colorDefaultReadCell_Dark;
            labelColorCellWrote.Background = colorDefaultWriteCell_Dark;
            labelColorCellError.Background = colorErrorCell_Dark;

            CheckBoxDarkMode.IsChecked = true;
        }
    }

    // Classe per caricare dati dal file di configurazione json, vecchia versione salvataggio
    public class SAVE
    {
        // Variabili interfaccia 
        public bool usingSerial { get; set; } //True -> Serial, False -> TCP

        public string modbusAddress { get; set; }

        //Vatiabili configurazione seriale
        public int serialPort { get; set; }
        public int serialSpeed { get; set; }
        public int serialParity { get; set; }
        public int serialStop { get; set; }

        public bool serialMaster { get; set; } //True -> Master, False -> False
        public bool serialRTU { get; set; }

        //Variabili configurazione tcp
        public string tcpClientIpAddress { get; set; }
        public string tcpClientPort { get; set; }
        public string tcpServerIpAddress { get; set; }
        public string tcpServerPort { get; set; }

        //VARIABILI TABPAGE

        //TabPage1 (Coils)
        public string textBoxCoilsAddress01 { get; set; }
        public string textBoxCoilNumber { get; set; }
        public string textBoxCoilsRange_A { get; set; }
        public string textBoxCoilsRange_B { get; set; }
        public string textBoxCoilsAddress05 { get; set; }
        public string textBoxCoilsValue05 { get; set; }
        public string textBoxCoilsAddress15_A { get; set; }
        public string textBoxCoilsAddress15_B { get; set; }
        public string textBoxCoilsValue15 { get; set; }
        public string textBoxGoToCoilAddress { get; set; }


        //TabPage2 (inputs)
        public string textBoxInputAddress02 { get; set; }
        public string textBoxInputNumber { get; set; }
        public string textBoxInputRange_A { get; set; }
        public string textBoxInputRange_B { get; set; }
        public string textBoxGoToInputAddress { get; set; }

        //TabPage3 (input registers)
        public string textBoxInputRegisterAddress04 { get; set; }
        public string textBoxInputRegisterNumber { get; set; }
        public string textBoxInputRegisterRange_A { get; set; }
        public string textBoxInputRegisterRange_B { get; set; }
        public string textBoxGoToInputRegisterAddress { get; set; }

        //TabPage4 (holding registers)
        public string textBoxHoldingAddress03 { get; set; }
        public string textBoxHoldingRegisterNumber { get; set; }
        public string textBoxHoldingRange_A { get; set; }
        public string textBoxHoldingRange_B { get; set; }
        public string textBoxHoldingAddress06 { get; set; }
        public string textBoxHoldingValue06 { get; set; }
        public string textBoxHoldingAddress16_A { get; set; }
        public string textBoxHoldingAddress16_B { get; set; }
        public string textBoxHoldingValue16 { get; set; }
        public string textBoxGoToHoldingAddress { get; set; }

        //TabPage5 (diagnostic)

        //TabPage6 (summary)
        public bool statoConsole { get; set; }

        //Altri elementi aggiunti dopo
        public string comboBoxCoilsAddress01_ { get; set; }
        public string comboBoxCoilsRange_A_ { get; set; }
        public string comboBoxCoilsRange_B_ { get; set; }
        public string comboBoxCoilsAddress05_ { get; set; }
        public string comboBoxCoilsValue05_ { get; set; }
        public string comboBoxCoilsAddress15_A_ { get; set; }
        public string comboBoxCoilsAddress15_B_ { get; set; }
        public string comboBoxCoilsValue15_ { get; set; }
        public string comboBoxInputAddress02_ { get; set; }
        public string comboBoxInputRange_A_ { get; set; }
        public string comboBoxInputRange_B_ { get; set; }
        public string comboBoxInputRegisterAddress04_ { get; set; }
        public string comboBoxInputRegisterRange_A_ { get; set; }
        public string comboBoxInputRegisterRange_B_ { get; set; }
        public string comboBoxHoldingAddress03_ { get; set; }
        public string comboBoxHoldingRange_A_ { get; set; }
        public string comboBoxHoldingRange_B_ { get; set; }
        public string comboBoxHoldingAddress06_ { get; set; }
        public string comboBoxHoldingValue06_ { get; set; }
        public string comboBoxHoldingAddress16_A_ { get; set; }
        public string comboBoxHoldingAddress16_B_ { get; set; }
        public string comboBoxHoldingValue16_ { get; set; }

        public string comboBoxCoilsRegistri_ { get; set; }
        public string comboBoxInputRegistri_ { get; set; }
        public string comboBoxInputRegRegistri_ { get; set; }
        public string comboBoxInputRegValori_ { get; set; }
        public string comboBoxHoldingRegistri_ { get; set; }
        public string comboBoxHoldingValori_ { get; set; }

        public string comboBoxCoilsAddress05_b_ { get; set; }
        public string comboBoxCoilsValue05_b_ { get; set; }
        public string comboBoxHoldingAddress06_b_ { get; set; }
        public string comboBoxHoldingValue06_b_ { get; set; }

        public string comboBoxCoilsOffset_ { get; set; }
        public string comboBoxInputOffset_ { get; set; }
        public string comboBoxInputRegOffset_ { get; set; }
        public string comboBoxHoldingOffset_ { get; set; }

        public string textBoxCoilsAddress05_b_ { get; set; }
        public string textBoxCoilsValue05_b_ { get; set; }
        public string textBoxHoldingAddress06_b_ { get; set; }
        public string textBoxHoldingValue06_b_ { get; set; }

        public string textBoxCoilsOffset_ { get; set; }
        public string textBoxInputOffset_ { get; set; }
        public string textBoxInputRegOffset_ { get; set; }
        public string textBoxHoldingOffset_ { get; set; }

        public bool checkBoxUseOffsetInTables_ { get; set; }
        public bool checkBoxUseOffsetInTextBox_ { get; set; }
        public bool checkBoxFollowModbusProtocol_ { get; set; }
        public bool checkBoxSavePackets_ { get; set; }
        public bool checkBoxCloseConsolAfterBoot_ { get; set; }
        public bool checkBoxCellColorMode_ { get; set; }
        public bool checkBoxViewTableWithoutOffset_ { get; set; }

        public string textBoxSaveLogPath_ { get; set; }

        public string comboBoxDiagnosticFunction_ { get; set; }

        public string textBoxDiagnosticFunctionManual_ { get; set; }

        public string colorDefaultReadCell_ { get; set; }
        public string colorDefaultWriteCell_ { get; set; }
        public string colorErrorCell_ { get; set; }

        public string pathToConfiguration_ { get; set; }

        public string TextBoxPollingInterval_ { get; set; }

        public bool? CheckBoxSendValuesOnEditCoillsTable_ { get; set; }
        public bool? CheckBoxSendValuesOnEditHoldingTable_ { get; set; }

        public string language { get; set; }

        public string textBoxReadTimeout { get; set; }
    }

    public class dataGridJson
    {
        public ModBusItem_Save[] items { get; set; }
    }

    public class ModBusItem_Save 
    {
        public string Offset { get; set; }
        public string Register { get; set; }
        public string Value { get; set; }
        public string Notes { get; set; }
    }
}
