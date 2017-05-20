using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;

namespace Nistec.Diagnostics
{
    public static class ProcessExtensions
    {
        /// <summary>
        /// Get the child processes for a given process
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static List<Process> GetChildProcesses(this Process process)
        {
            var results = new List<Process>();

            // query the management system objects for any process that has the current
            // process listed as it's parentprocessid
            string queryText = string.Format("select processid from win32_process where parentprocessid = {0}", process.Id);
            using (var searcher = new ManagementObjectSearcher(queryText))
            {
                foreach (var obj in searcher.Get())
                {
                    object data = obj.Properties["processid"].Value;
                    if (data != null)
                    {
                        // retrieve the process
                        var childId = Convert.ToInt32(data);
                        var childProcess = Process.GetProcessById(childId);

                        // ensure the current process is still live
                        if (childProcess != null)
                            results.Add(childProcess);
                    }
                }
            }
            return results;
        }
        /// <summary>
        /// Get the Parent Process ID for a given process
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static int? GetParentId(this Process process)
        {
            // query the management system objects
            string queryText = string.Format("select parentprocessid from win32_process where processid = {0}", process.Id);
            using (var searcher = new ManagementObjectSearcher(queryText))
            {
                foreach (var obj in searcher.Get())
                {
                    object data = obj.Properties["parentprocessid"].Value;
                    if (data != null)
                        return Convert.ToInt32(data);
                }
            }
            return null;
        }

        #region Process

        public static string RunProcessWithResults(string url, string args)
        {
            string response = null;

            ProcessStartInfo psi = new ProcessStartInfo(url);
            psi.RedirectStandardOutput = true;
            psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            psi.Arguments = args;
            //Process proc;
            using (Process exeProcess = Process.Start(psi))
            {
                System.IO.StreamReader stream = exeProcess.StandardOutput;
                exeProcess.WaitForExit();// (2000);
                if (exeProcess.HasExited)
                {
                    string output = stream.ReadToEnd();
                    response = output;
                }
            }
            return response;
        }

        public static void RunProcess(string url, string args)
        {

            // For the example
            //string alarmFileName = SrvConfig.SystemAlarmUrl;
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = url;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = args;


            // Start the process with the info we specified.
            // Call WaitForExit and then the using statement will close.
            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }

        #endregion

        public static void OpenProcessFile(string file)
        {
            ExecuteProcessFile(file);
        }

        public static void ExecuteProcessFile(string command)
        {
            System.Diagnostics.ProcessStartInfo p = new System.Diagnostics.ProcessStartInfo(command);
            p.UseShellExecute = true;
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = p;
            process.Start();
        }

    }

  
}
