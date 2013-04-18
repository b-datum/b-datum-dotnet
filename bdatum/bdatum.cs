using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Configuration;
using System.Collections;
using System.Collections.Specialized;

using System.Threading;
using System.Web;

//using SeasideResearch.LibCurlNet;

using System.Diagnostics;

using System.CodeDom;
using System.CodeDom.Compiler;

//It seems to need a higher dot net version
//using RestSharp;

using System.Runtime.InteropServices;

// Some reflections
using System.Reflection;

// Old Good Regex
using System.Text.RegularExpressions;


namespace bdatum
{
    
    public class bFileAgentNewFile : EventArgs
    {
        public bFile newfile { get; set; }
    }

    public class bFileAgentMessage : EventArgs
    {
        public string message { get; set; }
        public string colorname { get; set; }
        public bFileAgentMessage()
        {
            colorname = "Blue";
        }
    }

    class bWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).KeepAlive = true; // ?
                (request as HttpWebRequest).Timeout = System.Threading.Timeout.Infinite;
            }

            return base.GetWebRequest(address);
        }
    }

    #region Configuration

    public class bdatumConfigManager
    {
        public string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        //public string appPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private string fileSettings;
        private string filePathConfig;
        private string fileBlacklist;    

        public bOrganization Settings { get; set; }
        public bNode Node { get; set; }
        public List<string> Path { get; set; }
        public List<string> PathBlacklist { get; set; }

        public bdatumConfigManager()
        {
            if (String.IsNullOrEmpty(appPath))
            {
                appPath = @"c:\";
            }

            filePathConfig = appPath + @"\bdatum\pathinfo";
            fileSettings = appPath + @"\bdatum\settings";
            fileBlacklist = appPath + @"\bdatum\blacklist";

            Settings = new bOrganization();
            Node = new bNode();            
            Path = new List<string>();
            PathBlacklist = new List<string>();

            if (!File.Exists(appPath + @"\bdatum"))
            {
                System.IO.Directory.CreateDirectory(appPath + @"\bdatum");
            }

            LoadSettings();
            LoadPath();
            LoadBlackList();

            Node.node_key = Settings.node_key;
            Node.partner_key = Settings.partner_key;
            Node.blacklist = PathBlacklist;
        }

        public void Save()
        {
            SaveSettings();
            SavePath();
            SaveBlackList();

            Node.node_key = Settings.node_key;
            Node.partner_key = Settings.partner_key;
            Node.blacklist = PathBlacklist;
        }

        public void SaveSettings()
        {
            using (StreamWriter writer = new StreamWriter( fileSettings ) )
            {
                if (!String.IsNullOrEmpty(Settings.partner_key))
                {
                    writer.WriteLine("partner_key" + "\t" + Settings.partner_key);
                }
                if (!String.IsNullOrEmpty(Settings.node_key))
                {
                    writer.WriteLine("node_key" + "\t" + Settings.node_key);
                }
                if (Settings.last_successful_backup != null)
                {
                    writer.WriteLine("last_successful_backup" + "\t" + Settings.last_successful_backup.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }  
        }

        public void LoadSettings()
        {
            if ( File.Exists( fileSettings )){
                using (StreamReader reader = new StreamReader(fileSettings))
                {
                    while (!reader.EndOfStream)
                    {
                        string[] set = reader.ReadLine().Split('\t');
                        if (!String.IsNullOrEmpty(set[1]))
                        {
                            if (set[0] == "last_successful_backup")
                            {
                                Settings.last_successful_backup = DateTime.Parse(set[1], System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                PropertyInfo setinfo = Settings.GetType().GetProperty(set[0]);
                                setinfo.SetValue(Settings, set[1], null);
                            }
                        }
                    }
                }
            }            
        }

        public void SavePath()
        {
            using (StreamWriter writer = new StreamWriter(filePathConfig))
            {
                foreach (string file in Path)
                {                    
                    if (!String.IsNullOrEmpty(file))
                    {
                        writer.WriteLine(file);
                    }
                }
            }            
        }

        public void LoadPath()
        {
            if (File.Exists(filePathConfig))
            {
                using (StreamReader reader = new StreamReader(filePathConfig))
                {
                    while (!reader.EndOfStream)
                    {
                        string setpath = reader.ReadLine();
                        if (!String.IsNullOrEmpty(setpath))
                        {
                            Path.Add(setpath);                            
                        }
                    }
                }
            } 
        }

        public void SaveBlackList()
        {
            using (StreamWriter writer = new StreamWriter(fileBlacklist))
            {
                foreach (string file in PathBlacklist)
                {
                    if (!String.IsNullOrEmpty(file))
                    {
                        writer.WriteLine(file);
                    }
                }
            }    
        }

        public void LoadBlackList()
        {
            if (File.Exists(fileBlacklist))
            {
                using (StreamReader reader = new StreamReader(fileBlacklist))
                {
                    while (!reader.EndOfStream)
                    {
                        string setpath = reader.ReadLine();
                        if (!String.IsNullOrEmpty(setpath))
                        {
                            PathBlacklist.Add(setpath);
                        }
                    }
                }
            } 
        }

        public void SetBackupDone()
        {
            Settings.last_successful_backup = DateTime.Now;
        }

        public void ResetBackupDate()
        {
            Settings.last_successful_backup = new DateTime(1980, 01, 01);
        }

    }

    #endregion

    public class bFileAgent
    {

        #region atributes

        public List<string> path { get; set; }

        public bNode node { get; set; }
        private bool _IsRunning { get; set; }

        private bool _firstsync = false;

        public bool dummy_upload = false;
        public bool dummy_etag = false;

        // local path in absolute
        private Dictionary<string, bFile> filelist = new Dictionary<string, bFile>();

        private FileSystemWatcher watcher = new FileSystemWatcher();
        private Queue<bFile> syncronize = new Queue<bFile>();
        private bool syncronize_writing = false;

        // File Cache Info
        private string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private string FileCacheInfo;
        private Dictionary<string, string> FileCache = new Dictionary<string, string>();
        public DateTime reference { get; set; }
        public DateTime bigbang = new DateTime(1980, 01, 01);

        private bFileAgentMessage _message = new bFileAgentMessage();
        private bFileAgentNewFile bFileDetails = new bFileAgentNewFile();

        // File Recovery

        public string output_directory { get; set; }

        #endregion

        #region constructor

        public bFileAgent()
        {
            FileCacheInfo = appPath + @"\FileCacheInfo.dat";
            //ReadFileCacheInfo();
            _message.message = "Starting app";
            OnSendLogMessage(_message);
        }

        ~bFileAgent()
        {
           //_WriteFileCacheInfo();
        }

        #endregion

        #region Events

        public event EventHandler Updated;
        protected virtual void OnUpdated(EventArgs e)
        {
            if (Updated != null)
                Updated(this, e);
        }

        public event EventHandler AfterBackup;
        protected virtual void OnAfterBackup(EventArgs e)
        {
            if (AfterBackup != null)
                AfterBackup(this, e);
        }

        public event EventHandler AfterUpdateFileList;
        protected virtual void OnAfterUpdateFileList(EventArgs e)
        {
            if (AfterUpdateFileList != null)
                AfterUpdateFileList(this, e);
        }

        public delegate void AddedFileToListHandler(object sender, bFileAgentNewFile e);   
        public event AddedFileToListHandler AddedFileToList;
        protected virtual void OnAddedFileToList(bFileAgentNewFile e)
        {
            if (AddedFileToList != null)
                AddedFileToList(this, e);
        }

        public delegate void SendLogMessage(object sender, bFileAgentMessage e);
        public event SendLogMessage LogMessage;
        protected virtual void OnSendLogMessage(bFileAgentMessage e)
        {
            if (LogMessage != null)
                LogMessage(this, e);
        }
        
        // I don´t know if it should be a eventhandler
        public event EventHandler FileSyncError;
        protected virtual void OnFileSyncError(EventArgs e)
        {
            if (FileSyncError != null)
                FileSyncError(this, e);
        }

        #endregion

        #region FileCacheInfo

        private void ReadFileCacheInfo()
        {
            FileCache.Clear();

            _message.message = "Reading cache file data";
            OnSendLogMessage(_message);
            if (File.Exists(FileCacheInfo))
            {
                using ( StreamReader reader = new StreamReader( FileCacheInfo) )
                {
                    while (!reader.EndOfStream)
                    {
                        string [] cacheinfo = reader.ReadLine().Split('\t');
                        if (!String.IsNullOrEmpty(cacheinfo[1]))
                        {
                            FileCache.Add( cacheinfo[1], cacheinfo[0] );
                        }                        
                    } 
                }
            }
        }

        /* FileCacheLoadFileList()
         * 
         * Create a merged list of files to delete, to sent to queue. 
         * 
         * first the files, after the directories, ordered inversed by lenght ( not the best yet but works for now )
         * 
         * In true, send the all files to queue direct above, after send all directories ordered to the queue 
         *  
         * When the queue works... :)
         * 
         * TODO: Only delete from manifesto the file after it is really deleted from server
         * 
         * Dot net 4.0 
         * var pathsdodelete = from element in paths.Keys
         *       orderby element.Lenght
         *       select element;
         */
        private void FileCacheLoadFileList()
        {
            _message.message = "Reading cache file list";
            OnSendLogMessage(_message);

            ReadFileCacheInfo();

            List<string> todelete = new List<string>();            
            Dictionary<string, string> paths = new Dictionary<string, string>();

            foreach (string filename in FileCache.Keys)
            {
                if (!filelist.ContainsKey(filename))
                {
                    // create the bFile first.
                    // Race condition detected!!! 
                    if (Directory.Exists(filename) || File.Exists(filename))
                    {
                        bFile cached = this.addfile(filename);
                        cached.ETag = FileCache[filename];
                    }
                    else
                    {
                        // Class method?
                        bFile FileToDelete = new bFile(filename, this.node);

                        _message.message = " " + filename + " marked to delete";
                        OnSendLogMessage(_message);

                        if (FileToDelete.IsDirectory)
                        {
                            if (! paths.ContainsKey( FileToDelete.local_path )  )
                                paths.Add(FileToDelete.local_path, "d");
                        }
                        else
                        {
                            todelete.Add(filename);
                        }                        
                    }
                }
            }

            List<string> pathstodelete = new List<string>();
            foreach (string path in paths.Keys)
            {
                pathstodelete.Add(path);
            }
            pathstodelete.Sort(SortByLength);

            todelete.AddRange(pathstodelete);

            int count = 0;

            while (todelete.Count > 0 ||count > 5 )
            {
                count++;
                Thread.Sleep(1000);
                todelete = Delete(todelete);
                _message.message = "Trying again the files that failed to delete at server";
                OnSendLogMessage(_message);

            }           

        }

        private List<string> Delete(List<string> todelete)
        {
            // Delete all files
            List<string> notdeleted = new List<string>();
            foreach (string filename in todelete)
            {
                try
                {
                    bFile.delete(filename, node.auth_key());
                    filelist.Remove(filename);
                    _message.message = "SUCCESS: File " + filename + " Deleted";
                    OnSendLogMessage(_message);                    
                }
                catch (WebException e)
                {
                    
                    // add the file to queue to reprocess
                    switch (e.Status)
                    {
                        case WebExceptionStatus.NameResolutionFailure:
                            _message.message = "Connection timeout when deleting: " + filename;
                            OnSendLogMessage(_message);
                            notdeleted.Add(filename);
                            break;
                        case WebExceptionStatus.Timeout:
                            _message.message = "Connection timeout when deleting: " + filename;
                            OnSendLogMessage(_message);
                            notdeleted.Add(filename);
                            break;
                        case WebExceptionStatus.ProtocolError:
                            _message.message = "A response error from service was received: " + filename;
                            OnSendLogMessage(_message);
                            break;
                        default:
                            _message.message = "An unexpected answer from server: " + filename ;
                            OnSendLogMessage(_message);
                            _message.message = e.Status.ToString();
                            OnSendLogMessage(_message);
                            notdeleted.Add(filename);
                            break;
                    }
                    if (e.Status == WebExceptionStatus.ProtocolError)
                    {
                        HttpStatusCode code = ((HttpWebResponse)e.Response).StatusCode;
                    
                        switch (code)
                        {
                            case HttpStatusCode.Gone:
                                _message.message = "File already deleted from server: " + filename;
                                OnSendLogMessage(_message);
                                break;
                            case HttpStatusCode.NotFound:
                                _message.message = "File not found on server: " + filename;
                                notdeleted.Add(filename);
                                OnSendLogMessage(_message);
                                break;
                        }
                    }
                }                                
            }
            return notdeleted;
        }

        private static int SortByLength(string x, string y)
        {
            int compare = y.Length.CompareTo(x.Length);
            if (compare != 0)
            {
                return compare;
            }
            else
            {
                return y.CompareTo(x);
            }

        }

        private void _WriteFileCacheInfo()
        {
            using (StreamWriter writer = new StreamWriter(FileCacheInfo))
            {
                foreach (string key in filelist.Keys )
                {
                    bFile file = filelist[key];
                    //if (!String.IsNullOrEmpty(file.ETag) && !String.IsNullOrEmpty(file.local_path))
                    if (!String.IsNullOrEmpty(file.local_path))
                    {
                        if (file.IsDirectory)
                        {
                            //writer.WriteLine(file.ETag + '\t' + file.local_path + '\\');
                            writer.WriteLine(file.ETag + '\t' + file.local_path);
                        }
                        else
                        {
                            writer.WriteLine(file.ETag + '\t' + file.local_path);
                        }
                    }
                }
            }
         
        }

        // Etag rework cache
        public void FreshFileCacheInfo(DateTime Reference)
        {
            if (reference == null)
            {
                reference = Reference;
            }
            foreach( bFile file in filelist.Values )
            {
                _UpdateCachedFileETag(file);

                bFileDetails.newfile = file;
                OnAddedFileToList(bFileDetails);

            }

            OnUpdated(EventArgs.Empty);

            _WriteFileCacheInfo();
        }

        private void _UpdateCachedFileETag(bFile file)
        {
            if (!String.IsNullOrEmpty(file.local_path))
            {

                if (file.last_modified.CompareTo(reference) == 1)
                {
                    file.genETag();
                    file.status = "ETag cache";
                    if (FileCache.ContainsKey(file.local_path))
                    {
                        FileCache[file.local_path] = file.ETag;
                    }
                    else
                    {
                        FileCache.Add(file.local_path, file.ETag);
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(file.ETag))
                    {
                        if (FileCache.ContainsKey(file.local_path))
                        {
                            file.ETag = FileCache[file.local_path];
                        }
                        else
                        {
                            _message.message = "Cache miss for Etag" + file.local_path;
                            OnSendLogMessage(_message);
                            file.genETag();
                            file.status = "ETag cache";
                            FileCache.Add(file.local_path, file.ETag);
                        }
                    }
                }
            }
        }

        #endregion                

        #region FileAgent

        // For each file on path...
        // prepare...
        // run

        public void AddToQueue(bFile file)
        {
            lock (this)
            {
                syncronize.Enqueue(file); 
            }
        }

        public void Syncronizer()
        {
            lock (this)
            {
                if (syncronize.Count > 0)
                {
                    bFile sync = syncronize.Dequeue();

                    _message.message = "Processing " + sync.action + " of " + sync.local_path;
                    OnSendLogMessage(_message);

                    switch (sync.action)
                    {
                        case "upload":
                             sync.upload();                            
                             break;
                        case "delete":
                             sync.delete();
                             break;
                        default:
                            var donothing = "nod";
                            break;
                    }
                    bFileDetails.newfile = sync;
                    OnAddedFileToList(bFileDetails);
                    _message.message = "done with: " + sync.action + " of " + sync.local_path;
                    OnSendLogMessage(_message);
                    OnUpdated(EventArgs.Empty);
                    _WriteFileCacheInfo();
                }                
            }
        }

        // For now we are monitoring just the first path
        public void prepare()
        {
            _message.message = "File Agent prepared to look for changes on the"  + path[0];
            OnSendLogMessage(_message);

            watcher.Path = path[0];
            watcher.IncludeSubdirectories = true;

            // watch for changes in LastAccess and LastWrite times, 
            // and the renaming of files or directories
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Filter = "";

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
        }
        // Attrib is running?
        public void run()
        {
            _message.message = "File Agent On for: " + path[0];
            OnSendLogMessage(_message);

            watcher.EnableRaisingEvents = true;
            _IsRunning = true;

            // Should run on a thread, never returns
            while (_IsRunning)
            {
                Thread.Sleep(500);
                this.Syncronizer();
                
            }
        }

        public void stop()
        {
            watcher.EnableRaisingEvents = false;
            _IsRunning = false;
        }

        public bool IsRunning()
        {
            return _IsRunning;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            _message.message = "Somefile changed.. " + e.FullPath;
            OnSendLogMessage(_message);

            FileAttributes attr = File.GetAttributes(e.FullPath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                _message.message = "This is a directory, skipping for now:" + e.FullPath;
                OnSendLogMessage(_message);
                return;
            }
            
            // Calls the call back for the interface
            if (e.ChangeType.ToString() == "Created")
            {                
                // Refactor add
                bFile file_to_queue = new bFile(e.FullPath, node);                
                filelist.Add(file_to_queue.path, file_to_queue);
                // queue ( pointer it ? )
                file_to_queue.action = "upload";
                this.AddToQueue(file_to_queue);
                _message.message = "Fine, added this file to queue of processing for upload" + e.FullPath;
                OnSendLogMessage(_message);
                //filelist[newfile.path].upload();                
            }

            if (e.ChangeType.ToString() == "Deleted")
            {
                bFile file_to_queue = filelist[e.FullPath];
                file_to_queue.action = "delete";
                this.AddToQueue( file_to_queue ); //.delete();
                _message.message = "Fine, added this file to queue of processing for delete" + e.FullPath;
                OnSendLogMessage(_message);
            }

            if (e.ChangeType.ToString() == "Changed")
            {
                // Class method for return just paths
                bFile file_to_queue = new bFile(e.FullPath, node);
                file_to_queue.action = "upload";
                this.AddToQueue(file_to_queue);
                //filelist[file_to_queue.path].upload();
                _message.message = "Fine, added this file to queue of processing for update" + e.FullPath;
                OnSendLogMessage(_message);
            }
            // This event is trigged now by the sincronize
            //OnUpdated(EventArgs.Empty);
        }

        private  void OnRenamed(object source, RenamedEventArgs e)
        {
            OnUpdated(EventArgs.Empty);            
        }

        #endregion

        #region FileListManager

        public List<bFile> files()
        {
            List<bFile> files = new List<bFile>();
            foreach (var file in filelist.Values)
            {
                files.Add(file);
            }
            return files;
        }
        public int syncFileList()
        {
            _message.message = "Downloading the list of files to recover";
            OnSendLogMessage(_message);

            syncFileList("/");
            if (!_firstsync)
            {
                OnAfterUpdateFileList(EventArgs.Empty);
            }
            return filelist.Count;
        }
        public int syncFileList(string path)
        {
                _message.message = "Path: " + path;
                OnSendLogMessage(_message);

                foreach (bFile serverfile in node.list(path))
                {
                    // Is uri valid?                    
                    // /C/Users/Frederico/

                    serverfile.local_path = serverfile.path.Substring(1,1) + ":" + serverfile.path.Substring(2);

                    // TODO this when creating the file from server 

                    if (!serverfile.IsDirectory)
                    {
                        //Create directory
                    }
                    if (filelist.ContainsKey(serverfile.path))
                    {
                       // This file exists locally
                    }
                    else
                    {
                        if (serverfile.IsDirectory)
                        {
                            syncFileList(serverfile.path);
                        }
                        serverfile.status = "download"; // Deleting ( queue )
                        filelist.Add(serverfile.path, serverfile);
                        OnUpdated(EventArgs.Empty);

                        bFileDetails.newfile = serverfile;
                        OnAddedFileToList(bFileDetails);
                    }
                }
                _message.message = "Total of " + filelist.Count.ToString() + "files to recover";
                OnSendLogMessage(_message);

                return filelist.Count;
        }

        public List<bFile> RemoteDirectoryList(string path)
        {
            _message.message = "Path: " + path;
            OnSendLogMessage(_message);

            List<bFile> remotelist = new List<bFile>();

            foreach (bFile serverfile in node.list(path))
            {
                if (serverfile.IsDirectory)
                {
                    serverfile.local_path = serverfile.path.Substring(1, 1) + ":" + serverfile.path.Substring(2);

                    serverfile.status = "server";
                    remotelist.Add(serverfile);
                    OnUpdated(EventArgs.Empty);

                    bFileDetails.newfile = serverfile;
                    OnAddedFileToList(bFileDetails);
                }
            }

            return remotelist;
        }

        /* Remote dir list makes more sense in a tree
 * 
public List<bFile> RemoteDirectoryList(List<bFile> FileList, int level)
{          
    if (level > 0)            
    {               
        level--;

        _message.message = "Path: " + path;
        OnSendLogMessage(_message);

        foreach (bFile file in FileList)
        {
            List<bFile> files = node.list(file.path);

            foreach (bFile serverfile in files)
            {
                if (serverfile.IsDirectory)
                {
                    serverfile.local_path = serverfile.path.Substring(1, 1) + ":" + serverfile.path.Substring(2);
                    serverfile.status = "server";                            
                            
                    OnUpdated(EventArgs.Empty);

                    bFileDetails.newfile = serverfile;
                    OnAddedFileToList(bFileDetails);
                }                                               
            }
        }              
    }
    return FileList;                
}
 * 
 */

        // String : the date is a string now. 
        public int ServerFileList(string path, string ts = "")
        {
            _message.message = "Path: " + path;
            OnSendLogMessage(_message);

            // --  

            foreach (bFile serverfile in node.list_at_once(path,ts))
            {
                serverfile.local_path = serverfile.path.Substring(1, 1) + ":" + serverfile.path.Substring(2);
                
                filelist.Add(serverfile.path, serverfile);
                OnUpdated(EventArgs.Empty);

                bFileDetails.newfile = serverfile;
                OnAddedFileToList(bFileDetails);

            }
            _message.message = "Total of " + filelist.Count.ToString() + "files on server";
            OnSendLogMessage(_message);

            return filelist.Count;
             
        }

        /*  Read the local file list to backup 
         * 
         *  Fast will fail miserably if there is something wrong on the directory
         *  Slow will handler it better, but it is slower...
         *  They return true if they read the list from the cache
         */
        public int FastReadlocalDir()
        {
            _message.message = "Reading all files at once";
            OnSendLogMessage(_message);
            if ( bigbang.CompareTo(reference) < 0 )
            {
                FileCacheLoadFileList();

                _message.message = "There are " + filelist.Count + " to backup";
                OnSendLogMessage(_message);
                return filelist.Count;
            }
            else
            {
                _message.message = "Files list cache miss by date";
                OnSendLogMessage(_message);
                //var files = DirectoryExtensions.EnumerateFiles(this.path, "*.*");

                foreach (string loopPath in path)
                {
                    if (String.IsNullOrEmpty(loopPath))
                    {
                        continue;
                    }

                    string[] files = Directory.GetFiles(loopPath, "*.*", SearchOption.AllDirectories);

                    foreach (string file in files)
                    {
                        this._FastAddFile(file);
                    }
                    _WriteFileCacheInfo();
                }
                _message.message = "There are " + filelist.Count + " to backup";
                OnSendLogMessage(_message);
                return filelist.Count;
            }
            
        }
        public int SlowReadlocalDir()
        {
            //filelist.Clear();

            _message.message = "Reading all files one directory at time";
            OnSendLogMessage(_message);
            if (bigbang.CompareTo(reference) < 0)
            {
                // read from cache
                FileCacheLoadFileList();
                _message.message = "There are " + filelist.Count + " to backup";
                OnSendLogMessage(_message);
                //return filelist.Count;
            }
            else
            {
                _message.message = "Cache miss by date";
                OnSendLogMessage(_message);
            }


            // need to check all directories for changes
            foreach (string loopPath in path)
            {
                if (String.IsNullOrEmpty(loopPath))
                {
                    continue;
                }
                _readlocaldir(loopPath);
            }

            _message.message = "There are " + filelist.Count + " to backup";
            OnSendLogMessage(_message);
                
            _WriteFileCacheInfo();
            return filelist.Count;            
        }    
        public void _readlocaldir(string walkpath)
        {

            // if this path was not modified, we skip it. ( since last backup )
            // take the higher, last write or created
            var modified = System.IO.File.GetLastWriteTime(walkpath);
            var created = System.IO.File.GetCreationTime(walkpath);

            DateTime last_modified = created;

            if (modified.CompareTo(created) > 0)
                last_modified = modified;
                                   
            if ( last_modified.CompareTo(reference) < 0)
            {
                //return;
            }
            try
            {
                string[] files = Directory.GetFiles(walkpath, "*.*");

                foreach (string file in files)
                {
                    this.addfile(file);
                }
            }
            catch (Exception e)
            {
                var stop = e;
            };

            try
            {
                // do something to start download
                //if (file.last_modified.CompareTo(reference) > 0)
                string[] directories = Directory.GetDirectories(walkpath);
                foreach (string directory in directories)
                {
                    this.addfile(directory);
                    _readlocaldir(directory);
                }
            }
            catch (Exception e)
            {
                var stop = e;
            }

        }

        #endregion

        #region backup

        public void NonCachedFullBackup()
        {
            _message.message = "Will create a backup walking all directories checking all files";
            OnSendLogMessage(_message);
            foreach (string loopPath in path)
            {
                if (String.IsNullOrEmpty(loopPath))
                {
                    continue;
                }
                _readlocaldir_with_upload(loopPath, reference, false);
            }

            _message.message = "And we ended this backup, with " + filelist.Count + "files saved";
            OnSendLogMessage(_message);
            OnAfterBackup(EventArgs.Empty);
            _WriteFileCacheInfo();
        }
        public void NonCachedIncrementalBackup()
        {
            _message.message = "Will create a Incremental backup walking all directories checking modified all files";
            OnSendLogMessage(_message);
            foreach (string loopPath in path)
            {
                if (String.IsNullOrEmpty(loopPath))
                {
                    continue;
                }
                _readlocaldir_with_upload( loopPath, reference, true);
            }

            _message.message = "And we ended this backup, with " + filelist.Count + "files saved";
            OnSendLogMessage(_message);
            OnAfterBackup(EventArgs.Empty);
            _WriteFileCacheInfo();
        }
        public void FullBackup()
        {
            _message.message = "Asked for a full backup, starting it.";
            OnSendLogMessage(_message);
            if (this.filelist.Count == 0)
            {
                _message.message = "Seems that we don't have a file list, getting it.";
                OnSendLogMessage(_message);
                SlowReadlocalDir();
            }
            
            _backup(false);

            _message.message = "And we ended this backup, with " + filelist.Count + "files saved";
            OnSendLogMessage(_message);
            OnAfterBackup(EventArgs.Empty);
            _WriteFileCacheInfo();
        }
        // Reference
        public void IncrementalBackup()
        {
            _message.message = "Asked for a incremental backup, I will just upload the ones that was updated recently";
            OnSendLogMessage(_message);

            _message.message = "Getting the list of files";
            OnSendLogMessage(_message);
            SlowReadlocalDir();                
            
            _backup(true);
            _WriteFileCacheInfo();


            _message.message = "And we ended this backup, with " + filelist.Count + "files saved";
            OnSendLogMessage(_message);
            OnAfterBackup(EventArgs.Empty);
            
        }
        private void _backup( bool checkdate = true)
        {
            foreach (bFile file in filelist.Values)
            {
                _filebackup(checkdate, file);                
            }
            OnUpdated(EventArgs.Empty);
        }
        private void _filebackup(bool checkdate, bFile file)
        {            
            if (reference != null && checkdate)
            {
                if (file.last_modified.CompareTo(reference) > 0)
                {
                    _message.message = "Uploading " + file.local_path;
                    OnSendLogMessage(_message);
                    _UpdateCachedFileETag(file);
                    file.upload();
                }
                else
                {
                    _message.message = "It is not changed since last backup" + file.local_path;
                    OnSendLogMessage(_message);
                    file.status = "not updated since last backup";
                }
            }
            else
            {
                _message.message = "Uploading " + file.local_path;
                OnSendLogMessage(_message);
                file.upload();
            }
            bFileDetails.newfile = file;
            OnAddedFileToList(bFileDetails);
        }        
        public void _readlocaldir_with_upload(string walkpath, DateTime reference, bool checkdate)
        {
            string[] files = Directory.GetFiles(walkpath, "*.*");
            foreach (string file in files)
            {
                bFile newfile = this.addfile(file);
                _UpdateCachedFileETag(newfile);

                _filebackup(checkdate, newfile);

            }
            // do something to start download
            string[] directories = Directory.GetDirectories(walkpath);
            foreach (string directory in directories)
            {
                _readlocaldir_with_upload(directory,reference, checkdate);
            }
        }

        #endregion

        #region recover

        // input directory || output directory
        public void recover()
        {
            foreach (bFile file in filelist.Values)
            {
                if ( ! String.IsNullOrEmpty(output_directory))
                {
                    file.local_path = output_directory + file.local_path;
                }

                // check if the file is really downloaded
                // We do not recover files that are blacklisted!!!
                if (file.IsBlacklisted())
                {
                    continue;
                }
                file.download();
                //while ( !file.IsDirectory &&  ! File.Exists(file.local_path))
                //{
                //    file.download();
                //}

                bFileDetails.newfile = file;
                OnAddedFileToList(bFileDetails);
            }
        }

        #endregion

        public bFile addfile(string path)
        {
            bFile newfile = new bFile(path, node);
            if (!filelist.ContainsKey(newfile.path))
            {
                // When renaming the file, created/modified date does not work
                // Should be a flag to upload, but this is fine for now
                if ( ! FileCache.ContainsKey(path))
                {
                    newfile.last_modified = DateTime.Now;
                }

                filelist.Add(newfile.path, newfile);
                return newfile;
            }
            else
            {
                return filelist[newfile.path];
            }            
        }

        public void _FastAddFile(string path)
        {
            bFile newfile = new bFile(path, node);
            filelist.Add(newfile.path, newfile);
        }
        
        public bFile file(string name)
        {
            return filelist[name];
        }
    }

    public class b_http
    {

        public static string url = "https://api.b-datum.com/";

        // wrap it!!!
        // TODO: make it in the correct way and wrap it!
        
        public static Stream GET ( string path, string auth_key )
        {
            WebRequest request = WebRequest.Create( url + path );
            request.Method = "GET";

            string authotization_header =  ("Authorization: Basic " + auth_key );
            request.Headers.Add( authotization_header );

            try
            {
                WebResponse response = request.GetResponse();
                var status = (((HttpWebResponse)response).StatusDescription);

                Stream data_stream = response.GetResponseStream();
                StreamReader response_stream = new StreamReader(data_stream);

                return data_stream;
            }
            catch( WebException e ) 
            {
                throw e;
            }            
        }


        /*
         *  Returns a dictionaty of values on the header response
         *  Since the body is empty, it is hard to serialize now
         */ 
        public static string HEAD(string path, string auth_key)
        {
            WebRequest request = WebRequest.Create(url + path);
            request.Method = "HEAD";

            string authotization_header = ("Authorization: Basic " + auth_key);
            request.Headers.Add(authotization_header);

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            Stream data_stream = response.GetResponseStream();
            StreamReader response_stream = new StreamReader(data_stream);

            string responseFromServer = response_stream.ReadToEnd();

            return responseFromServer;
        }

        public static string POST_HEADERS(string path, string post_data)
        {
            WebRequest request = WebRequest.Create(url + path);
            request.Method = "POST";

            request.Headers.Add(post_data);

            byte[] byte_post_data = Encoding.UTF8.GetBytes("");

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byte_post_data.Length;

            Stream data_stream = request.GetRequestStream();
            data_stream.Write(byte_post_data, 0, byte_post_data.Length);
            data_stream.Close();

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            data_stream = response.GetResponseStream();

            StreamReader response_stream = new StreamReader(data_stream);
            string responseFromServer = response_stream.ReadToEnd();

            return responseFromServer;

        }

        public static string POST ( string path, string post_data, string auth_key = null )
        {
            WebRequest request = WebRequest.Create( url + path );
            request.Method = "POST";

            if (!String.IsNullOrEmpty(auth_key))
            {
                string authotization_header = ("Authorization: Basic " + auth_key);
                request.Headers.Add(authotization_header);
            }

            byte[] byte_post_data = Encoding.UTF8.GetBytes( post_data );

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byte_post_data.Length;

            Stream data_stream = request.GetRequestStream();
            data_stream.Write( byte_post_data, 0, byte_post_data.Length );
            data_stream.Close();

            WebResponse response = request.GetResponse();
            var status = (((HttpWebResponse)response).StatusDescription);

            data_stream = response.GetResponseStream();

            StreamReader response_stream = new StreamReader( data_stream );
            string responseFromServer =  response_stream.ReadToEnd();

            return responseFromServer;

        }

        public static string DELETE(string path, string auth_key)
        {
            Uri BD = new Uri(url);
            IWebProxy Iproxy = GlobalProxySelection.Select;
            ServicePoint sp = ServicePointManager.FindServicePoint(BD, Iproxy);
            sp.ConnectionLeaseTimeout = 9999;

            WebRequest request = WebRequest.Create(url + path);
            request.Method = "DELETE";
            request.Timeout = 10000;

            string authotization_header = ("Authorization: Basic " + auth_key);
            request.Headers.Add(authotization_header);

            WebResponse response = request.GetResponse();
            
            var status = (((HttpWebResponse)response).StatusDescription);

            Stream data_stream = response.GetResponseStream();
            StreamReader response_stream = new StreamReader(data_stream);

            string responseFromServer = response_stream.ReadToEnd();

            return responseFromServer;
        }

        private static string _GetMd5HashFromFile(string filename)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var buffer = md5.ComputeHash(File.ReadAllBytes(filename));
                var sb = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    sb.Append(buffer[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        /*
         *   Hand made post, the dot net client wasn´t working ok so 
         *   I wrote it byhand to help debug and fine adjust
         *   
         *   DEPRECATED :)
         */ 
        
    }

    public class bOrganization
    {
        public string api_key { get; set; }
        public string partner_key { get; set; }
        public string organization_id { get; set; }
        public string user_name { get; set; }

        // This variables shouldn't be here, in true is 
        // time to bdatumConfigManager have a object config 
        // or not.. :P

        public DateTime last_successful_backup { get; set; }
        // Should exist a cache timestamp
        // public DateTime 

        // Here for configuration
        public string node_key { get; set; }

        // Write a decent set
        private bNode _node;       

        //Bug
        public bNode new_node()
        {
            string json_answer = b_http.POST("/organization/" + organization_id + "/node", "api_key=" + api_key + "&name=" + this.user_name);

            bNode new_node = JsonConvert.DeserializeObject<bNode>(json_answer);

            _node = new_node;

            return new_node;
        } 

        public bNode node()
        {
            _node = new bNode();

            _node.organization = this.organization_id;
            _node.partner_key = this.partner_key;
            _node.node_key = node_key;

            return _node;
        }

        /*
         *  TODO:
         * 
         *  The constructor of the node should be here.
         *  The node should be not directly allocated.
         *  Finish the code organization. 
         * 
         */

    }

    public class bNode
    {
        public string name { get; set; }
        public string id { get; set; }
        public string organization { get; set; }        
        public string activation_key { get; set; }

        public string partner_key { get; set; }
        public string node_key { get; set; }

        public List<string> blacklist { get; set; }

        public string auth_key()
        {
            //return "WktZcUx6SHJUb2F5WVVlY1NNUVM6SnZkaUJlOWJIZmRCNEpLRm5HOGQ=";
            string to_encode = node_key + ":" + partner_key;
            byte[] bytes_to_encode = System.Text.ASCIIEncoding.ASCII.GetBytes(to_encode);
            return System.Convert.ToBase64String(bytes_to_encode);
        }

        public string activate()
        {
            string activate_parameters = "activation_key=" + activation_key + "&partner_key=" + partner_key;
            return b_http.POST("node/activate", activate_parameters);
        }

        public List<bFile> list(string path = "/")
        {
            Stream response = b_http.GET("storage?path=" + path, auth_key());

            StreamReader response_stream = new StreamReader(response);
            string responseFromServer = response_stream.ReadToEnd();

            JObject process_json = JObject.Parse(responseFromServer);

            //IList<JToken> files = process_json["objects"].Children();

            List<bFile> fileslist = new List<bFile>();

            // each one converts to json bFile
            foreach (var file in process_json["objects"].Children())
            {
                //string json_answer = b_http.POST("/organization/" + organization_id + "/node", "api_key=" + api_key + "&name=" + this.user_name);

                bFile new_file = JsonConvert.DeserializeObject<bFile>(file.ToString());
                new_file.node = this;
                new_file.remote();
                if (new_file.status == "object created")
                {
                    fileslist.Add(new_file);
                }

            }

            return fileslist;
        }

        public List<bFile> list_at_once(string path, string ts)
        {
            string listurl = "storage?flatten=1&path=" + path;

            if (!String.IsNullOrEmpty(ts))
            {
                listurl = listurl + "&ts=" + ts;
            }

            Stream response = b_http.GET(listurl, auth_key());

            StreamReader response_stream = new StreamReader(response);
            string responseFromServer = response_stream.ReadToEnd();

            JObject process_json = JObject.Parse(responseFromServer);

            //IList<JToken> files = process_json["objects"].Children();

            List<bFile> fileslist = new List<bFile>();

            // each one converts to json bFile
            foreach (var file in process_json["objects"].Children())
            {
                //string json_answer = b_http.POST("/organization/" + organization_id + "/node", "api_key=" + api_key + "&name=" + this.user_name);

                bFile new_file = JsonConvert.DeserializeObject<bFile>(file.ToString());
                new_file.node = this;
                new_file.remote();
                if (new_file.status == "object created")
                {
                    fileslist.Add(new_file);
                }

            }

            return fileslist;
        }
        
        /* TO DELETE
        public FileObjectList list_old ()
        {
            Stream response = b_http.GET("storage", auth_key() );            

            StreamReader response_stream = new StreamReader(response);
            string responseFromServer = response_stream.ReadToEnd();

            FileObjectList root = FileList.load_json(responseFromServer);

            return root;
        }
         */

        public string info ( string path )
        {
            return b_http.HEAD("storage/" + path, auth_key());
        }

        public string createpath(string path)
        {
            //string[] directories = path.Split(Path.DirectorySeparatorChar);
            string[] directories = path.Split('/');
            string build_path = "";

            foreach (string directory in directories)
            {
                build_path += directory + "/";

                WebClient wc = new WebClient();
                wc.Headers.Add("Authorization: Basic " + auth_key());
                     
                var result = wc.UploadFile(b_http.url + "storage?path=" + path, "PUT", path);
            }
            return null;
        }
    }

    public class bFile
    {
        #region atributes

        //private string _Etag;

        private List<string> chunks = new List<string>();
        private int lastpart = 0;

        public string local_path { get; set; }        
        public string ETag { get; set; }
        public string filename { get; set; }
        public string type { get; set; }

        public long local_size { get; set; }
        private bool _IsDirectory;
        public bool IsDirectory
        {
            get
            {
                return _IsDirectory;
            }
        }

        public List<string> curlcommands = new List<string>();

        // bla bla bla defines what to do
        // Dispatch table is your friend
        public string action { get; set; }

        public string id { get; set; }
        public string begin_ts { get; set; }
        public string node_id { get; set; }
        public string size { get; set; }
        public string version { get; set; }
        public string mimetype_id { get; set; }
        public string mime { get; set; }
        public string path { get; set;}

        public DateTime last_modified { get; set; }

        enum Status
        {
            initial, local, remote
        };

        public string status { get; set; }

        #endregion

        #region constructor

        // reference
        private bNode _node;
        public bNode node
        {
            get { return _node; }
            set { if (_node == null) { _node = value; } }
        }

        public bFile() 
        {

        }

        public bFile(string value) : this( value, null ) { }

        public bFile(string value, bNode node)
        {
            _node = node;

            Uri convert = new Uri(value);
            string disc = "/" + (convert.AbsolutePath).Substring(0, 1);
            path = disc  + (convert.AbsolutePath).Substring(2);

            filename = Path.GetFileName(path);

            local_path = Path.GetFullPath(value);

            // Yet another race condition
            if (File.Exists(local_path) || Directory.Exists(local_path))
            {
                FileInfo fileinfo = new FileInfo(local_path);
                FileAttributes attr = File.GetAttributes(local_path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    _IsDirectory = true;
                    if (path.Substring((this.path.Length - 1)) != "/")
                    {
                        path = path + "/";
                    }
                    if (local_path.Substring((this.local_path.Length - 1)) != "\\")
                    {                        
                        local_path = local_path + "\\";
                    }
                }
                else
                {
                    local_size = fileinfo.Length;
                    _IsDirectory = false;
                }

                DateTime modified = System.IO.File.GetLastWriteTime(local_path);
                DateTime created = System.IO.File.GetCreationTime(local_path);

                //if (file.last_modified.CompareTo(reference) > 0)
                if (modified.CompareTo(created) > 0)
                {
                    last_modified = modified;
                }
                else
                {
                    last_modified = created;
                }                

                status = "local";
            }
            else
            {
                if (this.path.Substring((this.path.Length - 1)) == "/")
                {
                    _IsDirectory = true;
                }
                else
                {
                    _IsDirectory = false;                
                }
                status = "remote";
            }          
        }

        #endregion        

        // Work around for the json builder
        public void remote()
        {

            string unit = this.path.Substring(1, 1);
            string path_withoutunit = this.path.Substring(3);

            string slash = this.path.Substring(2, 1);

            if (slash != "/")
            {
                status = "error";
                return;
            }

            if (this.path.Substring((this.path.Length - 1)) == "/")
            {
                _IsDirectory = true;
                local_path = Path.GetFullPath(unit + ":/" + path_withoutunit);
            }
            else
            {
                _IsDirectory = false;
                local_path = Path.GetFullPath(unit + ":/" + path_withoutunit);
            }

            status = "object created";
        }


        public void genETag()
        {
            ETag = _GetMd5HashFromFile(local_path);
        }

        // TODO fix these string return codes
        public string _upload_external_curl()
        {
            Process curl = new Process();

            curl.StartInfo.UseShellExecute = false;
            curl.StartInfo.FileName = "curl.exe";
            curl.StartInfo.CreateNoWindow = true;
            curl.StartInfo.Arguments = "-k -l -v -H \"Authorization: Basic " + _node.auth_key() + "\" -H \"Etag: " + ETag + "\" -T \"" + local_path + "\" -X PUT \"" + b_http.url + "storage?path=" + path + "\"";
            curl.StartInfo.RedirectStandardOutput = true;
            bool started = curl.Start();

            string output = curl.StandardOutput.ReadToEnd();
            curl.WaitForExit();

            if (String.IsNullOrEmpty(output))
            {
                status = "uploaded";
                return output;
            }
            else
            {
                // todo: raise exception
                status = "Upload Failed" + output;
                return output;
            }
        }

        public string upload()
        {
            if (String.IsNullOrEmpty( ETag ))
            {
                genETag();
            }

            // I'm jack gigantic monolitic method
            if (_blacklisted())
            {
                status = "ignore list";
                return null;
            }

            if (verifyIfExists())
            {
                this.status = "This file exists on backup";
                return null;
            }

            
            //if (local_size > 90 * 1024 * 1024)
            if (local_size > 7 * 1024 * 1024)
            {
                splitfile();

                bParts parts = new bParts();                

                Process curlinit = new Process();
                curlinit.StartInfo.UseShellExecute = false;
                curlinit.StartInfo.FileName = "curl.exe";
                curlinit.StartInfo.CreateNoWindow = true;
                curlinit.StartInfo.Arguments = "-k -l -v -H \"Authorization: Basic " + _node.auth_key() + "\"" + "\" -H \"Etag: " + ETag + "\" -X POST \"" + b_http.url + "storage?path=" + path + "&multipart=1";
                curlinit.StartInfo.RedirectStandardOutput = true;
                curlinit.Start();
                string answer = curlinit.StandardOutput.ReadToEnd();

                bMultipartInfo multipart = JsonConvert.DeserializeObject<bMultipartInfo>(answer);                    
                
                string upload_id = multipart.upload_id;

                curlinit.WaitForExit();

                curlcommands.Add(curlinit.StartInfo.Arguments);

                int part = 1;

                foreach (string chunk in chunks)
                {
                    
                    //resume
                    if (part < lastpart)
                    {
                        part++;
                        continue;
                    }

                    string md5sum = _GetMd5HashFromFile(chunk);

                    string[] detail = new string[] { part.ToString(), md5sum };

                    parts.parts.Add( detail);

                    Process curl = new Process();

                    curl.StartInfo.UseShellExecute = false;
                    curl.StartInfo.FileName = "curl.exe";
                    curl.StartInfo.CreateNoWindow = true;
                    curl.StartInfo.Arguments = "-k -l -v -H \"Authorization: Basic " + _node.auth_key() + "\" -H \"Etag: " + md5sum + "\" -T \"" + chunk + "\" -X PUT \"" + b_http.url + "storage?path=" + path + "&part=" + part.ToString() + "&upload_id=" + upload_id + "\"";                    
                    curl.StartInfo.RedirectStandardOutput = true;
                    bool started = curl.Start();

                    string output = curl.StandardOutput.ReadToEnd();
                    curl.WaitForExit();

                    curlcommands.Add(curl.StartInfo.Arguments);

                    //File.Delete(chunk);
                    if (String.IsNullOrEmpty(output))
                    {
                        status = "uploaded part " + part.ToString();
                        lastpart = part;                        
                    }
                    else
                    {
                        status = "Error: Upload part" + part.ToString() + " " + output;                        
                    }
                    part++;
                }

                string send = JsonConvert.SerializeObject(parts);
                
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(b_http.url + "storage?path=" + path + "&upload_id=" + upload_id );
                request.Method = "POST";
                
                request.Headers.Add("Authorization: Basic " + _node.auth_key());
                request.Headers.Add("Etag: " + ETag);
                request.ContentType = "application/json";
                request.Accept = "application/json";

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(send);
                }

                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();

                    status = "almost ended" + response.Headers.ToString();
                    return responseText;
                }              
            }
            else
            {
                return _upload_external_curl();
            }

        }

        private bool verifyIfExists()
        {
            // Check if file exists   POST /storage?path=/foo/bar.zip&check=1, Headers => [ Etag => 'abc123abcdef..']
            HttpWebRequest check_request = (HttpWebRequest)WebRequest.Create(b_http.url + "storage?path=" + path + "&check=1");
            check_request.Method = "POST";

            check_request.Headers.Add("Authorization: Basic " + _node.auth_key());
            check_request.Headers.Add("Etag: " + ETag);
            check_request.ContentType = "application/json";
            check_request.Accept = "application/json";          

            try
            {
                var check_response = (HttpWebResponse)check_request.GetResponse();
                using (var streamReader = new StreamReader(check_response.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                    //Now you have your response.
                    //or false depending on information in the response
                    if (check_response.StatusCode == HttpStatusCode.Created || check_response.StatusCode == HttpStatusCode.NoContent )
                    {
                        this.status = "uploaded*";
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return false;
        }        
        
        public string download()
        {

            if (_blacklisted())
            {
                status = "ignore list";
                return null;
            }

            if (this.IsDirectory)
            {
                return null;
            }

            string directory = Path.GetDirectoryName(this.local_path);

            if (! File.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Process curl = new Process();

            Uri path_url = new Uri(b_http.url + "storage?path=" + this.path);            

            curl.StartInfo.UseShellExecute = false;
            curl.StartInfo.FileName = "curl.exe";
            curl.StartInfo.CreateNoWindow = true;
            //curl.StartInfo.Arguments = "-k -v -H \"Authorization: Basic " + _node.auth_key() + "\" " + b_http.url + "storage?path=" + this.path + "   -o \"" + local_path + "\"";
            curl.StartInfo.Arguments = "-k -v -H \"Authorization: Basic " + _node.auth_key() + "\" " + path_url.AbsoluteUri + "   -o \"" + local_path + "\"";
            curl.StartInfo.RedirectStandardOutput = true;
            bool started = curl.Start();

            string output = curl.StandardOutput.ReadToEnd();
            curl.WaitForExit();

            if (String.IsNullOrEmpty(output))
            {
                status = "Downloaded";
                return output;
            }
            else
            {
                status = "Download Failed" + output;
                return output;
            }
        }

        // Copy and Paste, shame on me
        private string _GetMd5HashFromFile(string fullpath)
        {
            if (fullpath == null)
            {
                fullpath = local_path;
            }
            try
            {
                using (var md5 = new MD5CryptoServiceProvider())
                {
                    var buffer = md5.ComputeHash(File.ReadAllBytes(fullpath));
                    var sb = new StringBuilder();
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        sb.Append(buffer[i].ToString("x2"));
                    }

                    return sb.ToString();
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public string delete()
        {
            if (_blacklisted())
            {
                status = "ignore list";
                return null;
            }

            return b_http.DELETE("storage/" + path, _node.auth_key());
        }

        public static string delete( string value, string auth_key )
        {
            Uri convert = new Uri(value);
            string disc = "/" + (convert.AbsolutePath).Substring(0, 1);
            string path = disc + (convert.AbsolutePath).Substring(2);
            return b_http.DELETE("storage?path=" + path, auth_key);
        }

        public void info()
        {
            WebRequest request = WebRequest.Create( b_http.url + "storage?path=" + path);
            request.Method = "HEAD";

            string authotization_header = ("Authorization: Basic " + node.auth_key());
            request.Headers.Add(authotization_header);

            WebResponse response = request.GetResponse();
            
            // Dot net 2.0 arrays does not support directly contains
            List<string> headers = new List<string>(response.Headers.AllKeys);

            if (headers.Contains("ETag"))
            {
                ETag = response.Headers["ETag"];
            }
            
        }      
  
        public string UPLOADNEW()
        {  
            string auth_key = _node.auth_key();

            // ok
            //string boundary = "----------" + DateTime.Now.Ticks.ToString("x");
            string boundary = "xYzZY" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest upload = (HttpWebRequest)WebRequest.Create(b_http.url + "storage");
            upload.ContentType = "multipart/form-data; boundary=" + boundary;
            upload.Method = "POST";
            upload.Headers.Add("Authorization: Basic " + auth_key);
            upload.Headers.Add("ETag: " + this.ETag);                      

            //upload.AllowWriteStreamBuffering = false;

            Stream uploadStream = upload.GetRequestStream();

            FileStream uploadFile = new FileStream(this.local_path, FileMode.Open, FileAccess.Read);
            byte[] buffer = new Byte[4096];
            int bytesRead = 0;
            while (( bytesRead = uploadFile.Read(buffer, 0, buffer.Length)) != 0 )
                uploadStream.Write(buffer, 0, bytesRead);
            uploadFile.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n==" + boundary + "--\r\n");
            uploadStream.Write(trailer, 0, trailer.Length);
            uploadStream.Close();

            WebResponse response = upload.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader responseStreamReader = new StreamReader(responseStream);
            String returnString = responseStreamReader.ReadToEnd();

            uploadFile.Close();
            responseStream.Close();
            responseStreamReader.Close();
            uploadStream.Close();

            return returnString;
        }

        public bool splitfile()
        {
            // I need to check is enough disk space to continue.

            DriveInfo[] alldrives = DriveInfo.GetDrives();
            
            string temp_path = System.IO.Path.GetTempPath();

            string drive_letter = temp_path.Substring(0, 3);

            foreach (var drive in alldrives)
            {
                if (drive.Name == drive_letter)
                {
                    if (drive.TotalFreeSpace < local_size )
                    {
                        this.status = "Error: Not enough disk space";
                        throw new System.OperationCanceledException("Not enoughs disk space");
                    }
                }
            }

            // Resume
            if (lastpart > 0)
            {
                return true;
            }

            const int BUFFER_SIZE = 10 * 1024;    
        
            //int chunkSize = 90 * 1024 * 1024;
            int chunkSize = 7 * 1024 * 1024;

            byte[] buffer = new byte[BUFFER_SIZE];

            using (Stream input = File.OpenRead(local_path))
            {
                int index = 0;
                while (input.Position < input.Length)
                {
                    string temp_filename = temp_path + "\\" + filename + "__" + index.ToString();
                    chunks.Add(temp_filename);
                    using (Stream output = File.Create( temp_filename ))
                    {
                        int remaining = chunkSize, bytesRead;
                        while( remaining > 0 && ( bytesRead = input.Read(buffer, 0, Math.Min(remaining, BUFFER_SIZE))) > 0 )
                        {
                            output.Write(buffer, 0, bytesRead);
                            remaining -= bytesRead;
                        }
                    }
                    index++;
                    Thread.Sleep(500); 
                }
            }

            return true;
        }

        public bool IsBlacklisted()
        {
            return _blacklisted();
        }

        private bool _blacklisted()
        {
            List<string> blacklist;
            if (node.blacklist == null)
            {
                blacklist = new List<string>();
            }
            else
            {
                blacklist = node.blacklist;
            }

            string mypath = Path.GetDirectoryName(local_path);

            foreach (string path in blacklist)
            {
                
                if (! String.IsNullOrEmpty(mypath) &&  mypath.Contains(path))
                    return true;
            }
            return false;
        }
    }

    public class bFileInfo
    {
        public string Date { get; set; }
        public string Etag { get; set; }        
    }

    public class bMultipartInfo
    {
        public string upload_id { get; set; }        
    }

    public class bParts
    {
        public List<string[]> parts = new List<string[]>();
    }    

    #region deprecated

    public class Version
    {
        // order by version ( it is possible, check on api )
        public string timestamp { get; set; }
        public string version { get; set; }
        public string size { get; set; }
    }

    public class VersionList
    {
        public List<Version> versions { get; set; }
        public string type { get; set; }
    }

    public class FileObject
    {
        public VersionList versions { get; set; }
        public string name { get; set; }
    }

    public class FileObjectList
    {
        public List<FileObject> objects { get; set; }
        public string json { get; set; }
    }

    public static class FileList
    {

        public static FileObjectList load_json(string root_json)
        {
            FileObjectList root = new FileObjectList();
            root.objects = new List<FileObject>();
            root.json = root_json;

            JObject process_json = JObject.Parse(root_json);

            //IList<JToken> files = process_json["objects"].Children();

            foreach (JProperty file in process_json["objects"].Children())
            {
                string name = file.Name;
                string json = file.Value.ToString();

                FileObject fileObject = new FileObject();
                fileObject.name = name;

                //VersionList
                VersionList fileVersions = JsonConvert.DeserializeObject<VersionList>(json);

                fileObject.versions = fileVersions;

                root.objects.Add(fileObject);
            }

            return root;

        }

    }

    #endregion
}
