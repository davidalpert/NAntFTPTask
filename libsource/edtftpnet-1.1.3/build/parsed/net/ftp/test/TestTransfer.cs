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
// $Log: TestTransfer.cs,v $
// Revision 1.2  2004/11/20 22:39:32  bruceb
// added to edtFTPnet test category
//
// Revision 1.1  2004/11/13 19:14:33  bruceb
// first cut of tests
//
//

using System;
using System.IO;
using FTPTransferType = EnterpriseDT.Net.Ftp.FTPTransferType;
using FTPException = EnterpriseDT.Net.Ftp.FTPException;
using NUnit.Framework;

namespace EnterpriseDT.Net.Ftp.Test
{
	/// <summary>  
	/// Test get'ing and put'ing of remote files in various ways
	/// </summary>
	/// <author>      Bruce Blackshaw
	/// </author>
	/// <version>     $Revision: 1.2 $
	/// </version>
	[TestFixture]
    [Category("edtFTPnet")]
	public class TestTransfer:FTPTestCase
	{
		/// <summary>  Get name of log file
		/// 
		/// </summary>
		/// <returns> name of file to log to
		/// </returns>
		override internal string LogName
		{
			get
			{
				return "TestTransfer.log";
			}
		}
		
		/// <summary>  Test transfering a binary file</summary>
		[Test]
		public virtual void TransferBinary()
		{
			log.Debug("TransferBinary()");
			
			Connect();
			Login();
						
			// move to test directory
			ftp.ChDir(testdir);
			ftp.TransferType = FTPTransferType.BINARY;
			
			// put to a random filename
			string filename = GenerateRandomFilename();
			ftp.Put(localBinaryFile, filename);
			
			// get it back        
			ftp.Get(filename, filename);
            
            // get it back again - should be overwritten     
			ftp.Get(filename, filename);
			
			// delete remote file
			ftp.Delete(filename);
			try
			{
				ftp.ModTime(filename);
				Assert.Fail(filename + " should not be found");
			}
			catch (FTPException ex)
			{
				log.Debug("Expected exception: " + ex.Message);
			}
			
			// check equality of local files
			AssertIdentical(localBinaryFile, filename);
			
			// and delete local file
			FileInfo local = new FileInfo(filename);
            local.Delete();
			
			ftp.Quit();
		}
		
		/// <summary>  Test transfering a text file</summary>
		[Test]
        public virtual void TransferText()
		{			
			log.Debug("TransferText()");
			
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			ftp.TransferType = FTPTransferType.ASCII;
			
			// put to a random filename
			string filename = GenerateRandomFilename();
			ftp.Put(localTextFile, filename);
			
			// get it back        
			ftp.Get(filename, filename);
			
			// delete remote file
			ftp.Delete(filename);
			try
			{
				ftp.ModTime(filename);
				Assert.Fail(filename + " should not be found");
			}
			catch (FTPException ex)
			{
				log.Debug("Expected exception: " + ex.Message);
			}
			
			// check equality of local files
			AssertIdentical(localTextFile, filename);
			
			// and delete local file
			FileInfo local = new FileInfo(filename);
            local.Delete();
			
			ftp.Quit();
		}
		
		/// <summary> Test getting a byte array</summary>
        [Test]
		public virtual void GetBytes()
		{			
			log.Debug("GetBytes()");
			
			Connect();
			Login();
						
			// move to test directory
			ftp.ChDir(testdir);
			ftp.TransferType = FTPTransferType.BINARY;
			
			// get the file and work out its size
			string filename1 = GenerateRandomFilename();
			ftp.Get(filename1, remoteBinaryFile);
			FileInfo file1 = new FileInfo(filename1);
			
			// now get to a buffer and check the length
			byte[] result = ftp.Get(remoteBinaryFile);
            Assert.AreEqual(result.Length, file1.Length);
			
			// put the buffer         
			string filename2 = GenerateRandomFilename();
			ftp.Put(result, filename2);
			
			// get it back as a file
			ftp.Get(filename2, filename2);
			
			// remove it remotely
			ftp.Delete(filename2);
			
			// and now check files are identical
			FileInfo file2 = new FileInfo(filename2);
			AssertIdentical(file1, file2);
			
			// and finally delete them
            file1.Delete();
            file2.Delete();
            
			ftp.Quit();
		}
		
		/// <summary>  Test the stream functionality</summary>
        [Test]
        public virtual void TransferStream()
        {
			log.Debug("TransferStream()");
			
			Connect();
			Login();
			
    		// move to test directory
			ftp.ChDir(testdir);
			ftp.TransferType = FTPTransferType.BINARY;
			
			// get file as output stream        
			MemoryStream output = new MemoryStream();
			ftp.Get(output, remoteBinaryFile);
			
			// convert to byte array
			byte[] result1 = output.ToArray();
			
			// put this 
			string filename = GenerateRandomFilename();
            ftp.Put(new MemoryStream(result1), filename);
			
			// get it back
			byte[] result2 = ftp.Get(filename);
			
			// delete remote file
			ftp.Delete(filename);
			
			// and compare the buffers
			AssertIdentical(result1, result2);
			
			ftp.Quit();
		}
		
		/// <summary>  Test the append functionality in put()</summary>
        [Test]
        public virtual void PutAppend()
		{
			log.Debug("PutAppend()");
			
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			ftp.TransferType = FTPTransferType.BINARY;
			
			// put to a random filename
			string filename = GenerateRandomFilename();
			ftp.Put(localBinaryFile, filename);
			
			// second time, append
			ftp.Put(localBinaryFile, filename, true);
			
			// get it back & delete remotely
			ftp.Get(filename, filename);
			ftp.Delete(filename);
			
			// check it is twice the size
			FileInfo file1 = new FileInfo(localBinaryFile);
			FileInfo file2 = new FileInfo(filename);
			Assert.AreEqual(file1.Length * 2, file2.Length);
			
			// and finally delete it
            file2.Delete();
			
			ftp.Quit();
		}
		
		/// <summary>  Test transferring empty files</summary>
        [Test]
		public virtual void TransferEmpty()
		{
			log.Debug("TransferEmpty()");
			
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			
			// get an empty file
			ftp.Get(remoteEmptyFile, remoteEmptyFile);
			FileInfo empty = new FileInfo(remoteEmptyFile);
            Assert.AreEqual(0, empty.Length);
            
			// delete it
			empty.Delete();
			
			// put an empty file
			ftp.Put(localEmptyFile, localEmptyFile);
			
			// get it back as a different filename
			string filename = GenerateRandomFilename();
			ftp.Get(filename, localEmptyFile);
			empty = new FileInfo(filename);
			Assert.AreEqual(0, empty.Length);
			
			// delete file we got back (copy of our local empty file)
			empty.Delete();
			
			// and delete the remote empty file we
			// put there
			ftp.Delete(localEmptyFile);
			
			ftp.Quit();
		}
		
		/// <summary>  Test transferring non-existent files</summary>
		[Test]
		public virtual void TransferNonExistent()
		{	
			log.Debug("TransferNonExistent()");
			
			Connect();
			Login();
			
			// move to test directory
			ftp.ChDir(testdir);
			
			// generate a name & try to get it
			string filename = GenerateRandomFilename();
			try
			{
				ftp.Get(filename, filename);
				Assert.Fail(filename + " should not be found");
			}
			catch (FTPException ex)
			{
				log.Debug("Expected exception: " + ex.Message);
				
				// ensure we don't have a local file of that name produced
				FileInfo file = new FileInfo(filename);
                Assert.IsFalse(file.Exists);
			}
			
			// generate name & try to put
			filename = GenerateRandomFilename();
			try
			{
				ftp.Put(filename, filename);
				Assert.Fail(filename + " should not be found");
			}
			catch (FileNotFoundException ex)
			{
				log.Debug("Expected exception: " + ex.Message);
			}
			
			ftp.Quit();
		}
	}
}
