// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Kevin Dente (kevindente@yahoo.com)
// David Alpert (david@spinthemoose.com)

// This is useful for debugging where filesets are scanned from - scanning is one
// of the most intensive activities for NAnt
//#define DEBUG_REGEXES

/*
Examples:
"**\*.class" matches all .class files/dirs in a directory tree.

"test\a??.java" matches all files/dirs which start with an 'a', then two
more characters and then ".java", in a directory called test.

"**" matches everything in a directory tree.

"**\test\**\XYZ*" matches all files/dirs that start with "XYZ" and where
there is a parent directory called test (e.g. "abc\test\def\ghi\XYZ123").

Example of usage:

DirectoryScanner scanner = DirectoryScanner();
scanner.Includes.Add("**\\*.class");
scanner.Exlucdes.Add("modules\\*\\**");
scanner.BaseDirectory = "test";
scanner.Scan();
foreach (string filename in GetIncludedFiles()) {
    Console.WriteLine(filename);
}
*/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Util;

using Sourceforge.NAnt.Ftp.Tasks;
using Sourceforge.NAnt.Ftp.Util;

using EnterpriseDT.Net.Ftp;

namespace Sourceforge.NAnt.Ftp.Util {
    /// <summary>
    /// Used for searching a remote filesystem based on given include/exclude rules.
    /// </summary>
    /// <example>
    ///     <para>Simple client code for testing the class.</para>
    ///     <code>
    ///         while (true) {
    ///             DirectoryScanner scanner = new DirectoryScanner();
    ///
    ///             Console.Write("Scan Basedirectory : ");
    ///             string s = Console.ReadLine();
    ///             if (s.Length == 0) break;
    ///             scanner.BaseDirectory = s;
    ///
    ///             while(true) {
    ///                 Console.Write("Include pattern : ");
    ///                 s = Console.ReadLine();
    ///                 if (s.Length == 0) break;
    ///                 scanner.Includes.Add(s);
    ///             }
    ///
    ///             while(true) {
    ///                 Console.Write("Exclude pattern : ");
    ///                 s = Console.ReadLine();
    ///                 if (s.Length == 0) break;
    ///                 scanner.Excludes.Add(s);
    ///             }
    ///
    ///             foreach (string name in scanner.FileNames)
    ///                 Console.WriteLine("file:" + name);
    ///             foreach (string name in scanner.DirectoryNames)
    ///                 Console.WriteLine("dir :" + name);
    ///
    ///             Console.WriteLine("");
    ///         }
    ///     </code>
    /// </example>
    /// <history>
    ///     <change date="20020220" author="Ari H�nnik�inen">Added support for absolute paths and relative paths refering to parent directories ( ../ )</change>
    ///     <change date="20020221" author="Ari H�nnik�inen">Changed implementation because of performance reasons - now scanning each directory only once</change>
    ///     <change date="20030224" author="Brian Deacon (bdeacon at vidya dot com)">
    ///         Fixed a bug that was causing absolute pathnames to turn into an invalid regex pattern, and thus never match.
    ///     </change>
    /// </history>
    [Serializable()]
    public class RemoteDirectoryScanner : DirectoryScanner {
        #region Private Instance Fields

        // set to current directory in Scan if user doesn't specify something first.
        // keeping it null, lets the user detect if it's been set or not.
        private string _baseDirectory;

        // holds the nant patterns (absolute or relative paths)
        private StringCollectionWithGoodToString  _includes = new StringCollectionWithGoodToString();
        private StringCollectionWithGoodToString  _excludes = new StringCollectionWithGoodToString();

        // holds the nant patterns converted to regular expression patterns (absolute canonized paths)
        private ArrayList  _includePatterns;
        private ArrayList  _excludePatterns;

        // holds the nant patterns converted to non-regex names (absolute canonized paths)
        private StringCollectionWithGoodToString  _includeNames;
        private StringCollectionWithGoodToString  _excludeNames;

        // holds the result from a scan
        private ArrayList  _fileNames;
        private DirScannerStringCollection _directoryNames;

        // directories that should be scanned and directories scanned so far
        private DirScannerStringCollection _searchDirectories;
        private DirScannerStringCollection _scannedDirectories;
        private ArrayList _searchDirIsRecursive;

        private FTPTask _conn;
        #endregion Private Instance Fields

        public RemoteDirectoryScanner () {
        	_conn = new FTPTask();
        }
        public RemoteDirectoryScanner (FTPTask supervisor) {
        	_conn = supervisor;
        }
        public FTPTask Conn { 
        	get {return _conn;}
        	set {_conn = value;} 
        }
        
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static Hashtable cachedCaseSensitiveRegexes = new Hashtable();
        private static Hashtable cachedCaseInsensitiveRegexes = new Hashtable();

        #endregion Private Static Fields

        #region Implementation of ICloneable

        /// <summary>
        /// Creates a shallow copy of the <see cref="DirectoryScanner" />.
        /// </summary>
        /// <returns>
        /// A shallow copy of the <see cref="DirectoryScanner" />.
        /// </returns>
        public new object Clone() {
            RemoteDirectoryScanner clone = new RemoteDirectoryScanner(_conn);
            if (_baseDirectory != null) {
                clone._baseDirectory = _baseDirectory;
            }
            if (_directoryNames != null) {
                clone._directoryNames = (DirScannerStringCollection) 
                    _directoryNames.Clone();
            }
            if (_excludePatterns != null) {
                clone._excludePatterns = (ArrayList) 
                    _excludePatterns.Clone();
            }
            if (_excludeNames != null) {
                clone._excludeNames = (StringCollectionWithGoodToString) 
                    _excludeNames.Clone();
            }
            clone._excludes = (StringCollectionWithGoodToString) _excludes.Clone();
            if (_fileNames != null) {
            	clone._fileNames = (ArrayList) _fileNames.Clone();
            }
            if (_includePatterns != null) {
                clone._includePatterns = (ArrayList) 
                    _includePatterns.Clone();
            }
            if (_includeNames != null) {
                clone._includeNames = (StringCollectionWithGoodToString) 
                    _includeNames.Clone();
            }
            clone._includes = (StringCollectionWithGoodToString) _includes.Clone();
            if (_scannedDirectories != null) {
                clone._scannedDirectories = (DirScannerStringCollection) 
                    _scannedDirectories.Clone();
            }
            if (_searchDirectories != null) {
                clone._searchDirectories = (DirScannerStringCollection) 
                    _searchDirectories.Clone();
            }
            if (_searchDirIsRecursive != null) {
                clone._searchDirIsRecursive = (ArrayList) 
                    _searchDirIsRecursive.Clone();
            }
            return clone;
        }

        #endregion Implementation of ICloneable

        #region Public Instance Properties

        /// <summary>
        /// Gets the collection of include patterns.
        /// </summary>
        public new StringCollectionWithGoodToString Includes {
            get { return _includes; }
        }

        /// <summary>
        /// Gets the collection of exclude patterns.
        /// </summary>
        public new StringCollectionWithGoodToString Excludes {
            get { return _excludes; }
        }

        /// <summary>
        /// The base directory to scan. The default is the 
        /// <see cref="Environment.CurrentDirectory">current directory</see>.
        /// </summary>
        public new string BaseDirectory {
            get { 
                if (_baseDirectory == null) {
                    _baseDirectory = ".";
                }
                return _baseDirectory; 
            }
            set { 
                    _baseDirectory = value;
            }
        }

        /// <summary>
        /// Gets the list of files that match the given patterns.
        /// </summary>
        public new ArrayList FileNames {
            get {
                if (_fileNames == null) {
                    Scan();
                }
        		return _fileNames;
            }
        }

        /// <summary>
        /// Gets the list of directories that match the given patterns.
        /// </summary>
        public new DirScannerStringCollection DirectoryNames {
            get {
                if (_directoryNames == null) {
                    Scan();
                }
                return _directoryNames;
            }
        }

        /// <summary>
        /// Gets the list of directories that were scanned for files.
        /// </summary>
        public new DirScannerStringCollection ScannedDirectories {
            get {
                if (_scannedDirectories == null) {
                    Scan();
                }
                return _scannedDirectories;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Uses <see cref="Includes" /> and <see cref="Excludes" /> search criteria (relative to 
        /// <see cref="BaseDirectory" /> or absolute), to search for filesystem objects.
        /// </summary>
        /// <history>
        ///     <change date="20020220" author="Ari H�nnik�inen">Totally changed the scanning strategy</change>
        ///     <change date="20020221" author="Ari H�nnik�inen">Changed it again because of performance reasons</change>
        /// </history>
        public new void Scan() {
            _includePatterns = new ArrayList();
            _includeNames = new StringCollectionWithGoodToString ();
            _excludePatterns = new ArrayList();
            _excludeNames = new StringCollectionWithGoodToString ();
            _fileNames = new ArrayList();
            _directoryNames = new DirScannerStringCollection(_conn);
            _searchDirectories = new DirScannerStringCollection(_conn);
            _searchDirIsRecursive = new ArrayList();
            _scannedDirectories = new DirScannerStringCollection(_conn);

#if DEBUG_REGEXES
            Console.WriteLine("*********************************************************************");
            Console.WriteLine("DirectoryScanner.Scan()");
            Console.WriteLine("*********************************************************************");
            Console.WriteLine(new System.Diagnostics.StackTrace().ToString());


            Console.WriteLine("Base Directory: " + BaseDirectory);
            Console.WriteLine("Includes:");
            foreach (string strPattern in _includes)
                Console.WriteLine(strPattern);
            Console.WriteLine("Excludes:");
            foreach (string strPattern in _excludes)
                Console.WriteLine(strPattern);

            Console.WriteLine("--- Starting Scan ---");
#endif

            // convert given NAnt patterns to regex patterns with absolute paths
            // side effect: searchDirectories will be populated
            ConvertPatterns(_includes, _includePatterns, _includeNames, true);
            ConvertPatterns(_excludes, _excludePatterns, _excludeNames, false);

            for (int index = 0; index < _searchDirectories.Count; index++) {
                ScanDirectory(_searchDirectories[index], (bool) _searchDirIsRecursive[index]);
            }
            
#if DEBUG_REGEXES
            Console.WriteLine("*********************************************************************");
#endif
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Parses specified NAnt search patterns for search directories and 
        /// corresponding regex patterns.
        /// </summary>
        /// <param name="nantPatterns">In. NAnt patterns. Absolute or relative paths.</param>
        /// <param name="regexPatterns">Out. Regex patterns. Absolute canonical paths.</param>
        /// <param name="nonRegexFiles">Out. Non-regex files. Absolute canonical paths.</param>
        /// <param name="addSearchDirectories">In. Whether to allow a pattern to add search directories.</param>
        /// <history>
        ///     <change date="20020221" author="Ari H�nnik�inen">Created</change>
        /// </history>
        private void ConvertPatterns(StringCollection nantPatterns, ArrayList regexPatterns, StringCollection nonRegexFiles, bool addSearchDirectories) {
            string searchDirectory;
            string regexPattern;
            bool isRecursive;
            bool isRegex;

            foreach (string nantPattern in nantPatterns) {
                ParseSearchDirectoryAndPattern(addSearchDirectories, nantPattern, out searchDirectory, out isRecursive, out isRegex, out regexPattern);
                if (isRegex) {
                    RegexEntry entry = new RegexEntry();
                    entry.IsRecursive = isRecursive;
                    entry.BaseDirectory = searchDirectory;
                    entry.Pattern = regexPattern;
                    if (regexPattern.EndsWith(@"**/*") || regexPattern.EndsWith(@"**\*"))
                        logger.Warn( "**/* pattern may not produce desired results" );

                    regexPatterns.Add(entry);
                } else {
                    // David Alpert (david@spinthemoose.com)
                    // Tuesday, December 21, 2004
                    // string exactName = RPath.Combine(searchDirectory, regexPattern);
                    string exactName = regexPattern;
                    if (!nonRegexFiles.Contains(exactName)) {
                        nonRegexFiles.Add(exactName);
                    } 
                }
                
                if (!addSearchDirectories) {
                    continue;
                }
                int index = _searchDirectories.IndexOf(searchDirectory);

                // if the directory was found before, but wasn't recursive 
                // and is now, mark it as so
                if (index > -1) {
                    if (!(bool)_searchDirIsRecursive[index] && isRecursive) {
                        _searchDirIsRecursive[index] = isRecursive;
                    }
                }

                // if the directory has not been added, add it
                if (index == -1) {
                    _searchDirectories.Add(searchDirectory);
                    _searchDirIsRecursive.Add(isRecursive);
                }
            }
        }

        /// <summary>
        /// Given a NAnt search pattern returns a search directory and an regex 
        /// search pattern.
        /// </summary>
        /// <param name="isInclude">Whether this pattern is an include or exclude pattern</param>
        /// <param name="originalNAntPattern">NAnt searh pattern (relative to the Basedirectory OR absolute, relative paths refering to parent directories ( ../ ) also supported)</param>
        /// <param name="searchDirectory">Out. Absolute canonical path to the directory to be searched</param>
        /// <param name="recursive">Out. Whether the pattern is potentially recursive or not</param>
        /// <param name="isRegex">Out. Whether this is a regex pattern or not</param>
        /// <param name="regexPattern">Out. Regex search pattern (absolute canonical path)</param>
        /// <history>
        ///     <change date="20020220" author="Ari H�nnik�inen">Created</change>
        ///     <change date="20020221" author="Ari H�nnik�inen">Returning absolute regex patterns instead of relative nant patterns</change>
        ///     <change date="20030224" author="Brian Deacon (bdeacon at vidya dot com)">
        ///     Added replacing of slashes with Path.DirectorySeparatorChar to make this OS-agnostic.  Also added the Path.IsPathRooted check
        ///     to support absolute pathnames to prevent basedir = "/foo/bar" and pattern="/fudge/nugget" from being incorrectly turned into 
        ///     "/foo/bar/fudge/nugget".  (pattern = "fudge/nugget" would still be treated as relative to basedir)
        ///     </change>
        /// </history>
        private void ParseSearchDirectoryAndPattern(bool isInclude, string originalNAntPattern, out string searchDirectory, out bool recursive, out bool isRegex, out string regexPattern) {
            string s = originalNAntPattern;
            s = s.Replace('\\', RPath.DirectorySeparatorChar);
            s = s.Replace('/', RPath.DirectorySeparatorChar);

            // Get indices of pieces used for recursive check only
            int indexOfFirstDirectoryWildcard = s.IndexOf("**");
            int indexOfLastOriginalDirectorySeparator = s.LastIndexOf(RPath.DirectorySeparatorChar);

            // search for the first wildcard character (if any) and exclude the rest of the string beginnning from the character
            char[] wildcards = {'?', '*'};
            int indexOfFirstWildcard = s.IndexOfAny(wildcards);
            if (indexOfFirstWildcard != -1) { // if found any wildcard characters
                s = s.Substring(0, indexOfFirstWildcard);
            }

            // find the last DirectorySeparatorChar (if any) and exclude the rest of the string
            int indexOfLastDirectorySeparator = s.LastIndexOf(RPath.DirectorySeparatorChar);

            // The pattern is potentially recursive if and only if more than one base directory could be matched.
            // ie: 
            //    **
            //    **/*.txt
            //    foo*/xxx
            //    x/y/z?/www
            // This condition is true if and only if:
            //  - The first wildcard is before the last directory separator, or
            //  - The pattern contains a directory wildcard ("**")
            recursive = (indexOfFirstWildcard != -1 && (indexOfFirstWildcard < indexOfLastOriginalDirectorySeparator )) || indexOfFirstDirectoryWildcard != -1;

            // substring preceding the separator represents our search directory 
            // and the part following it represents nant search pattern relative 
            // to it
            if (indexOfLastDirectorySeparator != -1) {
                s = originalNAntPattern.Substring(0, indexOfLastDirectorySeparator);
                if (s.Length == 2 && s[1] == RPath.VolumeSeparatorChar) {
                    s += RPath.DirectorySeparatorChar;
                }
            } else {
                s = "";
            }
            
            //We only prepend BaseDirectory when s represents a relative path.
            if (RPath.IsPathRooted(s)) {
            	searchDirectory = new RemotePath(s, true).FullPath;
            } else {
                //We also (correctly) get to this branch of code when s.Length == 0
                // Note that I tried setting the base directory of unrooted exclude patterns to "" but this ends up
                // matching base directories where it shouldn't.
//              if (isInclude || indexOfFirstWildcard == -1)
//
// David Alpert (david@spinthemoose.com)
// Wednesday, December 22, 2004
// let's try simplifying this because i keep getting the search directory appended to the base direcotry (i.e. /net/test + test/ does not exist because we're already in test!)
//                    searchDirectory = new RemotePath(RPath.Combine(
//                        BaseDirectory, s), true).FullPath;
					searchDirectory = s;
//              else
//                  searchDirectory = String.Empty;

				if (searchDirectory==String.Empty) {
					searchDirectory = ".";
				}
            }
            
            string modifiedNAntPattern = originalNAntPattern.Substring(indexOfLastDirectorySeparator + 1);
            
            // if it's not a wildcard, just return
            if (indexOfFirstWildcard == -1) {
                regexPattern = CleanPath(BaseDirectory, originalNAntPattern);
                isRegex = false;
#if DEBUG_REGEXES
                Console.WriteLine( "Convert name: {0} -> {1}", originalNAntPattern, regexPattern );
#endif
                return;
            }

            //if the fs in case insensitive, make all the regex directories lowercase.
            regexPattern = ToRegexPattern(modifiedNAntPattern);

#if DEBUG_REGEXES
            Console.WriteLine( "Convert pattern: {0} -> [{1}]{2}", originalNAntPattern, searchDirectory, regexPattern );
#endif
            
            isRegex = true;
        }

        private bool IsCaseSensitiveFileSystem(string path) {
            // Windows (not case-sensitive) is backslash, others (e.g. Unix) are not
//            return (VolumeInfo.IsVolumeCaseSensitive(new Uri(_conn.ResolvePath(path) + RPath.DirectorySeparatorChar)));             
            return true;
        }

        /// <summary>
        /// Searches a directory recursively for files and directories matching 
        /// the search criteria.
        /// </summary>
        /// <param name="path">Directory in which to search (absolute canonical path)</param>
        /// <param name="recursive">Whether to scan recursively or not</param>
        /// <history>
        ///     <change date="20020221" author="Ari H�nnik�inen">Checking if the directory has already been scanned</change>
        /// </history>
        private void ScanDirectory(string path, bool recursive) {
            // scan each directory only once
            if (_scannedDirectories.Contains(path)) {
                return;
            }

            // add directory to list of scanned directories
            _scannedDirectories.Add(path);

            // if the path doesn't exist, return.
            if (!_conn.remoteDirExists(path)) {
                return;
            }

            // get info for the current directory
            RemotePath currentDir = new RemotePath(path, true);

            // check whether directory is on case-sensitive volume
            bool caseSensitive = IsCaseSensitiveFileSystem(path);
            string pathCompare = path;
            if (!caseSensitive)
                pathCompare = pathCompare.ToLower();

            CompareOptions compareOptions = CompareOptions.None;
            CompareInfo compare = CultureInfo.InvariantCulture.CompareInfo;
            
            if (!caseSensitive)
                compareOptions |= CompareOptions.IgnoreCase;

            ArrayList includedPatterns = new ArrayList();
            ArrayList excludedPatterns = new ArrayList();

            // Only include the valid patterns for this path
            foreach (RegexEntry entry in _includePatterns) {
                string baseDirectory = (caseSensitive ? entry.BaseDirectory : entry.BaseDirectory.ToLower()); 

                // check if the directory being searched is equal to the 
                // basedirectory of the RegexEntry
                if (compare.Compare(path, baseDirectory, compareOptions) == 0) {
                    includedPatterns.Add(entry);
                } else {
                    // check if the directory being searched is subdirectory of 
                    // basedirectory of RegexEntry

                    if (!entry.IsRecursive) {
                        continue;
                    }

                    // make sure basedirectory ends with directory separator
                    if (!StringUtils.EndsWith(baseDirectory, RPath.DirectorySeparatorChar)) {
                        baseDirectory += RPath.DirectorySeparatorChar;
                    }

                    if (pathCompare.StartsWith(baseDirectory)) {
                        includedPatterns.Add(entry);
                    }
                }
            }

            foreach (RegexEntry entry in _excludePatterns) {
                string baseDirectory = (caseSensitive ? entry.BaseDirectory : entry.BaseDirectory.ToLower()); 
                
                if (entry.BaseDirectory.Length == 0 || compare.Compare(path, baseDirectory, compareOptions) == 0) {
                    excludedPatterns.Add(entry);
                } else {
                    // check if the directory being searched is subdirectory of 
                    // basedirectory of RegexEntry

                    if (!entry.IsRecursive) {
                        continue;
                    }

                    // make sure basedirectory ends with directory separator
                    if (!StringUtils.EndsWith(baseDirectory, RPath.DirectorySeparatorChar)) {
                        baseDirectory += RPath.DirectorySeparatorChar;
                    }

                    if (pathCompare.StartsWith(baseDirectory)) {
                        excludedPatterns.Add(entry);
                    }
                }
            }

            // grab the directory listing and sort into dirs and files
            //
//            int dirCount = 0;
//            int fileCount = 0;
			RemotePath[] list = RemotePath.FromFTPFileArray(path, _conn.DirDetails(path));
			            
            //foreach (RemotePath dir in _conn.GetDirs(currentDir.FullPath))
            foreach (RemotePath dir in list)
            {
            	if (dir.IsDir) {
	                if (recursive) {
	                    // scan subfolders if we are running recursively
	                    ScanDirectory(dir.FullPath, true);
	                } else {
	                    // otherwise just test to see if the subdirectories are included
	                    if (IsPathIncluded(dir.FullPath, caseSensitive, includedPatterns, excludedPatterns)) {
	                        _directoryNames.Add(dir.FullPath);
	                    }
	                }
            	}
            }

            // scan files
            //foreach (RemotePath file in _conn.GetFiles(currentDir.FullPath)) {
            foreach (RemotePath file in list) {
            	if (file.IsFile) {
	                string filename = file.FullPath;
	                if (!caseSensitive)
	                    filename = filename.ToLower();
	                if (IsPathIncluded(filename, caseSensitive, includedPatterns, excludedPatterns)) {
	                    _fileNames.Add(file);
	                }
            	}
            }

            // check current path last so that delete task will correctly
            // delete empty directories.  This may *seem* like a special case
            // but it is more like formalizing something in a way that makes
            // writing the delete task easier :)
            if (IsPathIncluded(currentDir.FullPath, caseSensitive, includedPatterns, excludedPatterns)) {
                _directoryNames.Add(currentDir.Path);
            }
        }
        
        private bool TestRegex(string path, RegexEntry entry, bool caseSensitive) {
            Hashtable regexCache = caseSensitive ? cachedCaseSensitiveRegexes : cachedCaseInsensitiveRegexes;
            Regex r = (Regex)regexCache[entry.Pattern];
            
            if (r == null) {
                RegexOptions regexOptions = RegexOptions.Compiled;
                
                if (!caseSensitive)
                    regexOptions |= RegexOptions.IgnoreCase;
                    
                regexCache[entry.Pattern] = r = new Regex(entry.Pattern, regexOptions);
            }
            
            // Check to see if the empty string matches the pattern
            if (path.Length == entry.BaseDirectory.Length) {
#if DEBUG_REGEXES
                Console.WriteLine("{0} (empty string) [basedir={1}]", entry.Pattern, entry.BaseDirectory);
#endif
                return r.IsMatch(String.Empty);
            }

            bool endsWithSlash = StringUtils.EndsWith(entry.BaseDirectory, Path.DirectorySeparatorChar);
#if DEBUG_REGEXES
            Console.WriteLine("{0} ({1}) [basedir={2}]", entry.Pattern, path.Substring(entry.BaseDirectory.Length + ((endsWithSlash) ? 0 : 1)), entry.BaseDirectory);
#endif
            if (endsWithSlash) {
                return r.IsMatch(path.Substring(entry.BaseDirectory.Length));
            } else {
                return r.IsMatch(path.Substring(entry.BaseDirectory.Length + 1));
            }
        }

        private bool IsPathIncluded(string path, bool caseSensitive, ArrayList includedPatterns, ArrayList excludedPatterns) {
            bool included = false;
            
            CompareOptions compareOptions = CompareOptions.None;
            CompareInfo compare = CultureInfo.InvariantCulture.CompareInfo;
            
            if (!caseSensitive)
                compareOptions |= CompareOptions.IgnoreCase;
            
#if DEBUG_REGEXES
            Console.WriteLine("Test: {0}", path);
#endif
 
            // check path against include names
            foreach (string name in _includeNames) 
            {
#if DEBUG_REGEXES
                Console.WriteLine("Test include name: '{0}'", name);
#endif
                if (compare.Compare(name, path, compareOptions) == 0) 
                {
                    included = true;
#if DEBUG_REGEXES
                    Console.WriteLine("Included by name: {0}", name);
#endif
                    break;
                }
            }

            // check path against include regexes
            if (!included) {
                foreach (RegexEntry entry in includedPatterns) {
#if DEBUG_REGEXES
                    Console.Write("Test include pattern: ");
#endif
                    if (TestRegex(path, entry, caseSensitive)) 
                    {
                        included = true;
#if DEBUG_REGEXES
                        Console.WriteLine("Included by pattern: {0}", entry.Pattern);
#endif
                        break;
                    }
                }
            }
            
            // check path against exclude names
            if (included) {
                foreach (string name in _excludeNames) {
#if DEBUG_REGEXES
                    Console.WriteLine("Test exclude name: '{0}'", name);
#endif
                    if (compare.Compare(name, path, compareOptions) == 0) 
                    {
                        included = false;
#if DEBUG_REGEXES
                        Console.WriteLine("Excluded by name: {0}", name);
#endif
                        break;
                    }
                }
            }
            
            // check path against exclude regexes
            if (included) {
                foreach (RegexEntry entry in excludedPatterns) {
#if DEBUG_REGEXES
                    Console.Write("Test exclude pattern: ");
#endif
                    if (TestRegex(path, entry, caseSensitive)) 
                    {
                        included = false;
#if DEBUG_REGEXES
                        Console.WriteLine("Excluded by pattern: {0}", entry.Pattern);
#endif
                        break;
                    }
                }
            }

 #if DEBUG_REGEXES
             Console.WriteLine("Result: {0}", included);
 #endif
           return included;
        }

        #endregion Private Instance Methods

        #region Private Static Methods

        private static StringBuilder CleanPath(string nantPath) 
        {
            StringBuilder pathBuilder = new StringBuilder(nantPath);

            // NAnt patterns can use either / \ as a directory seperator.
            // We must replace both of these characters with Path.DirectorySeperatorChar
            pathBuilder.Replace('/',  RPath.DirectorySeparatorChar);
            pathBuilder.Replace('\\', RPath.DirectorySeparatorChar);
            
            return pathBuilder;
        }

        private static string CleanPath(string baseDirectory, string nantPath) 
        {
        	return new RemotePath(RPath.Combine(baseDirectory, CleanPath(nantPath).ToString()), true).FullPath;
        }

        /// <summary>
        /// Converts search pattern to a regular expression pattern.
        /// </summary>
        /// <param name="nantPattern">Search pattern relative to the search directory.</param>
        /// <returns>Regular expresssion</returns>
        /// <history>
        ///     <change date="20020220" author="Ari H�nnik�inen">Added parameter baseDir, using  it instead of class member variable</change>
        /// </history>
        private static string ToRegexPattern(string nantPattern) {
            StringBuilder pattern = CleanPath(nantPattern);

            // The '\' character is a special character in regular expressions
            // and must be escaped before doing anything else.
            pattern.Replace(@"\", @"\\");

            // Escape the rest of the regular expression special characters.
            // NOTE: Characters other than . $ ^ { [ ( | ) * + ? \ match themselves.
            // TODO: Decide if ] and } are missing from this list, the above
            // list of characters was taking from the .NET SDK docs.
            pattern.Replace(".", @"\.");
            pattern.Replace("$", @"\$");
            pattern.Replace("^", @"\^");
            pattern.Replace("{", @"\{");
            pattern.Replace("[", @"\[");
            pattern.Replace("(", @"\(");
            pattern.Replace(")", @"\)");
            pattern.Replace("+", @"\+");

            // Special case directory seperator string under Windows.
            string seperator = RPath.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);
            if (seperator == @"\") {
                seperator = @"\\";
            }

            // Convert NAnt pattern characters to regular expression patterns.

            // Start with ? - it's used below
            pattern.Replace("?", "[^" + seperator + "]?");

            // SPECIAL CASE: any *'s directory between slashes or at the end of the
            // path are replaced with a 1..n pattern instead of 0..n: (?<=\\)\*(?=($|\\))
            // This ensures that C:\*foo* matches C:\foo and C:\* won't match C:.
            pattern = new StringBuilder(Regex.Replace(pattern.ToString(), "(?<=" + seperator + ")\\*(?=($|" + seperator + "))", "[^" + seperator + "]+"));
            
            // SPECIAL CASE: to match subdirectory OR current directory, If
            // we do this then we can write something like 'src/**/*.cs'
            // to match all the files ending in .cs in the src directory OR
            // subdirectories of src.
            pattern.Replace(seperator + "**" + seperator, seperator + "(.|?" + seperator + ")?" );
            pattern.Replace("**" + seperator, ".|(?<=^|" + seperator + ")" );

            // .| is a place holder for .* to prevent it from being replaced in next line
            pattern.Replace("**", ".|");
            pattern.Replace("*", "[^" + seperator + "]*");
            pattern.Replace(".|", ".*"); // replace place holder string

            // Help speed up the search
            if (pattern.Length > 0) {
                pattern.Insert(0, '^'); // start of line
                pattern.Append('$'); // end of line
            }

            string patternText = pattern.ToString();

            if (patternText.StartsWith("^.*"))
                patternText = patternText.Substring(3);
            if (patternText.EndsWith(".*$"))
                patternText = patternText.Substring(0, pattern.Length-3);

            return patternText.ToString();
        }

        #endregion Private Static Methods

        private class RegexEntry {
            public bool        IsRecursive;
            public string    BaseDirectory;
            public string    Pattern;
        }
    }

    [Serializable()]
    public class StringCollectionWithGoodToString : StringCollection, ICloneable {
        #region Implementation of ICloneable

        /// <summary>
        /// Creates a shallow copy of the <see cref="StringCollectionWithGoodToString" />.
        /// </summary>
        /// <returns>
        /// A shallow copy of the <see cref="StringCollectionWithGoodToString" />.
        /// </returns>
        public virtual object Clone() {
            string[] strings = new string[Count];
            CopyTo(strings, 0);
            StringCollectionWithGoodToString clone = new StringCollectionWithGoodToString();
            clone.AddRange(strings);
            return clone;
        }

        #endregion Implementation of ICloneable

        #region Override implemenation of Object

        /// <summary>
        /// Creates a string representing a list of the strings in the collection.
        /// </summary>
        /// <returns>
        /// A string that represents the contents.
        /// </returns>
        public override string ToString() {
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.Append(":" + Environment.NewLine);
            foreach (string s in this) {
                sb.Append(s);
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        #endregion Override implemenation of Object
    }

    [Serializable()]
    public class DirScannerStringCollection : StringCollectionWithGoodToString {

    	private FTPTask _conn;
    	
    	public DirScannerStringCollection(FTPTask conn) {
    		_conn=conn;
    	}
    	
    	#region Override implementation of ICloneable

        /// <summary>
        /// Creates a shallow copy of the <see cref="DirScannerStringCollection" />.
        /// </summary>
        /// <returns>
        /// A shallow copy of the <see cref="DirScannerStringCollection" />.
        /// </returns>
        public override object Clone() {
            string[] strings = new string[Count];
            CopyTo(strings, 0);
            DirScannerStringCollection clone = new DirScannerStringCollection(_conn);
            clone.AddRange(strings);
            return clone;
        }

        #endregion Override implementation of ICloneable

        #region Override implementation of StringCollection

        /// <summary>
        /// Determines whether the specified string is in the 
        /// <see cref="DirScannerStringCollection" />.
        /// </summary>
        /// <param name="value">The string to locate in the <see cref="DirScannerStringCollection" />. The value can be <see langword="null" />.</param>
        /// <returns>
        /// <seee langword="true" /> if value is found in the <see cref="DirScannerStringCollection" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// String comparisons within the <see cref="DirScannerStringCollection" />
        /// are only case-sensitive if the filesystem on which <paramref name="value" />
        /// is located, is case-sensitive.
        /// </remarks>
        public new virtual bool Contains(string value) {
            return (IndexOf(value) > -1);

        }

        /// <summary>
        /// Searches for the specified string and returns the zero-based index 
        /// of the first occurrence within the <see cref="DirScannerStringCollection" />.
        /// </summary>
        /// <param name="value">The string to locate. The value can be <see langword="null" />.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of <paramref name="value" /> 
        /// in the <see cref="DirScannerStringCollection" />, if found; otherwise, -1.
        /// </returns>
        /// <remarks>
        /// String comparisons within the <see cref="DirScannerStringCollection" />
        /// are only case-sensitive if the filesystem on which <paramref name="value" />
        /// is located, is case-sensitive.
        /// </remarks>
        public new virtual int IndexOf(string value) {
            if (value == null || IsCaseSensitiveFileSystem(value)) {
                return base.IndexOf(value);
            } else {
                string lowercaseValue = value.ToLower(CultureInfo.InvariantCulture);
                foreach (string s in this) {
                    if (s.ToLower(CultureInfo.InvariantCulture) == lowercaseValue) {
                        return base.IndexOf(s);
                    }
                }
                return -1;
            }
        }

        #endregion Override implementation of StringCollection

        #region Private Instance Methods

        /// <summary>
        /// Determines whether the filesystem on which the specified path is 
        /// located is case-sensitive.
        /// </summary>
        /// <param name="path">The path of which should be determined whether its on a case-sensitive filesystem.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="path" /> is located on a 
        /// case-sensitive filesystem; otherwise, <see langword="false" />.
        /// </returns>
        private bool IsCaseSensitiveFileSystem(string path) {
//        	return PlatformHelper.IsVolumeCaseSensitive(_conn.ResolvePath(path)
//                + RPath.DirectorySeparatorChar); 
			return true;
        }

        #endregion Private Instance Methods
    }
}
