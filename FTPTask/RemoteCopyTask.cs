/*
 * Created by SharpDevelop.
 * User: David
 * Date: 12/7/2004
 * Time: 2:28 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using Systm.Xml;
using System.Collections;
using System.Collections.Specialized;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Tasks;

namespace FTPTask {

	/// <summary>FTP copy task</summary>
	/// <remarks>Adapted from <see cref="NAnt.Core.Tasks.CopyTask">NAnt.Core.Tasks.CopyTask</see>.</remarks>
	[TaskName("remoteCopy")]
	public class RemoteCopyTask : CopyTask {
	{
		protected 

		#region Override implementation of Task

        /// <summary>
        /// Executes the Copy task.
        /// </summary>
        /// <exception cref="BuildException">A file that has to be copied does not exist or could not be copied.</exception>
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (CopyFileSet.BaseDirectory == null) {
                CopyFileSet.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            // Clear previous copied files
            _fileCopyMap = new Hashtable();

            // copy a single file.
            if (SourceFile != null) {
                if (SourceFile.Exists) {e
                    FileInfo dstInfo = null;
                    if (ToFile != null) {
                        dstInfo = ToFile;
                    } else {
                        string dstFilePath = Path.Combine(ToDirectory.FullName, 
                            SourceFile.Name);
                        dstInfo = new FileInfo(dstFilePath);
                    }

                    // do the outdated check
                    bool outdated = (!dstInfo.Exists) || (SourceFile.LastWriteTime > dstInfo.LastWriteTime);

                    if (Overwrite || outdated) {
                        // add to a copy map of absolute verified paths
                        FileCopyMap.Add(SourceFile.FullName, dstInfo.FullName);
                        if (dstInfo.Exists && dstInfo.Attributes != FileAttributes.Normal) {
                            File.SetAttributes(dstInfo.FullName, FileAttributes.Normal);
                        }
                    }
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Could not find file '{0}' to copy.", SourceFile.FullName), 
                        Location);
                }
            } else { // copy file set contents.
                // get the complete path of the base directory of the fileset, ie, c:\work\nant\src
                DirectoryInfo srcBaseInfo = CopyFileSet.BaseDirectory;
                
                // if source file not specified use fileset
                foreach (string pathname in CopyFileSet.FileNames) {
                    FileInfo srcInfo = new FileInfo(pathname);
                    if (srcInfo.Exists) {
                        // will holds the full path to the destination file
                        string dstFilePath;

                        if (Flatten) {
                            dstFilePath = Path.Combine(ToDirectory.FullName, 
                                srcInfo.Name);
                        } else {
                            // Gets the relative path and file info from the full 
                            // source filepath
                            // pathname = C:\f2\f3\file1, srcBaseInfo=C:\f2, then 
                            // dstRelFilePath=f3\file1
                            string dstRelFilePath = "";
                            if (srcInfo.FullName.IndexOf(srcBaseInfo.FullName, 0) != -1) {
                                dstRelFilePath = srcInfo.FullName.Substring(
                                    srcBaseInfo.FullName.Length);
                            } else {
                                dstRelFilePath = srcInfo.Name;
                            }
                        
                            if (dstRelFilePath[0] == Path.DirectorySeparatorChar) {
                                dstRelFilePath = dstRelFilePath.Substring(1);
                            }
                        
                            // The full filepath to copy to.
                            dstFilePath = Path.Combine(ToDirectory.FullName, 
                                dstRelFilePath);
                        }
                        
                        // do the outdated check
                        FileInfo dstInfo = new FileInfo(dstFilePath);
                        bool outdated = (!dstInfo.Exists) || (srcInfo.LastWriteTime > dstInfo.LastWriteTime);

                        if (Overwrite || outdated) {
                            FileCopyMap.Add(srcInfo.FullName, dstFilePath);
                            if (dstInfo.Exists && dstInfo.Attributes != FileAttributes.Normal) {
                                File.SetAttributes(dstInfo.FullName, FileAttributes.Normal);
                            }
                        }
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Could not find file '{0}' to copy.", srcInfo.FullName), 
                            Location);
                    }
                }
                
                if (IncludeEmptyDirs && !Flatten) {
                    // create any specified directories that weren't created during the copy (ie: empty directories)
                    foreach (string pathname in CopyFileSet.DirectoryNames) {
                        DirectoryInfo srcInfo = new DirectoryInfo(pathname);
                        // skip directory if not relative to base dir of fileset
                        if (srcInfo.FullName.IndexOf(srcBaseInfo.FullName) == -1) {
                            continue;
                        }
                        string dstRelPath = srcInfo.FullName.Substring(srcBaseInfo.FullName.Length);
                        if (dstRelPath.Length > 0 && dstRelPath[0] == Path.DirectorySeparatorChar) {
                            dstRelPath = dstRelPath.Substring(1);
                        }

                        // The full filepath to copy to.
                        string destinationDirectory = Path.Combine(ToDirectory.FullName, dstRelPath);
                        if (!Directory.Exists(destinationDirectory)) {
                            try {
                                Directory.CreateDirectory(destinationDirectory);
                            } catch (Exception ex) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                "Failed to create directory '{0}'.", destinationDirectory ), 
                                 Location, ex);
                            }
                            Log(Level.Verbose, "Created directory '{0}'.", destinationDirectory);
                        }
                    }
                }
            }

            // do all the actual copy operations now
            DoFileOperations();
        }

        #endregion Override implementation of Task

        #region Protected Instance Methods

        /// <summary>
        /// Actually does the file copies.
        /// </summary>
        protected virtual void DoFileOperations() {
            int fileCount = FileCopyMap.Keys.Count;
            if (fileCount > 0 || Verbose) {
                if (ToFile != null) {
                    Log(Level.Info, "Copying {0} file{1} to '{2}'.", fileCount, (fileCount != 1) ? "s" : "", ToFile);
                } else {
                    Log(Level.Info, "Copying {0} file{1} to '{2}'.", fileCount, (fileCount != 1) ? "s" : "", ToDirectory);
                }

                // loop thru our file list
                foreach (string sourceFile in FileCopyMap.Keys) {
                    string destinationFile = (string) FileCopyMap[sourceFile];
                    if (Flatten) {
                        destinationFile = Path.Combine(ToDirectory.FullName, 
                            Path.GetFileName(destinationFile));
                    }
                    if (sourceFile == destinationFile) {
                        Log(Level.Verbose, "Skipping self-copy of '{0}'.", sourceFile);
                        continue;
                    }

                    try {
                        Log(Level.Verbose, "Copying '{0}' to '{1}'.", sourceFile, destinationFile);
                        
                        // create directory if not present
                        string destinationDirectory = Path.GetDirectoryName(destinationFile);
                        if (!Directory.Exists(destinationDirectory)) {
                            Directory.CreateDirectory(destinationDirectory);
                            Log(Level.Verbose, "Created directory '{0}'.", destinationDirectory);
                        }

                        // copy the file with filters
                        FileUtils.CopyFile(sourceFile, destinationFile, Filters, 
                            InputEncoding, OutputEncoding);
                    } catch (Exception ex) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Cannot copy '{0}' to '{1}'.", sourceFile, destinationFile), 
                            Location, ex);
                    }
                }
            }
        }

        #endregion Protected Instance Methods		
	}
}
