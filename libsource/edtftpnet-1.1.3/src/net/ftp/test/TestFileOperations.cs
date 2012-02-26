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
// $Log: TestFileOperations.cs,v $
// Revision 1.2  2004/11/20 22:39:32  bruceb
// added to edtFTPnet test category
//
// Revision 1.1  2004/11/13 19:14:33  bruceb
// first cut of tests
//
//

using System;
using System.IO;
using FTPException = EnterpriseDT.Net.Ftp.FTPException;
using FTPTransferType = EnterpriseDT.Net.Ftp.FTPTransferType;
using NUnit.Framework;

namespace EnterpriseDT.Net.Ftp.Test
{	
	/// <summary>  Tests various file operations
	/// 
	/// </summary>
	/// <author>          Bruce Blackshaw
	/// </author>
	/// <version>         $Revision: 1.2 $
	/// </version>
	[TestFixture]
    [Category("edtFTPnet")]
	public class TestFileOperations:FTPTestCase
	{
		/// <summary>  
		/// Get name of log file
		/// </summary>
		/// <returns> name of file to log to
		/// </returns>
		override internal string LogName
		{
			get
			{
				return "TestFileOperations.log";
			}			
		}
				
		/// <summary>  Test remote deletion</summary>
		[Test]
		public virtual void Delete()
		{
			// test existent & non-existent file
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			
			try
			{
				// try to delete a non-existent file
				string file = GenerateRandomFilename();
				log.Debug("Deleting a non-existent file");
				ftp.Delete(file);
			}
			catch (FTPException ex)
			{
				log.Debug("Expected exception: " + ex.Message);
			}
			
			// how to delete a file and make it repeatable?
			ftp.Quit();
		}
		
		/// <summary>  Test renaming of a file</summary>
		[Test]
		public virtual void Rename()
		{
			// test existent & non-existent file
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			
			// rename file
			string rename = remoteTextFile + ".renamed";
			ftp.Rename(remoteTextFile, rename);
			
			// get its mod time
			DateTime modTime = ftp.ModTime(rename);
			log.Debug(rename + ": " + modTime.ToString());
			
			// modtime on original file should fail
			try
			{
				ftp.ModTime(remoteTextFile);
			}
			catch (FTPException ex)
			{
				log.Debug("Expected exception: " + ex.Message);
			}
			
			// and rename file back again
			ftp.Rename(rename, remoteTextFile);
			
			// and modtime should succeed 
			modTime = ftp.ModTime(remoteTextFile);
			log.Debug(remoteTextFile + ": " + modTime.ToString());
			
			// modtime on renamed (first time) file should 
			// now fail (as we've just renamed it to original)
			try
			{
				ftp.ModTime(rename);
			}
			catch (FTPException ex)
			{
				log.Debug("Expected exception: " + ex.Message);
			}
			
			ftp.Quit();
		}
		
		
		/// <summary>  
        /// Test finding the modification time 
		/// of a file
		/// </summary>
		[Test]
		public virtual void Modtime()
		{			
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			
			// get modtime
			log.Debug("Modtime on existing file: " + remoteTextFile);
			DateTime modTime = ftp.ModTime(remoteTextFile);
			log.Debug(remoteTextFile + ": " + modTime.ToString());
			
			try
			{
				// get modtime on non-existent file
				string file = GenerateRandomFilename();
				log.Debug("Modtime on non-existent file");
				modTime = ftp.ModTime(file);
				log.Debug(remoteTextFile + ": " + modTime.ToString());
			}
			catch (FTPException ex)
			{
				log.Debug("Expected exception: " + ex.Message);
			}
			
			ftp.Quit();
		}
		
		/// <summary>Test the Size() method</summary>
		[Test]
		public virtual void Size()
		{			
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			ftp.TransferType = FTPTransferType.BINARY;
			
			// put to a random filename
			string filename = GenerateRandomFilename();
			ftp.Put(localTextFile, filename);
			
			// find size of local file
			FileInfo local = new FileInfo(localTextFile);
			long sizeLocal = local.Length;
			
			// find size of remote
			long sizeRemote = ftp.Size(filename);
			
			// delete remote file
			ftp.Delete(filename);
			
			if (sizeLocal != sizeRemote)
			{
				string msg = "Local size(" + sizeLocal + ") != remote size(" + sizeRemote + ")";
				log.Debug(msg);
				throw new System.Exception(msg);
			}
			
			ftp.Quit();
		}
	}
}
