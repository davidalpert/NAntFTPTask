using System;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Collections;
using System.Data;
using System.Threading;

namespace FTPClient
{
	/// <summary>
	/// A class to manage FTP connections and common FTP functions.
	/// </summary>
	public class FTPConnection
	{		
		#region constants
		private static int BLOCK_SIZE = 512;
		private static int DEFAULT_REMOTE_PORT = 21;
		private static int DATA_PORT_RANGE_FROM = 1500;
		private static int DATA_PORT_RANGE_TO = 65000;
		#endregion
		
		#region private members
		private TcpClient tcpClient;		// the client
		private FTPMode mode;				// the tcpip mode (active/passive)
		private int activeConnectionsCount;	// number of active connections
		private string remoteHost;			// the remote host

		private ArrayList messageList = new ArrayList();
		private bool logMessages;
		#endregion
		
		#region ct'rs
		/// <summary>Initializes an <see cref="FTPConnection">FTPConnection</see> object.</summary>
		public FTPConnection()
		{
			this.activeConnectionsCount = 0;
			this.mode = FTPMode.Active;
			this.logMessages = false;
		}
		#endregion

		#region public access members
		public ArrayList MessageList
		{
			get
			{
				return this.messageList;
			}
		}

		public bool LogMessages
		{
			get
			{
				return this.logMessages;
			}

			set
			{
				if(!value)
				{
					this.messageList = new ArrayList();
				}

				this.logMessages = value;
			}
		}
		#endregion
		
		#region open/close
		/// <overloads>Opens an FTP connection.</overloads>
		/// <summary>Opens an FTP connection to the given host, authenticating with the given username and password.</summary>
		/// <param name="remoteHost">The remote FTP host.</param>
		/// <param name="user">The login name to use when authenticating the connection.</param>
		/// <param name="password">The password to use when authenticating the connection.</param>
		public virtual void Open(string remoteHost, string user, string password)
		{
			Open(remoteHost, DEFAULT_REMOTE_PORT, user, password, FTPMode.Active);
		}
		/// <summary>Opens an FTP connection to the given host and the given mode, authenticating with the given username and password.</summary>
		/// <param name="remoteHost">The remote FTP host.</param>
		/// <param name="user">The login name to use when authenticating the connection.</param>
		/// <param name="password">The password to use when authenticating the connection.</param>
		/// <param name="mode">The <see cref="FTPMode">FTPMode</see> to set once the connection is open.</param>
		public virtual void Open(string remoteHost, string user, string password, FTPMode mode)
		{
			Open(remoteHost, DEFAULT_REMOTE_PORT, user, password, mode);
		}
		/// <summary>Opens an FTP connection to the given host at the given remote port, authenticating with the given username and password.</summary>
		/// <param name="remoteHost">The remote FTP host.</param>
		/// <param name="user">The login name to use when authenticating the connection.</param>
		/// <param name="password">The password to use when authenticating the connection.</param>
		/// <param name="remotePort">The remote port to request when connecting.</param>
		public virtual void Open(string remoteHost, int remotePort, string user, string password)
		{
			Open(remoteHost, remotePort, user, password, FTPMode.Active);
		}
		/// <summary>Opens an FTP connection to the given host at the given remote port, authenticating with the given username and password, and setting the given transfer mode.</summary>
		/// <param name="remoteHost">The remote FTP host.</param>
		/// <param name="user">The login name to use when authenticating the connection.</param>
		/// <param name="password">The password to use when authenticating the connection.</param>
		/// <param name="remotePort">The remote port to request when connecting.</param>
		/// <param name="pMode">The <see cref="FTPMode">FTPMode</see> to set once the connection is open.</param>
		public virtual void Open(string remoteHost, int remotePort, string user, string password, FTPMode pMode)
		{	
			ArrayList tempMessageList = new ArrayList();
			int returnValue;

			this.tcpClient = new TcpClient();
			this.mode = pMode;
			this.remoteHost = remoteHost;
			
			// As we cannot detect the local address from the TCPClient class, convert "127.0.0.1" and "localhost" to
            // the DNS record of this machine; this will ensure that the connection address and the PORT command address
            // are identical.  This fixes bug 854919.
            try {
	            if (remoteHost == "localhost" || remoteHost == "127.0.0.1")
				{
					remoteHost = LocalAddressList[0].ToString();
				}
            }
            catch (Exception ex)
            {
            	System.Diagnostics.Debug.Assert(false, ex.ToString());
            	return;
            }

			//CONNECT
			try
			{
				this.tcpClient.Connect(remoteHost, remotePort);
			}
			catch(Exception)
			{
				throw new IOException("Couldn't connect to remote server");
			}

			tempMessageList = Read();
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(returnValue != 220)
			{
				Close();
				throw new Exception((string)tempMessageList[0]);
			}

			//SEND USER
			tempMessageList = SendCommand("USER " + user);
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(!(returnValue == 331 || returnValue == 202))
			{
				Close();
				throw new Exception((string)tempMessageList[0]);
			}

			//SEND PASSWORD
			if(returnValue == 331)
			{
				tempMessageList = SendCommand("PASS " + password);
				returnValue = GetMessageReturnValue((string)tempMessageList[0]);
				if(!(returnValue == 230 || returnValue == 202))
				{
					Close();
					throw new Exception((string)tempMessageList[0]);
				}
			}
		}

		public bool IsConnected {
			get {return false;}
		}

		public virtual void Close()
		{
			ArrayList messageList = new ArrayList();

			if(this.tcpClient != null )
			{
				messageList = SendCommand("QUIT");
				this.tcpClient.Close();
			}
		}
		#endregion

		#region private utility functions
		/// <summary>Gets an array of <see cref="IPAddress">IPAddresses</see> that resolve to the host name of the local computer.</summary>
		/// <returns>An <see cref="Array">array</see> of <see cref="IPAddress">IPAddresses</see> that resolve to the host name of the local computer.</returns>
		public IPAddress[] LocalAddressList
		{
			get {return Dns.Resolve(Dns.GetHostName()).AddressList;}
		}		
		#endregion
		
		#region untested
		public ArrayList Dir(String mask)
		{
			ArrayList tmpList = Dir();

			DataTable table = new DataTable();
			table.Columns.Add("Name");
			for(int i = 0; i < tmpList.Count; i++)
			{
				DataRow row = table.NewRow();
				row["Name"] = (string)tmpList[i];
				table.Rows.Add(row);
			}

			DataRow [] rowList = table.Select("Name LIKE '" + mask + "'", "", DataViewRowState.CurrentRows);
			tmpList = new ArrayList();
			for(int i = 0; i < rowList.Length; i++)
			{
				tmpList.Add((string)rowList[i]["Name"]);
			}

			return tmpList;
		}
		
		public ArrayList Dir()
		{
			LockTcpClient();
			TcpListener listner = null;
			TcpClient client = null;
			NetworkStream networkStream = null;
			ArrayList tempMessageList = new ArrayList();
			int returnValue = 0;
			string returnValueMessage = "";
			ArrayList fileList = new ArrayList();

			SetTransferType(FTPFileTransferType.ASCII);

			if(this.mode == FTPMode.Active)
			{
				listner = CreateDataListner();
				listner.Start();
			}
			else
			{
				client = CreateDataClient();
			}

			tempMessageList = new ArrayList();
			tempMessageList = SendCommand("NLST");
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(!(returnValue == 150 || returnValue == 125))
			{
				throw new Exception((string)tempMessageList[0]);
			}

			if(this.mode == FTPMode.Active)
			{
				client = listner.AcceptTcpClient();
			}
			networkStream = client.GetStream();

			fileList = ReadLines(networkStream);

			if(tempMessageList.Count == 1)
			{
				tempMessageList = Read();
				returnValue = GetMessageReturnValue((string)tempMessageList[0]);
				returnValueMessage = (string)tempMessageList[0];
			}
			else
			{
				returnValue = GetMessageReturnValue((string)tempMessageList[1]);
				returnValueMessage = (string)tempMessageList[1];
			}

			if(!(returnValue == 226))
			{
				throw new Exception(returnValueMessage);
			}

			networkStream.Close();
			client.Close();

			if(this.mode == FTPMode.Active)
			{
				listner.Stop();
			}
			UnlockTcpClient();
			return fileList;
		}

		public void SendStream(Stream stream, string remoteFileName, FTPFileTransferType type)
		{
			LockTcpClient();
			TcpListener listner = null;
			TcpClient client = null;
			NetworkStream networkStream = null;
			ArrayList tempMessageList = new ArrayList();
			int returnValue = 0;
			string returnValueMessage = "";
			tempMessageList = new ArrayList();

			SetTransferType(type);

			if(this.mode == FTPMode.Active)
			{
				listner = CreateDataListner();
				listner.Start();
			}
			else
			{
				client = CreateDataClient();
			}

			tempMessageList = SendCommand("STOR " + remoteFileName);
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(!(returnValue == 150 || returnValue == 125))
			{
				throw new Exception((string)tempMessageList[0]);
			}

			if(this.mode == FTPMode.Active)
			{
				client = listner.AcceptTcpClient();
			}

			networkStream = client.GetStream();

			Byte[] buffer = new Byte[BLOCK_SIZE];
			int bytes = 0;
			int totalBytes = 0;

			while(totalBytes < stream.Length)
			{
				bytes = (int)stream.Read(buffer, 0, BLOCK_SIZE);
				totalBytes = totalBytes + bytes;
				networkStream.Write(buffer, 0, bytes);
			}

			stream.Close();

			networkStream.Close();
			client.Close();

			if(this.mode == FTPMode.Active)
			{
				listner.Stop();
			}

			if(tempMessageList.Count == 1)
			{
				tempMessageList = Read();
				returnValue = GetMessageReturnValue((string)tempMessageList[0]);
				returnValueMessage = (string)tempMessageList[0];
			}
			else
			{
				returnValue = GetMessageReturnValue((string)tempMessageList[1]);
				returnValueMessage = (string)tempMessageList[1];
			}

			if(!(returnValue == 226))
			{
				throw new Exception(returnValueMessage);
			}
			UnlockTcpClient();
		}

		public virtual void SendFile(string localFileName, FTPFileTransferType type)
		{
			SendFile(localFileName, Path.GetFileName(localFileName), type);
		}

		public virtual void SendFile(string localFileName, string remoteFileName, FTPFileTransferType type)
		{
			SendStream(new FileStream(localFileName,FileMode.Open), remoteFileName, type);
		}

		public void GetStream(string remoteFileName, Stream stream, FTPFileTransferType type)
		{
			TcpListener listner = null;
			TcpClient client = null;
			NetworkStream networkStream = null;
			ArrayList tempMessageList = new ArrayList();
			int returnValue = 0;
			string returnValueMessage = "";

			LockTcpClient();

			SetTransferType(type);

			if(this.mode == FTPMode.Active)
			{
				listner = CreateDataListner();
				listner.Start();
			}
			else
			{
				client = CreateDataClient();
			}

			tempMessageList = new ArrayList();
			tempMessageList = SendCommand("RETR " + remoteFileName);
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(!(returnValue == 150 || returnValue == 125))
			{
				throw new Exception((string)tempMessageList[0]);
			}

			if(this.mode == FTPMode.Active)
			{
				client = listner.AcceptTcpClient();
			}

			networkStream = client.GetStream();

			Byte[] buffer = new Byte[BLOCK_SIZE];
			int bytes = 0;

			bool read = true;
			while(read)
			{
				bytes = (int)networkStream.Read(buffer, 0, buffer.Length);
				stream.Write(buffer, 0, bytes);
				if(bytes == 0)
				{
					read = false;
				}
			}

			stream.Close();

			networkStream.Close();
			client.Close();

			if(this.mode == FTPMode.Active)
			{
				listner.Stop();
			}

			if(tempMessageList.Count == 1)
			{
				tempMessageList = Read();
				returnValue = GetMessageReturnValue((string)tempMessageList[0]);
				returnValueMessage = (string)tempMessageList[0];
			}
			else
			{
				returnValue = GetMessageReturnValue((string)tempMessageList[1]);
				returnValueMessage = (string)tempMessageList[1];
			}

			if(!(returnValue == 226))
			{
				throw new Exception(returnValueMessage);
			}

			UnlockTcpClient();
		}
		
		public virtual void GetFile(string remoteFileName, FTPFileTransferType type)
		{
			GetFile(remoteFileName, Path.GetFileName(remoteFileName), type);
		}

		public virtual void GetFile(string remoteFileName, string localFileName, FTPFileTransferType type)
		{
			GetStream(remoteFileName, new FileStream(localFileName,FileMode.Create), type);
		}

		public virtual void DeleteFile(String remoteFileName)
		{
			System.Threading.Monitor.Enter(this.tcpClient);
			ArrayList tempMessageList = new ArrayList();
			int returnValue = 0;
			tempMessageList = SendCommand("DELE " + remoteFileName);
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(returnValue != 250)
			{
				throw new Exception((string)tempMessageList[0]);
			}
			System.Threading.Monitor.Exit(this.tcpClient);
		}

		
		public virtual void MoveFile(string remoteFileName, string toRemotePath)
		{
			if(toRemotePath.Length > 0 && toRemotePath.Substring(toRemotePath.Length - 1, 1) != "/")
			{
				toRemotePath = toRemotePath + "/";
			}

			RenameFile(remoteFileName, toRemotePath + remoteFileName);
		}
		
		public virtual void RenameFile(string fromRemoteFileName, string toRemoteFileName)
		{
			System.Threading.Monitor.Enter(this.tcpClient);
			ArrayList tempMessageList = new ArrayList();
			int returnValue = 0;
			tempMessageList = SendCommand("RNFR " + fromRemoteFileName);
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(returnValue != 350)
			{
				throw new Exception((string)tempMessageList[0]);
			}
			tempMessageList = SendCommand("RNTO " + toRemoteFileName);
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(returnValue != 250)
			{
				throw new Exception((string)tempMessageList[0]);
			}
			System.Threading.Monitor.Exit(this.tcpClient);
		}

		public virtual void SetCurrentDirectory(String remotePath)
		{
			LockTcpClient();

			ArrayList tempMessageList = new ArrayList();
			int returnValue = 0;
			tempMessageList = SendCommand("CWD " + remotePath);
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(returnValue != 250)
			{
				throw new Exception((string)tempMessageList[0]);
			}
			UnlockTcpClient();
		}
		
		private void SetTransferType(FTPFileTransferType type)
		{
			switch (type)
			{
				case FTPFileTransferType.ASCII:
					SetMode("TYPE A");
					break;
				case FTPFileTransferType.Binary:
					SetMode("TYPE I");
					break;
				default:
					throw new Exception("Invalid File Transfer Type");
			}
		}

		private void SetMode(string mode)
		{
			LockTcpClient();

			ArrayList tempMessageList = new ArrayList();
			int returnValue = 0;
			tempMessageList = SendCommand(mode);
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(returnValue != 200)
			{
				throw new Exception((string)tempMessageList[0]);
			}
			UnlockTcpClient();
		}
		
		private TcpListener CreateDataListner()
		{
			int port = GetPortNumber();
			SetDataPort(port);

			TcpListener listner = new TcpListener(port);
			return listner; 
		}
		
		private TcpClient CreateDataClient()
		{
			int port = GetPortNumber();

			//IPEndPoint ep = new IPEndPoint(GetLocalAddressList()[0], port);

			TcpClient client = new TcpClient();

			//client.Connect(ep);
			client.Connect(this.remoteHost, port);

			return client;
		}
		
		private void SetDataPort(int portNumber)
		{
			LockTcpClient();

			ArrayList tempMessageList = new ArrayList();
			int returnValue = 0;
			int portHigh = portNumber >> 8;
			int portLow = portNumber & 255;

			tempMessageList = SendCommand("PORT " 
				+ LocalAddressList[0].ToString().Replace(".", ",")
				+ "," + portHigh.ToString() + "," + portLow);

			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(returnValue != 200)
			{
				throw new Exception((string)tempMessageList[0]);
			}
			UnlockTcpClient();

		}

		public virtual void MakeDir(string directoryName)
		{
			LockTcpClient();

			ArrayList tempMessageList = new ArrayList();
			int returnValue = 0;

			tempMessageList = SendCommand("MKD " + directoryName);
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(returnValue != 257)
			{
				throw new Exception((string)tempMessageList[0]);
			}

			UnlockTcpClient();
		}

		public virtual void RemoveDir(string directoryName)
		{
			LockTcpClient();

			ArrayList tempMessageList = new ArrayList();
			int returnValue = 0;

			tempMessageList = SendCommand("RMD " + directoryName);
			returnValue = GetMessageReturnValue((string)tempMessageList[0]);
			if(returnValue != 250)
			{
				throw new Exception((string)tempMessageList[0]);
			}

			UnlockTcpClient();
		}

		public ArrayList SendCommand(String command)
		{
			this.activeConnectionsCount++;

			Byte[] cmdBytes = Encoding.ASCII.GetBytes((command+"\r\n").ToCharArray());
			NetworkStream stream = this.tcpClient.GetStream();
			stream.Write(cmdBytes, 0, cmdBytes.Length);

			this.activeConnectionsCount--;

			return Read();
		}

		private ArrayList Read ()
		{
			NetworkStream stream = this.tcpClient.GetStream();
			ArrayList messageList = new ArrayList();

			ArrayList tempMessage = ReadLines(stream);

			int tryCount = 0;
			while(tempMessage.Count == 0)
			{
				if(tryCount == 10)
				{
					throw new Exception("Server does not return message to the message");
				}

				Thread.Sleep(100);
				tryCount++;
				tempMessage = ReadLines(stream);
			}

			while(((string)tempMessage[tempMessage.Count - 1]).Substring(3, 1) == "-")
			{
				messageList.AddRange(tempMessage);
				tempMessage = ReadLines(stream);
			}
			messageList.AddRange(tempMessage);

			AddMessagesToMessageList(messageList);

			return messageList;
		}
		
		private ArrayList ReadLines(NetworkStream stream)
		{
			ArrayList messageList = new ArrayList();
			char[] seperator = {'\n'};
			char[] toRemove = {'\r'};
			Byte[] buffer = new Byte[BLOCK_SIZE];
			int bytes = 0;
			string tmpMes = "";
			bool read = true;

			while(read)
			{
				bytes = stream.Read(buffer, 0, buffer.Length);
				tmpMes += Encoding.ASCII.GetString(buffer, 0, bytes);
				if(bytes < buffer.Length)
				{
					read = false;
				}
			}

			string[] mess = tmpMes.Split(seperator);
			for (int i = 0; i < mess.Length; i++)
			{
				if(mess[i].Length > 0)
				{
					messageList.Add(mess[i].Trim(toRemove));
				}
			}

			return messageList;
		}

		private int GetMessageReturnValue(string message)
		{
			return int.Parse(message.Substring(0, 3));
		}

		private int GetPortNumber()
		{
			LockTcpClient();
			int port = 0;
			switch (this.mode)
			{
				case FTPMode.Active:
					Random rnd = new Random((int)DateTime.Now.Ticks);
					port = DATA_PORT_RANGE_FROM + rnd.Next(DATA_PORT_RANGE_TO - DATA_PORT_RANGE_FROM);
					break;
				case FTPMode.Passive:
					ArrayList tempMessageList = new ArrayList();
					int returnValue = 0;
					tempMessageList = SendCommand("PASV");
					returnValue = GetMessageReturnValue((string)tempMessageList[0]);
					if(returnValue != 227)
					{
						if(((string)tempMessageList[0]).Length > 4)
						{
							throw new Exception((string)tempMessageList[0]);
						}
						else
						{
							throw new Exception((string)tempMessageList[0] + " Passive Mode not implemented");
						}
					}
					string message = (string)tempMessageList[0];
					int index1 = message.IndexOf(",", 0);
					int index2 = message.IndexOf(",", index1 + 1);
					int index3 = message.IndexOf(",", index2 + 1);
					int index4 = message.IndexOf(",", index3 + 1);
					int index5 = message.IndexOf(",", index4 + 1);
					int index6 = message.IndexOf(")", index5 + 1);
					port = 256 * int.Parse(message.Substring(index4 + 1, index5 - index4 - 1)) + int.Parse(message.Substring(index5 + 1, index6 - index5 - 1));
					break;
			}
			UnlockTcpClient();
			return port;
		}

		private void AddMessagesToMessageList(ArrayList messages)
		{
			if(this.logMessages)
			{
				this.messageList.AddRange(messages);
			}
		}

		private void LockTcpClient()
		{
			System.Threading.Monitor.Enter(this.tcpClient);
		}

		private void UnlockTcpClient()
		{
			System.Threading.Monitor.Exit(this.tcpClient);
		}
		#endregion
	}
}
