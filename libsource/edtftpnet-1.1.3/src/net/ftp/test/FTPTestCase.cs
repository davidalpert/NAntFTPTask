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
// $Log: FTPTestCase.cs,v $
// Revision 1.2  2004/11/20 22:37:16  bruceb
// tweaked setup/teardown so its for each test
//
// Revision 1.1  2004/11/13 19:15:01  bruceb
// first cut of tests
//

using System;
using System.IO;
using System.Configuration;
using FTPClient = EnterpriseDT.Net.Ftp.FTPClient;
using FTPConnectMode = EnterpriseDT.Net.Ftp.FTPConnectMode;
using FTPControlSocket = EnterpriseDT.Net.Ftp.FTPControlSocket;
using FileAppender = EnterpriseDT.Util.Debug.FileAppender;
using Level = EnterpriseDT.Util.Debug.Level;
using Logger = EnterpriseDT.Util.Debug.Logger;
using NUnit.Framework;
	
namespace EnterpriseDT.Net.Ftp.Test
{	
	/// <summary>  
    /// Generic NUnit test superclass for FTP testing
    /// </summary>
    /// <remarks>This class provides some
	/// useful methods for subclasses that implement the actual
	/// test cases
	/// </remarks>
	/// <author>          
    /// Bruce Blackshaw
	/// </author>
	/// <version>         
    /// $Revision: 1.2 $
	/// </version>
	abstract public class FTPTestCase
	{
		/// <summary>  
		/// Get name of log file
		/// </summary>
		/// <returns> name of file to log to
		/// </returns>
		abstract internal string LogName{get;}
				
		/// <summary>  Log stream</summary>
		internal Logger log;
		
		/// <summary>  Reference to the FTP client</summary>
		internal FTPClient ftp;
		
		/// <summary>  Remote test host</summary>
		internal string host;
		
		/// <summary>  Test user</summary>
		internal string user;
		
		/// <summary>  User password</summary>
		internal string password;
		
		/// <summary>  Connect mode for test</summary>
		internal FTPConnectMode connectMode;
		
		/// <summary>  Socket timeout</summary>
		internal int timeout;
		
		/// <summary>  Remote directory that remote test files/dirs are in</summary>
		internal string testdir;
		
		/// <summary>  Remote text file</summary>
		internal string remoteTextFile;
		
		/// <summary>  Local text file</summary>
		internal string localTextFile;
		
		/// <summary>  Remote binary file</summary>
		internal string remoteBinaryFile;
		
		/// <summary>  Local binary file</summary>
		internal string localBinaryFile;
		
		/// <summary>  Local empty file</summary>
		internal string localEmptyFile;
		
		/// <summary>  Remote empty file</summary>
		internal string remoteEmptyFile;
		
		/// <summary>  Remote empty dir</summary>
		internal string remoteEmptyDir;
		
		/// <summary>  Strict reply checking?</summary>
		internal bool strictReplies = true;
			
		/// <summary>  Initialize test properties</summary>
		public FTPTestCase()
		{
			log = Logger.GetLogger(typeof(FTPTestCase));
			
			Logger.CurrentLevel = Level.ALL;
						
			// initialise our test properties
			host = ConfigurationSettings.AppSettings["ftptest.host"];
			user = ConfigurationSettings.AppSettings["ftptest.user"];
			password = ConfigurationSettings.AppSettings["ftptest.password"];
			
			// active or passive?
			string connectMode = ConfigurationSettings.AppSettings["ftptest.connectmode"];
			if (connectMode != null && connectMode.ToUpper().Equals("active".ToUpper()))
				this.connectMode = FTPConnectMode.ACTIVE;
			else
				this.connectMode = FTPConnectMode.PASV;
			
			// socket timeout
			string timeout = ConfigurationSettings.AppSettings["ftptest.timeout"];
			this.timeout = System.Int32.Parse(timeout);
			
			string strict = ConfigurationSettings.AppSettings["ftptest.strictreplies"];
			if (strict != null && strict.ToUpper().Equals("false".ToUpper()))
				this.strictReplies = false;
			else
				this.strictReplies = true;
			
			// various test files and dirs
			testdir = ConfigurationSettings.AppSettings["ftptest.testdir"];
			localTextFile = ConfigurationSettings.AppSettings["ftptest.file.local.text"];
			remoteTextFile = ConfigurationSettings.AppSettings["ftptest.file.remote.text"];
			localBinaryFile = ConfigurationSettings.AppSettings["ftptest.file.local.binary"];
			remoteBinaryFile = ConfigurationSettings.AppSettings["ftptest.file.remote.binary"];
			localEmptyFile = ConfigurationSettings.AppSettings["ftptest.file.local.empty"];
			remoteEmptyFile = ConfigurationSettings.AppSettings["ftptest.file.remote.empty"];
			remoteEmptyDir = ConfigurationSettings.AppSettings["ftptest.dir.remote.empty"];
		}
		
		/// <summary>Setup is called before running each test</summary>
		[TestFixtureSetUp]
		internal virtual void FixtureSetUp()
		{
			Logger.AddAppender(new FileAppender(LogName));
			int[] ver = FTPClient.Version;
			log.Info("FTP version: " + ver[0] + "." + ver[1] + "." + ver[2]);
			log.Info("FTP build timestamp: " + FTPClient.BuildTimestamp);
		}
		
		/// <summary> Deallocate resources at close of fixture</summary>
		[TestFixtureTearDown]
		internal virtual void FixtureTearDown()
		{
			Logger.Shutdown();
            ftp = null;
		}
        
		/// <summary> Deallocate resources at close of each test</summary>
		[TearDown]
		internal virtual void TestTearDown()
		{
            ftp = null;
		}        
		
		/// <summary>  Connect to the server</summary>
		internal virtual void Connect()
		{
			Connect(0);
		}
		
		/// <summary>  Connect to the server </summary>
		internal virtual void Connect(int timeout)
		{
			// connect
			ftp = new FTPClient();
            ftp.RemoteHost = host;
            ftp.ControlPort = FTPControlSocket.CONTROL_PORT;
            ftp.Timeout = timeout;
			ftp.ConnectMode = connectMode;
			if (!strictReplies)
			{
				log.Warn("Strict replies not enabled");
				ftp.StrictReturnCodes = false;
			}
			ftp.Connect();
		}
		
		/// <summary>  Login to the server</summary>
		internal virtual void Login()
		{
			ftp.Login(user, password);
		}
		
		/// <summary>  
		/// Generate a random file name for testing
		/// </summary>
		/// <returns>  random filename
		/// </returns>
		internal virtual string GenerateRandomFilename()
		{
			DateTime now = DateTime.Now;
			Int64 ms = (long) now.Ticks;
			return ms.ToString();
		}
		
		/// <summary>  
		/// Test to see if two buffers are identical, byte for byte
		/// </summary>
		/// <param name="buf1">  first buffer
		/// </param>
		/// <param name="buf2">  second buffer
		/// </param>
		internal virtual void AssertIdentical(byte[] buf1, byte[] buf2)
		{	
			Assert.AreEqual(buf1.Length, buf2.Length);
			for (int i = 0; i < buf1.Length; i++)
				Assert.AreEqual(buf1[i], buf2[i]);
		}
		
		/// <summary>  Test to see if two files are identical, byte for byte
		/// 
		/// </summary>
		/// <param name="file1"> name of first file
		/// </param>
		/// <param name="file2"> name of second file
		/// </param>
		internal virtual void AssertIdentical(string file1, string file2)
		{
			FileInfo f1 = new FileInfo(file1);
			FileInfo f2 = new FileInfo(file2);
			AssertIdentical(f1, f2);
		}
		
		/// <summary>  
		/// Test to see if two files are identical, byte for byte
		/// </summary>
		/// <param name="file1"> first file object
		/// </param>
		/// <param name="file2"> second file object
		/// </param>
		internal virtual void AssertIdentical(FileInfo file1, FileInfo file2)
		{			
            log.Debug("Comparing [" + file1.Name + "," + file2.Name + "]");
			BufferedStream is1 = null;
			BufferedStream is2 = null;
			try
			{
				// check lengths first
				Assert.AreEqual(file1.Length, file2.Length);
				log.Debug("Identical size [" + file1.Name + "," + file2.Name + "]");
				
				// now check each byte
				is1 = new BufferedStream(new FileStream(file1.FullName, FileMode.Open, FileAccess.Read));
				is2 = new BufferedStream(new FileStream(file2.FullName, FileMode.Open, FileAccess.Read));
				int ch1 = 0;
				int ch2 = 0;
				while ((ch1 = is1.ReadByte()) != - 1 && (ch2 = is2.ReadByte()) != - 1)
				{
					Assert.AreEqual(ch1, ch2);
				}
				log.Debug("Contents equal");
			}
			catch (SystemException ex)
			{
				Assert.Fail("Caught exception: " + ex.Message);
			}
			finally
			{
				if (is1 != null)
					is1.Close();
				if (is2 != null)
					is2.Close();
			}
		}
	}
}
