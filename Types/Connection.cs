// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// David Alpert (david@spinthemoose.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;
using NAnt.Core.Types;

using EnterpriseDT.Net.Ftp;
using PasswordInputManager = ConsolePasswordInput.ConsolePasswordInput;

using Sourceforge.NAnt.Ftp;
using Sourceforge.NAnt.Ftp.Enum;
using Sourceforge.NAnt.Ftp.Tasks;
using Sourceforge.NAnt.Ftp.Util;


namespace Sourceforge.NAnt.Ftp.Types {
	/// <summary>
	/// Extends the <see cref="Credential"/> type to provides connection details for connecting to a remote host.
	/// </summary>
	/// <remarks>This class now acts as a wrapper to insulate the third-party libraries.</remarks>
	[ElementName("connection")]
	public class Connection : Credential {
		
		const string PasswordFile = ".passwords";
        //private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
		#region Public Instance Constructors
		/// <overloads>
		/// <summary>
		/// Initializes a new instance of the <see cref="Connection" /> class.
		/// </summary>
		/// </overloads>
		/// <summary>Init an empty connection instance.</summary>
		public Connection() {
			Init(String.Empty, String.Empty, String.Empty);
		}
		/// <summary>Init a connection instance with a server name.</summary>
		public Connection(string host)
		{
			Init(host, String.Empty, String.Empty);
		}
		/// <summary>Init a connection instance with a server name and username.</summary>
		public Connection(string host, string user)
		{
			Init(host, user, String.Empty);
		}
		/// <summary>Init a connection instance with a server name, username, and password.</summary>
		public Connection(string host, string user, string pass)
		{
			Init(host, user, pass);
		}
		/// <summary>Internal routine to perform the instance initialization.</summary>
		private void Init(string host, string user, string pass)
		{
			this.Domain = host;
			this.UserName = user;
			this.Password = pass;
		}
		#endregion Public Instance Constructors
		
		#region attributes
        /// <summary>
        /// The server target for this connection.  Provides an alternate name for the <see cref="Domain"/> attribute.
        /// </summary>
        [TaskAttribute("server", Required=true)]
        public string Server {
            get { return base.Domain; }
            set { base.Domain = StringUtils.ConvertEmptyToNull(value); }
        }
		
        [TaskAttribute("domain", Required=false, ProcessXml=false)]
        public new string Domain {
        	get { return base.Domain;}
        	set { base.Domain=value; }
        }
        #endregion
		
        #region private members
 		private FTPClient  	_client 	= null;	// reference into the FTP library
 		private FTPTask		_task		= null;	// reference used for logging
 		#endregion
                
 		#region helper functions
        /// validates a connection instance
        private void Validate() {
        
        	if (Server==null || Server==String.Empty) {
        		throw new FTPTaskException("Must specify a server before opening a connection.");
        	}
        	
        	LoadPasswords(this.ID);
        	
        	if (Password!=null && Password!=String.Empty && (UserName==null || UserName==String.Empty)) {
        		//throw new FTPTaskException("You cannot specify a password without a username.");       		
				UserName = "anonymous";
        	}
        }

        /// Loads username and password info from a file
        public bool LoadPasswords(string connectionName) {
        	bool success = true;
			try {
				XmlDocument doc = new XmlDocument();
				doc.Load(PasswordFile);
				string xpth = "/passwords/ftp[@connection='"+connectionName+"']";
				XmlNode n = doc.SelectNodes(xpth)[0];

				if (n==null) {
					success=false;
				} else {
					// credentials in password file overrides those from the build file
					// unless an override='false' attribute is included in the 'ftp' element.
					
					if ((n.Attributes["username"]!=null && n.Attributes["username"].Value!=String.Empty) 
					    && ((n.Attributes["override"]==null || n.Attributes["override"].Value.ToUpper()=="TRUE")
					        || (UserName == null || UserName==String.Empty))) {
						UserName = n.Attributes["username"].Value;
					}
					
					if ((n.Attributes["password"]!=null && n.Attributes["password"].Value!=String.Empty) 
						&& ((n.Attributes["override"]==null || n.Attributes["override"].Value.ToUpper()=="TRUE")
					        || (UserName == null || UserName==String.Empty))) {
						Password = n.Attributes["password"].Value;
					}
				}
			} catch (Exception ex) {
        		//_task.Log(Level.Info, "Error reading password file: {0}", ex.ToString());
        		ex.ToString();
        		success = false;
			}
        	return success;
        }
 		#endregion
 		
        #region connect / disconnect
        
        /// <summary>Gets a bool flag indicating whether or not the connection is open.</summary>
		/// <value><b>True</b> if the client exists; else <b>false</b>.</value>
		public bool IsConnected {
			get { return _client!=null;}
		}

		/// <summary>Open a connection to the specified server.</summary>
		/// <param name="task">The task using this connection</param>
		public void Open(FTPTask task) {

			if (_task!=null) {
				throw new FTPTaskException("This connection is already being used by "+_task.ToString());
			}

			if (task==null) {
				throw new FTPTaskException("Must specify the task that will use this connection.");
			}
	
			_task = task;
			
			if (IsConnected) {
				throw new FTPTaskException("This connection is already open.");
			}

			_task.Log(Level.Info, "Using '{0}':", ID);

			// ensure this connection object has all the info it needs
			Validate();
			
			if (!_task.Exec) {
				_task.Log(Level.Info, "-------------- Debugging the ftp queries --------------");
				_task.Log(Level.Info, "Connection will be attempted to scan remotely for <get> sets");
				_task.Log(Level.Info, "but no transfers will be attempted in either direction");
				_task.Log(Level.Info, "and neither local nor remote file trees will be modified.");
				_task.Log(Level.Info, "-------------------------------------------------------\n");				
			}
			
			_task.Log(Level.Info, "Connecting to '{0}' as '{1}' . . .", Server, UserName);
			
			_task.Log(Level.Verbose, "Instantiating the FTPClient & opening the connection...");
			_client = new FTPClient(Server);
			
			_task.Log(Level.Verbose, "Authenticating...");
			
			Login(UserName, Password);
			
			_task.Log(Level.Verbose, "and setting the connection mode to passive.");
			_client.ConnectMode = ParseConnectMode(ParseConnectMode(_task.ConnectMode));

			return;
		} // Open()
		
		/// <summary>Close a connection to a server</summary>
		public void Close() {
			if (IsConnected) {
				//throw new FTPTaskException("Connection is already closed");
				_task.Log(Level.Info, "Disconnecting from '{0}'", Server);
				_client.Quit();
				_client = null;
				_task = null;
			
			}

			return;
		} // ftpDisconnect()

        #endregion
        
        #region enum translation
		// parses connectmode attribute values, ensures that they are valid,
		// and returns the corresponding edtFTPnet enum value that can be 
		// passed directly to the edtFTPnet FTPClient.
		public static ConnectMode ParseConnectMode(string amode)
		{
			ConnectMode theMode;
			
			try {
				theMode = (ConnectMode)(ConnectMode.Parse(typeof(ConnectMode),amode.ToUpper()));
			} catch (Exception ex) {
				throw new FTPTaskException("'"+amode+"' is an invalid ConnectMode.", ex);
			}
			
			return theMode;	
//			switch (amode.ToUpper()) {
//				case "ACTIVE":
//					theMode = ConnectMode.ACTIVE;
//					break;
//				case "PASSIVE":
//					theMode = ConnectMode.PASSIVE;
//					break;
//				default:
//					throw new BuildException(String.Format("Invalid connectmode attribute '{0}'.\nMust be either 'active' or 'passive' (case-insensitive).", amode));
//			}			
//			
//			return theMode;
		}
		// parses connectmode attribute values, ensures that they are valid,
		// and returns the corresponding edtFTPnet enum value that can be 
		// passed directly to the edtFTPnet FTPClient.
		public static FTPConnectMode ParseConnectMode(ConnectMode amode)
		{
			FTPConnectMode theMode;
			
			switch (amode) {
				case ConnectMode.ACTIVE:
					theMode = FTPConnectMode.ACTIVE;
					break;
				case ConnectMode.PASSIVE:
					theMode = FTPConnectMode.PASV;
					break;
				default:
					throw new FTPTaskException(String.Format("Cannot parse ConnectionMode '{0}'.\nMust be either '{1}' or '{2}'.", amode, ConnectMode.ACTIVE, ConnectMode.PASSIVE));
			}			
			
			return theMode;
		}
        #endregion
        
        #region authentication

        /// <summary>Do a remote login while asking the user for a password through the console.</summary>
		private void Login(string user, string pass) {
			
			bool success = false;
			bool abort = false;
			
			bool promptu = user==null || user.ToUpper()=="PROMPT";
			bool promptp = pass==null || pass==String.Empty || pass.ToUpper()=="PROMPT";
			
			string defaultuser = "anonymous";

			string userprompt  = "Usr ["+defaultuser+"] > ";
			string passprompt  = "Pwd [abort] > "; // 14 chars
			
			string username = user;
			string password = pass;
			
			PasswordInputManager pim = new PasswordInputManager();
			// TODO: fix ConsolePasswordInput to correctly parse Backspace
			
			while (!success && !abort) {
				
				if (promptu) {
					_task.Log(Level.Info, "Please Enter Username:");
					
					// collect the input
					Console.Write(userprompt);
					username = Console.ReadLine();
				
					if (username==String.Empty) {
						username = defaultuser;
					}

					// update the prompt string and store the specified 
					// username in case we need another chance
					userprompt = userprompt.Replace(defaultuser, username);
					defaultuser = username;
				}
				
				if (promptp) {
					_task.Log(Level.Info, "Please Enter Password for "+username+":");
					
					// build the password prompt
					
					// collect the input
					Console.Write(passprompt);
					password = String.Empty;
					pim.PasswordInput(ref password, 256);
					Console.WriteLine(String.Empty);

					if (password==String.Empty) {
						abort = true;
					} 
				}
				
				if (!abort) {
					// todo: validate input
					try {
						_client.User(username);
						_client.Password(password);
						success = true;
					} catch (FTPException ex) {
						_task.Log(Level.Info, ex.Message);
						_task.Log(Level.Info, "Please try again.");
						promptu = true;
						promptp = true;
					}
				}
			}
			if (abort) {
				throw new FTPTaskException("Login Aborted by User");
			}
		} // Login
                
        #endregion
        
        #region navigation
		
        /// <summary>Gets the Present Working Directory (PWD) of this connection.</summary>
        /// <value>The PWD of this connection if connected, else <see cref="String.Empty">String.Empty</see></value>
        public string PWD {
			get { 
				if (IsConnected) {
					return _client.Pwd();
				} else {
					return String.Empty;
				}
			}
		}

        /// <overloads>
        /// <summary>Change Working Directory of the current connection.</summary>
        /// </overloads>
		/// <summary>Change Working Directory to the given remote path.</summary>
		/// <param name="path">The remote path to change to.  Can be absolute or relative.</param>
		/// <exception cref="FTPTaskException">The connection is closed.</exception>
		public void CWD(string path) {
			if (!IsConnected) {
				throw new FTPTaskException("Cannot execute CWD unless the connection is open.");
			}			
        	_client.ChDir(path);
        }
		/// <summary>Change Working Directory to the given remote path, creating non-existant directories as required.</summary>
		/// <param name="dir">The remote dir to change to.  Must be a direct subdirectory of the <see cref="PWD">PWD</see>.</param>
		/// <param name="createOnDemand">If <b>true</b>, the directory will be created as needed.  If <b>false</b> the request will succeed or fail as per the <b>edtFTPnet</b> library's <i>ChDir()</i> command.</param>
        public void CWD(string dir, bool createOnDemand) {
			if (createOnDemand || _task.createDirsOnDemand) {
				try {
					CWD(dir, _task.LevelExec);
				} catch (FTPException fex) {
					_task.Log(_task.LevelExec, fex.Message);
					_task.Log(_task.LevelExec, " + Creating {0} remotely.", RPath.Combine(PWD,dir));
					MkDir(dir);
					CWD(dir);
				}
			} else {
				CWD(dir, _task.LevelExec);
			}
		}
		/// <summary>Change Working Directory to the given remote path, dumping log output at the requested <see cref="Level">Level</see>.</summary>
		/// <param name="path">The remote path to change to.  Can be absolute or relative.</param>
		/// <param name="level">The <see cref="Level">Level</see> at which to emit logging output.</param>
		public void CWD(string path, Level level) {
			path = RPath.Clean(path);
			if (path!=String.Empty && path!="." && path!=PWD) {
				_task.Log(level, " + Changing remote directory to '{0}'", path);
				_task.Log(Level.Verbose, " +   Attempting CWD...");
				CWD(path);
				_task.Log(Level.Verbose, " +   CWD successful.");
			}
		}
        
		public void MkDir(string dirname) {
			if (!IsConnected) {
				throw new FTPTaskException("Cannot execute MkDir unless the connection is open.");
			}
			_client.MkDir(dirname);
		}
		
		#endregion
      
		#region dir info
		
		public FTPFile[] DirDetails(string path) {
			if (IsConnected) {
				return _client.DirDetails(path);
			} else {
				FTPFile[] result = {};
				return result;
			}
		}
		public string[] DirDetails(string path, bool full) {
			if (IsConnected) {
				return _client.Dir(path, full);
			} else {
				string[] result = {};
				return result;
			}
		}
		
		public RemotePath[] GetDirs(string path) {
			RemotePath[] list = RemotePath.FromFTPFileArray(path, DirDetails(path));
			int dircount = 0;
			foreach (RemotePath rpath in list) {
				if (rpath.IsDir) {
					dircount++;
				}
			}
			if (dircount==0) {
				RemotePath[] result = {};
				return result;
			} else {
				RemotePath[] result = new RemotePath[dircount];
				int z=0;
				foreach(RemotePath rpath in list) {
					if (rpath.IsDir) {
						result[z++] = rpath;
//						result[z].File   = rpath.File;
//						result[z++].Path = rpath.Path;
					}
				}
				return result;
			}
			
		}
		
		public RemotePath[] GetFiles(string path) {
			RemotePath[] list = RemotePath.FromFTPFileArray(path, DirDetails(path));
			int filecount = 0;
			foreach (RemotePath rpath in list) {
				if (rpath.IsFile) {
					filecount++;
				}
			}
			if (filecount==0) {
				RemotePath[] result = {};
				return result;
			} else {
				RemotePath[] result = new RemotePath[filecount];
				int z=0;
				foreach(RemotePath rpath in list) {
					if (rpath.IsFile) {
						result[z++] = rpath;
//						result[z].File   = rpath.File;
//						result[z++].Path = rpath.Path;
					}
				}
				return result;
			}
			
		}
		
		public void ShowDir(string dir) {
			_task.Log(Level.Info, "Remote Directory Listing:");
			if (IsConnected) {
				string[] dirlist = DirDetails(dir, true);
				foreach(string itemname in dirlist) {
					_task.Log(Level.Info, " + : {0}",itemname);
				}
			} else {
				_task.Log(Level.Info, "Not connected.");
			}			
		}
				
		#endregion
		
        #region simple file transfers
        
        // put one file
		public void Put(string fileName,
		                 string localpath, 
		                 string remotepath,
		                 FTPTransferType FtpType, 
		                 bool flatten, 
		                 bool createDirsOnDemand,
		                 bool updateOnly) {

			char [] dirseps = {Path.DirectorySeparatorChar, RPath.DirectorySeparatorChar};

			string localFilePath = String.Empty;	// path to 'fileName' locally, relative to 'localpath'
			string remoteFilePath = String.Empty;	// path to 'fileName' remotely, relative to 'remotepath'
			
			// convert fileName into relative paths...
			if (Path.GetDirectoryName(fileName).Length==0) {
				
				localFilePath = Path.GetFileName(fileName);
				
			} else if (Path.GetDirectoryName(fileName).StartsWith(localpath)) {
				
				// our abs path is longer than localpath, 
				// so the relative path is simple.
			
				localFilePath = fileName.Replace(localpath, String.Empty).Remove(0,1);
			
			} else if (localpath.StartsWith(Path.GetDirectoryName(fileName))) {

				// our abs path is shorter than the localpath
				// so our relative path is preceded by '..' references.
				
				localpath = localpath.Replace(Path.GetDirectoryName(fileName), String.Empty);

				int z = -1;

				z = localpath.IndexOfAny(dirseps);
				while( z >-1 ) {
					localFilePath += ".."+Path.DirectorySeparatorChar;
					localpath = localpath.Remove(z,1);
					z = localpath.IndexOfAny(dirseps);
				}
				localFilePath += Path.GetFileName(fileName);
			}
			
			// mirror the remote file path from the local file path, flattening if requested
			if (flatten) {
				remoteFilePath = Path.GetFileName(localFilePath);
			} else {
				remoteFilePath = localFilePath;
			}
			
			remoteFilePath = remoteFilePath.Replace(Path.DirectorySeparatorChar, RPath.DirectorySeparatorChar);
			
			_task.Log(_task.LevelLogfiles, "  file: {0}", localFilePath);
			_task.Log(_task.LevelExec,         "    to: {0}", Path.Combine(remotepath, remoteFilePath));

			if (IsConnected) {
				// store the pwd
				string pwd = PWD;
				
				if (_task.Exec) {
					// change to the requested directory, creating paths as required && requested
					string[] dirs = Path.GetDirectoryName(remoteFilePath).Split(dirseps);
					foreach(string dir in dirs) {
						CWD(dir, createDirsOnDemand);
					}
				}
				if (updateOnly 
				    && (DateTime.Compare(_client.ModTime(Path.GetFileName(remoteFilePath)),
				                         (new FileInfo(fileName)).LastWriteTime
				                        )>=0
				       )
				   ) {
					_task.Log(_task.LevelLogfiles, "Remote file is newer.");
				} else {
					_task.Log(Level.Verbose, "Putting the file as '{0}'", FtpType);
					_client.TransferType = FtpType;
					_client.Put(fileName, Path.GetFileName(remoteFilePath));
				}
	
				if (PWD!=pwd) {
					_task.Log(Level.Verbose, "Restoring the remote dir to {0}", pwd);
					CWD(pwd);
				}
			}
		}
		
		// get one file from the PWD to a localpath (PWD is arranged in Get.TransferFiles
		public void Get(string fileName,
		                 string localpath, 
		                 string remotepath,
		                 FTPTransferType FtpType, 
		                 bool flatten, 
		                 bool createDirsOnDemand,
		                 bool updateOnly) {

			// woohoo!  the remote parsing works for two test-cases!
			
			if (RPath.IsPathRooted(remotepath)) {
				// strip the initial directoryseperatorchar off 
				// this converts a remotepath rooted in the remote filesystem root
				// to a local path relative to the local-dir attribute.
				remotepath = remotepath.Remove(0,1);
			}
			
			DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(localpath, remotepath));

			if (flatten) {
				dirInfo = new DirectoryInfo(localpath);
			}
	
			_task.Log(_task.LevelLogfiles, "  file: {0}", RPath.Combine(PWD,fileName));
			_task.Log(_task.LevelExec,         "    to: {0}", Path.Combine(dirInfo.FullName, fileName));
//			this.Log(this.LevelExec,      "Getting {0}", fileName);
//			this.Log(Level.Verbose,   "   from {0}", PWD);
//			this.Log(this.LevelExec,      "     to {0}",dirInfo.FullName);
			
			if (IsConnected) {

				if (!dirInfo.Exists) {
					if (!createDirsOnDemand) {
						throw new FTPException("Local directory "+dirInfo.FullName+" does not exist to receive incoming FTP transfer.");
					}
					try {
						dirInfo.Create();
					} catch (Exception ex) {
						throw new FTPException("Local directory "+dirInfo.FullName+" could not be created to receive incoming FTP transfer: ", ex.Message);
					}
				}
				_client.Get(Path.Combine(dirInfo.FullName,fileName), fileName);
			}
		}
        #endregion

        #region scripting support
		public void RunScript(string script) {
			if (String.Empty != script) {
				string [] lines = Cut(script, Environment.NewLine);

				// store the strict return code state
				bool strict = _client.StrictReturnCodes;
				
				// turn strict return codes off so that we can match more easily
				_client.StrictReturnCodes = false;
				string [] validCodes = {
					"100",  // The requested action is being initiated, expect another reply before proceeding with a new command.
					"200",	// The requested action has been successfully completed.
					"300",	// The command has been accepted, but the requested action is dormant, pending receipt of further information.
					"400",	// The command was not accepted and the requested action did not take place, but the error condition is temporary and the action may be requested again.
					"500"	// The command was not accepted and the requested action did not take place
				};
					
				foreach(string line in lines) {
					if (String.Empty != line.Trim()) {
						_task.Log(Level.Verbose, "Executing {0}", line.Trim());
						//ArrayList listReturn = _client.SendCommand(line.Trim());
						string returned = _client.Quote(line.Trim(), validCodes);
						//foreach(object returned in listReturn) {
							_task.Log(Level.Info, "{0}", returned);
						//} // foreach
						//listReturn.Clear();
						//listReturn = null;
					} // if
				} // foreach
				
				// restore the original strict state
				_client.StrictReturnCodes = strict;
				
			} // if
			return;			
		}
		
		/// <summary>Cut a text like Split but with a seperator longer than one char.</summary>
		/// <param name="Input">Cut this text</param>
		/// <param name="Splitter">using this seperator</param>
		/// <returns>and return the elements as array</returns>
		private static string [] Cut(string Input, string Splitter) {

			//////////////////////////////////////////////////
			// local variables
			//////////////////////////////////////////////////
		
			string [] retVal= null;
			int position = 0;
			StringCollection sc = new StringCollection();

			//////////////////////////////////////////////////
			// input testing
			//////////////////////////////////////////////////

			if (null == Splitter) {
				throw new FTPTaskException("Cannot split a script on a null Splitter.");
			} // if

			if (String.Empty == Splitter) {
				throw new FTPTaskException("CannotSplit a script on an empty Splitter.");
			} // if

			//////////////////////////////////////////////////
			// Split
			//////////////////////////////////////////////////
      
			position = Input.IndexOf(Splitter);
		
			while(position > -1) {
				string untilSplitter = Input.Substring(0, position);
				Input = Input.Substring(position + Splitter.Length);
				position = Input.IndexOf(Splitter);
				if (untilSplitter.TrimStart().TrimEnd().Length > 0) {
					sc.Add(untilSplitter);
				} // if
			} // while
			sc.Add(Input);

			//////////////////////////////////////////////////
			// Transfer
			//////////////////////////////////////////////////
		
			retVal = new string [ sc.Count ];
			for(int run = 0; run < sc.Count; run++) {
				retVal[run] = sc[run];
			} // for
			sc = null;

			return retVal;
		} // Cut()

        #endregion
        
        #region logging - copied from NAnt.Core.Task (lines 212-287)
//        /// <summary>
//        /// Logs a message with the given priority.
//        /// </summary>
//        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
//        /// <param name="message">The message to be logged.</param>
//        /// <remarks>
//        /// <para>
//        /// The actual logging is delegated to the project.
//        /// </para>
//        /// <para>
//        /// If the <see cref="Verbose" /> attribute is set on the task and a 
//        /// message is logged with level <see cref="Level.Verbose" />, the 
//        /// priority of the message will be increased to <see cref="Level.Info" />.
//        /// when the threshold of the build log is <see cref="Level.Info" />.
//        /// </para>
//        /// <para>
//        /// This will allow individual tasks to run in verbose mode while
//        /// the build log itself is still configured with threshold 
//        /// <see cref="Level.Info" />.
//        /// </para>
//        /// </remarks>
//        public override void Log(Level messageLevel, string message) {
//            if (!IsLogEnabledFor(messageLevel)) {
//                return;
//            }
//
//            if (_verbose && messageLevel == Level.Verbose && Project.Threshold == Level.Info) {
//                Project.Log(this, Level.Info, message);
//            } else {
//                Project.Log(this, messageLevel, message);
//            }
//        }
//
//        /// <summary>
//        /// Logs a formatted message with the given priority.
//        /// </summary>
//        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
//        /// <param name="message">The message to log, containing zero or more format items.</param>
//        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
//        /// <remarks>
//        /// <para>
//        /// The actual logging is delegated to the project.
//        /// </para>
//        /// <para>
//        /// If the <see cref="Verbose" /> attribute is set on the task and a 
//        /// message is logged with level <see cref="Level.Verbose" />, the 
//        /// priority of the message will be increased to <see cref="Level.Info" />.
//        /// when the threshold of the build log is <see cref="Level.Info" />.
//        /// </para>
//        /// <para>
//        /// This will allow individual tasks to run in verbose mode while
//        /// the build log itself is still configured with threshold 
//        /// <see cref="Level.Info" />.
//        /// </para>
//        /// </remarks>
//        public override void Log(Level messageLevel, string message, params object[] args) {
//            string logMessage = string.Format(CultureInfo.InvariantCulture, message, args);
//            Log(messageLevel, logMessage);
//        }
//
//        /// <summary>
//        /// Determines whether build output is enabled for the given 
//        /// <see cref="Level" />.
//        /// </summary>
//        /// <param name="messageLevel">The <see cref="Level" /> to check.</param>
//        /// <returns>
//        /// <see langword="true" /> if messages with the given <see cref="Level" />
//        /// will be output in the build log; otherwise, <see langword="false" />.
//        /// </returns>
//        public bool IsLogEnabledFor(Level messageLevel) {
//            if (_verbose && messageLevel == Level.Verbose && Project.Threshold == Level.Info) {
//                return Level.Info >= Threshold;
//            }
//
//            return (messageLevel >= Threshold) && (messageLevel >= Project.Threshold);
//        }
        #endregion
	}
}
