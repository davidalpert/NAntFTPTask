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
// $Log: TestResume.cs,v $
// Revision 1.2  2004/11/20 22:38:22  bruceb
// catch and rethrow exceptions, double get() to test if appends
//
// Revision 1.1  2004/11/13 19:14:33  bruceb
// first cut of tests
//
//

using System;
using System.IO;
using NUnit.Framework;
using EnterpriseDT.Net.Ftp;

namespace EnterpriseDT.Net.Ftp.Test
{
	/// <summary>  
	/// Test resuming of binary transfers when interrupted
	/// </summary>
	/// <author>      Bruce Blackshaw
	/// </author>
	/// <version>     $Revision: 1.2 $
	/// </version>
    [TestFixture]
    [Category("edtFTPnet")]
	public class TestResume:FTPTestCase
	{
		/// <summary>  
		/// Get name of log file
		/// </summary>
		override internal string LogName
		{
			get
			{
				return "TestResume.log";
            }
		}
        
		/// <summary> 
        /// Flag used to determine if a transfer should be cancelled
        /// </summary>
        private bool cancelled = false;
        
        
		/// <summary> 
        /// Logs count of bytes transferred via event
        /// </summary>
	    internal void BytesTransferred(object obj, BytesTransferredEventArgs args) 
	    {
            if (!cancelled) 
            {
                log.Debug("BytesTransferred: cancelling transfer");
                ftp.CancelTransfer();
                cancelled = true;
            }
	    }

        /// <summary>Test resuming when putting a binary file</summary>
        [Test]
		public virtual void ResumePut()
		{
			log.Debug("ResumePut()");
			
            try 
            {
    			Connect();
    			Login();
    			
    			// move to test directory
    			ftp.ChDir(testdir);
    			ftp.TransferType = FTPTransferType.BINARY;
    			
    			// put to a random filename
    			string filename = GenerateRandomFilename();
                
                // set up event handler to cancel the transfer
                cancelled = false;
                BytesTransferredHandler handler = new BytesTransferredHandler(BytesTransferred);
    			ftp.BytesTransferred += handler;
                
    			// initiate the put
    			ftp.Put(localBinaryFile, filename);
                
                // remove handler now we've cancelled
    			ftp.BytesTransferred -= handler;
                
    			// should be cancelled - now resume
    			ftp.Resume();
    			
    			// put again - should append
    			ftp.Put(localBinaryFile, filename);
    			
    			// get it back        
    			ftp.Get(filename, filename);
    			
    			// check equality of local files
    			AssertIdentical(localBinaryFile, filename);
    			
    			// finally, just check cancelResume works
    			ftp.CancelResume();
    			
    			// get it back        
    			ftp.Get(filename, filename);
    			
    			// delete remote file
    			ftp.Delete(filename);
    			
    			// and delete local file
    			FileInfo local = new FileInfo(filename);
                local.Delete();
    
                ftp.Quit();
            }
            catch (Exception ex)
            {
                log.Error("ResumePut() failed", ex);
                throw ex;
            }
		}
		
		/// <summary>Test resuming when putting a binary file</summary>
        [Test]
        public virtual void ResumeGet()
		{
			log.Debug("ResumeGet()");
            try
            {
				Connect();
    			Login();
    			
    			// move to test directory
    			ftp.ChDir(testdir);
    			ftp.TransferType = FTPTransferType.BINARY;
    			
    			// put to a random filename
    			string filename1 = GenerateRandomFilename();
    			
                // set up event handler to cancel the transfer
                cancelled = false;
                BytesTransferredHandler handler = new BytesTransferredHandler(BytesTransferred);
    			ftp.BytesTransferred += handler;
    			
    			// initiate the get
                log.Debug("Getting '" + remoteBinaryFile + "' to '" + filename1 + "'");
   			    ftp.Get(filename1, remoteBinaryFile);
                    
                // remove handler for next transfer
                ftp.BytesTransferred -= handler;

    			// should be cancelled - now resume
    			ftp.Resume();
    			
    			// get again - should append
                log.Debug("Resuming '" + remoteBinaryFile + "' to '" + filename1 + "'");
    			ftp.Get(filename1, remoteBinaryFile);
    			
    			string filename2 = GenerateRandomFilename(); ;
    			
    			// do another get - complete this time    
    			ftp.Get(filename2, remoteBinaryFile);
    			
    			// check equality of local files
    			AssertIdentical(filename1, filename2);
    			
    			// and delete local files
    			FileInfo local = new FileInfo(filename1);
                local.Delete();
    			local = new FileInfo(filename2);
    			local.Delete();
    			
    			ftp.Quit();
            }
            catch (Exception ex)
            {
                log.Error("ResumeGet() failed", ex);
                throw ex;
            }

		}
	}
}
