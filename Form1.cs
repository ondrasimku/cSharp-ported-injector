using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Injector_FORMS
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        public Form1()
        {
            InitializeComponent();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size, int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, IntPtr dwStackSize, IntPtr lpStartAddress,
            IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        static readonly IntPtr INTPTR_ZERO = (IntPtr)0;
        string sDllPath;
        uint _procId = 0;

        private void Form1_Load(object sender, EventArgs e)
        {
            Process[] processList = Process.GetProcesses();
            foreach (Process process in processList)
            {
                Process[] newProcess = Process.GetProcessesByName(process.ProcessName);
                ListViewItem item = new ListViewItem(process.ProcessName);
                item.SubItems.Add(process.Id.ToString());
                item.Tag = process.Id;
                listView1.Items.Add(item);
            }

        }

        private void ListView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Added to prevent errors when nothing was selected
            if (listView1.SelectedItems.Count > 0)
            {
                if (listView1.SelectedItems[0].Tag != null)
                {
                    _procId = (uint)Convert.ToInt32(listView1.SelectedItems[0].Tag);
                }
                else
                {
                    _procId = 0;
                }
            }
        }

        private void Inject_Click(object sender, EventArgs e)
        {


            if (_procId != 0 && !File.Exists(sDllPath))
            {
                MessageBox.Show("Process or file not found.");
                return;
            }

            IntPtr hndProc = OpenProcess((0x2 | 0x8 | 0x10 | 0x20 | 0x400), 1, _procId);

            if (hndProc == INTPTR_ZERO)
            {
                MessageBox.Show("Can't open process!");
                return;
            }

            IntPtr lpLLAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (lpLLAddress == INTPTR_ZERO)
            {
                MessageBox.Show("Can't get LoadLibraryA!");
                return;
            }

            IntPtr lpAddress = VirtualAllocEx(hndProc, (IntPtr)null, (IntPtr)sDllPath.Length, (0x1000 | 0x2000), 0X40);

            if (lpAddress == INTPTR_ZERO)
            {
                MessageBox.Show("Can't allocate memory!");
                return;
            }

            byte[] bytes = Encoding.ASCII.GetBytes(sDllPath);

            if (WriteProcessMemory(hndProc, lpAddress, bytes, (uint)bytes.Length, 0) == 0)
            {
                MessageBox.Show("Can't write to memory!");
                return;
            }

            if (CreateRemoteThread(hndProc, (IntPtr)null, INTPTR_ZERO, lpLLAddress, lpAddress, 0, (IntPtr)null) == INTPTR_ZERO)
            {
                MessageBox.Show("Can't create remote thread!");
                return;
            }

            CloseHandle(hndProc);
        }

        private void TextBox1_Click(object sender, EventArgs e)
        {
            sDllPath = @"";
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Title = "Select DLL File";
            openDialog.Filter = "DLL Knihovny(*.dll) | *.dll";;
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                sDllPath += openDialog.FileName;
                textBox1.Text = sDllPath;
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            Process[] processList = Process.GetProcesses();
            foreach (Process process in processList)
            {
                Process[] newProcess = Process.GetProcessesByName(process.ProcessName);
                ListViewItem item = new ListViewItem(process.ProcessName);
                item.SubItems.Add(process.Id.ToString());
                item.Tag = process.Id;
                listView1.Items.Add(item);
            }
        }
    }
}
