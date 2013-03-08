using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Predication.Lumberjack
{
    /// <summary>
    /// <para>The <c type="RollingTraceListener">RollingTraceListener</c> is a TraceListener that logs to a file and checked that the log file stays below a specified file size (in bytes) every one hundred log file write operations.  When the maximum file size has been reached, the file is copied from c:\temp\AppFilename.log to c:\temp\AppFilename_old.log.  This means that there will only be two log files present at any time (the current one and the "_old" one)</para>
    /// <para>Usage: In the listeners config section the existing line (&lt;add name="LoggingTraceSwitch" type="System.Diagnostics.TextWriterTraceListener" initializeData="c:\temp\LogFileName.log"/&gt;) should be replaced with the following (&lt;add name="LoggingTraceSwitch" type="Predication.Lumberjack.RollingTraceListener, Predication.Lumberjack" initializeData="c:\temp\LogFileName.log"/&gt;) </para>
    /// <para>Maximum Filesize: By default the file size is limited to 1,000,000 bytes, but this can be changed by adding ";" and the new maximum file size (without commas) to the initialise data attribute  (e.g. &lt;add name="LoggingTraceSwitch" type="Predication.Lumberjack.RollingTraceListener, Predication.Lumberjack" initializeData="c:\temp\LogFileName.log"/>) should be replaced with the following (&lt;add name="LoggingTraceSwitch" type="Predication.Lumberjack.RollingTraceListener, Predication.Lumberjack" initializeData="c:\temp\LogFileName.log;1234567"/&gt;) </para>
    /// <para>If not initialiseData attribute is provided then the <c type="RollingTraceListener">RollingTraceListener</c> will try to guess the temporary location and application name to create a reasonable logging file path.  This is not recommended.</para>
    /// </summary>
    public class RollingTraceListener : TraceListener
    {

        #region private Members

        /// <summary>
        /// the filepath the log is written to
        /// </summary>
        private string _filepath;

        /// <summary>
        ///  the filepath the existing log file is moved to when it is too big
        /// </summary>
        private string _filepath_old;

        /// <summary>
        /// the filesize (in bytes) that is considered the upper-limit of big-ness
        /// </summary>
        private long _filesize;

        /// <summary>
        /// the number of times that file has been written to since its size was last checked
        /// </summary>
        private int _filesizeCheckCounter;

        #endregion

        #region public Constructors

        public RollingTraceListener() :
            base()
        {
            // there is no name so we need to guess
            // something like "C:\Temp\ApplicationName.log" ...
            Setup(GuessAtSuitableLogFilePath());
        }

        public RollingTraceListener(string name) :
            base(name)
        {
            Setup(name);
        }

        #endregion

        #region private Methods

        /// <summary>
        /// <para>Creates a suitable filepath for a logging file if nothing was declared in initialiseData.</para>
        /// <para>It looks for c:\temp, c:\windows\temp, C:\Users\USERNAME\AppData\Local\Temp (in that order)</para>
        /// <para>It uses the assembly name and appends ".log" (e.g. "c:\temp\Predication.Lumberjack.log")</para>
        /// </summary>
        /// <returns>a reasonable guess for a logging filepath in a writable location</returns>
        private string GuessAtSuitableLogFilePath()
        {
            string tempLocation = "C:\\Temp";
            if (!Directory.Exists(tempLocation))
            {
                tempLocation = "C:\\temp";
                if (!Directory.Exists(tempLocation))
                    tempLocation = null;
            }
            string windowsTempLocation = Environment.GetEnvironmentVariable("temp", EnvironmentVariableTarget.Machine);
            if (!Directory.Exists(windowsTempLocation))
                windowsTempLocation = null;
            string userTempLocation = Path.GetTempPath();
            if (!Directory.Exists(userTempLocation))
                userTempLocation = null;
            string baseFolder = tempLocation;
            if (string.IsNullOrEmpty(baseFolder))
            {
                baseFolder = windowsTempLocation;
                if (string.IsNullOrEmpty(baseFolder))
                {
                    baseFolder = userTempLocation;
                }
            }
            Assembly asm = Assembly.GetEntryAssembly();
            AssemblyName name = asm.GetName();
            string the_file_name = name.Name + ".log";
            string file_path = Path.Combine(baseFolder, the_file_name);
            return file_path;
        }

        /// <summary>
        /// works out the file path, the "_old" filepath and the maximum file size.
        /// </summary>
        /// <param name="initialiseData"></param>
        private void Setup(string initialiseData)
        {
            _filepath = null;
            _filepath_old = null;
            _filesize = 1000000;
            if (!string.IsNullOrEmpty(initialiseData))
            {
                string filename = initialiseData;
                if (initialiseData.Contains(';'))
                {
                    int semiColonPosition = initialiseData.LastIndexOf(";");
                    filename = initialiseData.Substring(0, semiColonPosition);
                    string numberCandidate = initialiseData.Substring(semiColonPosition + 1);
                    long maxfilesize = 0;
                    if (long.TryParse(numberCandidate, out maxfilesize))
                        _filesize = maxfilesize;
                }
                _filepath = filename;
                int dotPosition = filename.LastIndexOf('.');
                _filepath_old = filename.Insert(dotPosition, "_old");
            }
        }

        /// <summary>
        /// check the file size every hundred writes and move the file to the "_old" version
        /// if it is over the limit
        /// </summary>
        private void CheckFileSize()
        {
            if (_filesizeCheckCounter > 99)
                _filesizeCheckCounter = 0;
            if (_filesizeCheckCounter == 0)
            {
                FileInfo info = new FileInfo(_filepath);
                if (info.Exists && info.Length > _filesize)
                {
                    File.Delete(_filepath_old);
                    File.Move(_filepath, _filepath_old);
                }
            }
            _filesizeCheckCounter++;
        }

        #endregion

        #region overriden TraceListener Methods

        public override void Write(string message)
        {
            if (message != null)
            {
                CheckFileSize();
                File.AppendAllText(_filepath, message);
            }
        }

        public override void WriteLine(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                CheckFileSize();
                File.AppendAllText(_filepath, Environment.NewLine + message);
            }
        }

        #endregion

    }
}
