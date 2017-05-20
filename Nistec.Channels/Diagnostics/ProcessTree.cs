using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Nistec.Diagnostics
{
    public class ProcessTree
    {
        public ProcessTree(Process process)
        {
            this.Root = process;
            InitChildren();
        }

        // Recurively load children
        void InitChildren()
        {
            this.ChildProcesses = new List<ProcessTree>();

            // retrieve the child processes
            var childProcesses = this.Root.GetChildProcesses();

            // recursively build children
            foreach (var childProcess in childProcesses)
                this.ChildProcesses.Add(new ProcessTree(childProcess));
        }

        public Process Root { get; set; }

        public List<ProcessTree> ChildProcesses { get; set; }

        public int Id { get { return Root.Id; } }

        public string ProcessName { get { return Root.ProcessName; } }

        public long Memory { get { return Root.PrivateMemorySize64; } }

    }
}
