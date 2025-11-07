using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace MyTekkiDebloat.WinUI;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            // Extract original user SID from command line args if provided
            string? originalUserSid = null;
            if (args.Length > 0 && args[0].StartsWith("--original-user="))
            {
                originalUserSid = args[0].Substring("--original-user=".Length);
            }
            
            // Check if we're already running as admin
            if (!IsRunningAsAdmin())
            {
                // Get the original user SID before elevating
                var currentUserSid = GetCurrentUserSid();
                
                // Restart as admin, passing the original user SID
                RestartAsAdmin(currentUserSid);
                return; // Exit current instance
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            
            var mainForm = new MainForm(originalUserSid);
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application startup error:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static string GetCurrentUserSid()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User?.Value ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static void RestartAsAdmin(string originalUserSid)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule?.FileName ?? Application.ExecutablePath,
                Arguments = $"--original-user={originalUserSid}",
                Verb = "runas" // This requests admin privileges
            };

            Process.Start(processInfo);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User declined UAC prompt
            MessageBox.Show("Administrator privileges are required to run this application.\n\nThe application will now exit.", 
                "Administrator Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to restart as administrator: {ex.Message}", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}