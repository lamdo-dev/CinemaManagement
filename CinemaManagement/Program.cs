using System;
using System.Windows.Forms;
using CinemaManagement.Forms;

namespace CinemaManagement
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Cấu hình DPI và giao diện hệ thống cho WinForms .NET 10
            ApplicationConfiguration.Initialize();

            // Khởi chạy trực tiếp LoginForm theo mẫu UI đã code
            Application.Run(new LoginForm());
        }
    }
}