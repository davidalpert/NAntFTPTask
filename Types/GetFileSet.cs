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
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

using EnterpriseDT.Net.Ftp;

using Sourceforge.NAnt.Ftp.Enum;
using Sourceforge.NAnt.Ftp.Tasks;
using Sourceforge.NAnt.Ftp.Types;
using Sourceforge.NAnt.Ftp.Util;

namespace Sourceforge.NAnt.Ftp.Types {

	/// <summary>
	/// A specially derived <b>FileSet</b> element that is used in the &lt;ftp&gt; task.
	/// </summary>
	[ElementName("get")]
	public class GetFileSet : TransferFileSet {
		
		#region Private members
        private bool _hasScanned;
        private RemoteDirectoryScanner _scanner = new RemoteDirectoryScanner();
        
        // handled the same in local and remote operations
        // -- private bool _defaultExcludes = true;
        // -- private bool _failOnEmpty;
        // -- private DirectoryInfo _baseDirectory;
        // -- private StringCollection _asis = new StringCollection();
        // doesn't exist in remote operations
        // -- private PathScanner _pathFiles = new PathScanner();
        
		#endregion
		
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Public access methods
		
		public RemoteDirectoryScanner Scanner { get {return _scanner;}}
		
        /// <summary>
        /// Gets the collection of include patterns.
        /// </summary>
        public new StringCollection Includes {
            get { return _scanner.Includes; }
        }

        /// <summary>
        /// Gets the collection of exclude patterns.
        /// </summary>
        public new StringCollection Excludes {
            get { return _scanner.Excludes; }
        }
       
        /// <summary>
        /// Gets the collection of file names that match the fileset.
        /// </summary>
        /// <value>
        /// A collection that contains the file names that match the 
        /// <see cref="FileSet" />.
        /// </value>
        public new RemotePath[] FileNames {
            get {
                if (!_hasScanned) {
                    Scan();
                }
        		return (RemotePath[])_scanner.FileNames.ToArray(typeof(RemotePath));
            }
        }

        /// <summary>
        /// Gets the collection of directory names that match the fileset.
        /// </summary>
        /// <value>
        /// A collection that contains the directory names that match the 
        /// <see cref="FileSet" />.
        /// </value>
        public new StringCollection DirectoryNames {
            get { 
                if (!_hasScanned) {
                    Scan();
                }
                return _scanner.DirectoryNames;
            }
        }
        
        /// <summary>
        /// Gets the collection of directory names that were scanned for files.
        /// </summary>
        /// <value>
        /// A collection that contains the directory names that were scanned for
        /// files.
        /// </value>
        public new StringCollection ScannedDirectories {
            get { 
                if (!_hasScanned) {
                    Scan();
                }
                return _scanner.ScannedDirectories;
            }
        }
        
        /// <summary>
        /// The items to include in the fileset.
        /// </summary>
        [BuildElementArray("include")]
        public new Include[] IncludeElements {
            set {
                foreach (Include include in value) {
                    if (include.IfDefined && !include.UnlessDefined) {
                        if (include.AsIs || include.FromPath) {
                            logger.Debug(string.Format(CultureInfo.InvariantCulture, "Including AsIs=", include.Pattern));
                            AsIs.Add(include.Pattern);
                        } else {
                            logger.Debug(string.Format(CultureInfo.InvariantCulture, "Including pattern", include.Pattern));
                            Includes.Add(include.Pattern);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The files from which a list of patterns or files to include should 
        /// be obtained.
        /// </summary>
        [BuildElementArray("includesfile")]
        public new IncludesFile[] IncludesFiles {
            set {
                foreach (IncludesFile includesFile in value) {
                    if (includesFile.IfDefined && !includesFile.UnlessDefined) {
        				if (includesFile.AsIs || includesFile.FromPath) {
                            foreach (string pattern in includesFile.Patterns) {
                                logger.Debug(string.Format(CultureInfo.InvariantCulture, "Including AsIs=", pattern));
                                AsIs.Add(pattern);
                            }
                        } else {
                            foreach (string pattern in includesFile.Patterns) {
                                logger.Debug(string.Format(CultureInfo.InvariantCulture, "Including Pattern=", pattern));
                                Includes.Add(pattern);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines the most recently modified file in the fileset (by LastWriteTime of the <see cref="RemotePath"/>).
        /// </summary>
        /// <returns>
        /// The <see cref="RemotePath"/> of the file that has the newest (closest to present) last write time.
        /// </returns>
        public new RemotePath MostRecentLastWriteTimeFile {
            get{
				RemotePath newestPath = null;

                foreach (RemotePath rpath in FileNames) {
					if (newestPath == null) {
						newestPath = rpath;
					} else if (rpath.File.LastModified > newestPath.File.LastModified) {
						logger.Info(string.Format(CultureInfo.InvariantCulture, "'{0}' was newer than {1}", rpath.FullPath, newestPath.FullPath));
						newestPath = rpath;
					}
                }
                return newestPath;
            }
        }
        #endregion
		
        #region Implementation of ICloneable

        /// <summary>
        /// Creates a shallow copy of the <see cref="FileSet" />.
        /// </summary>
        /// <returns>
        /// A shallow copy of the <see cref="FileSet" />.
        /// </returns>
        public override object Clone() {
            GetFileSet clone = new GetFileSet();
            CopyTo(clone);
            return clone;
        }

        #endregion Implementation of ICloneable

        #region Override implementation of DataTypeBase

        public override void Reset() {
            // ensure that scanning will happen again for each use
            _hasScanned = false;
        }

        #endregion Override implementation of DataTypeBase

        #region Override implementation of Object

        public override string ToString() {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (!_hasScanned){
                sb.AppendFormat("Base remote path: {0}", BaseDirectory);
                sb.Append(Environment.NewLine);
                
                sb.Append("AsIs:");
                sb.Append(Environment.NewLine);
                sb.Append(AsIs.ToString());
                sb.Append(Environment.NewLine);

                sb.Append("Files:");
                sb.Append(Environment.NewLine);
                sb.Append(_scanner.ToString());
                sb.Append(Environment.NewLine);
            } else {
                sb.Append("Files:");
                sb.Append(Environment.NewLine);
                foreach (RemotePath rpath in this.FileNames) {
                    sb.Append(rpath.FullPath);
                    sb.Append(Environment.NewLine);
                }
                sb.Append("Dirs:");
                sb.Append(Environment.NewLine);
                foreach (string dir in this.DirectoryNames) {
                    sb.Append(dir);
                    sb.Append(Environment.NewLine);
                }
            }

            return sb.ToString();
        }

        #endregion Override implementation of Object

        #region Public Instance Methods

        public bool RemoteDirectoryExists(string dirname) {
        	return _scanner.Client.remoteDirExists(dirname);
        }
        
        public override void Scan() {
            try {
                _scanner.BaseDirectory = this.RemotePathString;

                _scanner.Scan();

                // add all the as-is patterns to the scanned files.
                foreach (string name in AsIs) {
                    if (RemoteDirectoryExists(name)) {
                        _scanner.DirectoryNames.Add(name);
                    } else {
//                		int sep = name.LastIndexOf(RPath.DirectorySeparatorChar);
//                		DateTime date = DateTime.Now;
//                		FTPFile ftpfile = new FTPFile(FTPFile.UNKNOWN, String.Empty, name.Substring(sep),
//                		                              0, false, ref date);
//                		RemotePath file = new RemotePath(ftpfile);
//                		file.Path = name.Substring(0,sep);
                		_scanner.FileNames.Add(new RemotePath(name, false));
                    }
                }

                _hasScanned = true;
            } catch (Exception ex) {
                throw new BuildException("Error scanning remote filesystem.", Location, ex);
            }

            if (FailOnEmpty && _scanner.FileNames.Count == 0) {
                throw new ValidationException(string.Format(CultureInfo.InvariantCulture, 
                    "The getfileset specified is empty after scanning '{0}' for: {1}", 
                    _scanner.BaseDirectory, _scanner.Includes.ToString()), 
                    Location);
            }
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Copies all instance data of the <see cref="FileSet" /> to a given
        /// <see cref="FileSet" />.
        /// </summary>
        protected void CopyTo(GetFileSet clone) {
            base.CopyTo(clone);

            clone._hasScanned = _hasScanned;
            clone._scanner = (RemoteDirectoryScanner) _scanner.Clone();
        }

        #endregion Protected Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Determines if a file has a more recent last write time than the 
        /// given time, or no longer exists.
        /// </summary>
        /// <param name="fileName">A file to check the last write time against.</param>
        /// <param name="targetLastWriteTime">The datetime to compare against.</param>
        /// <returns>
        /// The name of the file that has a last write time greater than 
        /// <paramref name="targetLastWriteTime" /> or that no longer exists; 
        /// otherwise, <see langword="null" />.
        /// </returns>
        public new static string FindMoreRecentLastWriteTime(string fileName, DateTime targetLastWriteTime) {
            StringCollection fileNames = new StringCollection();
            fileNames.Add(fileName);
            return FileSet.FindMoreRecentLastWriteTime(fileNames, targetLastWriteTime);
        }

        /// <summary>
        /// Determines if one of the given files has a more recent last write 
        /// time than the given time. If one of the given files no longer exists,
        /// the target will be considered out-of-date.
        /// </summary>
        /// <param name="fileNames">A collection of filenames to check the last write time against.</param>
        /// <param name="targetLastWriteTime">The datetime to compare against.</param>
        /// <returns>
        /// The name of the first file that has a last write time greater than 
        /// <paramref name="targetLastWriteTime" />; otherwise, null.
        /// </returns>
        public new static string FindMoreRecentLastWriteTime(StringCollection fileNames, DateTime targetLastWriteTime) {
            foreach (string fileName in fileNames) {
                // only check fully file names that have a full path
                if (RemotePathIsRooted(fileName)) {
//                    FileInfo fileInfo = new FileInfo(fileName);
//                    if (!fileInfo.Exists) {
//                        logger.Info(string.Format(CultureInfo.InvariantCulture, "File '{0}' no longer exist (so the target might need to be updated)", fileName, targetLastWriteTime));
//                        return fileName;
//                    }
//                    if (fileInfo.LastWriteTime > targetLastWriteTime) {
//                        logger.Info(string.Format(CultureInfo.InvariantCulture, "'{0}' was newer than {1}", fileName, targetLastWriteTime));
//                        return fileName;
//                    }
                }
            }
            return null;
        }
        
        public static bool RemotePathIsRooted(string filename) {
        	return filename.StartsWith("/");
        }
        #endregion Public Static Methods

        #region Public Instance Constructors
		public GetFileSet() {
			this.Direction = TransferDirection.GET;
		}
		#endregion
		
		public override void InitScanner() {
			// overridden by PutFileSet and Get			
			_scanner.Client = this.Conn;
		}
		public override void TransferFiles() {

			string owd = Conn.PWD;
			string lastPath = ".";

			// transfer the files
			foreach (RemotePath rpath in FileNames) {
				if (rpath.Dir!=lastPath) {
					if (RPath.IsPathRooted(rpath.Dir)) {
						Conn.CWD_Quiet(rpath.Dir);
					} else {
						Conn.CWD_Quiet(RPath.Combine(owd, rpath.Dir));
					}
					lastPath = rpath.Dir;
				}
				Conn.Connection.Get(rpath.Name,
				          LocalPath.ToString(),
				    	  rpath.Dir,
				    	  FTPTask.ParseTransferType(TransferType), 
				    	  Flatten,
				    	  CreateDirsOnDemand,
				    	  Update);
			}
			Conn.CWD_Quiet(owd);
		}
		public override int NumFiles {
			get { return this.FileNames.Length;}
		}

	}
}
