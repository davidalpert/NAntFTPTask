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

		public static string Combine(string p1, string p2) {
			if (RPath.IsPathRooted(p2)) {
				return p2;
			} else {			
				return p1 + RPath.DirectorySeparatorChar.ToString() + p2;
			}
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
		public static string StripSingleDotReferences(string path) {
			const string doubleText = "|--DOUBLE--DOTS--|";
			const string doubleDots = "..";
			string singleDot  = "." +RPath.DirectorySeparatorChar.ToString();
			
			// sub out any double-dot references
			path = path.Replace(doubleDots, doubleText);
			
			{
				path = path.Replace(singleDot , String.Empty);

				// a trailing . at this point is redundant.
				char [] trim1 = {'.'};
				path = path.TrimEnd(trim1);
				
				if (path.Length>1) {
					// then a trailing slash is redundant (i.e. not root)
					char [] trim2 = {RPath.DirectorySeparatorChar};
					path = path.TrimEnd(trim2);
				}
			}
			
			// replace any double-dot references
			path = path.Replace(doubleText, doubleDots);

			return path;
		}
		public static string FixSeps(string path) {
			return path.Replace(DOS_SEP, RPath.DirectorySeparatorChar);
		}
		public static string RemoveDoubleSeps(string path) {
			return path.Replace(RPath.DirectorySeparatorChar.ToString()+RPath.DirectorySeparatorChar.ToString(), RPath.DirectorySeparatorChar.ToString());
		}
		public static string Clean(string path) {
			path = RPath.FixSeps(path);
			path = RPath.StripSingleDotReferences(path);
			path = RPath.RemoveDoubleSeps(path);
			return path;
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
//		public RemotePath() {
//		}
		public RemotePath(string path, bool isDir) {
			int sep = path.LastIndexOf(DIR_SEP);
			// store the head of the path in an FTPFile
			_file = new FTPFile(FTPFile.UNKNOWN, path.Substring(sep+1), isDir);
			// store the tail (if any) in a string
			if (sep>0) {
				_path = path.Substring(0,sep);
				if (_path==".") {
					_path=String.Empty;
				} else if (_path.Length>2 && _path[1]==DIR_SEP) {
					_path=_path.Substring(2);
				}
			}
		}
		public RemotePath(FTPFile file) {
			_file = file;
			_path = String.Empty;
		}
		#endregion
		
		#region static methods
		public static RemotePath[] FromFTPFileArray(string basepath, FTPFile[] files) {
			RemotePath[] result = new RemotePath[files.GetUpperBound(0)+1];
			int z = 0;
			foreach(FTPFile file in files) {
				result[z] = new RemotePath(file);
				result[z++].Path = basepath;
//				result[z++].File = file;
			}
			return result;
		}
		#endregion
		
		#region public access methods
		// if _file==null then the entire path is in the _path
		// and we don't know if it's a dir or a file...
		public bool IsDir {
			get {return _file.Dir;}
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
//				if (_path==String.Empty || _path==".") {
//					return _file.Name;
//				} else {
					return Dir + _file.Name;				
//				}
			}
		}
		public string Name {
			get {
				return _file.Name;
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
