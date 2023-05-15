using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
namespace Encryptor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private int DownloadFile(string filePath, uint address)
        {
            bool isBinaryFile = true;
            Process process = new Process();
            if (Path.GetExtension(filePath) == ".hex")
            {
                isBinaryFile = false;
            }
            else
            {
                isBinaryFile = true;
            }
            process.StartInfo.FileName = "JFlash.exe";
            if (isBinaryFile)
            {
                process.StartInfo.Arguments = $" -openprjstm32f1.jflash -open{filePath},0x{address:X8} -connect -auto -disconnect -exit";               
            }
            else
            {
                process.StartInfo.Arguments = $" -openprjstm32f1.jflash -open{filePath} -connect -auto -disconnect -exit";
                
            }
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = false;
            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }

        private int LoadIdFile()
        {
            Process process = new Process();
            process.StartInfo.FileName = "JLink.exe";
            process.StartInfo.Arguments = " -device GD32F303RB -ExitOnError 1 -CommandFile readid.jlink";
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = false;
            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }

        private void GenerateEncryption(string idFile, string targetFile)
        {
            var bts = File.ReadAllBytes(idFile);
            var rand1 = new byte[31];
            var rand2 = new byte[31];
            Random random = new Random();
            random.NextBytes(rand1);
            random.NextBytes(rand2);

            var data = new byte[bts.Length + rand1.Length + rand2.Length];
            Array.Copy(rand1, 0, data, 0, rand1.Length);
            Array.Copy(bts, 0, data, rand1.Length, bts.Length);
            Array.Copy(rand2, 0, data, rand1.Length + bts.Length, rand2.Length);

            var crc = new byte[2];
            Utility.CRCCalc(data, data.Length, crc);


            using(FileStream fs = new FileStream(targetFile, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write(rand1);
                    writer.Write(rand2);
                    writer.Write(crc);
                }
            }
        }

        private void OnSelectProgramFile(object sender, RoutedEventArgs e)
        {
            var model = this.DataContext as MainViewModel;
            if(model != null)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == true)
                {
                    model.ProgramFile = dialog.FileName;
                }
            }
        }

        private void OnSelectParameterFile(object sender, RoutedEventArgs e)
        {
            var model = this.DataContext as MainViewModel;
            if (model != null)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == true)
                {
                    model.ParameterFile = dialog.FileName;
                }
            }
        }

        private void DownloadButtonClicked(object sender, RoutedEventArgs e)
        {
            var model = this.DataContext as MainViewModel;
            bool flag = false;
            if(model != null)
            {
                if (model.DownloadProgram)
                {
                    flag = true;
                    int exitCode = DownloadFile(model.ProgramFile, 0x08000000);
                    if (exitCode != 0)
                    {
                        MessageBox.Show("烧录主程序失败");
                        return;
                    }
                }

                if(model.DownloadParameter)
                {
                    flag = true;
                    int exitCode = DownloadFile(model.ParameterFile, 0x0801F800);
                    if (exitCode != 0)
                    {
                        MessageBox.Show("烧录参数文件失败");
                        return;
                    }
                }

                if(model.Encryption)
                {
                    flag = true;
                    int exitCode = LoadIdFile();
                    if(exitCode != 0)
                    {
                        MessageBox.Show("读取芯片ID失败");
                        return;
                    }
                    GenerateEncryption("id.bin", "encrypt.bin");//rand1+rand2+CRC    CRC=CRCCalc(rand1+ID+rand2)
                    exitCode = DownloadFile("encrypt.bin", 0x0801E000);//encrypt.bin放着rand1+rand2+CRC
                    if (exitCode != 0)
                    {
                        MessageBox.Show("烧录加密信息失败");
                        return;
                    }
                }

                if(flag)
                {
                    MessageBox.Show("烧录成功");
                }
            }
        }

        private int[] BaudRates = { 9600, 115200,2000000 };
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CbSeriPorts.ItemsSource = SerialPort.GetPortNames();
            CbSeriPorts.SelectedIndex = 0;
            Cbbaudrate.ItemsSource = BaudRates;
            Cbbaudrate.SelectedItem = 2000000;           
        }

        private SerialPort? serialport { get; set; }       
        private void CbSeriPorts_DropDownOpened(object sender, EventArgs e)
        {
            CbSeriPorts.ItemsSource = SerialPort.GetPortNames();
        }
       
        private void SendCommand(byte[] command)
        {
            // 写入命令字符串到串口
            if (!serialport.IsOpen)
                serialport.Open();
            serialport.Encoding = Encoding.Latin1;
            serialport.Write(command,0,command.Length);    
            // 等待一段时间以确保命令被发送并接收到反馈
            System.Threading.Thread.Sleep(50);  
            // 读取串口接收缓冲区中的所有可用数据作为反馈
            string response = serialport.ReadExisting();            
            byte[] res= Encoding.Latin1.GetBytes(response);            
            for(int i=0;i<res.Length;i++)
            {
                RecvData.Text += res[i].ToString("X2");
            }
            RecvData.Text += "\r\n";
            RecvData.ScrollToEnd();
            serialport.Close();
        }
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            serialport=new SerialPort();
            serialport.PortName = CbSeriPorts.Text;
            serialport.BaudRate = (int)Cbbaudrate.SelectedItem;
            serialport.Open();           
            if (serialport == null || !serialport.IsOpen)
            {
                MessageBox.Show("发送失败，串口未连接或已拔出");
            }
            else
            {               
                byte[] buf1 = new byte[] { 0x55, 0x11, 0x33, 0x77 };
                byte[] buf2 = new byte[] { 0xA1, 0x00, 0x00, 0xA1 };
                byte[] buf3 = new byte[] { 0xC1, 0x00, 0x00, 0x00, 0x20, 0x00, 0x02, 0x10, 0x00, 0xF3 };
                byte[] buf4 = new byte[] { 0xC1, 0x00, 0x00, 0x00, 0x60, 0x00, 0x02, 0x11, 0x00, 0xB2 };
                SendCommand(buf1);
                SendCommand(buf2);
                SendCommand(buf3);
                SendCommand(buf4);                                
            }         
        }     
    }
}
