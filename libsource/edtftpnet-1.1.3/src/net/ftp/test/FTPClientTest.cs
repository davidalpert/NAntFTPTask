// edtFTPnet
// 
// Copyright (C) 2004 Enterprise Distributed Technologies Ltd
// 
// www.enterprisedt.com
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 
// Bug fixes, suggestions and comments should posted on 
// http://www.enterprisedt.com/forums/index.php
// 
// Change Log:
// 
// $Log: FTPClientTest.cs,v $
// Revision 1.6  2004/11/06 22:38:25  bruceb
// tidied events
//
// Revision 1.4  2004/11/05 20:00:43  bruceb
// event testing
//
// Revision 1.3  2004/10/29 09:41:44  bruceb
// removed /// in file header
//

using System;
using System.Text;
using EnterpriseDT.Net.Ftp;
using EnterpriseDT.Util.Debug;

namespace EnterpriseDT.Net.Ftp.Test
{  
	/// <summary>  
	/// Basic test harness
	/// </summary>
	/// <author> 
	/// Bruce Blackshaw         
	/// </author>
	/// <version>         
	/// $Revision: 1.6 $
	/// </version>
	public class FTPClientTest
	{
		/// <summary> 
        /// Initialize logger
        /// </summary>
	    static FTPClientTest()
	    {
	        log = Logger.GetLogger(typeof(FTPClientTest));
	    }
	   
		/// <summary> 
        /// Logger
        /// </summary>
		internal static Logger log;
			
		/// <summary> 
        /// Logs start of transfer via event
        /// </summary>
	    internal static void LogTransferStarted(object obj, EventArgs args) 
	    {
	        log.Debug("Transfer Started");
	    }

		/// <summary> 
        /// Logs end of transfer via event
        /// </summary>
	    internal static void LogTransferComplete(object obj, EventArgs args) 
	    {
	        log.Debug("Transfer Complete");
	    }

		/// <summary> 
        /// Logs count of bytes transferred via event
        /// </summary>
	    internal static void BytesTransferred(object obj, BytesTransferredEventArgs args) 
	    {
	        log.Debug("Transferred: " + Convert.ToString(args.ByteCount));
	    }
	    
		/// <summary> 
        /// Log of messages
        /// </summary>
		internal static StringBuilder messages = new StringBuilder();
				
		/// <summary> 
		/// Log an FTP command being sent to the server
		/// </summary>
		internal static void LogCommand(object client, FTPMessageEventArgs message)
		{
			messages.Append(message.Message).Append("\n");
		}
		
		/// <summary> 
		/// Log an FTP reply being sent back to the client
		/// </summary>
		internal static void LogReply(object client, FTPMessageEventArgs message)
		{
			messages.Append(message.Message).Append("\n");
		}
	    
		/// <summary>   
		/// Test harness
		/// </summary>
		public static void Main(string[] args)
		{
			
			// we want remote host, user name and password
			if (args.Length < 7)
			{
				log.Debug(Convert.ToString(args.Length));
				Usage();
				System.Environment.Exit(1);
			}
			try
			{	
				// assign args to make it clear
				string host = args[0];
				string user = args[1];
				string password = args[2];
				string filename = args[3];
				string directory = args[4];
				string mode = args[5];
				string connMode = args[6];
				
				FTPClient ftp = new FTPClient();
				ftp.RemoteHost = host;
				ftp.ControlPort = 21;
				
				// set up message collector
				ftp.CommandSent += new FTPMessageHandler(FTPClientTest.LogCommand);
				ftp.ReplyReceived += new FTPMessageHandler(FTPClientTest.LogReply);
				ftp.TransferStarted += new EventHandler(FTPClientTest.LogTransferStarted);
				ftp.TransferComplete += new EventHandler(FTPClientTest.LogTransferComplete);
				
				// connect 
				ftp.Connect();
				ftp.Login(user, password);
				ftp.Quit();
				
				// connect again
				ftp = new FTPClient(host);
				ftp.CommandSent += new FTPMessageHandler(FTPClientTest.LogCommand);
				ftp.ReplyReceived += new FTPMessageHandler(FTPClientTest.LogReply);
				ftp.TransferStarted += new EventHandler(FTPClientTest.LogTransferStarted);
				ftp.TransferComplete += new EventHandler(FTPClientTest.LogTransferComplete);
				ftp.BytesTransferred += new BytesTransferredHandler(FTPClientTest.BytesTransferred);
				
				ftp.Login(user, password);
				
				// binary transfer
				if (mode.ToUpper().Equals("BINARY".ToUpper()))
				{
					ftp.TransferType = FTPTransferType.BINARY;
				}
				else if (mode.ToUpper().Equals("ASCII".ToUpper()))
				{
					ftp.TransferType = FTPTransferType.ASCII;
				}
				else
				{
					log.Debug("Unknown transfer type: " + args[5]);
					System.Environment.Exit(- 1);
				}
				
				// PASV or active?
				if (connMode.ToUpper().Equals("PASV".ToUpper()))
				{
					ftp.ConnectMode = FTPConnectMode.PASV;
				}
				else if (connMode.ToUpper().Equals("ACTIVE".ToUpper()))
				{
					ftp.ConnectMode = FTPConnectMode.ACTIVE;
				}
				else
				{
					log.Debug("Unknown connect mode: " + args[6]);
					System.Environment.Exit(- 1);
				}
				
				// change dir
				ftp.ChDir(directory);
				
				// Put a local file to remote host
				ftp.Put(filename, filename);
				
				// get bytes
				byte[] buf1 = ftp.Get(filename);
				log.Debug("Got " + buf1.Length + " bytes");
				
				// append local file
				try
				{
					ftp.Put(filename, filename, true);
				}
				catch (FTPException ex)
				{
					log.Debug("Append failed: " + ex.Message);
				}
				
				// get bytes again - should be 2 x
				//byte[] buf2 = ftp.Get(filename);
				//log.Debug("Got " + buf2.Length + " bytes");
				
				// rename
				ftp.Rename(filename, filename + ".new");
				
				// get a remote file - the renamed one
				ftp.Get(filename + ".tst", filename + ".new");
                
                // delete the remote file
                ftp.Delete(filename + ".new");
				
				// ASCII transfer
				ftp.TransferType = FTPTransferType.ASCII;
				
				// test that dir() works in full mode
				string[] listings = ftp.Dir(".", true);
				for (int i = 0; i < listings.Length; i++)
					log.Debug(listings[i]);
                
                // and now DirDetails test
                FTPFile[] files = ftp.DirDetails(".");
                log.Debug(files.Length + " files");
                for (int i = 0; i < files.Length; i++) {
                    log.Debug(files[i].ToString());
                }
				
				// try system()
				log.Debug(ftp.GetSystem());
				
				// try pwd()
				log.Debug(ftp.Pwd());
				
				ftp.Quit();
				
				log.Debug("******** message log ********");
				log.Debug(FTPClientTest.messages.ToString());
			}
			catch (SystemException ex)
			{
				log.Debug("Caught exception: " + ex.Message);
			}
			catch (FTPException ex)
			{
				log.Debug("Caught exception: " + ex.Message);
			}
		}
		
		
		/// <summary>  
        /// Basic usage statement
        /// </summary>
		public static void Usage()
		{
			log.Debug("Usage: ");
			log.Debug("FTPClientTest remotehost user password filename directory " + 
                      "(ascii|binary) (active|pasv)");
		}
	}
}