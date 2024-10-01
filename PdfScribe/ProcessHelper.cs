using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PdfScribe
{
    public static class ProcessHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct ParentProcessUtilities
        {
            // These members must match PROCESS_BASIC_INFORMATION
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reserved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass,
            ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        /// Gets the parent process of the specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(int id)
        {
            var process = Process.GetProcessById(id);
            return GetParentProcess(process.Handle);
        }

        /// <summary>
        /// Gets the parent process of the specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(IntPtr handle)
        {
            var pbi = new ParentProcessUtilities();
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }

        public static void SetProcessWorkingSet(
            int minSize,
            int maxSize)
        {
            try
            {
                var process = Process.GetCurrentProcess();
                process.MinWorkingSet = (IntPtr)minSize;
                process.MaxWorkingSet = (IntPtr)maxSize;

                Trace.TraceInformation(string.Format(Properties.Resources.SetProcessWorkingSetMsg, process.MinWorkingSet.ToString(), process.MaxWorkingSet.ToString()));
            }
            catch (Exception exc)
            {
                Trace.TraceError(Properties.Resources.SetProcessWorkingSetError, exc);
            }
        }

        public static void MinimizeProcessWorkingSet()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                process.MaxWorkingSet = process.MaxWorkingSet;

                Trace.TraceInformation(string.Format(Properties.Resources.MinimizeProcessWorkingSetMsg, process.WorkingSet64.ToString()));

                process.Dispose();
            }
            catch (Exception exc)
            {
                Trace.TraceError(Properties.Resources.MinimizeProcessWorkingSetError, exc);
            }
        }
    }
}
