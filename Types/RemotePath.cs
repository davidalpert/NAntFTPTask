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

using EnterpriseDT.Net.Ftp;

namespace Sourceforge.NAnt.Ftp.Util {
	public class RPath {

		const char DIR_SEP = '/';
		const char DOS_SEP = '\\';
		const char VOL_SEP = ':';

		public static string Combine(string dir, string name) {
			return dir + DIR_SEP.ToString() + name;
		}
		public static char DirectorySeparatorChar {
			get { return DIR_SEP;}
		}
		public static char VolumeSeparatorChar {
			get { return VOL_SEP;}
		}
		public static bool IsPathRooted(string path) {
			return path.StartsWith(DIR_SEP.ToString()) || path.StartsWith(DOS_SEP.ToString()); 
		}
	}
//	public class RemotePathArray : ArrayList {
//
//		public override object this[int index] {
//		 	get {
//				return (RemotePath)(base[index]);
//			}
//		 	set{
//				base[index] = value;
//			}
//		}
//		 
//	}
	public class RemotePath {

		const char DIR_SEP = '/';
		const char DOS_SEP = '\\';
		
		#region Private members 
		FTPFile _file 		 = null;
		private string _path = String.Empty;
		#endregion
		
		#region constructors
		public RemotePath() {
		}
		public RemotePath(string path) {
			_path = path;
		}
		public RemotePath(FTPFile file) {
			_file = file;
		}
		#endregion
		
		#region static methods
		public static RemotePath[] FromFTPFileArray(string basepath, FTPFile[] files) {
			RemotePath[] result = new RemotePath[files.GetUpperBound(0)];
			int z = 0;
			foreach(FTPFile file in files) {
				result[z].Path = basepath;
				result[z++].File = file;
			}
			return result;
		}
		#endregion
		
		#region public access methods
		// todo: HERE
		public bool IsDir {
			get {return _file!=null && _file.Dir;}
		}
		public bool IsFile {
			get {return !(_file.Dir);}
		}
		public bool IsRooted {
			get {return _path.StartsWith(DIR_SEP.ToString()) || _path.StartsWith(DOS_SEP.ToString());}
		}
		public bool IsRelative {
			get {return !(_path.StartsWith(DIR_SEP.ToString()) || _path.StartsWith(DOS_SEP.ToString()));}
		}
		#endregion
		
		public string Dir {
			get {
				if (_path==String.Empty) {
					return "."+DIR_SEP.ToString();
				} else {
					return _path+DIR_SEP.ToString();
				}
			}
		}
		public string FullPath {
			get { 
				string result = Dir;
				if (_file!=null)
					result += _file.Name;
				return result;
			}
		}
		public string Name {
			get {
				if (_file!=null) {
					return _file.Name;
				} else {
					Dir;
				}
			}
		}
		public FTPFile File {
			get {return _file;}
			set {_file = value;}
		}
		public string Path {
			get { return _path;}
			set { _path = value;}
		}
	}
	
}
